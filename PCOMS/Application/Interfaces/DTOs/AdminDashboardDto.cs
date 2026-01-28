namespace PCOMS.Application.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalClients { get; set; }
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalDevelopers { get; set; }

        public decimal TotalHours { get; set; }
        public decimal HoursThisWeek { get; set; }
    }
}
