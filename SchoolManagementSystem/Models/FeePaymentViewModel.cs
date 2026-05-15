using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class FeePaymentViewModel
    {
        public int PaymentId { get; set; }
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        public string ClassName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public string ReceiptPath { get; set; }
        public string FeeType { get; set; }
        public string PaymentMode { get; set; }


    }

    public class OfflineFeePaymentViewModel
    {
        [Required(ErrorMessage = "Student name is required")] // ✅ CUSTOM MESSAGE
        public int ParentId { get; set; }

        // ❌ Remove Required from StudentName
        public string StudentName { get; set; }

        [Required(ErrorMessage = "Class is required")]
        public string ClassName { get; set; }

        //[Required(ErrorMessage = "Amount is required")]
        public decimal Amount { get; set; }

        //[Required(ErrorMessage = "Fee type is required")]
        public string FeeType { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [CustomValidation(typeof(OfflineFeePaymentViewModel), "ValidatePaymentDate")]
        public DateTime PaymentDate { get; set; }

        // Custom validation method
        public static ValidationResult ValidatePaymentDate(DateTime paymentDate, ValidationContext context)
        {
            if (paymentDate > DateTime.Today)
            {
                return new ValidationResult("⚠️ Future dates are not allowed! Please select today or an earlier date.");
            }
            return ValidationResult.Success;
        }
    }


}