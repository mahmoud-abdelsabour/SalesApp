using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication2.Models.Data;

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

            var cookie = httpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null) return false;

            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                if (ticket == null || ticket.Expired) return false;

                var sessionToken = ticket.UserData;
                if (string.IsNullOrEmpty(sessionToken)) return false;

                using (var db = new AppDbContext())
                {
                    var session = db.ConcurrentSessions
                        .FirstOrDefault(s => s.SessionToken == sessionToken);

                    if (session == null) return false;

                    session.LastActivity = DateTime.Now;
                    db.SaveChanges();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Auth/Login");
        }
    }
}