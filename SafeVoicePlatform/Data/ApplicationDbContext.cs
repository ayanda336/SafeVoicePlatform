using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeVoicePlatform.Models;

namespace SafeVoicePlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Institution> Institutions { get; set; }
        public DbSet<SupportOrganization> SupportOrganizations { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportAttachment> ReportAttachments { get; set; }
        public DbSet<ReportStatusHistory> ReportStatusHistories { get; set; }
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Report>()
                .HasOne(r => r.Institution)
                .WithMany(i => i.Reports)
                .HasForeignKey(r => r.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SupportOrganization relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.SupportOrganization)
                .WithMany(s => s.Staff)
                .HasForeignKey(u => u.SupportOrgId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Referral relationships
            builder.Entity<Referral>()
                .HasOne(r => r.Report)
                .WithMany()
                .HasForeignKey(r => r.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Referral>()
                .HasOne(r => r.SupportOrganization)
                .WithMany(s => s.Referrals)
                .HasForeignKey(r => r.SupportOrgId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Survey relationships - NO CASCADE for responses
            builder.Entity<SurveyQuestion>()
                .HasOne(q => q.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // SurveyResponse - all with Restrict to avoid multiple cascade paths
            builder.Entity<SurveyResponse>()
                .HasOne(r => r.Survey)
                .WithMany(s => s.Responses)
                .HasForeignKey(r => r.SurveyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SurveyResponse>()
                .HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SurveyResponse>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes
            builder.Entity<Report>()
                .HasIndex(r => r.Status);

            builder.Entity<Report>()
                .HasIndex(r => r.CreatedAt);

            builder.Entity<Report>()
                .HasIndex(r => r.ReferenceNumber)
                .IsUnique()
                .HasFilter("[ReferenceNumber] IS NOT NULL");

            builder.Entity<SupportOrganization>()
                .HasIndex(s => s.Name)
                .IsUnique();

            builder.Entity<Referral>()
                .HasIndex(r => r.Status);

            builder.Entity<Referral>()
                .HasIndex(r => r.ReferredAt);

            // Survey Indexes
            builder.Entity<SurveyResponse>()
                .HasIndex(r => new { r.SurveyId, r.UserId, r.QuestionId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            builder.Entity<SurveyResponse>()
                .HasIndex(r => r.SurveyId);

            builder.Entity<SurveyResponse>()
                .HasIndex(r => r.QuestionId);

            builder.Entity<SurveyResponse>()
                .HasIndex(r => r.RespondedAt);
        }
    }
}