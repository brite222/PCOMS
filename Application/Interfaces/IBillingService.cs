using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IBillingService
    {
        ClientBillingDto GetClientBilling(
            int clientId,
            DateTime start,
            DateTime end
        );
    }
}
