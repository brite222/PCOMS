using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IInvoiceService
    {
        // Invoice CRUD
        Task<InvoiceDto?> CreateInvoiceAsync(CreateInvoiceDto dto, string userId);
        Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<InvoiceDto?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<IEnumerable<InvoiceDto>> GetInvoicesAsync(InvoiceFilterDto filter);
        Task<IEnumerable<InvoiceDto>> GetClientInvoicesAsync(int clientId);
        Task<IEnumerable<InvoiceDto>> GetProjectInvoicesAsync(int projectId);
        Task<bool> UpdateInvoiceAsync(UpdateInvoiceDto dto, string userId);
        Task<bool> DeleteInvoiceAsync(int id);

        // Generate from time/expenses
        Task<InvoiceDto?> GenerateInvoiceFromTimeEntriesAsync(GenerateInvoiceFromTimeDto dto, string userId);
        Task<List<CreateInvoiceItemDto>> GetBillableTimeEntriesAsync(int projectId, DateTime fromDate, DateTime toDate);
        Task<List<CreateInvoiceItemDto>> GetBillableExpensesAsync(int projectId, DateTime fromDate, DateTime toDate);

        // Invoice actions
        Task<bool> SendInvoiceAsync(int invoiceId, string userId);
        Task<bool> MarkAsSentAsync(int invoiceId);
        Task<bool> MarkAsViewedAsync(int invoiceId);
        Task<bool> CancelInvoiceAsync(int invoiceId, string userId);

        // Payment
        Task<PaymentDto?> RecordPaymentAsync(RecordPaymentDto dto, string userId);
        Task<IEnumerable<PaymentDto>> GetInvoicePaymentsAsync(int invoiceId);
        Task<bool> DeletePaymentAsync(int paymentId);

        // Calculations
        Task RecalculateInvoiceTotalsAsync(int invoiceId);
        Task UpdateInvoiceStatusAsync(int invoiceId);
        Task<string> GenerateInvoiceNumberAsync();

        // Recurring
        Task<InvoiceDto?> CreateRecurringInvoiceAsync(int parentInvoiceId, string userId);
        Task ProcessRecurringInvoicesAsync();

        // Reports
        Task<InvoiceReportDto> GetInvoiceReportAsync(DateTime fromDate, DateTime toDate, int? clientId = null);
        Task<ClientInvoiceReportDto> GetClientInvoiceReportAsync(int clientId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<decimal> GetTotalOutstandingAsync(int? clientId = null);
        Task<decimal> GetTotalOverdueAsync(int? clientId = null);
    }
}