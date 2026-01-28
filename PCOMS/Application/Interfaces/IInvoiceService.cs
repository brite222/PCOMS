using PCOMS.Models;

namespace PCOMS.Application.Interfaces
{
    public interface IInvoiceService
    {
        Invoice CreateInvoice(int clientId, DateTime from, DateTime to);
    }
}
