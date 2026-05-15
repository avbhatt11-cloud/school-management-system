using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("Attendance")]
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int ParentId { get; set; }   // 👈 Student info Parent table me hai

        [Required]
        public int StaffId { get; set; }

        [Column(TypeName = "date")]

        public DateTime AttendanceDate { get; set; }

        [Required, StringLength(10)]
        public string Status { get; set; }   // Present / Absent / Late

        public DateTime CreatedAt { get; set; }

        // Relations
        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }



    }
}
