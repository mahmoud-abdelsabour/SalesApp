using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext db = new AppDbContext();
        private readonly SessionService sessions;

        public AuthController()
        {
            sessions = new SessionService(db);
        }

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

            var result = sessions.Login(model.Username, model.Password, model.TakeOver, Request.UserAgent);

            switch (result.Status)
            {
                case LoginStatus.InvalidCredentials:
                    ModelState.AddModelError("", Resources.Labels.InvalidCredentials);
                    return View(model);

                case LoginStatus.AlreadyLoggedIn:
                    // Ask the user to confirm ending the other session
                    ModelState.AddModelError("", Resources.Labels.AlreadyLoggedIn);
                    ViewBag.SessionConflict = true;
                    ModelState.Remove("TakeOver");
                    return View(model);
            }

            Response.Cookies.Add(AuthCookieHelper.CreateLoginCookie(result.Username, result.SessionToken));
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            sessions.EndSession(AuthCookieHelper.GetSessionToken(Request));
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public JsonResult KeepAlive()
        {
            sessions.ValidateAndTouch(AuthCookieHelper.GetSessionToken(Request));
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
