using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Controllers
{
    /// <summary>
    /// Base for the logged-in area: requires login, owns the DbContext, and holds the
    /// small HTTP helpers every controller needs, so the controllers stay thin.
    /// </summary>
    [CustomAuthorize]
    public abstract class AppController : Controller
    {
        protected readonly AppDbContext Db = new AppDbContext();

        /// <summary>Feeds the shared _Pagination partial.</summary>
        protected void SetPaging<T>(PagedResult<T> page, object routeValues)
        {
            ViewBag.CurrentPage = page.Page;
            ViewBag.TotalPages = page.TotalPages;
            ViewBag.RouteValues = routeValues;
        }

        /// <summary>Shows a failed service result next to its form field (or as a general error).</summary>
        protected void AddModelError(ServiceResult result)
        {
            ModelState.AddModelError(result.Field ?? "", result.Error);
        }

        /// <summary>"Saved" flash message, then back to a list.</summary>
        protected ActionResult RedirectWithSuccess(string action = "Index")
        {
            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction(action);
        }

        /// <summary>The { success, message } JSON the list pages' AJAX buttons expect.</summary>
        protected JsonResult JsonOutcome(ServiceResult result)
        {
            return Json(new { success = result.Success, message = result.Success ? result.Message : result.Error });
        }

        /// <summary>Excel download for a table (all rows).</summary>
        protected ActionResult ExcelFile(TableReport table, string baseName)
        {
            var bytes = ExcelExportHelper.Build(table.Title, table.Columns, table.Rows);
            return File(bytes, ExcelExportHelper.ContentType, ExcelExportHelper.FileName(baseName));
        }

        /// <summary>Printable page for a table (the browser's "Save as PDF").</summary>
        protected ActionResult PrintableReport(TableReport table)
        {
            return View("~/Views/Shared/ListReport.cshtml", table);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}
