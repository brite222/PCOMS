using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // CREATE INVOICE
        // ==========================================
        public async Task<InvoiceDto?> CreateInvoiceAsync(CreateInvoiceDto dto, string userId)
        {
            try
            {
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    ProjectId = dto.ProjectId,
                    ClientId = dto.ClientId,
                    InvoiceDate = dto.InvoiceDate,
                    DueDate = dto.DueDate,
                    TaxRate = dto.TaxRate,
                    DiscountAmount = dto.DiscountAmount,
                    Notes = dto.Notes,
                    Terms = dto.Terms,
                    IsRecurring = dto.IsRecurring,
                    RecurringFrequency = dto.RecurringFrequency,
                    Status = InvoiceStatus.Draft,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Add invoice items
                foreach (var itemDto in dto.InvoiceItems)
                {
                    var item = new InvoiceItem
                    {
                        InvoiceId = invoice.Id,
                        Description = itemDto.Description,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TimeEntryId = itemDto.TimeEntryId,
                        ExpenseId = itemDto.ExpenseId,
                        Order = itemDto.Order
                    };
                    _context.InvoiceItems.Add(item);
                }

                await _context.SaveChangesAsync();

                // Recalculate totals
                await RecalculateInvoiceTotalsAsync(invoice.Id);

                // Set next recurring date if applicable
                if (invoice.IsRecurring && invoice.RecurringFrequency.HasValue)
                {
                    invoice.NextRecurringDate = CalculateNextRecurringDate(invoice.InvoiceDate, invoice.RecurringFrequency.Value);
                    await _context.SaveChangesAsync();
                }

                return await GetInvoiceByIdAsync(invoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                throw;
            }
        }

        // ==========================================
        // GET INVOICE BY ID
        // ==========================================
        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Project)
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (invoice == null) return null;

            var creator = await _context.Users.FindAsync(invoice.CreatedBy);

            return new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                ProjectId = invoice.ProjectId,
                ProjectName = invoice.Project.Name,
                ClientId = invoice.ClientId,
                ClientName = invoice.Client.Name,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status.ToString(),
                Subtotal = invoice.Subtotal,
                TaxRate = invoice.TaxRate,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                Balance = invoice.Balance,
                Notes = invoice.Notes,
                Terms = invoice.Terms,
                IsRecurring = invoice.IsRecurring,
                RecurringFrequency = invoice.RecurringFrequency?.ToString(),
                NextRecurringDate = invoice.NextRecurringDate,
                CreatedBy = invoice.CreatedBy,
                CreatedAt = invoice.CreatedAt,
                InvoiceItems = invoice.InvoiceItems.Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    InvoiceId = item.InvoiceId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Amount,
                    TimeEntryId = item.TimeEntryId,
                    ExpenseId = item.ExpenseId,
                    Order = item.Order
                }).OrderBy(i => i.Order).ToList(),
                Payments = invoice.Payments.Where(p => !p.IsDeleted).Select(p => new PaymentDto
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    Notes = p.Notes,
                    RecordedBy = p.RecordedBy,
                    RecordedByName = _context.Users.Find(p.RecordedBy)?.Email ?? "Unknown",
                    CreatedAt = p.CreatedAt
                }).ToList()
            };
        }

        // ==========================================
        // GET INVOICES WITH FILTER
        // ==========================================
        public async Task<IEnumerable<InvoiceDto>> GetInvoicesAsync(InvoiceFilterDto filter)
        {
            var query = _context.Invoices
                .Include(i => i.Project)
                .Include(i => i.Client)
                .Where(i => !i.IsDeleted)
                .AsQueryable();

            if (filter.ProjectId.HasValue)
                query = query.Where(i => i.ProjectId == filter.ProjectId.Value);

            if (filter.ClientId.HasValue)
                query = query.Where(i => i.ClientId == filter.ClientId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(i => i.Status.ToString() == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= filter.ToDate.Value);

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                query = query.Where(i => i.DueDate < DateTime.Today && i.Status != InvoiceStatus.Paid);

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                dtos.Add(new InvoiceDto
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    ProjectId = invoice.ProjectId,
                    ProjectName = invoice.Project.Name,
                    ClientId = invoice.ClientId,
                    ClientName = invoice.Client.Name,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    Status = invoice.Status.ToString(),
                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = invoice.AmountPaid,
                    Balance = invoice.Balance,
                    CreatedAt = invoice.CreatedAt
                });
            }

            return dtos;
        }

        // ==========================================
        // GENERATE INVOICE FROM TIME ENTRIES
        // ==========================================
        public async Task<InvoiceDto?> GenerateInvoiceFromTimeEntriesAsync(GenerateInvoiceFromTimeDto dto, string userId)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Client)
                    .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

                if (project == null)
                    throw new InvalidOperationException("Project not found");

                var invoiceItems = new List<CreateInvoiceItemDto>();

                // Get billable time entries
                var timeItems = await GetBillableTimeEntriesAsync(dto.ProjectId, dto.FromDate, dto.ToDate);
                invoiceItems.AddRange(timeItems);

                // Get billable expenses if requested
                if (dto.IncludeExpenses)
                {
                    var expenseItems = await GetBillableExpensesAsync(dto.ProjectId, dto.FromDate, dto.ToDate);
                    invoiceItems.AddRange(expenseItems);
                }

                if (!invoiceItems.Any())
                    throw new InvalidOperationException("No billable items found for the selected period");

                var createDto = new CreateInvoiceDto
                {
                    ProjectId = dto.ProjectId,
                    ClientId = project.ClientId,
                    InvoiceDate = DateTime.Today,
                    DueDate = dto.DueDate,
                    TaxRate = dto.TaxRate,
                    DiscountAmount = dto.DiscountAmount,
                    Notes = dto.Notes,
                    Terms = dto.Terms,
                    InvoiceItems = invoiceItems
                };

                return await CreateInvoiceAsync(createDto, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice from time entries");
                throw;
            }
        }

        // ==========================================
        // GET BILLABLE TIME ENTRIES
        // ==========================================
        public async Task<List<CreateInvoiceItemDto>> GetBillableTimeEntriesAsync(int projectId, DateTime fromDate, DateTime toDate)
        {
            var timeEntries = await _context.TimeEntries
                .Where(e => e.ProjectId == projectId
                    && e.Date >= fromDate
                    && e.Date <= toDate
                    && e.IsBillable
                    && e.Status == TimeEntryStatus.Approved
                    && !e.IsDeleted)
                .ToListAsync();

            var items = new List<CreateInvoiceItemDto>();
            int order = 0;

            foreach (var entry in timeEntries)
            {
                items.Add(new CreateInvoiceItemDto
                {
                    Description = $"{entry.Description} ({entry.Date:MMM dd, yyyy})",
                    Quantity = entry.Hours,
                    UnitPrice = entry.HourlyRate ?? 0m,
                    TimeEntryId = entry.Id,
                    Order = order++
                });
            }

            return items;
        }

        // ==========================================
        // GET BILLABLE EXPENSES
        // ==========================================
        public async Task<List<CreateInvoiceItemDto>> GetBillableExpensesAsync(int projectId, DateTime fromDate, DateTime toDate)
        {
            var expenses = await _context.Expenses
                .Where(e => e.ProjectId == projectId
                    && e.ExpenseDate >= fromDate
                    && e.ExpenseDate <= toDate
                    && e.IsBillable
                    && e.Status == ExpenseStatus.Approved
                    && !e.IsDeleted)
                .ToListAsync();

            var items = new List<CreateInvoiceItemDto>();
            int order = 100; // Start after time entries

            foreach (var expense in expenses)
            {
                items.Add(new CreateInvoiceItemDto
                {
                    Description = $"{expense.Category} - {expense.Description} ({expense.ExpenseDate:MMM dd, yyyy})",
                    Quantity = 1,
                    UnitPrice = expense.Amount,
                    ExpenseId = expense.Id,
                    Order = order++
                });
            }

            return items;
        }

        // ==========================================
        // RECALCULATE INVOICE TOTALS
        // ==========================================
        public async Task RecalculateInvoiceTotalsAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return;

            // Calculate subtotal
            invoice.Subtotal = invoice.InvoiceItems.Sum(item => item.Amount);

            // Calculate tax
            invoice.TaxAmount = invoice.Subtotal * (invoice.TaxRate / 100);

            // Calculate total
            invoice.TotalAmount = invoice.Subtotal + invoice.TaxAmount - invoice.DiscountAmount;

            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update status based on payments
            await UpdateInvoiceStatusAsync(invoiceId);
        }

        // ==========================================
        // UPDATE INVOICE STATUS
        // ==========================================
        public async Task UpdateInvoiceStatusAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return;

            // Don't update if cancelled or refunded
            if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Refunded)
                return;

            // Check payment status
            if (invoice.Balance <= 0)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            else if (invoice.DueDate < DateTime.Today && invoice.Status != InvoiceStatus.Draft)
            {
                invoice.Status = InvoiceStatus.Overdue;
            }

            await _context.SaveChangesAsync();
        }

        // ==========================================
        // RECORD PAYMENT
        // ==========================================
        public async Task<PaymentDto?> RecordPaymentAsync(RecordPaymentDto dto, string userId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
                if (invoice == null)
                    throw new InvalidOperationException("Invoice not found");

                if (dto.Amount > invoice.Balance)
                    throw new InvalidOperationException($"Payment amount (₦{dto.Amount:N2}) exceeds invoice balance (₦{invoice.Balance:N2})");

                var payment = new Payment
                {
                    InvoiceId = dto.InvoiceId,
                    Amount = dto.Amount,
                    PaymentDate = dto.PaymentDate,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.ReferenceNumber,
                    Notes = dto.Notes,
                    RecordedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                // Update invoice amount paid
                invoice.AmountPaid += dto.Amount;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update invoice status
                await UpdateInvoiceStatusAsync(dto.InvoiceId);

                var recordedBy = await _context.Users.FindAsync(userId);

                return new PaymentDto
                {
                    Id = payment.Id,
                    InvoiceId = payment.InvoiceId,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    ReferenceNumber = payment.ReferenceNumber,
                    Notes = payment.Notes,
                    RecordedBy = payment.RecordedBy,
                    RecordedByName = recordedBy?.Email ?? "Unknown",
                    CreatedAt = payment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment");
                throw;
            }
        }

        // ==========================================
        // GENERATE INVOICE NUMBER
        // ==========================================
        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"INV-{year}-";

            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastInvoice != null)
            {
                var lastNumber = lastInvoice.InvoiceNumber.Replace(prefix, "");
                if (int.TryParse(lastNumber, out int num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        // ==========================================
        // SEND INVOICE
        // ==========================================
        public async Task<bool> SendInvoiceAsync(int invoiceId, string userId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice == null) return false;

                if (invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Sent;
                    invoice.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // TODO: Send email notification to client

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice");
                return false;
            }
        }

        // ==========================================
        // RECURRING INVOICES
        // ==========================================
        private DateTime CalculateNextRecurringDate(DateTime currentDate, RecurringFrequency frequency)
        {
            return frequency switch
            {
                RecurringFrequency.Weekly => currentDate.AddDays(7),
                RecurringFrequency.BiWeekly => currentDate.AddDays(14),
                RecurringFrequency.Monthly => currentDate.AddMonths(1),
                RecurringFrequency.Quarterly => currentDate.AddMonths(3),
                RecurringFrequency.Yearly => currentDate.AddYears(1),
                _ => currentDate.AddMonths(1)
            };
        }

        public async Task<InvoiceDto?> CreateRecurringInvoiceAsync(int parentInvoiceId, string userId)
        {
            var parent = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == parentInvoiceId && i.IsRecurring);

            if (parent == null) return null;

            var dto = new CreateInvoiceDto
            {
                ProjectId = parent.ProjectId,
                ClientId = parent.ClientId,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                TaxRate = parent.TaxRate,
                DiscountAmount = parent.DiscountAmount,
                Notes = parent.Notes,
                Terms = parent.Terms,
                IsRecurring = true,
                RecurringFrequency = parent.RecurringFrequency,
                InvoiceItems = parent.InvoiceItems.Select(item => new CreateInvoiceItemDto
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Order = item.Order
                }).ToList()
            };

            var newInvoice = await CreateInvoiceAsync(dto, userId);

            if (newInvoice != null && parent.RecurringFrequency.HasValue)
            {
                parent.NextRecurringDate = CalculateNextRecurringDate(DateTime.Today, parent.RecurringFrequency.Value);
                await _context.SaveChangesAsync();
            }

            return newInvoice;
        }

        public async Task ProcessRecurringInvoicesAsync()
        {
            var dueInvoices = await _context.Invoices
                .Where(i => i.IsRecurring && i.NextRecurringDate <= DateTime.Today && !i.IsDeleted)
                .ToListAsync();

            foreach (var invoice in dueInvoices)
            {
                await CreateRecurringInvoiceAsync(invoice.Id, invoice.CreatedBy);
            }
        }

        // ==========================================
        // REPORTS
        // ==========================================
        public async Task<InvoiceReportDto> GetInvoiceReportAsync(DateTime fromDate, DateTime toDate, int? clientId = null)
        {
            var query = _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Project)
                .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate && !i.IsDeleted);

            if (clientId.HasValue)
                query = query.Where(i => i.ClientId == clientId.Value);

            var invoices = await query.ToListAsync();

            return new InvoiceReportDto
            {
                ReportType = "Invoice Report",
                FromDate = fromDate,
                ToDate = toDate,
                TotalInvoiced = invoices.Sum(i => i.TotalAmount),
                TotalPaid = invoices.Sum(i => i.AmountPaid),
                TotalOutstanding = invoices.Sum(i => i.Balance),
                TotalOverdue = invoices.Where(i => i.DueDate < DateTime.Today && i.Balance > 0).Sum(i => i.Balance),
                InvoiceCount = invoices.Count,
                PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid),
                OverdueCount = invoices.Count(i => i.DueDate < DateTime.Today && i.Balance > 0),
                RevenueByClient = invoices.GroupBy(i => i.Client.Name).ToDictionary(g => g.Key, g => g.Sum(i => i.TotalAmount)),
                RevenueByProject = invoices.GroupBy(i => i.Project.Name).ToDictionary(g => g.Key, g => g.Sum(i => i.TotalAmount)),
                InvoicesByStatus = invoices.GroupBy(i => i.Status.ToString()).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<ClientInvoiceReportDto> GetClientInvoiceReportAsync(int clientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            var filter = new InvoiceFilterDto
            {
                ClientId = clientId,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = 1000
            };

            var invoices = (await GetInvoicesAsync(filter)).ToList();

            return new ClientInvoiceReportDto
            {
                ClientId = clientId,
                ClientName = client.Name,
                TotalInvoiced = invoices.Sum(i => i.TotalAmount),
                TotalPaid = invoices.Sum(i => i.AmountPaid),
                TotalOutstanding = invoices.Sum(i => i.Balance),
                InvoiceCount = invoices.Count,
                Invoices = invoices
            };
        }

        public async Task<decimal> GetTotalOutstandingAsync(int? clientId = null)
        {
            var query = _context.Invoices.Where(i => !i.IsDeleted && i.TotalAmount > i.AmountPaid);

            if (clientId.HasValue)
                query = query.Where(i => i.ClientId == clientId.Value);

            return await query.SumAsync(i => i.TotalAmount - i.AmountPaid);
        }

        public async Task<decimal> GetTotalOverdueAsync(int? clientId = null)
        {
            var query = _context.Invoices.Where(i => !i.IsDeleted
                && i.TotalAmount > i.AmountPaid
                && i.DueDate < DateTime.Today);

            if (clientId.HasValue)
                query = query.Where(i => i.ClientId == clientId.Value);

            return await query.SumAsync(i => i.TotalAmount - i.AmountPaid);
        }
        

        // ==========================================
        // ADDITIONAL METHODS
        // ==========================================
        public async Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var id = await _context.Invoices
                .Where(i => i.InvoiceNumber == invoiceNumber)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            return id > 0 ? await GetInvoiceByIdAsync(id) : null;
        }

        public async Task<IEnumerable<InvoiceDto>> GetClientInvoicesAsync(int clientId)
        {
            return await GetInvoicesAsync(new InvoiceFilterDto { ClientId = clientId, PageSize = 1000 });
        }

        public async Task<IEnumerable<InvoiceDto>> GetProjectInvoicesAsync(int projectId)
        {
            return await GetInvoicesAsync(new InvoiceFilterDto { ProjectId = projectId, PageSize = 1000 });
        }

        public async Task<bool> UpdateInvoiceAsync(UpdateInvoiceDto dto, string userId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == dto.Id);

            if (invoice == null) return false;

            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.DueDate = dto.DueDate;
            invoice.TaxRate = dto.TaxRate;
            invoice.DiscountAmount = dto.DiscountAmount;
            invoice.Notes = dto.Notes;
            invoice.Terms = dto.Terms;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Update items
            _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
            foreach (var itemDto in dto.InvoiceItems)
            {
                _context.InvoiceItems.Add(new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TimeEntryId = itemDto.TimeEntryId,
                    ExpenseId = itemDto.ExpenseId,
                    Order = itemDto.Order
                });
            }

            await _context.SaveChangesAsync();
            await RecalculateInvoiceTotalsAsync(invoice.Id);

            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return false;

            invoice.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsSentAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            invoice.Status = InvoiceStatus.Sent;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsViewedAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            if (invoice.Status == InvoiceStatus.Sent)
                invoice.Status = InvoiceStatus.Viewed;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId, string userId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PaymentDto>> GetInvoicePaymentsAsync(int invoiceId)
        {
            var payments = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var dtos = new List<PaymentDto>();
            foreach (var payment in payments)
            {
                var user = await _context.Users.FindAsync(payment.RecordedBy);
                dtos.Add(new PaymentDto
                {
                    Id = payment.Id,
                    InvoiceId = payment.InvoiceId,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    ReferenceNumber = payment.ReferenceNumber,
                    Notes = payment.Notes,
                    RecordedBy = payment.RecordedBy,
                    RecordedByName = user?.Email ?? "Unknown",
                    CreatedAt = payment.CreatedAt
                });
            }
            return dtos;
        }

        public async Task<bool> DeletePaymentAsync(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return false;

            var invoice = await _context.Invoices.FindAsync(payment.InvoiceId);
            if (invoice != null)
            {
                invoice.AmountPaid -= payment.Amount;
                await UpdateInvoiceStatusAsync(invoice.Id);
            }

            payment.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}