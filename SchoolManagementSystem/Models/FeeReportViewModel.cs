using System;
using System.Collections.Generic;

namespace SchoolManagementSystem.Models
{
    public class FeeReportViewModel
    {
        // ===== FILTERS =====
        public int? Day { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string ClassName { get; set; }
        public string FeeType { get; set; }
        public string Status { get; set; }

        // ===== RESULTS =====
        public List<FeeReportRow> Rows { get; set; } = new List<FeeReportRow>();

        // ===== SUMMARY =====
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
        public int TotalRecords { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }

        // ===== DROPDOWNS =====
        public List<string> ClassList { get; set; } = new List<string>();
    }

    public class FeeReportRow
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string FeeType { get; set; }
        public int? Period { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMode { get; set; }
        public string ParentName { get; set; }
        public string ContactNo { get; set; }
    }
}