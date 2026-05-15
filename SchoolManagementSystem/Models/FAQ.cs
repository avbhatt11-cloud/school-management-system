using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FAQ
    {
        [Key]
        public int FAQId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(40, ErrorMessage = "Name cannot exceed 40 characters")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Name must contain only letters")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Email is required")]
        [StringLength(50, ErrorMessage = "Email cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
 ErrorMessage = "Enter valid email like abc@gmail.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Question is required")]
        [StringLength(500, MinimumLength = 10,
 ErrorMessage = "Question must be between 10 to 500 characters")]
        public string Question { get; set; }

        //[Required(ErrorMessage = "Answer is required")]
        [StringLength(500, ErrorMessage = "Answer must be 500 characters")]  // Make sure this is a reasonable length, not too short
        public string Answer { get; set; }

        [StringLength(50)]
        public string AskedBy { get; set; } // "Visitor" ya "Parent"

        public DateTime AskedOn { get; set; }
        public DateTime? AnsweredOn { get; set; }
        // Parent relation
        public int? ParentId { get; set; }
        public virtual Parent Parent { get; set; }
    }
}