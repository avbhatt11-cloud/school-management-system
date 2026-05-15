using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Xml;

namespace SchoolManagementSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection") { }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Parent> Parents { get; set; }
        //public DbSet<Student> Students { get; set; }
        public DbSet<Gallery> Gallery { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<FeePayment> FeePayments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FAQ> FAQs { get; set; }

       
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<LeaveReport> LeaveReports { get; set; }
        public DbSet<ExamSchedule> ExamSchedules { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<TimeTable> TimeTables { get; set; }
        // For Staff timetable
        //public DbSet<StaffClassSubject> StaffClassSubjects { get; set; }


    }

    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Staffs
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Parents
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        // Optional: navigation property
        public virtual ICollection<Feedback> Feedbacks { get; set; }
    }
    

}
