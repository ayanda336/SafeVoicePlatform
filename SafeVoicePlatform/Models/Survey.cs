using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class Survey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Survey Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SurveyQuestion>? Questions { get; set; }
        public virtual ICollection<SurveyResponse>? Responses { get; set; }
    }

    public class SurveyQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SurveyId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "multiple_choice";

        [StringLength(500)]
        [Display(Name = "Options (comma separated)")]
        public string? Options { get; set; }

        [Display(Name = "Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = true;

        [ForeignKey("SurveyId")]
        public virtual Survey? Survey { get; set; }
    }

    public class SurveyResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SurveyId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Answer { get; set; } = string.Empty;

        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

        public bool IsAnonymous { get; set; } = true;

        [ForeignKey("SurveyId")]
        public virtual Survey? Survey { get; set; }

        [ForeignKey("QuestionId")]
        public virtual SurveyQuestion? Question { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}