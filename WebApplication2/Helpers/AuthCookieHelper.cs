using System;
using System.Web;
using System.Web.Security;

namespace WebApplication2.Helpers
{
    /// <summary>
    /// Reads and writes the forms-authentication cookie.
    /// The ticket's UserData holds the session token (see ConcurrentSessions).
    /// </summary>
    public static class AuthCookieHelper
    {
        public const int SessionHours = 3;

        /// <summary>The decrypted ticket, or null when there is no valid cookie.</summary>
        public static FormsAuthenticationTicket GetTicket(HttpRequestBase request)
        {
            var cookie = request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null) return null;

            try
            {
                return FormsAuthentication.Decrypt(cookie.Value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>The session token stored in the login cookie, or null.</summary>
        public static string GetSessionToken(HttpRequestBase request)
        {
            var ticket = GetTicket(request);
            return ticket == null ? null : ticket.UserData;
        }

        /// <summary>A new login cookie carrying the username and session token.</summary>
        public static HttpCookie CreateLoginCookie(string username, string sessionToken)
        {
            var ticket = new FormsAuthenticationTicket(
                1, username, DateTime.Now,
                DateTime.Now.AddHours(SessionHours), false, sessionToken);

            return new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
        }

        /// <summary>
        /// The current cookie re-issued under a new username (same session token and expiry),
        /// or null when there is no valid cookie.
        /// </summary>
        public static HttpCookie CreateRenamedCookie(HttpRequestBase request, string newUsername)
        {
            var old = GetTicket(request);
            if (old == null) return null;

            var ticket = new FormsAuthenticationTicket(
                old.Version, newUsername, old.IssueDate,
                old.Expiration, old.IsPersistent, old.UserData);

            return new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
        }
    }
}
