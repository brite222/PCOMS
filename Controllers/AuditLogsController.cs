using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using System.Text;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index(AuditLogFilterDto? filter)
        {
            filter ??= new AuditLogFilterDto();

            var logs = await _auditService.GetAllAsync();
            var statistics = await _auditService.GetStatisticsAsync();

            ViewBag.Statistics = statistics;
            ViewBag.Filter = filter;
            ViewBag.Actions = await _auditService.GetDistinctActionsAsync();
            ViewBag.Entities = await _auditService.GetDistinctEntitiesAsync();
            ViewBag.Users = await _auditService.GetDistinctUsersAsync();

            return View(logs);
        }

        // GET: AuditLogs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var logs = await _auditService.GetAllAsync();
            var log = logs.FirstOrDefault(l => l.Id == id);

            if (log == null)
            {
                return NotFound();
            }

            return View(log);
        }

        // GET: AuditLogs/Export
        public async Task<IActionResult> Export(AuditLogFilterDto? filter)
        {
            filter ??= new AuditLogFilterDto { PageSize = int.MaxValue };

            var logs = await _auditService.GetFilteredAsync(filter);

            var csv = new StringBuilder();
            csv.AppendLine("ID,User,Action,Entity,Entity ID,Date,Old Value,New Value,Details");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id}," +
                              $"\"{log.UserEmail}\"," +
                              $"\"{log.Action}\"," +
                              $"\"{log.Entity}\"," +
                              $"{log.EntityId}," +
                              $"\"{log.PerformedAt:yyyy-MM-dd HH:mm:ss}\"," +
                              $"\"{log.OldValue?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.NewValue?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.Details?.Replace("\"", "\"\"")}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> Statistics()
        {
            var statistics = await _auditService.GetStatisticsAsync();
            return View(statistics);
        }
    }
}