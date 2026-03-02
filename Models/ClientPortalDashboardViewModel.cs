using PCOMS.Models;

namespace PCOMS.ViewModels
{
    public class ClientPortalDashboardViewModel
    {
        public string ClientName { get; set; } = string.Empty;
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public List<ClientProjectViewModel> Projects { get; set; } = new();
        public int PendingInvoices { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public List<ClientInvoiceViewModel> RecentInvoices { get; set; } = new();
        public int TotalDocuments { get; set; }
        public List<ClientDocumentViewModel> RecentDocuments { get; set; } = new();
    }

    public class ClientProjectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class ClientInvoiceViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
    }

    public class ClientDocumentViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
    }
}