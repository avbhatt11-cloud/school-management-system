using Razorpay.Api;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls.WebParts;
using System.Xml;


namespace SchoolManagementSystem.Controllers
{
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ================= PRIVATE HELPERS =================
        private bool IsParent()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Parent";
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Account");
        }

        private int GetLoggedInParentId()
        {
            return Convert.ToInt32(Session["ParentId"]);
        }

        // ================================================================ DASHBOARD ========================================================================
        public ActionResult Index()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.Find(parentId);

            if (parent == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Username = Session["Username"];
            ViewBag.StudentName = parent.StudentFirstName + " " + parent.StudentLastName;
            ViewBag.ClassName = parent.ClassName;
            ViewBag.StudentGender = parent.StudentGender;
            ViewBag.ContactNumber = parent.ContactNumber;
            ViewBag.ClassTeacher = GetClassTeacher(parent.ClassName);

            // ================= 7 DAYS CALCULATION =================
            DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);
            DateTime today = DateTime.Today;

            // ============= DASHBOARD STATISTICS =============

            // Attendance Rate (last 30 days)
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var attendanceRecords = db.Attendances
                .Where(a => a.ParentId == parentId && a.AttendanceDate >= thirtyDaysAgo)
                .ToList();

            double attendanceRate = 0;
            if (attendanceRecords.Any())
            {
                int presentCount = attendanceRecords.Count(a => a.Status == "Present");
                attendanceRate = (double)presentCount / attendanceRecords.Count * 100;
            }

            // Present This Month
            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            int presentThisMonth = db.Attendances
                .Count(a => a.ParentId == parentId
                         && a.AttendanceDate >= firstDayOfMonth
                         && a.Status == "Present");

            // Classes Today
            string todayDay = DateTime.Today.DayOfWeek.ToString();
            int classesToday = db.TimeTables
                .Count(t => t.ClassName == parent.ClassName && t.WeekDay == todayDay);

            // Study Materials Count
            int studyMaterialsCount = db.Materials
                .Count(m => m.ClassName == parent.ClassName && m.IsActive);

            ViewBag.AttendanceRate = Math.Round(attendanceRate, 1);
            ViewBag.PresentThisMonth = presentThisMonth;
            ViewBag.ClassesToday = classesToday;
            ViewBag.StudyMaterialsCount = studyMaterialsCount;

            // ============= RECENT ACTIVITIES (LAST 7 DAYS ONLY) =============
            var recentActivities = new List<RecentActivityViewModel>();

            // 1. Fee Payments (Last 7 Days)
            var payments = db.FeePayments
                .Where(f => f.ParentId == parentId &&
                           f.Status == "Paid" &&
                           f.PaymentDate.HasValue &&
                           f.PaymentDate.Value >= sevenDaysAgo &&
                           f.PaymentDate.Value <= DateTime.Now)
                .OrderByDescending(f => f.PaymentDate)
                .Take(5)
                .ToList();

            foreach (var payment in payments)
            {
                var fee = db.Fees.Find(payment.FeeId);
                if (fee != null)
                {
                    recentActivities.Add(new RecentActivityViewModel
                    {
                        Date = payment.PaymentDate.Value,
                        Message = $"Fee payment: {fee.FeeType} - ₹{fee.Amount}",
                        Icon = "bi-cash-coin",
                        Type = "success"
                    });
                }
            }

            // 2. Study Materials (Last 7 Days)
            var materials = db.Materials
                .Where(m => m.ClassName == parent.ClassName &&
                           m.IsActive &&
                           m.CreatedAt >= sevenDaysAgo &&
                           m.CreatedAt <= DateTime.Now)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var material in materials)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = material.CreatedAt,
                    Message = $"New study material: {material.Title}",
                    Icon = "bi-book-fill",
                    Type = "info"
                });
            }

            // 3. Exam Schedules (Last 7 Days)
            var exams = db.ExamSchedules
                .Where(e => e.ClassName == parent.ClassName &&
                           e.CreatedAt >= sevenDaysAgo &&
                           e.CreatedAt <= DateTime.Now)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var exam in exams)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = exam.CreatedAt,
                    Message = $"Exam schedule posted: {exam.ExamType}",
                    Icon = "bi-calendar-event",
                    Type = "warning"
                });
            }

            // 4. Results (Last 7 Days)
            var results = db.Results
                .Where(r => r.ParentId == parentId &&
                           r.CreatedAt >= sevenDaysAgo &&
                           r.CreatedAt <= DateTime.Now)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var result in results)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = result.CreatedAt,
                    Message = $"Result uploaded: {result.ExamType}",
                    Icon = "bi-trophy-fill",
                    Type = "primary"
                });
            }

            // 5. Leave Reports (Last 7 Days)
            var leaves = db.LeaveReports
                .Where(l => l.ParentId == parentId &&
                           l.Status != "Pending" &&
                           l.CreatedAt >= sevenDaysAgo &&
                           l.CreatedAt <= DateTime.Now)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var leave in leaves)
            {
                string leaveIcon = leave.Status == "Approved" ? "bi-check-circle-fill" : "bi-x-circle-fill";
                string leaveType = leave.Status == "Approved" ? "success" : "danger";

                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = leave.CreatedAt,
                    Message = $"Leave {leave.Status.ToLower()}: {leave.Title}",
                    Icon = leaveIcon,
                    Type = leaveType
                });
            }

            // 6. Notifications (Last 7 Days)
            var notifications = db.Notifications
                .Where(n => n.Role == "Parent" &&
                           n.CreatedDate >= sevenDaysAgo &&
                           n.CreatedDate <= DateTime.Now)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToList();

            foreach (var notification in notifications)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = notification.CreatedDate,
                    Message = notification.Message,
                    Icon = "bi-bell-fill",
                    Type = "info"
                });
            }

            // 7. FAQs (Answered in Last 7 Days)
            var faqs = db.FAQs
                .Where(f => f.ParentId == parentId &&
                           !string.IsNullOrEmpty(f.Answer) &&
                           f.AnsweredOn.HasValue &&
                           f.AnsweredOn.Value >= sevenDaysAgo &&
                           f.AnsweredOn.Value <= DateTime.Now)
                .OrderByDescending(f => f.AnsweredOn)
                .Take(5)
                .ToList();

            foreach (var faq in faqs)
            {
                string question = faq.Question.Length > 40 ? faq.Question.Substring(0, 40) + "..." : faq.Question;
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = faq.AnsweredOn.Value,
                    Message = $"FAQ answered: {question}",
                    Icon = "bi-question-circle-fill",
                    Type = "secondary"
                });
            }

            // 8. Timetable Updates (Last 7 Days)
            var latestTimetables = db.TimeTables
                .Where(t => t.ClassName == parent.ClassName &&
                           t.CreatedDate >= sevenDaysAgo &&
                           t.CreatedDate <= DateTime.Now)
                .OrderByDescending(t => t.CreatedDate)
                .Take(3)
                .ToList();

            foreach (var tt in latestTimetables)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Date = tt.CreatedDate,
                    Message = $"Timetable uploaded: {tt.SubjectName} - {tt.WeekDay}",
                    Icon = "bi-clock-fill",
                    Type = "info"
                });
            }

            // Sort by date and send to view (take top 20)
            ViewBag.RecentActivities = recentActivities
                .OrderByDescending(a => a.Date)
                .Take(20)
                .ToList();  

            return View();
        }

        // Helper method
        private string GetClassTeacher(string className)
        {
            var teacher = db.Staffs
                .FirstOrDefault(s => s.ClassAssigned == className && s.IsActive);

            return teacher != null ? teacher.FirstName + " " + teacher.LastName : "Not Assigned";
        }
        // ================================================================= 
        // Parent: Manage Profile
        // =================================================================

        // GET: Parent/ManageProfile
        public ActionResult ManageProfile()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == parentId);

            if (parent == null)
                return HttpNotFound();

            return View(parent);
        }

        // POST: Parent/ManageProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageProfile(Parent model)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == parentId);

            if (parent == null)
                return HttpNotFound();

            // ❌ If validation fails
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ✅ CHECK IF ANY CHANGES WERE MADE
            bool hasChanges = false;

            // Check Student Details Changes
            if (parent.StudentFirstName != model.StudentFirstName ||
                parent.StudentMiddleName != model.StudentMiddleName ||
                parent.StudentLastName != model.StudentLastName ||
                parent.StudentGender != model.StudentGender ||
                parent.ClassName != model.ClassName)
            {
                hasChanges = true;
            }

            // Check Parent Details Changes
            if (parent.ParentFirstName != model.ParentFirstName ||
                parent.ParentMiddleName != model.ParentMiddleName ||
                parent.ParentLastName != model.ParentLastName ||
                parent.Gender != model.Gender ||
                parent.ContactNumber != model.ContactNumber ||
                parent.Email != model.Email ||
                parent.Address != model.Address ||
                parent.Username != model.Username)
            {
                hasChanges = true;
            }

            // ✅ IF NO CHANGES, DON'T SAVE AND DON'T SHOW SUCCESS MESSAGE
            if (!hasChanges)
            {
                TempData["Error"] = "No changes were made to the profile.";
                return RedirectToAction("ManageProfile");
            }

            // ================= UPDATE STUDENT DETAILS =================
            parent.StudentFirstName = model.StudentFirstName;
            parent.StudentMiddleName = model.StudentMiddleName;
            parent.StudentLastName = model.StudentLastName;
            parent.StudentGender = model.StudentGender;
            //parent.ClassName = model.ClassName;

            // ================= UPDATE PARENT DETAILS =================
            parent.ParentFirstName = model.ParentFirstName;
            parent.ParentMiddleName = model.ParentMiddleName;
            parent.ParentLastName = model.ParentLastName;
            parent.Gender = model.Gender;
            parent.ContactNumber = model.ContactNumber;
            parent.Email = model.Email;
            parent.Address = model.Address;

            // ================= UPDATE LOGIN DETAILS =================
            parent.Username = model.Username;

            // Save changes
            db.Configuration.ValidateOnSaveEnabled = false;
            db.SaveChanges();
            db.Configuration.ValidateOnSaveEnabled = true;

            // ✅ SHOW SUCCESS MESSAGE ONLY WHEN CHANGES WERE MADE
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("ManageProfile");
        }
        // ================================================================== view gallery ============================================================================
        public ActionResult ViewGallery()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            var galleryPhotos = db.Gallery
                                  .OrderByDescending(g => g.CreatedAt)
                                  .ToList();

            return View(galleryPhotos);
        }
        // ================================================================== Pay Fees Page ============================================================================
        public ActionResult PayFees()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);

            var payments = db.FeePayments
                             .Include("Fee")  // ✅ આ important છે - Fee table join થશે
                             .Where(fp => fp.ParentId == parentId)
                             .Select(fp => new FeePaymentViewModel
                             {
                                 PaymentId = fp.PaymentId,
                                 ClassName = fp.Fee.ClassName,
                                 FeeType = fp.Fee.FeeType,  // ✅ નવું field
                                 Amount = fp.Fee.Amount,
                                 Status = fp.Status,
                                 PaymentDate = fp.PaymentDate,
                                 ReceiptPath = fp.ReceiptPath  // ✅ નવું field
                             }).ToList();

            return View(payments);
        }
        // ============================================================ Initiate Payment =========================
        [HttpPost]
        public async Task<ActionResult> InitiatePayment(int id)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            var payment = db.FeePayments
                .Include("Fee")
                .Include("Parent")
                .FirstOrDefault(fp => fp.PaymentId == id);

            if (payment == null || payment.Status != "Pending")
            {
                TempData["Error"] = "Invalid payment request.";
                return RedirectToAction("PayFees");
            }

            try
            {
                // ✅ GET DOMAIN DYNAMICALLY
                string appUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                System.Diagnostics.Debug.WriteLine($"[PARENT] App URL: {appUrl}");

                // ✅ PASS DOMAIN TO SERVICE
                var cashfreeService = new CashfreeService(appUrl);
                var parent = payment.Parent;

                // Validate parent email/phone
                if (string.IsNullOrWhiteSpace(parent.Email))
                {
                    TempData["Error"] = "Parent email is not set. Please update your profile first.";
                    return RedirectToAction("ManageProfile");
                }

                var orderResponse = await cashfreeService.CreateOrder(
                    payment.Fee.Amount,
                    payment.PaymentId,
                    $"{parent.ParentFirstName} {parent.ParentLastName}",
                    parent.Email,
                    parent.ContactNumber ?? "9999999999"
                );

                System.Diagnostics.Debug.WriteLine($"[PARENT] Order Response: PaymentSessionId={orderResponse.PaymentSessionId}");

                ViewBag.PaymentSessionId = orderResponse.PaymentSessionId;
                ViewBag.OrderId = orderResponse.OrderId;
                ViewBag.PaymentId = id;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PARENT] InitiatePayment Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PARENT] Stack Trace: {ex.StackTrace}");

                TempData["Error"] = "Payment initiation failed: " + ex.Message;
                return RedirectToAction("PayFees");
            }
        }

        // ============================================================ Payment Callback =========================
        public async Task<ActionResult> PaymentCallback(int payment_id, string order_id)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            try
            {
                // ✅ GET DOMAIN DYNAMICALLY
                string appUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                var cashfreeService = new CashfreeService(appUrl);

                System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Order ID: {order_id}, Payment ID: {payment_id}");

                var paymentStatus = await cashfreeService.GetPaymentStatus(order_id);

                var payment = db.FeePayments
                    .Include("Fee")
                    .Include("Parent")
                    .FirstOrDefault(fp => fp.PaymentId == payment_id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment record not found.";
                    return RedirectToAction("PayFees");
                }

                System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Order Status: {paymentStatus.OrderStatus}");

                if (paymentStatus.OrderStatus == "PAID")
                {
                    payment.Status = "Paid";
                    payment.PaymentDate = DateTime.Now;
                    payment.TransactionId = paymentStatus.CfOrderId;
                    payment.PaymentMethod = !string.IsNullOrEmpty(paymentStatus.PaymentMethod)
                        ? FormatPaymentMethod(paymentStatus.PaymentMethod)
                        : "Online Payment";

                    // Generate Receipt
                    string receiptPath = Server.MapPath("~/Content/uploads/Receipts/");
                    if (!System.IO.Directory.Exists(receiptPath))
                    {
                        System.IO.Directory.CreateDirectory(receiptPath);
                    }

                    string receiptFileName = ReceiptGenerator.GenerateReceipt(
                        payment,
                        payment.Parent,
                        payment.Fee,
                        receiptPath
                    );
                    payment.ReceiptPath = "/Content/uploads/Receipts/" + receiptFileName;

                    db.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Payment saved successfully");
                    TempData["Success"] = "Payment successful! Receipt generated.";
                }
                else if (paymentStatus.OrderStatus == "ACTIVE")
                {
                    System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Payment still pending");
                    TempData["Error"] = "Payment is still pending. Please complete the payment.";
                }
                else
                {
                    payment.Status = "Failed";
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Payment failed. Status: {paymentStatus.OrderStatus}");
                    TempData["Error"] = "Payment failed or was cancelled.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PAYMENT_CALLBACK] Stack Trace: {ex.StackTrace}");

                TempData["Error"] = "Payment verification failed: " + ex.Message;
            }

            return RedirectToAction("PayFees");
        }

        // ✅ Helper method to format payment method names nicely
        private string FormatPaymentMethod(string method)
        {
            if (string.IsNullOrEmpty(method)) return "Online Payment";

            switch (method.ToLower())
            {
                case "upi":
                    return "UPI";
                case "card":
                case "debit_card":
                    return "Debit Card";
                case "credit_card":
                    return "Credit Card";
                case "netbanking":
                    return "Net Banking";
                case "wallet":
                    return "Wallet";
                case "paylater":
                    return "Pay Later";
                default:
                    return method.Replace("_", " ");
            }
        }
        // ============================================================ Download Receipt =========================
        public ActionResult DownloadReceipt(int id)
        {
            if (Session["ParentId"] == null) return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var payment = db.FeePayments
                .FirstOrDefault(fp => fp.PaymentId == id && fp.ParentId == parentId);

            if (payment == null || string.IsNullOrEmpty(payment.ReceiptPath))
            {
                TempData["Error"] = "Receipt not found.";
                return RedirectToAction("PayFees");
            }

            string filePath = Server.MapPath(payment.ReceiptPath);
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "Receipt file not found.";
                return RedirectToAction("PayFees");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "text/html", $"Receipt_{payment.PaymentId}.html");
        }


        // ==================================================================== ASK FAQ LIST (GET) ======================================================================
        public ActionResult AskFAQs()
        {
            if (Session["ParentId"] == null) return RedirectToAction("Login", "Account");

            var myFaqs = db.FAQs
                           .Where(f => f.AskedBy == "Parent")
                           .OrderByDescending(f => f.AskedOn)
                           .ToList();

            ViewBag.MyFAQs = myFaqs;
            return View(new FAQ());
        }

        // ============== ASK (POST) ==============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AskFAQs(FAQ faq)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                faq.AskedBy = "Parent";
                faq.AskedOn = DateTime.Now;

                // 🔥 THIS IS THE FIX
                faq.ParentId = Convert.ToInt32(Session["ParentId"]);

                db.FAQs.Add(faq);
                db.SaveChanges();

                TempData["Message"] = "Your question has been submitted successfully!";
                return RedirectToAction("AskFAQs");
            }

            ViewBag.MyFAQs = db.FAQs
                               .Where(f => f.AskedBy == "Parent")
                               .OrderByDescending(f => f.AskedOn)
                               .ToList();

            return View(faq);
        }

        
        //=========================================================================== View Attendance =========================================================================
        public ActionResult ViewAttendance(DateTime? fromDate, DateTime? toDate)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            // ================= VALIDATION =================
            if (fromDate.HasValue ^ toDate.HasValue)
            {
                ViewBag.Error = "Please select both From Date and To Date.";
            }

            if (fromDate.HasValue && toDate.HasValue)
            {
                if (fromDate > toDate)
                {
                    ViewBag.Error = "From Date cannot be greater than To Date.";
                }

                if (fromDate > DateTime.Today || toDate > DateTime.Today)
                {
                    ViewBag.Error = "Future dates are not allowed.";
                }
            }

            int parentId = Convert.ToInt32(Session["ParentId"]);

            // Base query
            var attendanceQuery = db.Attendances
                                    .Where(a => a.ParentId == parentId);

            // From date filter
            if (fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                attendanceQuery = attendanceQuery.Where(a =>
                    DbFunctions.TruncateTime(a.AttendanceDate) >= start);
            }

            // To date filter
            if (toDate.HasValue)
            {
                DateTime end = toDate.Value.Date;
                attendanceQuery = attendanceQuery.Where(a =>
                    DbFunctions.TruncateTime(a.AttendanceDate) <= end);
            }

            // 🔥 NO GROUP BY (important fix)
            var attendance = attendanceQuery
                                .OrderByDescending(a => a.AttendanceDate)
                                .ToList();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalRecords = attendance.Count;

            return View(attendance);
        }

        //======================VIEW TIME TABLE==============================
        // GET: ViewTimeTable
        public ActionResult ViewTimeTable(string selectedDay = null)
        {
            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == parentId);

            if (parent == null)
            {
                TempData["Error"] = "Parent not found.";
                return RedirectToAction("Index");
            }

            // ================= GET STUDENT'S CLASS =================
            string studentClass = parent.ClassName; // This property already exists in your Parent model

            if (string.IsNullOrEmpty(studentClass))
            {
                TempData["Error"] = "No class assigned to your child.";
                return RedirectToAction("Index");
            }

            ViewBag.StudentClass = studentClass; // Pass to view to display
                                                 // ================= END =================

            // Create the days list for dropdown
            ViewBag.Days = new SelectList(
                new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                selectedDay
            );

            // ================= VALIDATION: Check if day is selected =================
            if (string.IsNullOrEmpty(selectedDay) || selectedDay == "-- Select Day --")
            {
                ViewBag.Message = "Please select the day.";
                return View(new List<TimeTable>());
            }
            // ================= END =================

            // Sunday check
            if (selectedDay == "Sunday")
            {
                ViewBag.Message = "Today is a holiday!";
                return View(new List<TimeTable>());
            }

            // ================= FILTER BY STUDENT'S CLASS =================
            // Get timetable for selected day AND student's class only
            var timetable = db.TimeTables
                .Where(t => t.WeekDay == selectedDay && t.ClassName == studentClass)
                .OrderBy(t => t.StartTime)
                .ToList();
            // ================= END =================

            // If no data found for the selected day
            if (!timetable.Any())
            {
                ViewBag.Message = $"No timetable found for class {studentClass} on {selectedDay}.";
            }

            return View(timetable);
        }

        // =================================================================== Parent: View Class Materials ===================================================
        public ActionResult ViewMaterial()
        {
            if (!IsParent()) return RedirectToLogin();

            // ParentId from session
            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == parentId);
            if (parent == null) return HttpNotFound();

            // Parent ke student ki class (jo Parent table me hi hai)
            string studentClass = parent.ClassName;

            // Us class ke materials load karo
            var materials = db.Materials
                              .Include("Staff")
                              .Where(m => m.IsActive && m.ClassName == studentClass)
                              .OrderByDescending(m => m.CreatedAt)
                              .ToList();

            ViewBag.StudentClass = studentClass;
            return View(materials);
        }



        [HttpGet]
        public ActionResult DownloadMaterial(int id)
        {
            if (!IsParent())
                return RedirectToLogin();

            var material = db.Materials.FirstOrDefault(m => m.Id == id);
            if (material == null)
                return HttpNotFound();

            if (string.IsNullOrEmpty(material.FilePath))
                return HttpNotFound();

            string filePath = Server.MapPath(material.FilePath);
            if (!System.IO.File.Exists(filePath))
                return HttpNotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(
                fileBytes,
                System.Net.Mime.MediaTypeNames.Application.Octet,
                material.FileName
            );
        }

        // ================================================================= 
        // Parent: View Exam Schedule
        // =================================================================

        // GET: Parent/ViewExamSchedule
        public ActionResult ViewExamSchedule()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.Find(parentId);

            if (parent == null)
                return HttpNotFound();

            // Parent ka class leke schedule fetch karo
            var schedules = db.ExamSchedules
                              .Where(e => e.ClassName == parent.ClassName)
                              .OrderByDescending(e => e.CreatedAt)
                              .ToList();

            return View(schedules);
        }

        // ================================================================= 
        // Parent: Download Exam Schedule
        // =================================================================

        [HttpGet]
        public ActionResult DownloadExamSchedule(int id)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.Find(parentId);

            if (parent == null)
                return HttpNotFound();

            var exam = db.ExamSchedules.Find(id);

            if (exam == null || exam.ClassName != parent.ClassName)
            {
                TempData["Error"] = "Exam schedule not found or unauthorized!";
                return RedirectToAction("ViewExamSchedule");
            }

            if (string.IsNullOrEmpty(exam.ScheduleFilePath))
            {
                TempData["Error"] = "File path is missing.";
                return RedirectToAction("ViewExamSchedule");
            }

            string filePath = Server.MapPath(exam.ScheduleFilePath);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("ViewExamSchedule");
            }

            // Read file bytes
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            // ✅ SET SUCCESS MESSAGE IN SESSION (TempData won't work with File download)
            Session["DownloadSuccess"] = $"Exam schedule '{exam.ScheduleFileName}' downloaded successfully!";

            // Return file for download
            return File(
                fileBytes,
                System.Net.Mime.MediaTypeNames.Application.Octet,
                exam.ScheduleFileName
            );
        }



        // ================================================================= Parent: Give Leave Report =================================================================

        // GET: Parent/GiveLeaveReport
        public ActionResult GiveLeaveReport()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            return View();
        }

        // POST: Parent/GiveLeaveReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GiveLeaveReport(LeaveReport report, HttpPostedFileBase Attachment)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var parent = db.Parents.FirstOrDefault(p => p.ParentId == parentId);

            if (parent == null)
            {
                ModelState.AddModelError("", "Parent not found.");
                return View(report);
            }

            // ✅ COMPREHENSIVE FILE VALIDATION (Like Staff's PostExamSchedule)
            if (Attachment != null && Attachment.ContentLength > 0)
            {
                // ✅ 1. Validate File Extension
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(Attachment.FileName)?.ToLower();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Invalid file type! Only PDF, DOC, DOCX, JPG, and PNG files are allowed.";
                    return View(report);
                }

                // ✅ 2. Validate File Size (Maximum 5 MB)
                const int maxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB in bytes
                if (Attachment.ContentLength > maxFileSizeInBytes)
                {
                    double fileSizeMB = Attachment.ContentLength / (1024.0 * 1024.0);
                    TempData["Error"] = $"File size is {fileSizeMB:F2} MB. Maximum allowed size is 5 MB. Please select a smaller file.";
                    return View(report);
                }

                // ✅ 3. Validate File Name Length
                if (Attachment.FileName.Length > 100)
                {
                    TempData["Error"] = "File name is too long. Please rename the file to less than 100 characters.";
                    return View(report);
                }
            }


            // ✅ End Date >= Start Date check
            if (report.StartDate.HasValue && report.EndDate.HasValue)
            {
                if (report.EndDate.Value < report.StartDate.Value)
                {
                    TempData["Error"] = "End Date cannot be less than Start Date.";
                    return View(report);
                }
            }
            if (ModelState.IsValid)
            {
                report.ParentId = parent.ParentId;
                report.Status = "Pending";
                report.CreatedAt = DateTime.Now;

                if (report.StartDate != null && report.EndDate != null)
                {
                    report.Days = (report.EndDate.Value - report.StartDate.Value).Days + 1;
                }

                // Handle file attachment
                if (Attachment != null && Attachment.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Content/uploads/LeaveReports/");

                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }

                    // ✅ Generate unique file name to avoid conflicts (like Staff controller)
                    string fileExtension = Path.GetExtension(Attachment.FileName);
                    string uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;
                    string fullPath = Path.Combine(folderPath, uniqueFileName);

                    Attachment.SaveAs(fullPath);

                    // ✅ Save with unique file name
                    report.AttachmentPath = "~/Content/uploads/LeaveReports/" + uniqueFileName;
                    report.AttachmentFileName = Path.GetFileName(Attachment.FileName); // Keep original name for display
                    report.AttachmentSize = Attachment.ContentLength;
                    report.AttachmentContentType = Attachment.ContentType;
                }

                db.LeaveReports.Add(report);
                db.SaveChanges();

                TempData["Message"] = "Leave Report Submitted Successfully!";
                return RedirectToAction("GiveLeaveReport");
            }

            return View(report);
        }
        // ===================================================================== Parent: View Leave Reports ===========================================
        public ActionResult ViewLeaveReports()
        {
            if (Session["ParentId"] == null) return RedirectToAction("Login", "Account");

            int parentId = Convert.ToInt32(Session["ParentId"]);
            var reports = db.LeaveReports
                .Where(r => r.ParentId == parentId)
                .Include("Parent")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(reports);
        }

    

        // =====================================================================
        // Parent - View Result
        // =====================================================================
        public ActionResult ViewResult()
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Parent");

            int parentId = Convert.ToInt32(Session["ParentId"]);

            var results = db.Results
                            .Include(r => r.Parent)
                            .Where(r => r.ParentId == parentId)
                            .OrderByDescending(r => r.CreatedAt)
                            .ToList();

            return View(results);
        }

        // Download Result File
        public ActionResult DownloadResult(int id)
        {
            if (Session["ParentId"] == null)
                return RedirectToAction("Login", "Parent");

            int parentId = Convert.ToInt32(Session["ParentId"]);

            var result = db.Results.FirstOrDefault(r => r.Id == id && r.ParentId == parentId);

            if (result == null || string.IsNullOrEmpty(result.ResultFile))
            {
                TempData["Error"] = "Result file not found.";
                return RedirectToAction("ViewResult");
            }

            string filePath = Path.Combine(Server.MapPath("~/Uploads/Results"), result.ResultFile);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File does not exist on server.";
                return RedirectToAction("ViewResult");
            }

            string contentType = "application/octet-stream";
            string extension = Path.GetExtension(result.ResultFile).ToLower();

            if (extension == ".pdf")
                contentType = "application/pdf";
            else if (extension == ".jpg" || extension == ".jpeg")
                contentType = "image/jpeg";
            else if (extension == ".png")
                contentType = "image/png";

            // Generate a user-friendly download filename
            var parent = db.Parents.Find(parentId);
            string studentName = parent.StudentFirstName + "_" + parent.StudentLastName;
            string downloadFileName = $"Result_{studentName}_{result.ExamType}{extension}";

            return File(filePath, contentType, downloadFileName);
        }

        //===================================================================== Get Notification ===============================================================
        public ActionResult GetNotification()
        {
            if (!IsParent()) return RedirectToLogin();

            // Sirf Parent role wali notifications laayenge (latest first)
            var notifications = db.Notifications
                                  .Where(n => n.Role == "Parent")
                                  .OrderByDescending(n => n.CreatedDate)
                                  .ToList();

            return View(notifications);
        }



        // ====================================================================== Give Feedback =================================================================
        // GET: Give Feedback
        public ActionResult GiveFeedback()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GiveFeedback(Feedback feedback)
        {
            if (Session["ParentId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                int parentId = Convert.ToInt32(Session["ParentId"]);
                feedback.ParentId = parentId;
                feedback.CreatedAt = DateTime.Now;

                db.Feedbacks.Add(feedback);
                db.SaveChanges();

                // ✅ SUCCESS MESSAGE
                TempData["FeedbackSuccess"] = "✅ Feedback submitted successfully!";

                return RedirectToAction("ViewFeedback");
            }

            return View(feedback);
        }


        // GET: View Feedback with Admin Reply
        public ActionResult ViewFeedback()
        {
            int parentId = (int)Session["ParentId"];
            var feedbacks = db.Feedbacks
                              .Where(f => f.ParentId == parentId)
                              .OrderByDescending(f => f.CreatedAt)
                              .ToList();

            return View(feedbacks);
        }

        // 🔓 Public - All Parents Feedback
        public ActionResult AllFeedback()
        {
            var feedbacks = db.Feedbacks
                              .Include("Parent") // IMPORTANT
                              .OrderByDescending(f => f.CreatedAt)
                              .ToList();

            return View(feedbacks);
        }



    }
}