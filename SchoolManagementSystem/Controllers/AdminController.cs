using SchoolManagementSystem.Filters;
using SchoolManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace SchoolManagementSystem.Controllers
{
    [RoleAuthorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ================= DASHBOARD =================
        //public ActionResult Index()
        //{
        //    if (!IsAdmin()) return RedirectToLogin();
        //    ViewBag.Username = Session["Username"];
        //    return View();
        //}



        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToLogin();

            ViewBag.Username = Session["Username"];

            // ============ STAFF STATISTICS ============
            ViewBag.TotalStaff = db.Staffs.Count(s => s.IsActive);
            ViewBag.IsActiveStaff = db.Staffs.Count(s => s.IsActive);
            ViewBag.InActiveStaff = db.Staffs.Count(s => !s.IsActive);

            // ============ PARENTS STATISTICS ============
            ViewBag.TotalParents = db.Parents.Count(p => p.IsActive);

            // ============ FEE COLLECTION STATISTICS ============
            var feeCollection = db.FeePayments
                .Where(fp => fp.Status != null && fp.Status.ToLower() == "paid")
                .Join(db.Fees,
                      fp => fp.FeeId,
                      f => f.FeeId,
                      (fp, f) => f.Amount)
                .Sum(amount => (decimal?)amount) ?? 0;

            ViewBag.FeeCollection = feeCollection;
            //============Panding Fees-----------------//
            ViewBag.PendingFeesCount = db.FeePayments
                .Count(f => f.Status.ToLower() == "pending");
            // ============ STANDARD-WISE FEE COLLECTION DATA ============
            var currentYear = DateTime.Now.Year;
            var yearStart = new DateTime(currentYear, 1, 1);
            var yearEnd = new DateTime(currentYear, 12, 31);

            // Get all standards with their fee collection
            var standardFees = db.FeePayments
                .Where(fp => fp.Status != null &&
                       fp.Status.ToLower() == "paid" &&
                       fp.PaymentDate != null &&
                       fp.PaymentDate >= yearStart &&
                       fp.PaymentDate <= yearEnd)
                .Join(db.Fees,
                      fp => fp.FeeId,
                      f => f.FeeId,
                      (fp, f) => new { f.ClassName, f.Amount })
                .GroupBy(x => x.ClassName)
                .Select(g => new
                {
                    Standard = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Standard)
                .ToList();

            ViewBag.StandardFees = standardFees;

            // ============ NOTIFICATIONS STATISTICS ============
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            ViewBag.PendingNotifications = db.Notifications
                .Count(n => n.CreatedDate >= sevenDaysAgo);

            // ============ FEEDBACK STATISTICS ============
            ViewBag.PendingFeedback = db.Feedbacks
                .Count(f => string.IsNullOrEmpty(f.Reply));

            // ============ FAQ STATISTICS ============
            ViewBag.UnansweredFAQs = db.FAQs
                .Count(f => string.IsNullOrEmpty(f.Answer));

            // ============ GALLERY STATISTICS ============
            ViewBag.GalleryItems = db.Gallery.Count();

            return View();
        }
        // ================= STAFF CRUD =================

        public ActionResult ManageStaff(bool showInactive = false)
        {
            if (!IsAdmin()) return RedirectToLogin();

            ViewBag.ShowInactive = showInactive;

            // Filter staff based on IsActive
            var staffList = showInactive
                ? db.Staffs.Where(s => !s.IsActive).ToList()  // show only inactive
                : db.Staffs.Where(s => s.IsActive).ToList(); // show only active

            return View(staffList);
        }


        public ActionResult CreateStaff()
        {
            if (!IsAdmin()) return RedirectToLogin();

            ModelState.Clear();   // ⭐ VERY IMPORTANT

            return View(new Staff()); // Empty Model
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CreateStaff(Staff model, HttpPostedFileBase Photo)
        {
            if (!IsAdmin()) return RedirectToLogin();

            // ✅ Add manual validation for password in CREATE (since we removed [Required])
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
            }

            if (string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Confirm Password is required");
            }

            // Check if passwords match
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (db.Staffs.Any(s => s.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            if (db.Staffs.Any(s => s.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }
            // ✅ Check duplicate Class Assigned
            if (db.Staffs.Any(s => s.ClassAssigned == model.ClassAssigned))
            {
                ModelState.AddModelError("ClassAssigned",
                    "This class is already assigned to another staff.");

                return View(model);
            }


            // ✅ Photo Validation (Server Side)
            if (Photo != null && Photo.ContentLength > 0)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                string extension = Path.GetExtension(Photo.FileName).ToLower();

                // Check Extension
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Photo",
                        "Only JPG, JPEG, PNG and GIF image formats are allowed.");
                    return View(model);
                }

                // Check Size (5MB)
                if (Photo.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Photo",
                        "Image size must be less than 5 MB.");
                    return View(model);
                }

                // Check Filename Length
                if (Photo.FileName.Length > 30)
                {
                    ModelState.AddModelError("Photo",
                        " Filename is too long!");
                    return View(model);
                }
            }


            model.PhotoPath = SavePhoto(Photo);
            model.CreatedAt = DateTime.Now;
            model.IsActive = true;

            db.Staffs.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Staff member added successfully!";
            return RedirectToAction("ManageStaff");
        }
        public ActionResult DetailsStaff(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();
            var staff = db.Staffs.Find(id);
            if (staff == null) return HttpNotFound();
            return View(staff);
        }

        public ActionResult EditStaff(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var staff = db.Staffs.Find(id);

            if (staff == null)
                return HttpNotFound();

            // ✅ Clear password fields
            staff.Password = "";
            staff.ConfirmPassword = "";



            return View(staff);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditStaff(Staff model, HttpPostedFileBase Photo)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var staff = db.Staffs.Find(model.Id);

            if (staff == null)
                return HttpNotFound();

            // ✅ Password Handling
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                model.Password = staff.Password;
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword",
                        "Confirm password is required.");
                }
                else if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword",
                        "Password and Confirm Password do not match.");
                }
            }

            // ✅ Photo Validation (Server Side)
            if (Photo != null && Photo.ContentLength > 0)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                string extension = Path.GetExtension(Photo.FileName).ToLower();

                // Check Extension
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Photo",
                        "Only JPG, JPEG, PNG and GIF image formats are allowed.");
                    return View(model);
                }

                // Check Size (5MB)
                if (Photo.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Photo",
                        "Image size must be less than 5 MB.");
                    return View(model);
                }

                // ✅ Check Filename Length - FIXED
                if (Photo.FileName.Length > 30)
                {
                    ModelState.AddModelError("Photo",
                        "Filename is too long!.");
                    return View(model);  // ✅ Now inside the if block
                }
            }

            // ✅ Model validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ✅ Duplicate email
            if (db.Staffs.Any(s => s.Email == model.Email && s.Id != model.Id))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // ✅ Duplicate username
            if (db.Staffs.Any(s => s.Username == model.Username && s.Id != model.Id))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            // ✅ Check duplicate Class Assigned (except current staff)
            if (db.Staffs.Any(s => s.ClassAssigned == model.ClassAssigned && s.Id != model.Id))
            {
                ModelState.AddModelError("ClassAssigned",
                    "This class is already assigned to another staff.");
                return View(model);
            }

            // ✅ Update fields
            staff.Username = model.Username;
            staff.FirstName = model.FirstName;
            staff.LastName = model.LastName;
            staff.Email = model.Email;
            staff.ContactNo = model.ContactNo;
            staff.Address = model.Address;
            staff.ClassAssigned = model.ClassAssigned;
            staff.Designation = model.Designation;
            staff.Gender = model.Gender;
            staff.Education = model.Education;
            staff.Password = model.Password;

            // ✅ Photo upload
            if (Photo != null && Photo.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(staff.PhotoPath))
                {
                    DeletePhoto(staff.PhotoPath);
                }

                staff.PhotoPath = SavePhoto(Photo);
            }

            try
            {
                db.Entry(staff).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Property: {ve.PropertyName} Error: {ve.ErrorMessage}");
                    }
                }
                return View(model);
            }

            TempData["Success"] = "Staff updated successfully!";
            return RedirectToAction("ManageStaff");
        }

        public ActionResult DeleteStaff(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();
            var staff = db.Staffs.Find(id);
            if (staff == null) return HttpNotFound();
            return View(staff);
        }

        [HttpPost, ActionName("DeleteStaff"), ValidateAntiForgeryToken]
        public ActionResult DeleteStaffConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var staff = db.Staffs.Find(id);
            if (staff == null) return HttpNotFound();

            if (!staff.IsActive)
                return RedirectToAction("ManageStaff"); // already inactive

            // ✅ Soft Delete
            staff.IsActive = false;

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Collect all validation errors
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => $"Property: {e.PropertyName}, Error: {e.ErrorMessage}");

                string fullErrorMessage = string.Join("; ", errorMessages);

                // ✅ Debug output (Visual Studio Output window me dikh jaayega)
                System.Diagnostics.Debug.WriteLine("Validation Errors: " + fullErrorMessage);

                // ✅ Browser me bhi dikhane ke liye
                return new HttpStatusCodeResult(500, "Validation failed: " + fullErrorMessage);
            }
            TempData["Success"] = "Staff deactivated successfully!";
            return RedirectToAction("ManageStaff");

        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RestoreStaff(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var staff = db.Staffs.Find(id);
            if (staff == null) return HttpNotFound();

            staff.IsActive = true;
            db.SaveChanges();

            // Inactive view par hi wapas jao
            TempData["Success"] = "Staff restored successfully!";
            return RedirectToAction("ManageStaff", new { showInactive = true });


        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HardDeleteStaff(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var staff = db.Staffs.Find(id);

            if (staff == null)
            {
                TempData["Error"] = "Staff not found!";
                return RedirectToAction("ManageStaff");
            }

            // ✅ First delete related Attendance
            var attendanceList = db.Attendances
                                    .Where(a => a.StaffId == id)
                                    .ToList();

            if (attendanceList.Any())
            {
                db.Attendances.RemoveRange(attendanceList);
            }

            // ✅ Delete Photo
            if (!string.IsNullOrEmpty(staff.PhotoPath))
            {
                DeletePhoto(staff.PhotoPath);
            }

            // ✅ Now delete staff
            db.Staffs.Remove(staff);
            db.SaveChanges();

            TempData["Success"] = "Staff deleted permanently!";

            return RedirectToAction("ManageStaff");
        }

        //=================================================== VIEW PARENTS START ====================================


        public ActionResult ViewParents(string selectedClass, string status)
        {

            var parents = db.Parents.AsQueryable();

            if (!string.IsNullOrEmpty(selectedClass))
            {
                parents = parents.Where(p => p.ClassName == selectedClass);
            }


            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                    parents = parents.Where(p => p.IsActive == true);
                else if (status == "Inactive")
                    parents = parents.Where(p => p.IsActive == false);
            }

            // Class dropdown ke liye unique class list bhej rahe
            ViewBag.ClassList = db.Parents
                .Select(p => p.ClassName)
                .Distinct()
                .ToList();

            ViewBag.SelectedClass = selectedClass;
            ViewBag.Status = status;

            return View(parents.ToList());
        }

        // ================================================================= GALLERY CRUD ===========================================================================

        public ActionResult ManageGallery()
        {
            if (!IsAdmin()) return RedirectToLogin();
            var list = db.Gallery.OrderByDescending(g => g.CreatedAt).ToList();
            return View(list);
        }

        public ActionResult CreateGallery()
        {
            if (!IsAdmin()) return RedirectToLogin();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGallery(Gallery model, HttpPostedFileBase PhotoFile)
        {
            if (!IsAdmin()) return RedirectToLogin();

            // Required
            if (PhotoFile == null || PhotoFile.ContentLength == 0)
            {
                ModelState.AddModelError("PhotoFile", "Please select a photo");
            }
            // File name length check
            if (PhotoFile != null && PhotoFile.FileName.Length > 100)
            {
                ModelState.AddModelError("PhotoFile", "File name is too long. Please rename file.");
            }
            // Size
            if (PhotoFile != null && PhotoFile.ContentLength > 20 * 1024 * 1024)
            {
                ModelState.AddModelError("PhotoFile", "Max 20MB allowed");
            }

            // Format
            if (PhotoFile != null)
            {
                string ext = Path.GetExtension(PhotoFile.FileName).ToLower();

                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif" };

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG, JPEG, PNG, GIF allowed");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            // Short Name
            string fileName = Guid.NewGuid().ToString("N").Substring(0, 8)
                            + Path.GetExtension(PhotoFile.FileName);

            string folder = Server.MapPath("~/Content/Uploads/Gallery");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, fileName);

            PhotoFile.SaveAs(path);

            model.PhotoPath = "/Content/Uploads/Gallery/" + fileName;

            if (model.PhotoPath.Length > 50)
            {
                ModelState.AddModelError("PhotoFile", "File path too long");
                return View(model);
            }

            model.CreatedAt = DateTime.Now;

            db.Gallery.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Photo uploaded successfully";

            return RedirectToAction("ManageGallery");
        }

        public ActionResult DetailsGallery(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();
            var gallery = db.Gallery.Find(id);
            if (gallery == null) return HttpNotFound();
            return View(gallery);
        }

        public ActionResult EditGallery(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var gallery = db.Gallery.Find(id);

            if (gallery == null)
                return HttpNotFound();

            // Safety check
            if (string.IsNullOrEmpty(gallery.PhotoPath))
            {
                gallery.PhotoPath = "/Content/Uploads/no-image.png"; // default image
            }

            return View(gallery);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditGallery(Gallery model, HttpPostedFileBase PhotoFile)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var gallery = db.Gallery.Find(model.Id);

            if (gallery == null)
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.PhotoName))
                ModelState.AddModelError("PhotoName", "Photo name is required");
            // File name length
            if (PhotoFile != null && PhotoFile.FileName.Length > 100)
            {
                ModelState.AddModelError("PhotoFile", "File name is too long. Please rename file.");
            }

            ValidatePhoto(PhotoFile, false); // Optional

            if (!ModelState.IsValid)
                return View(model);

            gallery.PhotoName = model.PhotoName;

            // New Photo Upload
            if (PhotoFile != null && PhotoFile.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(gallery.PhotoPath))
                {
                    string oldPath = Server.MapPath(gallery.PhotoPath);

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }


                string fileName = Guid.NewGuid().ToString("N").Substring(0, 8)
                                  + Path.GetExtension(PhotoFile.FileName);

                string folder = Server.MapPath("~/Content/Uploads/Gallery");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);

                PhotoFile.SaveAs(path);

                gallery.PhotoPath = "/Content/Uploads/Gallery/" + fileName;

                if (gallery.PhotoPath.Length > 50)
                {
                    ModelState.AddModelError("PhotoFile", "File path too long");
                    return View(model);
                }
            }

            db.SaveChanges();

            TempData["Success"] = "Photo updated successfully ✅";

            return RedirectToAction("ManageGallery");
        }

        public ActionResult DeleteGallery(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();
            var gallery = db.Gallery.Find(id);
            if (gallery == null) return HttpNotFound();
            return View(gallery);
        }
        [HttpPost, ActionName("DeleteGallery"), ValidateAntiForgeryToken]
        public ActionResult DeleteGalleryConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var gallery = db.Gallery.Find(id);

            if (gallery == null) return HttpNotFound();

            string filePath = Server.MapPath(gallery.PhotoPath);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            db.Gallery.Remove(gallery);
            db.SaveChanges();

            TempData["Success"] = "Photo deleted successfully ✅";

            return RedirectToAction("ManageGallery");
        }

        private bool ValidatePhoto(HttpPostedFileBase file, bool isRequired = true)
        {
            if (isRequired && (file == null || file.ContentLength == 0))
            {
                ModelState.AddModelError("PhotoFile", "Photo is required");
                return false;
            }

            if (file != null)
            {
                if (file.ContentLength > 20 * 1024 * 1024)
                {
                    ModelState.AddModelError("PhotoFile", "Max 20MB allowed");
                    return false;
                }

                string ext = Path.GetExtension(file.FileName).ToLower();

                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif" };

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG, JPEG, PNG, GIF allowed");
                    return false;
                }
            }

            return true;
        }

        public ActionResult Gallery()
        {
            var list = db.Gallery.OrderByDescending(g => g.CreatedAt).ToList();
            return View(list);
        }
        // ================================================================================
        // Manage Fees (Read)
        // ================================================================================
        public ActionResult ManageFees()
        {
            var fees = db.Fees.ToList();
            return View(fees);
        }

        // ========================= Create Fee =========================
        public ActionResult CreateFee()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateFee(Fee fee)
        {
            if (!ModelState.IsValid) return View(fee);

            int currentYear = DateTime.Now.Year;

            // All fees of same class in current year
            var classYearFees = db.Fees
                .Where(f => f.ClassName == fee.ClassName && f.CreatedAt.Year == currentYear)
                .ToList();

            bool hasSixMonth = classYearFees.Any(f => f.FeeType == "6-Month");
            bool hasAnnual = classYearFees.Any(f => f.FeeType == "Annual");
            int sixMonthCount = classYearFees.Count(f => f.FeeType == "6-Month");

            // 🔒 If 6-Month already started → block Annual forever
            if (hasSixMonth && fee.FeeType == "Annual")
            {
                TempData["Error"] = "You cannot add Annual fee because 6-Month fee has already been added for this class in the current year.";
                return RedirectToAction("CreateFee");
            }

            // 🔒 If Annual already started → block 6-Month forever
            if (hasAnnual && fee.FeeType == "6-Month")
            {
                TempData["Error"] = "You cannot add 6-Month fee because Annual fee has already been added for this class in the current year.";
                return RedirectToAction("CreateFee");
            }

            // ❌ 6-Month max 2 times
            if (fee.FeeType == "6-Month" && sixMonthCount >= 2)
            {
                TempData["Error"] = "6-Month fee can be added only twice per year for this class.";
                return RedirectToAction("CreateFee");
            }

            // ❌ Annual only once
            if (fee.FeeType == "Annual" && hasAnnual)
            {
                TempData["Error"] = "Annual fee can be added only once per year for this class.";
                return RedirectToAction("CreateFee");
            }

            // ✅ AUTO-ASSIGN PERIOD
            if (fee.FeeType == "6-Month")
            {
                fee.Period = sixMonthCount + 1; // Will be 1 or 2
            }
            else
            {
                fee.Period = 1; // Annual is always period 1
            }

            // ✅ Save
            fee.CreatedAt = DateTime.Now;
            db.Fees.Add(fee);
            db.SaveChanges();

            TempData["Message"] = "Fee added successfully!";
            return RedirectToAction("ManageFees");
        }

        // ========================= Delete Fee =========================
        public ActionResult DeleteFee(int id)
        {
            var fee = db.Fees.Find(id);
            if (fee == null) return HttpNotFound();
            return View(fee);
        }

        [HttpPost, ActionName("DeleteFee")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFeeConfirmed(int id)
        {
            try
            {
                var fee = db.Fees.Find(id);
                if (fee == null) return HttpNotFound();

                // First, check if this fee has any related payments
                var relatedPayments = db.FeePayments.Where(fp => fp.FeeId == id).ToList();

                if (relatedPayments.Any())
                {
                    // Delete all related fee payments first
                    db.FeePayments.RemoveRange(relatedPayments);
                }

                // Now delete the fee
                db.Fees.Remove(fee);

                // Save changes only ONCE
                db.SaveChanges();

                TempData["Message"] = "Fee Deleted Successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting fee: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Delete Fee Error: " + ex.ToString());
            }

            return RedirectToAction("ManageFees");
        }
        public ActionResult AssignFeeToClass(string className)
        {
            // ⚠️ CHANGED: Get ALL fees for this class (not just first one)
            var fees = db.Fees
                .Where(f => f.ClassName == className)
                .OrderBy(f => f.Period) // Order by period
                .ToList();

            if (!fees.Any()) return HttpNotFound();

            var parents = db.Parents
                .Where(p => p.ClassName == className && p.IsActive)
                .ToList();

            int assignedCount = 0;

            foreach (var parent in parents)
            {
                // ⚠️ CHANGED: Assign ALL fees that haven't been assigned yet
                foreach (var fee in fees)
                {
                    // Only assign if NOT already assigned
                    if (!db.FeePayments.Any(fp => fp.ParentId == parent.ParentId && fp.FeeId == fee.FeeId))
                    {
                        db.FeePayments.Add(new FeePayment
                        {
                            ParentId = parent.ParentId,
                            FeeId = fee.FeeId,
                            StudentName = parent.StudentFirstName + " " +
                                        (string.IsNullOrEmpty(parent.StudentMiddleName) ? "" : parent.StudentMiddleName + " ") +
                                        parent.StudentLastName,
                            Status = "Pending",
                            PaymentDate = null
                        });
                        assignedCount++;
                    }
                }
            }

            db.SaveChanges();

            TempData["Message"] = $"Fee assigned successfully to {assignedCount} student(s) of {className}.";
            return RedirectToAction("ManageFees");
        }

        // ========================= Add Offline Fee =========================
        public ActionResult AddOfflineFee()
        {
            // Get distinct class names for dropdown
            ViewBag.Classes = db.Fees.Select(f => f.ClassName).Distinct().ToList();
            ViewBag.FeeTypes = new List<string> { "6-Month", "Annual" };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddOfflineFee(OfflineFeePaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ ParentId se directly parent dhundo (name comparison hatao)
                    var parent = db.Parents
                        .FirstOrDefault(p =>
                            p.ParentId == model.ParentId
                            && p.IsActive);

                    if (parent == null)
                    {
                        TempData["Error"] = "Student not found!";
                        ViewBag.Classes = db.Fees.Select(f => f.ClassName).Distinct().ToList();
                        return View(model);
                    }

                    // ✅ Sirf PENDING fee dhundo for this student (Period order se)
                    var pendingFeePayment = (from fp in db.FeePayments
                                             join f in db.Fees on fp.FeeId equals f.FeeId
                                             where fp.ParentId == parent.ParentId
                                                   && f.ClassName == model.ClassName
                                                   && f.FeeType == model.FeeType
                                                   && fp.Status == "Pending"
                                             orderby f.Period ascending
                                             select fp).FirstOrDefault();

                    if (pendingFeePayment == null)
                    {
                        TempData["Error"] = "No pending fee found for this student. Fee may already be paid!";
                        ViewBag.Classes = db.Fees.Select(f => f.ClassName).Distinct().ToList();
                        return View(model);
                    }

                    // ✅ Pending fee ko Paid karo
                    pendingFeePayment.Status = "Paid";
                    pendingFeePayment.PaymentDate = model.PaymentDate;
                    pendingFeePayment.PaymentMode = "Offline";
                    pendingFeePayment.StudentName = parent.StudentFirstName + " " + parent.StudentLastName;
                    pendingFeePayment.TransactionId = "OFFLINE-" + DateTime.Now.Ticks;
                    pendingFeePayment.PaymentMethod = "Cash/Cheque";

                    db.SaveChanges();

                    TempData["Message"] = "Offline fee payment added successfully!";
                    return RedirectToAction("ManageFees");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error: " + ex.Message;
                    System.Diagnostics.Debug.WriteLine("Add Offline Fee Error: " + ex.ToString());
                }
            }

            ViewBag.Classes = db.Fees.Select(f => f.ClassName).Distinct().ToList();
            return View(model);
        }
        public ActionResult ViewFeeStatus(string className)
        {
            var payments = (from fp in db.FeePayments
                            join pr in db.Parents on fp.ParentId equals pr.ParentId
                            join f in db.Fees on fp.FeeId equals f.FeeId
                            where f.ClassName == className
                            select new FeePaymentViewModel
                            {
                                PaymentId = fp.PaymentId,
                                StudentFirstName = pr.StudentFirstName,
                                StudentLastName = pr.StudentLastName,
                                ClassName = f.ClassName,
                                Amount = f.Amount,
                                FeeType = f.FeeType + (f.FeeType == "6-Month" ? " (Period " + f.Period + ")" : ""), // Show period
                                Status = fp.Status,
                                PaymentDate = fp.PaymentDate,
                                PaymentMode = fp.PaymentMode ?? "Online"
                            }).ToList();

            ViewBag.ClassName = className;
            return View(payments);
        }
        public JsonResult GetStudentsByClass(string className)
        {
            var students = db.Parents
                .Where(p => p.ClassName == className && p.IsActive)
                .Select(p => new
                {
                    Id = p.ParentId,
                    Name = p.StudentFirstName + " " + p.StudentLastName
                })
                .ToList();

            return Json(students, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetStudentFeeDetails(int parentId)
        {
            // ✅ Sirf PENDING fee return karo
            var feeInfo = (from fp in db.FeePayments
                           join f in db.Fees on fp.FeeId equals f.FeeId
                           where fp.ParentId == parentId
                                 && fp.Status == "Pending"
                           orderby f.Period ascending
                           select new
                           {
                               f.Amount,
                               f.FeeType,
                               f.Period,
                               fp.Status
                           }).FirstOrDefault();

            return Json(feeInfo, JsonRequestBehavior.AllowGet);
        }

        // =========================================================
        // ADD THIS ACTION inside AdminController class
        // =========================================================

        public ActionResult FeeReport(FeeReportViewModel filter)
        {
            if (!IsAdmin()) return RedirectToLogin();

            var vm = new FeeReportViewModel
            {
                Day = filter.Day,
                Month = filter.Month,
                Year = filter.Year,
                ClassName = filter.ClassName,
                FeeType = filter.FeeType,
                Status = filter.Status,
                ClassList = db.Fees
                              .Select(f => f.ClassName)
                              .Distinct()
                              .OrderBy(c => c)
                              .ToList()
            };

            // Base join: FeePayments -> Fees -> Parents
            var query =
                from fp in db.FeePayments
                join f in db.Fees on fp.FeeId equals f.FeeId
                join p in db.Parents on fp.ParentId equals p.ParentId
                select new
                {
                    fp.StudentName,
                    f.ClassName,
                    f.FeeType,
                    f.Period,
                    f.Amount,
                    fp.Status,
                    fp.PaymentDate,
                    fp.PaymentMode,
                    ParentName = p.ParentFirstName + " " + p.ParentLastName,
                    p.ContactNumber
                };

            // Apply filters
            if (!string.IsNullOrEmpty(filter.ClassName))
                query = query.Where(x => x.ClassName == filter.ClassName);

            if (!string.IsNullOrEmpty(filter.FeeType))
                query = query.Where(x => x.FeeType == filter.FeeType);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(x => x.Status.ToLower() == filter.Status.ToLower());

            if (filter.Year.HasValue)
                query = query.Where(x =>
                    (x.PaymentDate != null && x.PaymentDate.Value.Year == filter.Year.Value));

            if (filter.Month.HasValue)
                query = query.Where(x =>
                    x.PaymentDate != null && x.PaymentDate.Value.Month == filter.Month.Value);

            if (filter.Day.HasValue)
                query = query.Where(x =>
                    x.PaymentDate != null && x.PaymentDate.Value.Day == filter.Day.Value);

            var rows = query
                .OrderBy(x => x.ClassName)
                .ThenBy(x => x.StudentName)
                .ToList()
                .Select(x => new FeeReportRow
                {
                    StudentName = x.StudentName,
                    ClassName = x.ClassName,
                    FeeType = x.FeeType,
                    Period = x.Period,
                    Amount = x.Amount,
                    Status = x.Status,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentMode,
                    ParentName = x.ParentName,
                    ContactNo = x.ContactNumber
                }).ToList();

            vm.Rows = rows;
            vm.TotalRecords = rows.Count;
            vm.PaidCount = rows.Count(r => r.Status?.ToLower() == "paid");
            vm.PendingCount = rows.Count(r => r.Status?.ToLower() != "paid");
            vm.TotalCollected = rows.Where(r => r.Status?.ToLower() == "paid").Sum(r => r.Amount);
            vm.TotalPending = rows.Where(r => r.Status?.ToLower() != "paid").Sum(r => r.Amount);

            return View(vm);
        }

        //================================================================ ManageNotification ==========================================================================

        public ActionResult ManageNotification()
        {
            var notifications = db.Notifications.OrderByDescending(n => n.CreatedDate).ToList();
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNotification(Notification model)
        {
            if (!ModelState.IsValid)
            {
                var list = db.Notifications
                             .OrderByDescending(n => n.CreatedDate)
                             .ToList();

                return View("ManageNotification", list);
            }

            model.CreatedDate = DateTime.Now;

            db.Notifications.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Notification sent successfully.";

            return RedirectToAction("ManageNotification");
        }

        public ActionResult DeleteNotification(int id)
        {
            var notification = db.Notifications.Find(id);
            if (notification != null)
            {
                db.Notifications.Remove(notification);
                db.SaveChanges();
            }
            TempData["Success"] = "Notification deleted successfully.";
            return RedirectToAction("ManageNotification");
        }


        //================================================================= Manage FAQS ===============================================================================
        public ActionResult ManageFAQs()
        {
            if (!IsAdmin()) return RedirectToLogin();
            var list = db.FAQs.OrderByDescending(f => f.AskedOn).ToList();
            return View(list);
        }

        //======================= Answer FAQ ==============================
        public ActionResult AnswerFAQ(int id)
        {
            var faq = db.FAQs.Find(id);
            if (faq == null) return HttpNotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AnswerFAQ(FAQ model)
        {
            var faq = db.FAQs.Find(model.FAQId);
            if (faq == null) return HttpNotFound();

            faq.Answer = model.Answer;
            faq.AnsweredOn = DateTime.Now;
            db.SaveChanges();

            return RedirectToAction("ManageFAQs");
        }


        // ========================================================================== Manage Feedback =========================================================

        // GET: Manage Feedback (All Parents' Feedback)
        public ActionResult ManageFeedback()
        {
            var feedbacks = db.Feedbacks
                              .OrderByDescending(f => f.CreatedAt)
                              .ToList();
            return View(feedbacks);
        }

        // GET: Reply Page
        public ActionResult Reply(int id)
        {
            var feedback = db.Feedbacks.Find(id);
            if (feedback == null) return HttpNotFound();
            return View(feedback);
        }

        // POST: Save Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(FormCollection form)
        {
            int id = int.Parse(form["FeedbackId"]);
            string reply = form["Reply"];

            System.Diagnostics.Debug.WriteLine($"FormCollection - ID: {id}, Reply: {reply}");

            if (string.IsNullOrWhiteSpace(reply))
            {
                TempData["Error"] = "Reply cannot be empty!";
                return RedirectToAction("Reply", new { id = id });
            }

            var feedback = db.Feedbacks.Find(id);
            if (feedback != null)
            {
                feedback.Reply = reply.Trim();
                feedback.RepliedAt = DateTime.Now;

                db.Entry(feedback).State = System.Data.Entity.EntityState.Modified;
                int result = db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"Rows affected: {result}");

                TempData["Success"] = "Reply submitted successfully!";
            }
            else
            {
                TempData["Error"] = "Feedback not found!";
            }

            return RedirectToAction("ManageFeedback");
        }

        // POST: Delete Feedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFeedback(int id)
        {
            try
            {
                var feedback = db.Feedbacks
                                .Include(f => f.Parent)  // ✅ Load Parent data
                                .FirstOrDefault(f => f.FeedbackId == id);

                if (feedback == null)
                {
                    TempData["Error"] = "Feedback not found!";
                    return RedirectToAction("ManageFeedback");
                }

                // Store parent name before deleting (in case we need it)
                string parentName = feedback.Parent != null
                    ? $"{feedback.Parent.ParentFirstName} {feedback.Parent.ParentLastName}"
                    : "Unknown Parent";

                db.Feedbacks.Remove(feedback);
                db.SaveChanges();

                TempData["Success"] = $"Feedback from {parentName} deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the feedback.";
                System.Diagnostics.Debug.WriteLine($"Delete Error: {ex.Message}");
            }

            return RedirectToAction("ManageFeedback");
        }
        // ================= PRIVATE HELPERS =================
        private bool IsAdmin()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Admin";
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Account");
        }

        private string SavePhoto(HttpPostedFileBase photo)
        {
            if (photo == null || photo.ContentLength <= 0)
                return null;

            var uploads = Server.MapPath("~/Content/uploads/staff/");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
            var path = Path.Combine(uploads, fileName);
            photo.SaveAs(path);
            return "/Content/uploads/staff/" + fileName;
        }

        private void DeletePhoto(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Server.MapPath(relativePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }



    }
}