using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Personal Information
        public string? FullName { get; set; }
        public string? Department { get; set; }

        // Role-specific fields
        public int? InstitutionId { get; set; }
        public int? SupportOrgId { get; set; }

        // Account status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Optional: For tracking reports anonymously
        public bool AllowAnonymousReporting { get; set; } = true;

        // Navigation properties
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }

        [ForeignKey("SupportOrgId")]
        public virtual SupportOrganization? SupportOrganization { get; set; }

        public virtual ICollection<Report>? Reports { get; set; }
    }
}