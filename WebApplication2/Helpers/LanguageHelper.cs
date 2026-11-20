using System;
using System.Globalization;
using System.Threading;
using System.Web;

namespace WebApplication2.Helpers
{
    public static class LanguageHelper
    {
        private const string CookieName = "lang";

        public static void SetLanguage(string lang)
        {
            var cookie = new HttpCookie(CookieName, lang)
            {
                Expires = DateTime.Now.AddYears(1)
            };
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static string GetLanguage()
        {
            var cookie = HttpContext.Current.Request.Cookies[CookieName];
            return cookie?.Value ?? "en";
        }

        public static bool IsArabic()
        {
            return GetLanguage() == "ar";
        }

        /// <summary>Picks the English or Arabic text for the current language.</summary>
        public static string T(string en, string ar)
        {
            return IsArabic() ? ar : en;
        }

        public static void ApplyLanguage()
        {
            var lang = GetLanguage();
            var culture = new CultureInfo(lang == "ar" ? "ar-EG" : "en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}