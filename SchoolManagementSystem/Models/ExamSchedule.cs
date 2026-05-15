using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class ExamSchedule
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class name is required")]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Exam type is required")]
        [Display(Name = "Exam Type")]
        public string ExamType { get; set; }

        [Display(Name = "Schedule File Path")]
        public string ScheduleFilePath { get; set; }

        [Display(Name = "Schedule File Name")]
        public string ScheduleFileName { get; set; }

        [Display(Name = "Posted By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Posted Date")]
        public DateTime CreatedAt { get; set; }
    }
}
