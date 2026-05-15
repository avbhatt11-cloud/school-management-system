using System;
using System.Linq;
using System.Web.Mvc;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    public class AdminAccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Login
        public ActionResult Login()
        {
            // Redirect if already logged in
            if (Session["Role"] != null && Session["Role"].ToString() == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Username and Password are required.");
                return View(model);
            }

            // DEBUG: Check what you're searching for
            var searchUsername = model.Username;
            var searchPassword = model.Password;

            // Put breakpoint here and check values
            var admin = db.Admins
    .Where(a =>
        a.Username.Equals(searchUsername, StringComparison.Ordinal) &&
        a.Password.Equals(searchPassword, StringComparison.Ordinal))
    .FirstOrDefault();


            if (admin != null)
            {
                Session["Role"] = "Admin";
                Session["Username"] = admin.Username;
                Session["AdminId"] = admin.Id;
                Session["LoginType"] = "Admin";
                return RedirectToAction("Index", "Admin");
            }

            ModelState.AddModelError("", "Invalid Admin credentials.");
            return View(model);
        }

        // Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "AdminAccount");
        }
    }
}