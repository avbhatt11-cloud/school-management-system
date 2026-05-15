//using System;
//using System.ComponentModel.DataAnnotations;

//namespace SchoolManagementSystem.Models
//{
//    public class Student
//    {
//        [Key]
//        public int Id { get; set; }

//        [Required, StringLength(50)]
//        public string FirstName { get; set; }

//        [StringLength(50)]
//        public string MiddleName { get; set; }

//        [Required, StringLength(50)]
//        public string LastName { get; set; }

//        [Required]
//        public string Gender { get; set; } // Male, Female, Other

//        [Required]
//        public string ClassName { get; set; } // Example: "Grade 1"

//        [Required, StringLength(50)]
//        public string ParentFirstName { get; set; }

//        [Required, StringLength(50)]
//        public string ParentLastName { get; set; }

//        [Required, StringLength(15)]
//        [Phone]
//        public string ContactNumber { get; set; }

//        [Required, StringLength(200)]
//        public string Address { get; set; }

//        public DateTime CreatedAt { get; set; }
//    }
//}
