using System.Web.Mvc;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    /// <summary>The one place for application-wide options.</summary>
    public class SettingsController : AppController
    {
        private readonly SettingsService settings;

        public SettingsController()
        {
            settings = new SettingsService(Db);
        }

        // GET: Settings
        public ActionResult Index()
        {
            return View(settings.GetAll());
        }

        // POST: Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(SettingsViewModel model)
        {
            var result = settings.Save(model);
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Index");
        }
    }
}
