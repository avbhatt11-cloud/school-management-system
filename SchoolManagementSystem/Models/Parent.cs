using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Parents")]
    public class Parent
    {
        [Key]
        public int ParentId { get; set; }

        // ===================== Student Details =====================
        [Required(ErrorMessage = "Student First Name is required")]
        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Only letters allowed")]
        public string StudentFirstName { get; set; }

        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        [RegularExpression(@"^[A-Za-z]*$", ErrorMessage = "Only letters allowed")]
        public string StudentMiddleName { get; set; }

        [Required(ErrorMessage = "Student Last Name is required")]
        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Only letters allowed")]
        public string StudentLastName { get; set; }

        [Required(ErrorMessage = "Student Gender is required")]
        public string StudentGender { get; set; }

        [Required(ErrorMessage = "Class is required")]
        public string ClassName { get; set; }

        // ===================== Parent Details =====================
        [Required(ErrorMessage = "Parent First Name is required")]
        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Only letters allowed")]
        public string ParentFirstName { get; set; }

        [RegularExpression(@"^[A-Za-z]*$", ErrorMessage = "Only letters allowed")]
        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        public string ParentMiddleName { get; set; }

        [Required(ErrorMessage = "Parent Last Name is required")]
        [StringLength(20, ErrorMessage = "Maximum 20 characters allowed")]
        [RegularExpression(@"^[A-Za-z]+$", ErrorMessage = "Only letters allowed")]
        public string ParentLastName { get; set; }

        [Required(ErrorMessage = "Parent Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be exactly 10 digits")]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [StringLength(50, ErrorMessage = "Email cannot exceed 50 characters")]
        [Display(Name = "Email")]
        [RegularExpression(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        ErrorMessage = "Enter valid email like: abc56@gmail.com"
        )]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters")]
        public string Address { get; set; }

        // ===================== Login =====================
        [Required(ErrorMessage = "Username is required")]
        [StringLength(20)]
        public string Username { get; set; }

        // ✅ UPDATED: No validation attributes - All validation in Controller
        // Password: 5-8 characters, letters + numbers allowed
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}