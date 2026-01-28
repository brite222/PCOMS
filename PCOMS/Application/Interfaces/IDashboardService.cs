using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IDashboardService
    {
        AdminDashboardDto GetAdminDashboard();
        DeveloperDashboardDto GetDeveloperDashboard(string developerId);
    }
}
