using System;
using System.Collections.Generic;

namespace SafeVoicePlatform.Models
{
    public class InstitutionDashboardViewModel
    {
        public Institution? Institution { get; set; }

        // Statistics
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int UnderReviewReports { get; set; }
        public int ResolvedReports { get; set; }
        public int UrgentReports { get; set; }

        // Reports by type
        public Dictionary<string, int> ReportsByType { get; set; } = new Dictionary<string, int>();

        // Monthly trend
        public Dictionary<string, int> MonthlyTrend { get; set; } = new Dictionary<string, int>();

        // Recent reports
        public List<Report> RecentReports { get; set; } = new List<Report>();

        // Status distribution for chart
        public List<StatusCountItem> StatusDistribution { get; set; } = new List<StatusCountItem>();
    }
}