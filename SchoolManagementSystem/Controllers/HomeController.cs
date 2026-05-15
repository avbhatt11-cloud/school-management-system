using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Gallery()
        {
            var list = db.Gallery
                         .OrderByDescending(g => g.CreatedAt)
                         .ToList();
            return View(list);
        }
        // GET
        public ActionResult AskFAQ()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AskFAQ(FAQ faq)
        {
            if (!ModelState.IsValid)
            {
                return View(faq);
            }

            faq.AskedBy = "Visitor";
            faq.AskedOn = DateTime.Now;

            db.FAQs.Add(faq);
            db.SaveChanges();

            TempData["Success"] = "Question submitted successfully!";

            return RedirectToAction("AskFAQ");
        }

        public ActionResult MyQuestions()
        {
            return View();
        }

        [HttpPost]
        public ActionResult MyQuestions(string email)
        {
            var data = db.FAQs
                         .Where(x => x.Email == email)
                         .OrderByDescending(x => x.AskedOn)
                         .ToList();

            return View(data);
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult OurStaff()
        {
            var staffList = db.Staffs.Where(s => s.IsActive).ToList();
            return View(staffList);
        }
    }
}