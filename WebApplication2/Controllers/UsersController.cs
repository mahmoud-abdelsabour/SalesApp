using System.Web.Mvc;
using WebApplication2.Helpers;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class UsersController : AppController
    {
        private readonly UserService users;

        public UsersController()
        {
            users = new UserService(Db, new SessionService(Db));
        }

        // GET: Users
        public ActionResult Index()
        {
            return View(users.GetAll());
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

            var result = users.Create(model);
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int id)
        {
            var model = users.Get(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, UserViewModel model)
        {
            // The password isn't edited here
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
                return View(model);

            var result = users.Update(id, model, User.Identity.Name);
            if (result.IsNotFound)
                return HttpNotFound();
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            // The login cookie stores the username: reissue it so the current session keeps working
            if (result.Value)
            {
                var cookie = AuthCookieHelper.CreateRenamedCookie(Request, model.Username);
                if (cookie != null)
                    Response.Cookies.Add(cookie);
            }

            return RedirectWithSuccess();
        }

        // GET: Users/ResetPassword/5
        public ActionResult ResetPassword(int id)
        {
            var form = users.GetResetPasswordForm(id, User.Identity.Name);
            if (form.IsNotFound)
                return HttpNotFound();
            if (!form.Success)
            {
                TempData["Error"] = form.Error;
                return RedirectToAction("Index");
            }

            return View(form.Value);
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int id, ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = users.ResetPassword(id, model, User.Identity.Name);
            if (result.IsNotFound)
                return HttpNotFound();
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction("Index");
            }

            return RedirectWithSuccess();
        }

        // POST: Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleActive(int id)
        {
            var result = users.ToggleActive(id, User.Identity.Name, AuthCookieHelper.GetSessionToken(Request));
            if (!result.Success)
                return Json(new { success = false, message = result.Error });

            return Json(new
            {
                success = true,
                message = result.Message,
                isActive = result.Value.IsActive,
                signOut = result.Value.SignOut
            });
        }
    }
}
