using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Remoting.Lifetime;

namespace SchoolManagementSystem.Models
{
    public class LeaveReport
    {
        [Key]
        public int Id { get; set; }

        // Parent se hi Student info handle karenge
        public int? ParentId { get; set; }
        public virtual Parent Parent { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(50, ErrorMessage = "Title cannot exceed 50 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required")]
        public DateTime? EndDate { get; set; }
        public int? Days { get; set; }

        public string AttachmentFileName { get; set; }
        public string AttachmentPath { get; set; }
        public string AttachmentContentType { get; set; }
        public long? AttachmentSize { get; set; }

        public string Status { get; set; } = "Pending";
        //public DateTime? ReviewedAt { get; set; }
        //public string StaffComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        //[ForeignKey("ReviewedByStaff")]
        ////public int? ReviewedByStaffId { get; set; }
        //public virtual Staff ReviewedByStaff { get; set; }

        [NotMapped]
        public string StudentName
        {
            get
            {
                if (Parent == null) return string.Empty;
                if (!string.IsNullOrWhiteSpace(Parent.StudentMiddleName))
                    return $"{Parent.StudentFirstName} {Parent.StudentMiddleName} {Parent.StudentLastName}";
                return $"{Parent.StudentFirstName} {Parent.StudentLastName}";
            }
        }

        [NotMapped]
        public string ClassName => Parent?.ClassName ?? string.Empty;

        [NotMapped]
        public int TotalDays
        {
            get
            {
                if (StartDate != null && EndDate != null)
                    return (EndDate.Value - StartDate.Value).Days + 1;

                return 0;
            }
        }

    }
}
