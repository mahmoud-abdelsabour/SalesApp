using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Controllers
{
    public class AuthController : Controller
    {
        private AppDbContext db = new AppDbContext();

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var encryptedPassword = EncryptionHelper.Encrypt(model.Password);
            var user = db.Users.FirstOrDefault(u =>
                u.Username == model.Username &&
                u.PasswordEncrypted == encryptedPassword &&
                u.IsActive);

            if (user == null)
            {
                ModelState.AddModelError("", Resources.Labels.InvalidCredentials);
                return View(model);
            }

            // Clean up expired sessions (older than 3 hours)
            var expiry = DateTime.Now.AddHours(-3);
            var expiredSessions = db.ConcurrentSessions.Where(s => s.LastActivity < expiry).ToList();
            db.ConcurrentSessions.RemoveRange(expiredSessions);
            db.SaveChanges();

            // Check concurrent session
            var existingSession = db.ConcurrentSessions.FirstOrDefault(s => s.UserId == user.Id);
            if (existingSession != null)
            {
                ModelState.AddModelError("", Resources.Labels.AlreadyLoggedIn);
                return View(model);
            }

            // Create session token
            var sessionToken = Guid.NewGuid().ToString();
            var session = new ConcurrentSession
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                LoginTime = DateTime.Now,
                LastActivity = DateTime.Now,
                DeviceInfo = Request.UserAgent
            };
            db.ConcurrentSessions.Add(session);
            db.SaveChanges();

            // Set forms auth cookie with session token
            var ticket = new FormsAuthenticationTicket(
                1, user.Username, DateTime.Now,
                DateTime.Now.AddHours(3), false, sessionToken);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            Response.Cookies.Add(cookie);

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            RemoveCurrentSession();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public JsonResult KeepAlive()
        {
            var token = GetCurrentSessionToken();
            if (token != null)
            {
                var session = db.ConcurrentSessions.FirstOrDefault(s => s.SessionToken == token);
                if (session != null)
                {
                    session.LastActivity = DateTime.Now;
                    db.SaveChanges();
                }
            }
            return Json(new { success = true });
        }
        [AllowAnonymous]
        public ActionResult SetLanguage(string lang)
        {
            if (lang == "ar" || lang == "en")
                LanguageHelper.SetLanguage(lang);

            var returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home");
            return Redirect(returnUrl);
        }

        private void RemoveCurrentSession()
        {
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
        }

        private string GetCurrentSessionToken()
        {
            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null) return null;

            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                return ticket?.UserData;
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}