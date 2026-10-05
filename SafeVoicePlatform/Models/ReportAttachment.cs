using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeVoicePlatform.Models
{
    public class ReportAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportId { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(50)]
        public string FileType { get; set; } = string.Empty; // image, document, video

        public long FileSize { get; set; } // in bytes

        [Display(Name = "Uploaded At")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ReportId")]
        public virtual Report? Report { get; set; }
    }
}