using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Services;

namespace WebApplication2.Filters
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Respect [AllowAnonymous]
            var actionDescriptor = httpContext.Items["ActionDescriptor"] as ActionDescriptor;
            if (actionDescriptor != null)
            {
                bool isAnonymous = actionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                                   actionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true);
                if (isAnonymous) return true;
            }

            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            var ticket = AuthCookieHelper.GetTicket(httpContext.Request);
            if (ticket == null || ticket.Expired) return false;

            try
            {
                // The cookie is only valid while its session row exists (single-session control)
                using (var db = new AppDbContext())
                {
                    return new SessionService(db).ValidateAndTouch(ticket.UserData);
                }
            }
            catch
            {
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Drop a stale login cookie (e.g. its session was taken over or expired);
            // otherwise Login would see an "authenticated" user and redirect back here forever.
            FormsAuthentication.SignOut();
            filterContext.Result = new RedirectResult("~/Auth/Login");
        }
    }
}