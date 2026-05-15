using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(30, ErrorMessage = "Title can be maximum 30 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(100, ErrorMessage = "Description can be maximum 100 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "File name is required")]
        [StringLength(150, ErrorMessage = "File name is too long")]
        public string FileName { get; set; }

        [Required(ErrorMessage = "File path is required")]
        [StringLength(200, ErrorMessage = "File path is too long")]
        public string FilePath { get; set; }

        public string ContentType { get; set; }

        public long? FileSize { get; set; }

        [Required]
        public string ClassName { get; set; }

        [Required]
        public int? StaffId { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }

        public virtual Staff Staff { get; set; }
    }
}
