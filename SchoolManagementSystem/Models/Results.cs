using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Results")]
    public class Result
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Student selection is required")]
        [ForeignKey("Parent")]
        public int ParentId { get; set; }
        public virtual Parent Parent { get; set; }

        [Required(ErrorMessage = "Exam type is required")]
        [StringLength(50)]
        public string ExamType { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [StringLength(255)]
        public string ResultFile { get; set; }
    }
}
