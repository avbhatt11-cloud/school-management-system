using System;
using System.Linq;
using System.Web.Mvc;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ======================= GET: Login =======================
        public ActionResult Login()
        {
            return View();
        }

        // ======================= POST: Login =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            // 🔒 Basic validation
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Username and Password are required.");
                return View(model);
            }

            string inputUsername = model.Username;
            string inputPassword = model.Password;

            // =========================================================
            // 🔍 STAFF LOGIN (CASE-SENSITIVE)
            // =========================================================
            var staff = db.Staffs
                .Where(s =>
                    s.Username.Equals(inputUsername, StringComparison.Ordinal) &&
                    s.Password.Equals(inputPassword, StringComparison.Ordinal))
                .FirstOrDefault();

            if (staff != null)
            {
                if (!staff.IsActive)
                {
                    ModelState.AddModelError("", "Your account is deactivated. Please contact Admin.");
                    return View(model);
                }

                Session["Role"] = "Staff";
                Session["StaffId"] = staff.Id;
                Session["Username"] = staff.Username;
                Session["ClassAssigned"] = staff.ClassAssigned;
                Session["LoginType"] = "Staff";

                return RedirectToAction("Index", "Staff");
            }

            // =========================================================
            // 🔍 PARENT LOGIN (CASE-SENSITIVE)
            // =========================================================
            var parent = db.Parents
                .Where(p =>
                    p.Username.Equals(inputUsername, StringComparison.Ordinal) &&
                    p.Password.Equals(inputPassword, StringComparison.Ordinal))
                .FirstOrDefault();

            if (parent != null)
            {
                if (!parent.IsActive)
                {
                    ModelState.AddModelError("", "Your account is deactivated. Please contact Admin.");
                    return View(model);
                }

                Session["Role"] = "Parent";
                Session["ParentId"] = parent.ParentId;
                Session["Username"] = parent.Username;
                Session["LoginType"] = "Parent";

                return RedirectToAction("Index", "Parent");
            }

            // =========================================================
            // ❌ NO MATCH FOUND
            // =========================================================
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        // ======================= Logout =======================
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
