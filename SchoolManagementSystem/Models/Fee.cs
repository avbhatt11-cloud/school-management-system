using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class Fee
    {
        [Key]
        public int FeeId { get; set; }

        [Required(ErrorMessage = "Class is required")]
        [StringLength(50)]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 9999999999, ErrorMessage = "Max 10 digits allowed")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Fee type is required")]
        public string FeeType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int Period { get; set; } // NEW: 1 for first 6-month, 2 for second 6-month, 1 for Annual
    }
}
