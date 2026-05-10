using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Controllers
{
    [CustomAuthorize]
    public class UsersController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Users
        public ActionResult Index()
        {
            var users = db.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .OrderBy(u => u.Username)
                .ToList();

            return View(users);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View(new UserViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
                ModelState.AddModelError("Password", "Password is required");

            if (!ModelState.IsValid)
                return View(model);

            // Check username uniqueness
            bool exists = db.Users.Any(u => u.Username.ToLower() == model.Username.ToLower());
            if (exists)
            {
                ModelState.AddModelError("Username", Resources.Labels.UsernameDuplicate);
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PasswordEncrypted = EncryptionHelper.Encrypt(model.Password),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            var model = new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, UserViewModel model)
        {
            // Remove password validation for edit
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
                return View(model);

            // Check username uniqueness excluding self
            bool exists = db.Users.Any(u =>
                u.Username.ToLower() == model.Username.ToLower() && u.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Username", Resources.Labels.UsernameDuplicate);
                return View(model);
            }

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            user.Username = model.Username;
            user.FullName = model.FullName;
            user.Email = model.Email;

            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // GET: Users/ResetPassword/5
        public ActionResult ResetPassword(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(new ResetPasswordViewModel
            {
                Id = user.Id,
                Username = user.Username
            });
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int id, ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            user.PasswordEncrypted = EncryptionHelper.Encrypt(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // POST: Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleActive(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            // Cannot deactivate last active user
            if (user.IsActive)
            {
                int activeCount = db.Users.Count(u => u.IsActive);
                if (activeCount <= 1)
                    return Json(new { success = false, message = Resources.Labels.CannotDeactivateLastAdmin });
            }

            user.IsActive = !user.IsActive;
            db.SaveChanges();

            string message = user.IsActive ? Resources.Labels.SaveSuccess : Resources.Labels.UserDeactivated;

            // If user deactivated themselves, sign them out
            if (!user.IsActive && user.Username == User.Identity.Name)
            {
                // Remove concurrent session
                var token = GetCurrentSessionToken();
                if (token != null)
                {
                    var session = db.ConcurrentSessions.FirstOrDefault(s => s.SessionToken == token);
                    if (session != null)
                    {
                        db.ConcurrentSessions.Remove(session);
                        db.SaveChanges();
                    }
                }
                return Json(new { success = true, message, isActive = user.IsActive, signOut = true });
            }

            return Json(new { success = true, message, isActive = user.IsActive, signOut = false });
        }
        private string GetCurrentSessionToken()
        {
            var cookie = Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName];
            if (cookie == null) return null;
            try
            {
                var ticket = System.Web.Security.FormsAuthentication.Decrypt(cookie.Value);
                return ticket?.UserData;
            }
            catch { return null; }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}