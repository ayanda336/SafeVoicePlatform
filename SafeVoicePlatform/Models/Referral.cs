using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportId { get; set; }

        [Required]
        public int SupportOrgId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Referred At")]
        public DateTime ReferredAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Referred By")]
        public string? ReferredBy { get; set; }

        [Display(Name = "Contact Made")]
        public bool ContactMade { get; set; } = false;

        [Display(Name = "Contact Date")]
        public DateTime? ContactDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Outcome")]
        public string? Outcome { get; set; }

        // Navigation properties
        [ForeignKey("ReportId")]
        public virtual Report? Report { get; set; }

        [ForeignKey("SupportOrgId")]
        public virtual SupportOrganization? SupportOrganization { get; set; }
    }
}