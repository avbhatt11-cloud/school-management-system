using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Staffs")]
    public class Staff
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, ErrorMessage = "Username cannot exceed 20 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(20, ErrorMessage = "First name cannot exceed 20 characters")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "First name must contain only letters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }


        [Required(ErrorMessage = "Last name is required")]
        [StringLength(20, ErrorMessage = "Last name cannot exceed 20 characters")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Last name must contain only letters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(
    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    ErrorMessage = "Enter valid email like: abc56@gmail.com"
)]
        [StringLength(50, ErrorMessage = "Email cannot exceed 50 characters")]
        [Display(Name = "Email")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be exactly 10 digits")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Contact number must be exactly 10 digits")]
        [Display(Name = "Contact No")]
        public string ContactNo { get; set; }

        // ✅ NO StringLength - validation happens only in controller
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Password must contain only letters and numbers")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [NotMapped]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Confirm Password must contain only letters and numbers")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters")]
        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Class assigned is required")]
        [StringLength(50)]
        [Display(Name = "Class Assigned")]
        public string ClassAssigned { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(20, ErrorMessage = "Designation cannot exceed 20 characters")]
        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [StringLength(10)]
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Education is required")]
        [StringLength(20, ErrorMessage = "Education cannot exceed 20 characters")]
        [Display(Name = "Education")]
        public string Education { get; set; }

        [StringLength(500)]
        public string PhotoPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
