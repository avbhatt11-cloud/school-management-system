
using SchoolManagementSystem.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace SchoolManagementSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ===================================================================== PRIVATE HELPERS ==========================================================================
        private bool IsStaff()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Staff";
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Account");
        }

        private void PopulateClassAssigned(string selected = null)
        {
            var classes = new[]
            {
                "KG","1","2","3","4","5","6","7","8","9","10",
                "11 Science","11 Commerce","11 Arts",
                "12 Science","12 Commerce","12 Arts"
            }.ToList();

            ViewBag.ClassList = new SelectList(classes, selected);
        }


        // ===================================================================== DASHBOARD ==========================================================================

        public ActionResult Index()
        {
            if (Session["StaffId"] == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Username = Session["Username"];
            ViewBag.ClassAssigned = Session["ClassAssigned"];

            if (!IsStaff()) return RedirectToLogin();

            string classAssigned = Session["ClassAssigned"]?.ToString();
            int staffId = Convert.ToInt32(Session["StaffId"]);

            var recentActivities = new List<RecentActivityViewModel>();
            var importantReminders = new List<RecentActivityViewModel>();

            if (string.IsNullOrEmpty(classAssigned))
            {
                ViewBag.RecentActivities = recentActivities;
                ViewBag.ImportantReminders = importantReminders;
                return View();
            }

            // ================= 7 DAYS CALCULATION =================
            DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);
            DateTime today = DateTime.Today;

            // ================= DASHBOARD STATISTICS =================
            int totalStudents = db.Parents
                .Count(p => p.ClassName == classAssigned && p.IsActive);

            int presentToday = db.Attendances
                .Count(a => a.Parent.ClassName == classAssigned
                         && a.AttendanceDate == DateTime.Today
                         && a.Status == "Present");

            int pendingLeaves = db.LeaveReports
                .Count(l => l.Parent.ClassName == classAssigned && l.Status == "Pending");

            int materialsUploaded = db.Materials
                .Count(m => m.ClassName == classAssigned && m.IsActive);

            // ================= CLASS OVERVIEW =================
            int activeStudents = totalStudents;

            double averageAttendance = 0;
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);

            var attendanceRecords = db.Attendances
                .Where(a => a.Parent.ClassName == classAssigned
                         && a.AttendanceDate >= thirtyDaysAgo)
                .ToList();

            if (attendanceRecords.Any())
            {
                int presentCount = attendanceRecords.Count(a => a.Status == "Present");
                averageAttendance = (double)presentCount / attendanceRecords.Count * 100;
            }

            // ================= SEND TO VIEW =================
            ViewBag.TotalStudents = totalStudents;
            ViewBag.PresentToday = presentToday;
            ViewBag.PendingLeaveRequests = pendingLeaves;
            ViewBag.MaterialsUploaded = materialsUploaded;
            ViewBag.ActiveStudents = activeStudents;
            ViewBag.AverageAttendance = Math.Round(averageAttendance, 1);

            // ================= RECENT ACTIVITIES (LAST 7 DAYS ONLY - FROM sevenDaysAgo TO NOW) =================

            // 1. Attendance Marked by THIS STAFF in Last 7 Days
            var attendanceActivities = db.Attendances
                .Include("Parent")
                .Where(a => a.StaffId == staffId &&
                           a.Parent.ClassName == classAssigned &&
                           a.CreatedAt >= sevenDaysAgo &&
                           a.CreatedAt <= DateTime.Now)
                .GroupBy(a => a.AttendanceDate)
                .Select(g => new { Date = g.Key, Count = g.Count(), CreatedAt = g.Max(x => x.CreatedAt) })
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var attendance in attendanceActivities)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = attendance.CreatedAt,
                    Message = $"Attendance marked for {attendance.Date.ToString("MMM dd, yyyy")} ({attendance.Count} students)",
                    Icon = "bi-calendar-check",
                    Type = "success"
                });
            }

            // 2. Materials Uploaded by THIS STAFF in Last 7 Days
            var materialActivities = db.Materials
                .Where(m => m.StaffId == staffId &&
                           m.ClassName == classAssigned &&
                           m.IsActive &&
                           m.CreatedAt >= sevenDaysAgo &&
                           m.CreatedAt <= DateTime.Now)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var material in materialActivities)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = material.CreatedAt,
                    Message = $"Material uploaded: {material.Title}",
                    Icon = "bi-cloud-upload",
                    Type = "info"
                });
            }

            // 3. Timetable Added by THIS STAFF in Last 7 Days
            var timetableActivities = db.TimeTables
                .Where(t => t.CreatedByStaffId == staffId &&
                           t.ClassName == classAssigned &&
                           t.CreatedDate >= sevenDaysAgo &&
                           t.CreatedDate <= DateTime.Now)
                .OrderByDescending(t => t.CreatedDate)
                .Take(5)
                .ToList();

            foreach (var tt in timetableActivities)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = tt.CreatedDate,
                    Message = $"Timetable added: {tt.SubjectName} - {tt.WeekDay}",
                    Icon = "bi-table",
                    Type = "info"
                });
            }

            // 4. Exam Schedule Posted in Last 7 Days
            var examActivities = db.ExamSchedules
                .Where(e => e.ClassName == classAssigned &&
                           e.CreatedAt >= sevenDaysAgo &&
                           e.CreatedAt <= DateTime.Now)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var exam in examActivities)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = exam.CreatedAt,
                    Message = $"Exam schedule posted: {exam.ExamType}",
                    Icon = "bi-calendar-event",
                    Type = "warning"
                });
            }

            // 5. Results Posted in Last 7 Days
            var resultActivities = db.Results
                .Include("Parent")
                .Where(r => r.Parent.ClassName == classAssigned &&
                           r.CreatedAt >= sevenDaysAgo &&
                           r.CreatedAt <= DateTime.Now)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var result in resultActivities)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = result.CreatedAt,
                    Message = $"Result posted: {result.ExamType} - {result.Parent.StudentFirstName}",
                    Icon = "bi-award",
                    Type = "primary"
                });
            }

            // 6. Leave Approved/Rejected in Last 7 Days
            var leaveActivities = db.LeaveReports
                .Include("Parent")
                .Where(l => l.Parent.ClassName == classAssigned &&
                           l.Status != "Pending" &&
                           l.CreatedAt >= sevenDaysAgo &&
                           l.CreatedAt <= DateTime.Now)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var leave in leaveActivities)
            {
                string leaveIcon = leave.Status == "Approved" ? "bi-check-circle" : "bi-x-circle";
                string leaveType = leave.Status == "Approved" ? "success" : "danger";

                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = leave.CreatedAt,
                    Message = $"Leave {leave.Status.ToLower()}: {leave.Parent.StudentFirstName}",
                    Icon = leaveIcon,
                    Type = leaveType
                });
            }

            // Sort by date and take top 15
            ViewBag.RecentActivities = recentActivities
                .OrderByDescending(a => a.Date)
                .Take(15)
                .ToList();

            // ================= IMPORTANT REMINDERS (LAST 7 DAYS ONLY - FROM sevenDaysAgo TO NOW) =================

            // 1. Pending Leave Requests (Created in Last 7 Days)
            var pendingLeaveReminders = db.LeaveReports
                .Include("Parent")
                .Where(l => l.Parent.ClassName == classAssigned &&
                           l.Status == "Pending" &&
                           l.CreatedAt >= sevenDaysAgo &&
                           l.CreatedAt <= DateTime.Now)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var leave in pendingLeaveReminders)
            {
                importantReminders.Add(new RecentActivityViewModel
                {
                    Date = leave.CreatedAt,
                    Type = "info",
                    Icon = "bi-clock-fill",
                    Message = $"Pending leave: {leave.Parent.StudentFirstName} - {leave.Reason}"
                });
            }

            // 2. Unanswered FAQs (Asked in Last 7 Days)
            var unansweredFaqs = db.FAQs
                .Where(f => string.IsNullOrEmpty(f.Answer) &&
                           f.AskedOn >= sevenDaysAgo &&
                           f.AskedOn <= DateTime.Now)
                .OrderByDescending(f => f.AskedOn)
                .Take(5)
                .ToList();

            foreach (var faq in unansweredFaqs)
            {
                string question = faq.Question.Length > 60 ? faq.Question.Substring(0, 60) + "..." : faq.Question;
                importantReminders.Add(new RecentActivityViewModel
                {
                    Date = faq.AskedOn,
                    Type = "secondary",
                    Icon = "bi-question-circle-fill",
                    Message = $"{faq.Name} asked: {question}"
                });
            }

            // 3. Admin Notifications for Staff (Posted in Last 7 Days)
            var staffNotifications = db.Notifications
                .Where(n => n.Role == "Staff" &&
                           n.CreatedDate >= sevenDaysAgo &&
                           n.CreatedDate <= DateTime.Now)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToList();

            foreach (var notification in staffNotifications)
            {
                importantReminders.Add(new RecentActivityViewModel
                {
                    Date = notification.CreatedDate,
                    Type = "info",
                    Icon = "bi-bell-fill",
                    Message = notification.Message
                });
            }

            // 4. Low Attendance Alert (Students with <75% attendance in last 7 days)
            var lowAttendanceStudents = db.Attendances
                .Include("Parent")
                .Where(a => a.Parent.ClassName == classAssigned &&
                           a.AttendanceDate >= sevenDaysAgo &&
                           a.AttendanceDate <= today)
                .GroupBy(a => new { a.ParentId, a.Parent.StudentFirstName })
                .Select(g => new
                {
                    StudentName = g.Key.StudentFirstName,
                    TotalDays = g.Count(),
                    PresentDays = g.Count(x => x.Status == "Present"),
                    LastDate = g.Max(x => x.AttendanceDate)
                })
                .ToList()
                .Where(x => x.TotalDays > 0 && ((double)x.PresentDays / x.TotalDays * 100) < 75)
                .OrderBy(x => x.PresentDays)
                .Take(3)
                .ToList();

            foreach (var student in lowAttendanceStudents)
            {
                double percentage = Math.Round((double)student.PresentDays / student.TotalDays * 100, 0);
                importantReminders.Add(new RecentActivityViewModel
                {
                    Date = student.LastDate,
                    Type = "warning",
                    Icon = "bi-exclamation-triangle-fill",
                    Message = $"Low attendance alert: {student.StudentName} ({percentage}% in last 7 days)"
                });
            }

            // Sort by date and take top 12
            ViewBag.ImportantReminders = importantReminders
                .OrderByDescending(r => r.Date)
                .Take(12)
                .ToList();

            return View();
        }

        // Helper method to format time ago
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
            {
                if (timeSpan.TotalHours < 2)
                    return "1 hour ago";
                return $"{(int)timeSpan.TotalHours} hours ago";
            }
            if (timeSpan.TotalDays < 2)
                return "Yesterday at " + dateTime.ToString("h:mm tt");
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";

            return dateTime.ToString("MMM dd, yyyy");
        }

        // ========================================================== PROFILE MANAGEMENT ===========================================================

        // GET: ManageProfile
        [HttpGet]
        public ActionResult ManageProfile()
        {

            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.Find(staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index");
            }

            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageProfile(Staff model, HttpPostedFileBase PhotoUpload)
        {
            // 🔥 REMOVE unused validations
            ModelState.Remove("Password");
            ModelState.Remove("Username");
            ModelState.Remove("ClassAssigned");

            if (!ModelState.IsValid)
                return View(model);

            var staff = db.Staffs.FirstOrDefault(s => s.Id == model.Id);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index");
            }

            bool isChanged = false;


            if (staff.FirstName != model.FirstName)
            {
                staff.FirstName = model.FirstName;
                isChanged = true;
            }

            if (staff.LastName != model.LastName)
            {
                staff.LastName = model.LastName;
                isChanged = true;
            }

            if (staff.Email != model.Email)
            {
                staff.Email = model.Email;
                isChanged = true;
            }

            if (staff.ContactNo != model.ContactNo)
            {
                staff.ContactNo = model.ContactNo;
                isChanged = true;
            }

            if (staff.Gender != model.Gender)
            {
                staff.Gender = model.Gender;
                isChanged = true;
            }

            if (staff.Address != model.Address)
            {
                staff.Address = model.Address;
                isChanged = true;
            }

            if (staff.Designation != model.Designation)
            {
                staff.Designation = model.Designation;
                isChanged = true;
            }

            if (staff.Education != model.Education)
            {
                staff.Education = model.Education;
                isChanged = true;
            }


            // 🔥 VERY IMPORTANT
            staff.Username = staff.Username;
            staff.ClassAssigned = staff.ClassAssigned;
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();
            db.Configuration.ValidateOnSaveEnabled = true;

            // 🔥 ADD THIS (IMPORTANT)
            staff.ClassAssigned = staff.ClassAssigned;
            // OR if admin edits:
            // staff.ClassAssigned = model.ClassAssigned;

            // ================= PHOTO UPLOAD =================
            if (PhotoUpload != null && PhotoUpload.ContentLength > 0)
            {
                // ✅ Size validation (5 MB)
                if (PhotoUpload.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("PhotoUpload",
                        "Photo size must be less than 5 MB.");
                    return View(model);
                }

                // ✅ Extension validation
                string ext = Path.GetExtension(PhotoUpload.FileName).ToLower();
                string[] allowedExt = { ".jpg", ".jpeg", ".png" };

                if (!allowedExt.Contains(ext))
                {
                    ModelState.AddModelError("PhotoUpload",
                        "Only JPEG and PNG image formats are allowed.");
                    return View(model);
                }

                // ✅ Ensure folder exists
                string folderPath = Server.MapPath("~/Content/uploads/staff/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // ✅ Unique file name
                string fileName = Guid.NewGuid().ToString() + ext;
                string fullPath = Path.Combine(folderPath, fileName);

                // ✅ Save file
                PhotoUpload.SaveAs(fullPath);

                // ✅ Save relative path to DB
                staff.PhotoPath = "~/Content/uploads/staff/" + fileName;
                isChanged = true;
            }

            if (isChanged)
            {

                // 🔴 REQUIRED FIELDS ko re-assign (VERY IMPORTANT)
                staff.Username = staff.Username;
                staff.ClassAssigned = staff.ClassAssigned;

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;

                TempData["Success"] = "Profile updated successfully!";
            }

            return RedirectToAction("ManageProfile");
        }

        // ====================================================================== MANAGE PARENTS ======================================================================
        public ActionResult ManageParents(bool showInactive = false)
        {
            if (Session["ClassAssigned"] == null) return RedirectToAction("Login", "Account");
            string classAssigned = Session["ClassAssigned"].ToString();
            ViewBag.ShowInactive = showInactive;
            var parents = showInactive
                ? db.Parents.Where(p => p.ClassName == classAssigned && !p.IsActive).ToList()
                : db.Parents.Where(p => p.ClassName == classAssigned && p.IsActive).ToList();
            return View(parents);
        }

        // ====================================================================== 
        // GET: Staff/CreateParent
        // ======================================================================
        public ActionResult CreateParent()
        {
            // Get the logged-in staff's username/email from session or authentication
            string staffUsername = Session["Username"]?.ToString();
            // or use: User.Identity.Name if using ASP.NET Identity
            if (string.IsNullOrEmpty(staffUsername))
            {
                return RedirectToAction("Login", "Account");
            }
            // Get the staff details from database
            var staff = db.Staffs.FirstOrDefault(s => s.Username == staffUsername || s.Email == staffUsername);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff details not found.";
                return RedirectToAction("Dashboard", "Staff");
            }
            // Pass the staff's assigned class to the view
            ViewBag.StaffClassName = staff.ClassAssigned;
            return View();
        }

        // ======================================================================
        // POST: Staff/CreateParent
        // ======================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateParent(Parent parent)
        {
            // ✅ Updated password validation - 5-8 characters, letters + numbers allowed
            if (string.IsNullOrEmpty(parent.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
            }
            else if (parent.Password.Length < 5 || parent.Password.Length > 8)
            {
                ModelState.AddModelError("Password", "Password must be between 5 to 8 characters");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(parent.Password, @"^[A-Za-z0-9]+$"))
            {
                ModelState.AddModelError("Password", "Password must contain only letters and numbers");
            }

            if (string.IsNullOrEmpty(parent.ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Confirm Password is required");
            }

            // Check if passwords match
            if (!string.IsNullOrEmpty(parent.Password) && !string.IsNullOrEmpty(parent.ConfirmPassword)
                && parent.Password != parent.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password do not match.");
            }

            if (!ModelState.IsValid)
            {
                string staffUsername = Session["Username"]?.ToString();
                var staff = db.Staffs.FirstOrDefault(s => s.Username == staffUsername || s.Email == staffUsername);
                ViewBag.StaffClassName = staff?.ClassAssigned;
                return View(parent);
            }

            string currentStaffUsername = Session["Username"]?.ToString();
            var currentStaff = db.Staffs.FirstOrDefault(s => s.Username == currentStaffUsername || s.Email == currentStaffUsername);
            if (currentStaff != null)
            {
                parent.ClassName = currentStaff.ClassAssigned;
            }

            // ================= DUPLICATE CHECK =================
            if (db.Parents.Any(p => p.Username == parent.Username))
            {
                ModelState.AddModelError("Username", "This username already exists.");
                ViewBag.StaffClassName = currentStaff?.ClassAssigned;
                return View(parent);
            }
            if (db.Parents.Any(p => p.Email == parent.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                ViewBag.StaffClassName = currentStaff?.ClassAssigned;
                return View(parent);
            }

            // ================= SAVE =================
            db.Parents.Add(parent);
            db.SaveChanges();

            // ✅ SUCCESS MESSAGE - redirect back to CreateParent page
            TempData["Success"] = "Parent added successfully!";
            return RedirectToAction("CreateParent", "Staff");
        }

        // ========================= EDIT PARENT - GET ========================== 
        public ActionResult EditParent(int id)
        {
            var parent = db.Parents.Find(id);
            if (parent == null)
                return HttpNotFound();
            return View(parent);
        }

        // ========================= EDIT PARENT - POST ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditParent(Parent model)
        {
            // ✅ Clear password validation errors from model state
            if (ModelState.ContainsKey("Password"))
                ModelState["Password"].Errors.Clear();
            if (ModelState.ContainsKey("ConfirmPassword"))
                ModelState["ConfirmPassword"].Errors.Clear();

            // ✅ THEN - Check password logic
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                // No password change - keep old password
                var existingParent = db.Parents.Find(model.ParentId);
                if (existingParent != null)
                {
                    model.Password = existingParent.Password;
                }
            }
            else
            {
                // Password provided - validate it
                if (model.Password.Length < 5 || model.Password.Length > 8)
                {
                    ModelState.AddModelError("Password", "Password must be between 5 to 8 characters");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Password, @"^[A-Za-z0-9]+$"))
                {
                    ModelState.AddModelError("Password", "Password must contain only letters and numbers");
                }

                // Check confirm password
                if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword", "Confirm password is required when changing password.");
                }
                else if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password do not match.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the parent in database
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == model.ParentId);
            if (parent == null)
                return RedirectToAction("ManageParents");

            // Update Student Details
            parent.StudentFirstName = string.IsNullOrWhiteSpace(model.StudentFirstName)
                ? parent.StudentFirstName
                : model.StudentFirstName;
            parent.StudentMiddleName = model.StudentMiddleName;
            parent.StudentLastName = string.IsNullOrWhiteSpace(model.StudentLastName)
                ? parent.StudentLastName
                : model.StudentLastName;
            parent.StudentGender = model.StudentGender;

            // Update Parent Details
            parent.ParentFirstName = model.ParentFirstName;
            parent.ParentMiddleName = model.ParentMiddleName;
            parent.ParentLastName = model.ParentLastName;
            parent.Gender = model.Gender;
            parent.ContactNumber = model.ContactNumber;
            parent.Email = model.Email;
            parent.Address = model.Address;

            // ✅ FIXED: Now Username CAN be updated
            parent.Username = model.Username;

            // Keep ClassName as it is (read-only)
            parent.ClassName = parent.ClassName;

            // ✅ Handle Password Update (optional)
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                parent.Password = model.Password;
            }

            // Disable validation temporarily and save
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();
            db.Configuration.ValidateOnSaveEnabled = true;

            // Set success message and redirect to same page
            TempData["Success"] = "Parent updated successfully!";
            return RedirectToAction("EditParent", new { id = model.ParentId });
        }

        public ActionResult DeleteParent(int id)
        {
            var parent = db.Parents.Find(id);
            if (parent == null) return HttpNotFound();
            return View(parent);
        }


        [HttpPost, ActionName("DeleteParent")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int ParentId)
        {
            var parent = db.Parents.Find(ParentId);
            if (parent == null)
                return HttpNotFound();

            // 🔥 Delete dependent tables FIRST
            db.Attendances.RemoveRange(db.Attendances.Where(a => a.ParentId == ParentId));
            db.LeaveReports.RemoveRange(db.LeaveReports.Where(l => l.ParentId == ParentId));
            db.FeePayments.RemoveRange(db.FeePayments.Where(f => f.ParentId == ParentId));
            db.Results.RemoveRange(db.Results.Where(r => r.ParentId == ParentId));

            // ✅ THIS WAS MISSING
            db.Feedbacks.RemoveRange(db.Feedbacks.Where(f => f.ParentId == ParentId));

            // 🔥 Finally delete parent
            db.Parents.Remove(parent);

            db.SaveChanges();

            TempData["Message"] = "Parent deleted successfully!";
            return RedirectToAction("ManageParents");
        }



        public ActionResult DetailsParent(int id)
        {
            var parent = db.Parents.Find(id);
            if (parent == null) return HttpNotFound();
            return View(parent);
        }

        [HttpPost]
        public ActionResult DeactivateParent(int id)
        {
            var parent = db.Parents.Find(id);
            if (parent != null)
            {
                parent.IsActive = false;

                // 🔥 IMPORTANT LINE
                db.Configuration.ValidateOnSaveEnabled = false;

                db.SaveChanges();

                // 🔄 turn validation back ON (best practice)
                db.Configuration.ValidateOnSaveEnabled = true;

                TempData["ParentMessage"] = "Parent deactivated successfully!";
            }
            return RedirectToAction("ManageParents");
        }


        [HttpPost]
        public ActionResult ActivateParent(int id)
        {
            var parent = db.Parents.Find(id);
            if (parent != null)
            {
                parent.IsActive = true;

                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();
                db.Configuration.ValidateOnSaveEnabled = true;

                TempData["ParentMessage"] = "Parent activated successfully!";
            }
            return RedirectToAction("ManageParents");
        }

        // ================================================================================ ATTENDANCE ====================================================================
        public ActionResult ManageStudentsAttendance()
        {
            if (Session["ClassAssigned"] == null || Session["StaffId"] == null)
                return RedirectToAction("Login", "Account");

            string classAssigned = Session["ClassAssigned"].ToString();
            var students = db.Parents
                             .Where(p => p.ClassName == classAssigned && p.IsActive)
                             .ToList();

            ViewBag.ClassName = classAssigned;

            // ✅ ADD THIS
            ViewBag.AttendanceAlreadyMarked = db.Attendances
                .Any(a => a.Parent.ClassName == classAssigned &&
                          a.AttendanceDate == DateTime.Today);

            return View(students);
        }

        [HttpPost]
        public ActionResult SaveAttendance(int[] parentIds, string[] statuses)
        {
            if (parentIds == null || statuses == null)
            {
                TempData["Message"] = "Invalid attendance data!";
                return RedirectToAction("ManageStudentsAttendance");
            }

            if (statuses.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                TempData["Message"] = "Please select attendance status for all students!";
                return RedirectToAction("ManageStudentsAttendance");
            }

            // ✅ Declare staffId and classAssigned ONCE here
            int staffId = Convert.ToInt32(Session["StaffId"]);
            string classAssigned = Session["ClassAssigned"]?.ToString();

            // ✅ Already marked check
            bool alreadyMarkedToday = db.Attendances
                .Any(a => a.Parent.ClassName == classAssigned &&
                          a.AttendanceDate == DateTime.Today);

            if (alreadyMarkedToday)
            {
                TempData["ErrorMessage"] = "Attendance for today has already been marked!";
                return RedirectToAction("ManageStudentsAttendance");
            }

            // ✅ No duplicate int staffId here — already declared above
            if (parentIds != null && statuses != null)
            {
                int len = Math.Min(parentIds.Length, statuses.Length);
                for (int i = 0; i < len; i++)
                {
                    int parentId = parentIds[i];
                    string status = statuses[i];
                    var parent = db.Parents.Find(parentId);
                    if (parent == null || !parent.IsActive) continue;

                    bool alreadyMarked = db.Attendances.Any(a =>
                        a.ParentId == parentId && a.AttendanceDate == DateTime.Today);
                    if (!alreadyMarked)
                    {
                        db.Attendances.Add(new Attendance
                        {
                            ParentId = parentId,
                            StaffId = staffId,
                            AttendanceDate = DateTime.Today,
                            Status = status,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                db.SaveChanges();
            }

            TempData["Message"] = "Attendance saved successfully!";
            return RedirectToAction("ManageStudentsAttendance");
        }





        // =========================================================
        // ADD THIS ACTION inside StaffController class
        // =========================================================

        public ActionResult AttendanceReport(AttendanceReportViewModel filter)
        {
            if (Session["StaffId"] == null)
                return RedirectToAction("Login", "Account");

            int staffId = Convert.ToInt32(Session["StaffId"]);
            string classAssigned = Session["ClassAssigned"]?.ToString();

            var vm = new AttendanceReportViewModel
            {
                Day = filter.Day,
                Month = filter.Month,
                Year = filter.Year,
                Status = filter.Status,
                StudentName = filter.StudentName,
            };

            // Base query — only this staff's class
            var query =
                from a in db.Attendances
                join p in db.Parents on a.ParentId equals p.ParentId
                join s in db.Staffs on a.StaffId equals s.Id
                where a.StaffId == staffId
                select new
                {
                    StudentName = p.StudentFirstName + " " + p.StudentLastName,
                    p.ClassName,
                    a.Status,
                    a.AttendanceDate,
                    StaffName = s.FirstName + " " + s.LastName,
                    p.Email  // ← ADD

                };

            // Apply filters



            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(x => x.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.StudentName))
                query = query.Where(x => x.StudentName.Contains(filter.StudentName)
                                      || x.Email.Contains(filter.StudentName));

            if (filter.Year.HasValue)
                query = query.Where(x => x.AttendanceDate.Year == filter.Year.Value);

            if (filter.Month.HasValue)
                query = query.Where(x => x.AttendanceDate.Month == filter.Month.Value);

            if (filter.Day.HasValue)
                query = query.Where(x => x.AttendanceDate.Day == filter.Day.Value);

            var rows = query
                .OrderByDescending(x => x.AttendanceDate)
                .ThenBy(x => x.StudentName)
                .ToList()
                .Select(x => new AttendanceReportRow
                {
                    StudentName = x.StudentName,
                    ClassName = x.ClassName,
                    Status = x.Status,
                    AttendanceDate = x.AttendanceDate,
                    StaffName = x.StaffName,
                    Email = x.Email,  // ← ADD

                }).ToList();

            vm.Rows = rows;
            vm.TotalRecords = rows.Count;
            vm.PresentCount = rows.Count(r => r.Status == "Present");
            vm.AbsentCount = rows.Count(r => r.Status == "Absent");
            vm.LateCount = rows.Count(r => r.Status == "Late");

            return View(vm);
        }
      
        // ======================================================================================== LEAVE REPORTS ================================================================
        public ActionResult ViewLeaveReport()
        {
            if (!IsStaff()) return RedirectToLogin();
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.Find(staffId);
            if (staff == null) return RedirectToLogin();

            string classAssigned = staff.ClassAssigned;
            var reports = db.LeaveReports
                            .Include("Parent")
                            .Where(r => r.Parent != null &&
                                        r.Parent.ClassName == classAssigned &&
                                        r.Parent.IsActive)
                            .OrderByDescending(r => r.CreatedAt)
                            .ToList();

            return View(reports);
        }

        public ActionResult ApproveLeave(int id)
        {
            if (!IsStaff()) return RedirectToLogin();
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.Find(staffId);
            if (staff == null) return RedirectToLogin();

            var leave = db.LeaveReports.Include("Parent").FirstOrDefault(l => l.Id == id);
            if (leave != null && leave.Parent != null &&
                leave.Parent.ClassName == staff.ClassAssigned &&
                leave.Parent.IsActive)
            {
                leave.Status = "Approved";
                db.SaveChanges();
            }
            return RedirectToAction("ViewLeaveReport");
        }

        public ActionResult RejectLeave(int id)
        {
            if (!IsStaff()) return RedirectToLogin();
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.Find(staffId);
            if (staff == null) return RedirectToLogin();

            var leave = db.LeaveReports.Include("Parent").FirstOrDefault(l => l.Id == id);
            if (leave != null && leave.Parent != null &&
                leave.Parent.ClassName == staff.ClassAssigned &&
                leave.Parent.IsActive)
            {
                leave.Status = "Rejected";
                db.SaveChanges();
            }
            return RedirectToAction("ViewLeaveReport");
        }


        //=================================================================================== UPLOAD MATERIAL ===================================================================


        // GET: UploadMaterial
        public ActionResult UploadMaterial()
        {
            if (!IsStaff())
                return RedirectToLogin();

            // Staff ki assigned class session se nikaalo
            string classAssigned = Session["ClassAssigned"]?.ToString();
            if (string.IsNullOrEmpty(classAssigned))
                return RedirectToAction("Login", "Account");

            // Sirf us class ka material hi dikhna chahiye
            var materials = db.Materials
                .Include("Staff")
                .Where(m => m.IsActive && m.ClassName == classAssigned)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            ViewBag.ClassAssigned = classAssigned;

            return View(materials);
        }

        // =================================================================================== UPLOAD MATERIAL ===================================================================

        // POST: UploadMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterial(HttpPostedFileBase file, string Title, string Description)
        {
            if (!IsStaff())
                return RedirectToLogin();

            string classAssigned = Session["ClassAssigned"]?.ToString();
            int staffId = Convert.ToInt32(Session["StaffId"]);

            // ✅ 1. VALIDATE TITLE
            if (string.IsNullOrWhiteSpace(Title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction("UploadMaterial");
            }

            if (Title.Length > 20)
            {
                TempData["Error"] = "Title cannot exceed 20 characters.";
                return RedirectToAction("UploadMaterial");
            }

            // ✅ 2. VALIDATE DESCRIPTION
            if (string.IsNullOrWhiteSpace(Description))
            {
                TempData["Error"] = "Description is required.";
                return RedirectToAction("UploadMaterial");
            }

            if (Description.Length > 50)
            {
                TempData["Error"] = "Description cannot exceed 50 characters.";
                return RedirectToAction("UploadMaterial");
            }

            // ✅ 3. VALIDATE FILE - FILE MUST BE PRESENT
            if (file == null || file.ContentLength == 0)
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction("UploadMaterial");
            }

            // ✅ 4. VALIDATE FILE EXTENSION
            var allowedExt = new[]
            {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx",
        ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".mp4"
    };

            var ext = Path.GetExtension(file.FileName)?.ToLower();

            if (string.IsNullOrEmpty(ext) || !allowedExt.Contains(ext))
            {
                TempData["Error"] = "Invalid file type! Only PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX, JPG, PNG, and MP4 files are allowed.";
                return RedirectToAction("UploadMaterial");
            }

            // ✅ 5. VALIDATE FILE SIZE (Maximum 20 MB)
            const int maxFileSizeInBytes = 20 * 1024 * 1024; // 20 MB
            if (file.ContentLength > maxFileSizeInBytes)
            {
                double fileSizeMB = file.ContentLength / (1024.0 * 1024.0);
                TempData["Error"] = $"File size is {fileSizeMB:F2} MB. Maximum allowed size is 20 MB. Please select a smaller file.";
                return RedirectToAction("UploadMaterial");
            }

            // ✅ 6. VALIDATE FILE NAME LENGTH
            if (file.FileName.Length > 150)
            {
                TempData["Error"] = "File name is too long. Please rename the file to less than 150 characters.";
                return RedirectToAction("UploadMaterial");
            }

            try
            {
                // ✅ 7. CREATE UPLOAD FOLDER IF NOT EXISTS
                var uploadsFolder = Server.MapPath("~/Uploads/Materials/");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // ✅ 8. GENERATE UNIQUE FILE NAME (Prevents conflicts)
                var uniqueName = Guid.NewGuid().ToString("N") + ext;
                var fullPath = Path.Combine(uploadsFolder, uniqueName);

                // ✅ 9. SAVE FILE
                file.SaveAs(fullPath);

                // ✅ 10. CREATE DATABASE RECORD
                var model = new Material
                {
                    Title = Title,
                    Description = Description,
                    FileName = Path.GetFileName(file.FileName), // Keep original name for display
                    FilePath = "/Uploads/Materials/" + uniqueName, // Save unique name in path
                    ContentType = file.ContentType,
                    FileSize = file.ContentLength,
                    ClassName = classAssigned,
                    StaffId = staffId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                db.Materials.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Material uploaded successfully!";
                return RedirectToAction("UploadMaterial");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error uploading file: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"Upload Material Error: {ex.ToString()}");
                return RedirectToAction("UploadMaterial");
            }
        }
        // GET: DownloadMaterial
        // GET: Staff/DownloadMaterial/5
        public ActionResult DownloadMaterial(int id)
        {
            if (!IsStaff())
                return RedirectToLogin();

            var material = db.Materials.FirstOrDefault(m => m.Id == id && m.IsActive);
            if (material == null)
                return HttpNotFound();

            var path = Server.MapPath(material.FilePath);
            if (!System.IO.File.Exists(path))
                return HttpNotFound();

            Response.Headers.Remove("Content-Disposition");
            Response.AddHeader("Content-Disposition", "inline");

            return File(path, material.ContentType);
        }

        

        

        // POST: DeleteMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMaterial(int id)
        {
            if (!IsStaff())
                return RedirectToLogin();

            var material = db.Materials.FirstOrDefault(m => m.Id == id);

            if (material == null)
            {
                TempData["Error"] = "Material not found.";
                return RedirectToAction("UploadMaterial");
            }

            if (!string.IsNullOrEmpty(material.FilePath))
            {
                string fullPath = Server.MapPath(material.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            db.Materials.Remove(material);
            db.SaveChanges();

            TempData["Success"] = "Material deleted successfully!";
            return RedirectToAction("UploadMaterial");
        }


        //================================================= Post Result [GET] ===================================================

        [HttpGet]
        public ActionResult PostResult()
        {
            if (!IsStaff()) return RedirectToLogin();

            string username = Session["Username"].ToString();
            var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index");
            }

            string staffClass = staff.ClassAssigned;
            ViewBag.StaffClass = staffClass;

            var students = db.Parents
                .Where(p => p.ClassName == staffClass && p.IsActive)
                .Select(p => new
                {
                    p.ParentId,
                    StudentFullName = p.StudentFirstName + " " +
                                      (string.IsNullOrEmpty(p.StudentMiddleName) ? "" : p.StudentMiddleName + " ") +
                                      p.StudentLastName
                })
                .OrderBy(p => p.StudentFullName)
                .ToList();

            ViewBag.Parents = new SelectList(students, "ParentId", "StudentFullName");

            ViewBag.UploadedResults = db.Results
                .Include(r => r.Parent)
                .Where(r => r.Parent.ClassName == staffClass)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View();
        }

        //================================================= Post Result [POST] ===================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostResult(Result result, HttpPostedFileBase ResultFile)
        {
            if (!IsStaff()) return RedirectToLogin();

            string username = Session["Username"].ToString();
            var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index");
            }

            string staffClass = staff.ClassAssigned;

            var selectedParent = db.Parents.Find(result.ParentId);
            if (selectedParent == null || !selectedParent.IsActive || selectedParent.ClassName != staffClass)
            {
                ModelState.AddModelError("", "Invalid student selected.");
            }

            if (ResultFile == null || ResultFile.ContentLength == 0)
            {
                ModelState.AddModelError("", "Please upload a result file.");
            }
            else
            {
                string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                string fileExtension = Path.GetExtension(ResultFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Only PDF and image files (.pdf, .jpg, .jpeg, .png) are allowed.");
                }

                if (ResultFile.ContentLength > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File size must not exceed 10MB.");
                }

                if (ResultFile.FileName.Length > 100)
                {
                    ModelState.AddModelError("", "File name is too long. Please rename the file to less than 100 characters.");
                }
            }

            if (string.IsNullOrEmpty(result.ExamType))
            {
                ModelState.AddModelError("", "Please select an exam type.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string uploadPath = Server.MapPath("~/Uploads/Results");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string fileExtension = Path.GetExtension(ResultFile.FileName);
                    string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
                    string fileName = $"Result_{uniqueId}{fileExtension}";
                    string fullPath = Path.Combine(uploadPath, fileName);

                    ResultFile.SaveAs(fullPath);
                    result.ResultFile = fileName;
                    result.CreatedAt = DateTime.Now;

                    db.Results.Add(result);
                    db.SaveChanges();

                    TempData["Success"] = "Result successfully uploaded!";
                    return RedirectToAction("PostResult");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error uploading file: " + ex.Message);
                }
            }

            ViewBag.StaffClass = staffClass;

            var students = db.Parents
                .Where(p => p.ClassName == staffClass && p.IsActive)
                .Select(p => new
                {
                    p.ParentId,
                    StudentFullName = p.StudentFirstName + " " +
                                      (string.IsNullOrEmpty(p.StudentMiddleName) ? "" : p.StudentMiddleName + " ") +
                                      p.StudentLastName
                })
                .OrderBy(p => p.StudentFullName)
                .ToList();

            ViewBag.Parents = new SelectList(students, "ParentId", "StudentFullName", result.ParentId);

            ViewBag.UploadedResults = db.Results
                .Include(r => r.Parent)
                .Where(r => r.Parent.ClassName == staffClass)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(result);
        }

        //================================================= View Result File ===================================================

        public ActionResult ViewResultFile(int id)
        {
            if (!IsStaff()) return RedirectToLogin();

            string username = Session["Username"].ToString();
            var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("PostResult");
            }

            string staffClass = staff.ClassAssigned;

            var result = db.Results.Include(r => r.Parent).FirstOrDefault(r => r.Id == id);

            if (result == null || result.Parent.ClassName != staffClass || string.IsNullOrEmpty(result.ResultFile))
            {
                TempData["Error"] = "Result file not found.";
                return RedirectToAction("PostResult");
            }

            string filePath = Path.Combine(Server.MapPath("~/Uploads/Results"), result.ResultFile);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File does not exist on server.";
                return RedirectToAction("PostResult");
            }

            string contentType = GetContentType(result.ResultFile); // ← Uses helper method

            return File(filePath, contentType);
        }

        //================================================= Delete Result ===================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteResult(int id)
        {
            if (!IsStaff()) return RedirectToLogin();

            string username = Session["Username"].ToString();
            var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index");
            }

            string staffClass = staff.ClassAssigned;

            var result = db.Results.Include(r => r.Parent).FirstOrDefault(r => r.Id == id);

            if (result == null)
            {
                TempData["Error"] = "Result not found.";
                return RedirectToAction("PostResult");
            }

            if (result.Parent.ClassName != staffClass)
            {
                TempData["Error"] = "You don't have permission to delete this result.";
                return RedirectToAction("PostResult");
            }

            try
            {
                if (!string.IsNullOrEmpty(result.ResultFile))
                {
                    string filePath = Path.Combine(Server.MapPath("~/Uploads/Results"), result.ResultFile);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                db.Results.Remove(result);
                db.SaveChanges();

                TempData["Success"] = "Result deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting result: " + ex.Message;
            }

            return RedirectToAction("PostResult");
        }

        //================================================= Get Content Type (Helper) ===================================================

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }

        // End of StaffController class


            // ===============================================================================
            // EXAM SCHEDULE METHODS
            // ===============================================================================

            //================================================= Post ExamSchedule [GET] ===================================================

            public ActionResult PostExamSchedule()
            {
                if (!IsStaff()) return RedirectToLogin();

                string username = Session["Username"].ToString();
                var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

                if (staff == null)
                {
                    TempData["Error"] = "Staff not found.";
                    return RedirectToAction("Index");
                }

                ViewBag.StaffClass = staff.ClassAssigned;
                ViewBag.ExamTypes = new SelectList(new[] { "Mid-Term", "Final" });
                ViewBag.ExamSchedules = db.ExamSchedules
                    .Where(e => e.ClassName == staff.ClassAssigned)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList();

                return View();
            }

            //================================================= Post ExamSchedule [POST] ===================================================

            [HttpPost]
            [ValidateAntiForgeryToken]
            public ActionResult PostExamSchedule(string ExamType, HttpPostedFileBase ScheduleFile)
            {
                if (!IsStaff()) return RedirectToLogin();

                string username = Session["Username"].ToString();
                var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

                if (staff == null)
                {
                    TempData["Error"] = "Staff not found.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(ExamType))
                {
                    ModelState.AddModelError("ExamType", "Please select exam type.");
                }

                if (ScheduleFile == null || ScheduleFile.ContentLength == 0)
                {
                    ModelState.AddModelError("ScheduleFile", "Please upload an exam schedule file.");
                }
                else
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(ScheduleFile.FileName)?.ToLower();

                    if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ScheduleFile", "Only PDF, DOC, DOCX, JPG, and PNG files are allowed.");
                    }

                    const int maxFileSize = 10 * 1024 * 1024;
                    if (ScheduleFile.ContentLength > maxFileSize)
                    {
                        ModelState.AddModelError("ScheduleFile", "File size cannot exceed 10 MB.");
                    }

                    if (ScheduleFile.FileName.Length > 100)
                    {
                        ModelState.AddModelError("ScheduleFile", "File name is too long. Please rename the file to less than 100 characters.");
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        string uploadsFolder = Server.MapPath("~/Uploads/ExamSchedules/");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string fileExtension = Path.GetExtension(ScheduleFile.FileName);
                        string uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        ScheduleFile.SaveAs(filePath);

                        var examSchedule = new ExamSchedule
                        {
                            ClassName = staff.ClassAssigned,
                            ExamType = ExamType,
                            ScheduleFilePath = "/Uploads/ExamSchedules/" + uniqueFileName,
                            ScheduleFileName = Path.GetFileName(ScheduleFile.FileName),
                            CreatedBy = username,
                            CreatedAt = DateTime.Now
                        };

                        db.ExamSchedules.Add(examSchedule);
                        db.SaveChanges();

                        TempData["Success"] = "Exam schedule posted successfully!";
                        return RedirectToAction("PostExamSchedule");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error uploading file: " + ex.Message;
                    }
                }

                ViewBag.StaffClass = staff.ClassAssigned;
                ViewBag.ExamTypes = new SelectList(new[] { "Mid-Term", "Final" }, ExamType);
                ViewBag.ExamSchedules = db.ExamSchedules
                    .Where(e => e.ClassName == staff.ClassAssigned)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList();

                return View();
            }

            //================================================= View ExamSchedule File ===================================================

            public ActionResult ViewExamScheduleFile(int id)
            {
                if (!IsStaff()) return RedirectToLogin();

                string username = Session["Username"].ToString();
                var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

                if (staff == null)
                {
                    TempData["Error"] = "Staff not found.";
                    return RedirectToAction("PostExamSchedule");
                }

                var schedule = db.ExamSchedules.FirstOrDefault(e => e.Id == id && e.ClassName == staff.ClassAssigned);

                if (schedule == null || string.IsNullOrEmpty(schedule.ScheduleFilePath))
                {
                    TempData["Error"] = "Exam schedule file not found.";
                    return RedirectToAction("PostExamSchedule");
                }

                string filePath = Server.MapPath(schedule.ScheduleFilePath);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File does not exist on server.";
                    return RedirectToAction("PostExamSchedule");
                }

                string contentType = GetContentType(schedule.ScheduleFilePath); // ← Uses helper method

                return File(filePath, contentType);
            }

            //================================================= Delete ExamSchedule ===================================================

            [HttpPost]
            [ValidateAntiForgeryToken]
            public ActionResult DeleteExamSchedule(int id)
            {
                if (!IsStaff()) return RedirectToLogin();

                string username = Session["Username"].ToString();
                var staff = db.Staffs.FirstOrDefault(s => s.Username == username);

                if (staff == null)
                {
                    TempData["Error"] = "Staff not found.";
                    return RedirectToAction("Index");
                }

                var exam = db.ExamSchedules.Find(id);

                if (exam == null || exam.ClassName != staff.ClassAssigned)
                {
                    TempData["Error"] = "Exam schedule not found or unauthorized!";
                    return RedirectToAction("PostExamSchedule");
                }

                try
                {
                    if (!string.IsNullOrEmpty(exam.ScheduleFilePath))
                    {
                        string filePath = Server.MapPath(exam.ScheduleFilePath);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    db.ExamSchedules.Remove(exam);
                    db.SaveChanges();

                    TempData["Success"] = "Exam schedule deleted successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error deleting exam schedule: " + ex.Message;
                }

                return RedirectToAction("PostExamSchedule");
            }

            // ========================================= Manage TimeTable ===============================================

            // GET: ManageTimeTable
            public ActionResult ManageTimeTable()
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);

            // Get class assigned from Staff table
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);
            if (staff == null || string.IsNullOrEmpty(staff.ClassAssigned))
            {
                TempData["Error"] = "No class assigned to you.";
                return RedirectToAction("Index", "Staff");
            }

            ViewBag.ClassAssigned = staff.ClassAssigned;
            ViewBag.WeekDays = new SelectList(new[]
            {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    });

            // Fetch existing timetable entries for this class
            var timetableEntries = db.TimeTables
                .Where(t => t.ClassName == staff.ClassAssigned)
                .OrderBy(t => t.WeekDay)
                .ThenBy(t => t.StartTime)
                .ToList();

            ViewBag.TimeTableEntries = timetableEntries;

            return View();
        }

        // POST: ManageTimeTable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageTimeTable(TimeTable model)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);

            // Get class assigned from Staff table
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);
            if (staff == null || string.IsNullOrEmpty(staff.ClassAssigned))
            {
                TempData["Error"] = "No class assigned to you.";
                return RedirectToAction("Index", "Staff");
            }

            if (ModelState.IsValid)
            {
                model.CreatedByStaffId = staffId;
                model.ClassName = staff.ClassAssigned; // Set ClassName automatically

                if (model.EndTime > model.StartTime)
                {
                    model.DurationInMinutes = (int)(model.EndTime - model.StartTime).TotalMinutes;
                }
                else
                {
                    TempData["Error"] = "End time must be greater than start time.";

                    // Reload data for view
                    ViewBag.ClassAssigned = staff.ClassAssigned;
                    ViewBag.WeekDays = new SelectList(new[]
                    {
                "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            });

                    var timetableEntries = db.TimeTables
                        .Where(t => t.ClassName == staff.ClassAssigned)
                        .OrderBy(t => t.WeekDay)
                        .ThenBy(t => t.StartTime)
                        .ToList();
                    ViewBag.TimeTableEntries = timetableEntries;

                    return View(model);
                }

                db.TimeTables.Add(model);
                db.SaveChanges();

                TempData["Message"] = $"TimeTable entry added successfully for {staff.ClassAssigned}!";
                return RedirectToAction("ManageTimeTable");
            }

            // If ModelState is invalid, reload data
            ViewBag.ClassAssigned = staff.ClassAssigned;
            ViewBag.WeekDays = new SelectList(new[]
            {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    });

            var entries = db.TimeTables
                .Where(t => t.ClassName == staff.ClassAssigned)
                .OrderBy(t => t.WeekDay)
                .ThenBy(t => t.StartTime)
                .ToList();
            ViewBag.TimeTableEntries = entries;

            TempData["Error"] = "Please fill all fields correctly.";
            return View(model);
        }

        // GET: View TimeTable Details
        // GET: View TimeTable Details
        public ActionResult ViewTimeTable(int id)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index", "Staff");
            }

            var timeTable = db.TimeTables.Find(id);
            if (timeTable == null || timeTable.ClassName != staff.ClassAssigned)
            {
                TempData["Error"] = "TimeTable entry not found or you don't have permission to view it.";
                return RedirectToAction("ManageTimeTable");
            }

            ViewBag.ClassAssigned = staff.ClassAssigned;
            return View(timeTable);
        }

        // GET: Edit TimeTable Entry
        public ActionResult EditTimeTable(int id)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index", "Staff");
            }

            var timeTable = db.TimeTables.Find(id);
            if (timeTable == null || timeTable.ClassName != staff.ClassAssigned)
            {
                TempData["Error"] = "TimeTable entry not found or you don't have permission to edit it.";
                return RedirectToAction("ManageTimeTable");
            }

            ViewBag.ClassAssigned = staff.ClassAssigned;
            ViewBag.WeekDays = new SelectList(new[]
            {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    }, timeTable.WeekDay);

            return View(timeTable);
        }

        // POST: Edit TimeTable Entry
        // POST: Edit TimeTable Entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTimeTable(TimeTable model)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index", "Staff");
            }

            if (ModelState.IsValid)
            {
                var timeTable = db.TimeTables.Find(model.TimeTableId);
                if (timeTable == null || timeTable.ClassName != staff.ClassAssigned)
                {
                    TempData["Error"] = "TimeTable entry not found or you don't have permission to edit it.";
                    return RedirectToAction("ManageTimeTable");
                }

                if (model.EndTime > model.StartTime)
                {
                    timeTable.SubjectName = model.SubjectName;
                    timeTable.TeacherName = model.TeacherName;
                    timeTable.WeekDay = model.WeekDay;
                    timeTable.StartTime = model.StartTime;
                    timeTable.EndTime = model.EndTime;
                    timeTable.DurationInMinutes = (int)(model.EndTime - model.StartTime).TotalMinutes;

                    db.Entry(timeTable).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Message"] = "TimeTable entry updated successfully!";
                    return RedirectToAction("ManageTimeTable");
                }
                else
                {
                    TempData["Error"] = "End time must be greater than start time.";
                }
            }

            ViewBag.ClassAssigned = staff.ClassAssigned;
            ViewBag.WeekDays = new SelectList(new[]
            {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    }, model.WeekDay);

            return View(model);
        }

        // GET: Delete TimeTable Entry (Confirmation Page)
        public ActionResult DeleteTimeTable(int id)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index", "Staff");
            }

            var timeTable = db.TimeTables.Find(id);
            if (timeTable == null || timeTable.ClassName != staff.ClassAssigned)
            {
                TempData["Error"] = "TimeTable entry not found or you don't have permission to delete it.";
                return RedirectToAction("ManageTimeTable");
            }

            ViewBag.ClassAssigned = staff.ClassAssigned;
            return View(timeTable);
        }

        // POST: Delete TimeTable Entry (Confirmed)
        [HttpPost, ActionName("DeleteTimeTable")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTimeTableConfirmed(int id)
        {
            int staffId = Convert.ToInt32(Session["StaffId"]);
            var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found.";
                return RedirectToAction("Index", "Staff");
            }

            var timeTable = db.TimeTables.Find(id);
            if (timeTable == null || timeTable.ClassName != staff.ClassAssigned)
            {
                TempData["Error"] = "TimeTable entry not found or you don't have permission to delete it.";
                return RedirectToAction("ManageTimeTable");
            }

            db.TimeTables.Remove(timeTable);
            db.SaveChanges();

            TempData["Message"] = "TimeTable entry deleted successfully!";
            return RedirectToAction("ManageTimeTable");
        }
        // ============================================================================ FAQs & Notifications (unchanged) ========================================================
        public ActionResult ManageFAQs()
        {
            if (!IsStaff()) return RedirectToLogin();
            var list = db.FAQs.OrderByDescending(f => f.AskedOn).ToList();
            return View(list);
        }

        public ActionResult AnswerFAQ(int id)
        {
            if (!IsStaff()) return RedirectToLogin();
            var faq = db.FAQs.Find(id);
            if (faq == null) return HttpNotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AnswerFAQ(FAQ model)
        {
            if (!IsStaff())
                return RedirectToLogin();

            // 🔥 REMOVE VALIDATION FOR ALL READ-ONLY FIELDS
            ModelState.Remove("Name");
            ModelState.Remove("Email");
            ModelState.Remove("Question");
            ModelState.Remove("AskedBy");
            ModelState.Remove("AskedOn");
            ModelState.Remove("ParentId");
            ModelState.Remove("Parent");

            if (!ModelState.IsValid)
            {
                // Reload the FAQ to show the form again with validation errors
                var originalFaq = db.FAQs.Find(model.FAQId);
                if (originalFaq == null)
                    return HttpNotFound();

                // Preserve all the original data
                model.Question = originalFaq.Question;
                model.AskedBy = originalFaq.AskedBy;
                model.AskedOn = originalFaq.AskedOn;
                model.Name = originalFaq.Name;
                model.Email = originalFaq.Email;

                return View(model);
            }

            var faq = db.FAQs.Find(model.FAQId);
            if (faq == null)
                return HttpNotFound();

            // Only update the Answer field
            faq.Answer = model.Answer;
            faq.AnsweredOn = DateTime.Now;

            db.Entry(faq).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Answer submitted successfully!";
            return RedirectToAction("ManageFAQs");
        }

        // Temporary GET method for testing
        [HttpGet]
        public ActionResult DeleteFAQ(int id)
        {
            if (!IsStaff())
                return RedirectToLogin();

            try
            {
                var faq = db.FAQs.Find(id);

                if (faq == null)
                {
                    TempData["Error"] = "FAQ not found!";
                    return RedirectToAction("ManageFAQs");
                }

                db.FAQs.Remove(faq);
                db.SaveChanges();

                TempData["Success"] = "FAQ deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting FAQ: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Delete FAQ Error: " + ex.ToString());
            }

            return RedirectToAction("ManageFAQs");
        }
        public ActionResult ViewNotification()
        {
            if (!IsStaff()) return RedirectToLogin();
            var notifications = db.Notifications
                                  .Where(n => n.Role == "Staff")
                                  .OrderByDescending(n => n.CreatedDate)
                                  .ToList();
            return View(notifications);
        }
    }
}
