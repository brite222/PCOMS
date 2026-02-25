using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Data;
using PCOMS.Models;
using PCOMS.Models.Enums;

namespace PCOMS.Application.Services
{
    public class TimeTrackingService : ITimeTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimeTrackingService> _logger;

        public TimeTrackingService(
            ApplicationDbContext context,
            ILogger<TimeTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // TIME ENTRIES
        // ==========================================
        public async Task<TimeEntryDto?> CreateTimeEntryAsync(CreateTimeEntryDto dto, string userId)
        {
            try
            {
                var entry = new TimeEntry
                {
                    ProjectId = dto.ProjectId,
                    TaskId = dto.TaskId,
                    UserId = userId,
                    Date = dto.Date,
                    Hours = dto.Hours,
                    Description = dto.Description ?? "",
                    IsBillable = dto.IsBillable,
                    HourlyRate = dto.HourlyRate,
                    Status = TimeEntryStatus.Submitted,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeEntries.Add(entry);
                await _context.SaveChangesAsync();

                return await GetTimeEntryByIdAsync(entry.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry");
                throw;
            }
        }

        public async Task<TimeEntryDto?> GetTimeEntryByIdAsync(int id)
        {
            var entry = await _context.TimeEntries
                .Include(e => e.Project)
                .Include(e => e.Task)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (entry == null) return null;

            var user = await _context.Users.FindAsync(entry.UserId);
            var approver = entry.ApprovedBy != null ? await _context.Users.FindAsync(entry.ApprovedBy) : null;

            return new TimeEntryDto
            {
                Id = entry.Id,
                ProjectId = entry.ProjectId,
                ProjectName = entry.Project.Name,
                TaskId = entry.TaskId,
                TaskTitle = entry.Task?.Title,
                UserId = entry.UserId,
                UserName = user?.UserName ?? "Unknown",
                Date = entry.Date,
                Hours = entry.Hours,
                Description = entry.Description,
                IsBillable = entry.IsBillable,
                Status = entry.Status.ToString(),
                ApprovedBy = entry.ApprovedBy,
                ApprovedByName = approver?.UserName,
                ApprovedAt = entry.ApprovedAt,
                ApprovalNotes = entry.ApprovalNotes,
                HourlyRate = entry.HourlyRate,
                CreatedAt = entry.CreatedAt
            };
        }

        public async Task<IEnumerable<TimeEntryDto>> GetTimeEntriesAsync(TimeEntryFilterDto filter)
        {
            var query = _context.TimeEntries
                .Include(e => e.Project)
                .Include(e => e.Task)
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (filter.ProjectId.HasValue)
                query = query.Where(e => e.ProjectId == filter.ProjectId.Value);

            if (filter.TaskId.HasValue)
                query = query.Where(e => e.TaskId == filter.TaskId.Value);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(e => e.UserId == filter.UserId);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.Date >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.Date <= filter.ToDate.Value);

            if (filter.IsBillable.HasValue)
                query = query.Where(e => e.IsBillable == filter.IsBillable.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(e => e.Status.ToString() == filter.Status);

            var entries = await query
                .OrderByDescending(e => e.Date)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = new List<TimeEntryDto>();

            foreach (var entry in entries)
            {
                var user = await _context.Users.FindAsync(entry.UserId);
                var approver = entry.ApprovedBy != null ? await _context.Users.FindAsync(entry.ApprovedBy) : null;

                dtos.Add(new TimeEntryDto
                {
                    Id = entry.Id,
                    ProjectId = entry.ProjectId,
                    ProjectName = entry.Project.Name,
                    TaskId = entry.TaskId,
                    TaskTitle = entry.Task?.Title,
                    UserId = entry.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Date = entry.Date,
                    Hours = entry.Hours,
                    Description = entry.Description,
                    IsBillable = entry.IsBillable,
                    Status = entry.Status.ToString(),
                    ApprovedBy = entry.ApprovedBy,
                    ApprovedByName = approver?.UserName,
                    ApprovedAt = entry.ApprovedAt,
                    ApprovalNotes = entry.ApprovalNotes,
                    HourlyRate = entry.HourlyRate,
                    CreatedAt = entry.CreatedAt
                });
            }

            return dtos;
        }

        public async Task<IEnumerable<TimeEntryDto>> GetUserTimeEntriesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetTimeEntriesAsync(new TimeEntryFilterDto
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = 1000
            });
        }

        public async Task<IEnumerable<TimeEntryDto>> GetProjectTimeEntriesAsync(int projectId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetTimeEntriesAsync(new TimeEntryFilterDto
            {
                ProjectId = projectId,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = 1000
            });
        }

        public async Task<bool> UpdateTimeEntryAsync(UpdateTimeEntryDto dto, string userId)
        {
            try
            {
                var entry = await _context.TimeEntries.FindAsync(dto.Id);
                if (entry == null || entry.IsDeleted) return false;

                // Only owner can edit, and only if not approved/invoiced
                if (entry.UserId != userId || entry.Status == TimeEntryStatus.Approved || entry.Status == TimeEntryStatus.Invoiced)
                    return false;

                if (dto.ProjectId.HasValue) entry.ProjectId = dto.ProjectId.Value;
                if (dto.TaskId.HasValue) entry.TaskId = dto.TaskId;
                if (dto.Date.HasValue) entry.Date = dto.Date.Value;
                if (dto.Hours.HasValue) entry.Hours = dto.Hours.Value;
                if (dto.Description != null) entry.Description = dto.Description;
                if (dto.IsBillable.HasValue) entry.IsBillable = dto.IsBillable.Value;
                if (dto.HourlyRate.HasValue) entry.HourlyRate = dto.HourlyRate;

                entry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry");
                return false;
            }
        }

        public async Task<bool> DeleteTimeEntryAsync(int id, string userId)
        {
            try
            {
                var entry = await _context.TimeEntries.FindAsync(id);
                if (entry == null) return false;

                // Only owner can delete, and only if not approved/invoiced
                if (entry.UserId != userId || entry.Status == TimeEntryStatus.Approved || entry.Status == TimeEntryStatus.Invoiced)
                    return false;

                entry.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry");
                return false;
            }
        }

        public async Task<bool> ApproveTimeEntryAsync(ApproveTimeEntryDto dto, string approvedBy)
        {
            try
            {
                var entry = await _context.TimeEntries.FindAsync(dto.TimeEntryId);
                if (entry == null || entry.IsDeleted) return false;

                if (entry.Status == TimeEntryStatus.Approved ||
      entry.Status == TimeEntryStatus.Rejected)
                    return false;


                entry.Status = dto.IsApproved ? TimeEntryStatus.Approved : TimeEntryStatus.Rejected;
                entry.ApprovedBy = approvedBy;
                entry.ApprovedAt = DateTime.UtcNow;
                entry.ApprovalNotes = dto.Notes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving time entry");
                return false;
            }
        }

        public async Task<bool> RejectTimeEntryAsync(int timeEntryId, string rejectedBy, string? notes)
        {
            return await ApproveTimeEntryAsync(new ApproveTimeEntryDto
            {
                TimeEntryId = timeEntryId,
                IsApproved = false,
                Notes = notes
            }, rejectedBy);
        }

        // ==========================================
        // TIMESHEETS
        // ==========================================
        public async Task<TimesheetDto?> CreateTimesheetAsync(CreateTimesheetDto dto, string userId)
        {
            try
            {
                var weekStart = GetWeekStartDate(dto.WeekStartDate).Result;
                var weekEnd = weekStart.AddDays(6);

                var timesheet = new Timesheet
                {
                    UserId = userId,
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    Status = TimesheetStatus.Draft,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Timesheets.Add(timesheet);
                await _context.SaveChangesAsync();

                return await GetTimesheetByIdAsync(timesheet.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timesheet");
                throw;
            }
        }

        public async Task<TimesheetDto?> GetTimesheetByIdAsync(int id)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.TimeEntries)
                    .ThenInclude(e => e.Project)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (timesheet == null) return null;

            var user = await _context.Users.FindAsync(timesheet.UserId);
            var approver = timesheet.ApprovedBy != null ? await _context.Users.FindAsync(timesheet.ApprovedBy) : null;

            var entries = new List<TimeEntryDto>();
            foreach (var entry in timesheet.TimeEntries.Where(e => !e.IsDeleted))
            {
                var entryUser = await _context.Users.FindAsync(entry.UserId);
                entries.Add(new TimeEntryDto
                {
                    Id = entry.Id,
                    ProjectId = entry.ProjectId,
                    ProjectName = entry.Project.Name,
                    TaskId = entry.TaskId,
                    UserId = entry.UserId,
                    UserName = entryUser?.UserName ?? "Unknown",
                    Date = entry.Date,
                    Hours = entry.Hours,
                    Description = entry.Description,
                    IsBillable = entry.IsBillable,
                    Status = entry.Status.ToString(),
                    HourlyRate = entry.HourlyRate,
                    CreatedAt = entry.CreatedAt
                });
            }

            return new TimesheetDto
            {
                Id = timesheet.Id,
                UserId = timesheet.UserId,
                UserName = user?.UserName ?? "Unknown",
                WeekStartDate = timesheet.WeekStartDate,
                WeekEndDate = timesheet.WeekEndDate,
                TotalHours = timesheet.TotalHours,
                BillableHours = timesheet.BillableHours,
                NonBillableHours = timesheet.NonBillableHours,
                Status = timesheet.Status.ToString(),
                SubmittedAt = timesheet.SubmittedAt,
                ApprovedBy = timesheet.ApprovedBy,
                ApprovedByName = approver?.UserName,
                ApprovedAt = timesheet.ApprovedAt,
                ApprovalNotes = timesheet.ApprovalNotes,
                Notes = timesheet.Notes,
                CreatedAt = timesheet.CreatedAt,
                TimeEntries = entries
            };
        }

        public async Task<TimesheetDto?> GetUserTimesheetForWeekAsync(string userId, DateTime weekStartDate)
        {
            var weekStart = GetWeekStartDate(weekStartDate).Result;

            var timesheet = await _context.Timesheets
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.WeekStartDate == weekStart && !t.IsDeleted);

            if (timesheet == null) return null;

            return await GetTimesheetByIdAsync(timesheet.Id);
        }

        public async Task<IEnumerable<TimesheetDto>> GetUserTimesheetsAsync(string userId)
        {
            var timesheets = await _context.Timesheets
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.WeekStartDate)
                .ToListAsync();

            var dtos = new List<TimesheetDto>();

            foreach (var timesheet in timesheets)
            {
                var dto = await GetTimesheetByIdAsync(timesheet.Id);
                if (dto != null) dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<TimesheetDto>> GetPendingTimesheetsAsync()
        {
            var timesheets = await _context.Timesheets
                .Where(t => t.Status == TimesheetStatus.Submitted && !t.IsDeleted)
                .OrderBy(t => t.SubmittedAt)
                .ToListAsync();

            var dtos = new List<TimesheetDto>();

            foreach (var timesheet in timesheets)
            {
                var dto = await GetTimesheetByIdAsync(timesheet.Id);
                if (dto != null) dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<bool> UpdateTimesheetAsync(UpdateTimesheetDto dto)
        {
            try
            {
                var timesheet = await _context.Timesheets.FindAsync(dto.Id);
                if (timesheet == null || timesheet.IsDeleted) return false;

                if (timesheet.Status != TimesheetStatus.Draft)
                    return false;

                if (dto.Notes != null) timesheet.Notes = dto.Notes;

                timesheet.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timesheet");
                return false;
            }
        }

        public async Task<bool> SubmitTimesheetAsync(int timesheetId, string userId)
        {
            try
            {
                var timesheet = await _context.Timesheets
                    .Include(t => t.TimeEntries)
                    .FirstOrDefaultAsync(t => t.Id == timesheetId);

                if (timesheet == null || timesheet.IsDeleted || timesheet.UserId != userId)
                    return false;

                if (timesheet.Status != TimesheetStatus.Draft)
                    return false;

                // Calculate totals
                var entries = timesheet.TimeEntries.Where(e => !e.IsDeleted).ToList();
                timesheet.TotalHours = entries.Sum(e => e.Hours);
                timesheet.BillableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
                timesheet.NonBillableHours = entries.Where(e => !e.IsBillable).Sum(e => e.Hours);

                timesheet.Status = TimesheetStatus.Submitted;
                timesheet.SubmittedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting timesheet");
                return false;
            }
        }

        public async Task<bool> ApproveTimesheetAsync(ApproveTimesheetDto dto, string approvedBy)
        {
            try
            {
                var timesheet = await _context.Timesheets.FindAsync(dto.TimesheetId);
                if (timesheet == null || timesheet.IsDeleted) return false;

                if (timesheet.Status != TimesheetStatus.Submitted)
                    return false;

                timesheet.Status = dto.IsApproved ? TimesheetStatus.Approved : TimesheetStatus.Rejected;
                timesheet.ApprovedBy = approvedBy;
                timesheet.ApprovedAt = DateTime.UtcNow;
                timesheet.ApprovalNotes = dto.Notes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving timesheet");
                return false;
            }
        }

        public async Task<bool> RejectTimesheetAsync(int timesheetId, string rejectedBy, string? notes)
        {
            return await ApproveTimesheetAsync(new ApproveTimesheetDto
            {
                TimesheetId = timesheetId,
                IsApproved = false,
                Notes = notes
            }, rejectedBy);
        }

        public async Task<bool> DeleteTimesheetAsync(int id)
        {
            try
            {
                var timesheet = await _context.Timesheets.FindAsync(id);
                if (timesheet == null) return false;

                if (timesheet.Status != TimesheetStatus.Draft)
                    return false;

                timesheet.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timesheet");
                return false;
            }
        }

        // Continue in next part...

        // ==========================================
        // WORK SCHEDULE
        // ==========================================
        public async Task<WorkScheduleDto?> CreateWorkScheduleAsync(CreateWorkScheduleDto dto)
        {
            try
            {
                var hoursPerDay = (decimal)(dto.EndTime - dto.StartTime).TotalHours;

                var schedule = new WorkSchedule
                {
                    UserId = dto.UserId,
                    DayOfWeek = dto.DayOfWeek,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    HoursPerDay = hoursPerDay,
                    IsWorkingDay = dto.IsWorkingDay,
                    EffectiveFrom = dto.EffectiveFrom,
                    EffectiveTo = dto.EffectiveTo
                };

                _context.WorkSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(dto.UserId);

                return new WorkScheduleDto
                {
                    Id = schedule.Id,
                    UserId = schedule.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    DayOfWeek = schedule.DayOfWeek.ToString(),
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    HoursPerDay = schedule.HoursPerDay,
                    IsWorkingDay = schedule.IsWorkingDay,
                    EffectiveFrom = schedule.EffectiveFrom,
                    EffectiveTo = schedule.EffectiveTo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work schedule");
                throw;
            }
        }

        public async Task<IEnumerable<WorkScheduleDto>> GetUserWorkScheduleAsync(string userId)
        {
            var schedules = await _context.WorkSchedules
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderBy(s => s.DayOfWeek)
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);

            return schedules.Select(s => new WorkScheduleDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = user?.UserName ?? "Unknown",
                DayOfWeek = s.DayOfWeek.ToString(),
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                HoursPerDay = s.HoursPerDay,
                IsWorkingDay = s.IsWorkingDay,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo
            }).ToList();
        }

        public async Task<bool> DeleteWorkScheduleAsync(int id)
        {
            try
            {
                var schedule = await _context.WorkSchedules.FindAsync(id);
                if (schedule == null) return false;

                schedule.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work schedule");
                return false;
            }
        }

        // ==========================================
        // REPORTS
        // ==========================================
        public async Task<TimeReportDto> GetTimeReportAsync(DateTime fromDate, DateTime toDate, int? projectId = null)
        {
            var filter = new TimeEntryFilterDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                ProjectId = projectId,
                Status = "Approved",
                PageSize = 10000
            };

            var entries = await GetTimeEntriesAsync(filter);

            var report = new TimeReportDto
            {
                ReportType = "Time Report",
                FromDate = fromDate,
                ToDate = toDate,
                TotalHours = entries.Sum(e => e.Hours),
                BillableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours),
                NonBillableHours = entries.Where(e => !e.IsBillable).Sum(e => e.Hours),
                HoursByProject = entries.GroupBy(e => e.ProjectName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours)),
                HoursByUser = entries.GroupBy(e => e.UserName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours)),
                DailyHours = entries.GroupBy(e => e.Date.Date).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours)),
                TopEntries = entries.OrderByDescending(e => e.Hours).Take(10).ToList()
            };

            return report;
        }

        public async Task<UserTimeReportDto> GetUserTimeReportAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            var entries = await GetUserTimeEntriesAsync(userId, fromDate, toDate);
            var user = await _context.Users.FindAsync(userId);

            var totalDays = entries.Select(e => e.Date.Date).Distinct().Count();

            return new UserTimeReportDto
            {
                UserId = userId,
                UserName = user?.UserName ?? "Unknown",
                FromDate = fromDate,
                ToDate = toDate,
                TotalHours = entries.Sum(e => e.Hours),
                BillableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours),
                AverageHoursPerDay = totalDays > 0 ? entries.Sum(e => e.Hours) / totalDays : 0,
                TotalDaysWorked = totalDays,
                HoursByProject = entries.GroupBy(e => e.ProjectName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours))
            };
        }

        public async Task<ProjectTimeReportDto> GetProjectTimeReportAsync(int projectId, DateTime fromDate, DateTime toDate)
        {
            var entries = await GetProjectTimeEntriesAsync(projectId, fromDate, toDate);
            var project = await _context.Projects.FindAsync(projectId);

            return new ProjectTimeReportDto
            {
                ProjectId = projectId,
                ProjectName = project?.Name ?? "Unknown",
                FromDate = fromDate,
                ToDate = toDate,
                TotalHours = entries.Sum(e => e.Hours),
                BillableHours = entries.Where(e => e.IsBillable).Sum(e => e.Hours),
                HoursByUser = entries.GroupBy(e => e.UserName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours)),
                HoursByTask = entries.Where(e => e.TaskTitle != null).GroupBy(e => e.TaskTitle!).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours))
            };
        }

        public async Task<Dictionary<string, decimal>> GetHoursByProjectAsync(DateTime fromDate, DateTime toDate)
        {
            var entries = await GetTimeEntriesAsync(new TimeEntryFilterDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                Status = "Approved",
                PageSize = 10000
            });

            return entries.GroupBy(e => e.ProjectName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));
        }

        public async Task<Dictionary<string, decimal>> GetHoursByUserAsync(DateTime fromDate, DateTime toDate, int? projectId = null)
        {
            var entries = await GetTimeEntriesAsync(new TimeEntryFilterDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                ProjectId = projectId,
                Status = "Approved",
                PageSize = 10000
            });

            return entries.GroupBy(e => e.UserName).ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));
        }

        public async Task<decimal> GetUserTotalHoursAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var entries = await GetUserTimeEntriesAsync(userId, fromDate, toDate);
            return entries.Where(e => e.Status == "Approved").Sum(e => e.Hours);
        }

        public async Task<decimal> GetProjectTotalHoursAsync(int projectId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var entries = await GetProjectTimeEntriesAsync(projectId, fromDate, toDate);
            return entries.Where(e => e.Status == "Approved").Sum(e => e.Hours);
        }

        public async Task<DateTime> GetWeekStartDate(DateTime date)
        {
            // Get Monday of the week
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}