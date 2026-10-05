using System;
using System.Collections.Generic;

namespace SafeVoicePlatform.Models
{
    public class SupportOrgDashboardViewModel
    {
        public SupportOrganization? Organization { get; set; }

        // Statistics
        public int TotalReferrals { get; set; }
        public int PendingReferrals { get; set; }
        public int InProgressReferrals { get; set; }
        public int CompletedReferrals { get; set; }
        public int UrgentReferrals { get; set; }

        // Referrals by type
        public Dictionary<string, int> ReferralsByType { get; set; } = new Dictionary<string, int>();

        // Monthly trend
        public Dictionary<string, int> MonthlyTrend { get; set; } = new Dictionary<string, int>();

        // Recent referrals
        public List<Referral> RecentReferrals { get; set; } = new List<Referral>();

        // Status distribution
        public List<StatusCountItem> StatusDistribution { get; set; } = new List<StatusCountItem>();
    }

    public class StatusCountItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}