using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    [Table("FeePayments")]
    public class FeePayment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int ParentId { get; set; }

        [Required]
        public int FeeId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? PaymentDate { get; set; }

        [StringLength(100)]
        public string StudentName { get; set; }

        [StringLength(50)]
        public string TransactionId { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [StringLength(200)]
        public string ReceiptPath { get; set; }

        [StringLength(20)]
        public string PaymentMode { get; set; } = "Online";

        // Navigation properties
        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }

        [ForeignKey("FeeId")]
        public virtual Fee Fee { get; set; }
    }
}





