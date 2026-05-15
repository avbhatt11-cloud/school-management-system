using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class TimeTable
    {
        [Key]
        public int TimeTableId { get; set; }

        // ================= Subject =================
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(30, ErrorMessage = "Subject can be maximum 20 characters")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Subject can contain only letters")]
        [Display(Name = "Subject")]
        public string SubjectName { get; set; }

        // ================= Teacher =================
        [Required(ErrorMessage = "Teacher name is required")]
        [StringLength(40, ErrorMessage = "Teacher name can be maximum 20 characters")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Teacher name can contain only letters")]
        [Display(Name = "Teacher Name")]
        public string TeacherName { get; set; }

        // ================= Week Day =================
        [Required(ErrorMessage = "Week day is required")]
        [Display(Name = "Week Day")]
        public string WeekDay { get; set; }

        // ================= Time =================
        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        // ================= Duration =================
        // ❌ REQUIRED NAHI
        [Display(Name = "Duration (Minutes)")]
        public int DurationInMinutes { get; set; }

        // ================= System Fields =================
        // ❌ Form se nahi aata → controller me set hota hai
        public int CreatedByStaffId { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ClassName { get; set; }

        // Constructor
        public TimeTable()
        {
            CreatedDate = DateTime.Now;
        }
    }
}
