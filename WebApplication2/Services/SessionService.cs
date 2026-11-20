using System;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;

namespace WebApplication2.Services
{
    public enum LoginStatus
    {
        Success,
        InvalidCredentials,
        AlreadyLoggedIn
    }

    public class LoginResult
    {
        public LoginStatus Status { get; set; }
        public string Username { get; set; }
        public string SessionToken { get; set; }
    }

    /// <summary>
    /// Login and single-session control: each user may have one active session (ConcurrentSessions row).
    /// </summary>
    public class SessionService
    {
        private readonly AppDbContext db;

        public SessionService(AppDbContext db)
        {
            this.db = db;
        }

        /// <summary>
        /// Checks the credentials and opens a session. When the user already has a session,
        /// it is ended only if <paramref name="takeOver"/> is true; otherwise AlreadyLoggedIn is returned.
        /// </summary>
        public LoginResult Login(string username, string password, bool takeOver, string deviceInfo)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user == null || !PasswordHelper.Verify(password, user.PasswordEncrypted))
                return new LoginResult { Status = LoginStatus.InvalidCredentials };

            // Transparently upgrade legacy 3DES passwords to PBKDF2
            if (PasswordHelper.NeedsUpgrade(user.PasswordEncrypted))
            {
                user.PasswordEncrypted = PasswordHelper.Hash(password);
                db.SaveChanges();
            }

            RemoveExpiredSessions();

            var existingSessions = db.ConcurrentSessions.Where(s => s.UserId == user.Id).ToList();
            if (existingSessions.Any())
            {
                if (!takeOver)
                    return new LoginResult { Status = LoginStatus.AlreadyLoggedIn };

                // Take over: end the old session(s); the old browser is logged out on its next request
                db.ConcurrentSessions.RemoveRange(existingSessions);
                db.SaveChanges();
            }

            var sessionToken = Guid.NewGuid().ToString();
            db.ConcurrentSessions.Add(new ConcurrentSession
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                LoginTime = DateTime.Now,
                LastActivity = DateTime.Now,
                DeviceInfo = deviceInfo
            });
            db.SaveChanges();

            return new LoginResult { Status = LoginStatus.Success, Username = user.Username, SessionToken = sessionToken };
        }

        /// <summary>
        /// True when the token belongs to an active session; also records the activity time.
        /// Used on every authorized request and by the idle-warning "keep alive".
        /// </summary>
        public bool ValidateAndTouch(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken)) return false;

            var session = db.ConcurrentSessions.FirstOrDefault(s => s.SessionToken == sessionToken);
            if (session == null) return false;

            session.LastActivity = DateTime.Now;
            db.SaveChanges();
            return true;
        }

        /// <summary>Ends one session (logout).</summary>
        public void EndSession(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken)) return;

            var session = db.ConcurrentSessions.FirstOrDefault(s => s.SessionToken == sessionToken);
            if (session != null)
            {
                db.ConcurrentSessions.Remove(session);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Ends every session of a user (e.g. when the user is deactivated).
        /// Saves together with any other pending changes on the same context.
        /// </summary>
        public void EndAllSessions(int userId)
        {
            var sessions = db.ConcurrentSessions.Where(s => s.UserId == userId).ToList();
            db.ConcurrentSessions.RemoveRange(sessions);
            db.SaveChanges();
        }

        private void RemoveExpiredSessions()
        {
            var expiry = DateTime.Now.AddHours(-AuthCookieHelper.SessionHours);
            var expired = db.ConcurrentSessions.Where(s => s.LastActivity < expiry).ToList();
            db.ConcurrentSessions.RemoveRange(expired);
            db.SaveChanges();
        }
    }
}
