using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class WorkSchedule
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public decimal HoursPerDay { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}