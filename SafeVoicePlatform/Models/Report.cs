using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Report Type")]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Incident Date")]
        [DataType(DataType.DateTime)]
        public DateTime? IncidentDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Is Anonymous")]
        public bool IsAnonymous { get; set; } = false;

        [StringLength(50)]
        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Status Updated At")]
        public DateTime? StatusUpdatedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Is Urgent")]
        public bool IsUrgent { get; set; } = false;

        [Display(Name = "Witness Information")]
        public string? WitnessInfo { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public string? UserId { get; set; }
        public int? InstitutionId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }

        public virtual ICollection<ReportAttachment>? Attachments { get; set; }
        public virtual ICollection<ReportStatusHistory>? StatusHistory { get; set; }
    }
}