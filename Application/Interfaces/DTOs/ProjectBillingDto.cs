public class ProjectBillingDto
{
    public string ProjectName { get; set; } = null!;
    public decimal HourlyRate { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalAmount { get; set; }
}
