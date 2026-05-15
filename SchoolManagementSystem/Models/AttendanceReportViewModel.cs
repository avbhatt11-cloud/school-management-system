using System;
using System.Collections.Generic;

namespace SchoolManagementSystem.Models
{
    public class AttendanceReportViewModel
    {
        // ===== FILTERS =====
        public int? Day { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string Status { get; set; }   // Present | Absent | Late

        public string StudentName { get; set; }  // Status ke neeche

        // ===== RESULTS =====
        public List<AttendanceReportRow> Rows { get; set; } = new List<AttendanceReportRow>();

        // ===== SUMMARY =====
        public int TotalRecords { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
    }

    public class AttendanceReportRow
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string Status { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string StaffName { get; set; }

        public string Email { get; set; }
    }
}