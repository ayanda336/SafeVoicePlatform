using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class ReportStatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? ChangedBy { get; set; }

        [ForeignKey("ReportId")]
        public virtual Report? Report { get; set; }
    }
}