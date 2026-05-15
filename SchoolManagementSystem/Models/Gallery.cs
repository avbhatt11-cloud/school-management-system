using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Gallery")]
    public class Gallery
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Photo name is required")]
        [StringLength(200, ErrorMessage = "Photo name max 200 characters")]
        public string PhotoName { get; set; }

        [StringLength(50, ErrorMessage = "Photo path must be under 50 characters")]
        public string PhotoPath { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
