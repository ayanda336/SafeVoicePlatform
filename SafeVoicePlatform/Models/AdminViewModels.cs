using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SafeVoicePlatform.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalReports { get; set; }
        public int TotalInstitutions { get; set; }
        public int TotalSupportOrganizations { get; set; }
        public int PendingReports { get; set; }
        public int UrgentReports { get; set; }
        public int AnonymousReports { get; set; }
        public int TotalReferrals { get; set; }

        public List<Report> RecentReports { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();
        public List<StatusCountItem> ReportsByType { get; set; } = new();
        public List<StatusCountItem> ReportsByStatus { get; set; } = new();
    }

    public class UserAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? InstitutionId { get; set; }
        public int? SupportOrgId { get; set; }
        public string? InstitutionName { get; set; }
        public string? SupportOrgName { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; }
        public List<string> SelectedRoles { get; set; } = new();
        public List<SelectListItem> AllRoles { get; set; } = new();
        public int? InstitutionId { get; set; }
        public int? SupportOrgId { get; set; }
    }
}