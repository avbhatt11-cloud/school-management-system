using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Feedbacks")]
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [Required]
        public int ParentId { get; set; }

        [Required]
        [Display(Name = "Your Feedback")]
        [StringLength(200, ErrorMessage = "Feedback cannot exceed 200 characters.")]
        public string Message { get; set; }

        public string Reply { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? RepliedAt { get; set; }

        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }
    }
}
