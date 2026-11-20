using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    /// <summary>What happened when a user was activated / deactivated.</summary>
    public class ToggleActiveOutcome
    {
        public bool IsActive { get; set; }

        /// <summary>True when the user deactivated their own account (the page then logs them out).</summary>
        public bool SignOut { get; set; }
    }

    public class UserService
    {
        private const string OwnPasswordOnlyError = "You can only reset your own password.";

        private readonly AppDbContext db;
        private readonly SessionService sessions;

        public UserService(AppDbContext db, SessionService sessions)
        {
            this.db = db;
            this.sessions = sessions;
        }

        public List<UserViewModel> GetAll()
        {
            return db.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .OrderBy(u => u.Username)
                .ToList();
        }

        /// <summary>The user for editing (inactive users can be edited too), or null.</summary>
        public UserViewModel Get(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return null;

            return new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive
            };
        }

        public ServiceResult Create(UserViewModel model)
        {
            if (UsernameExists(model.Username, null))
                return ServiceResult.Fail(Resources.Labels.UsernameDuplicate, "Username");

            db.Users.Add(new User
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PasswordEncrypted = PasswordHelper.Hash(model.Password),
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        /// <summary>
        /// Updates a user. The result value is true when the signed-in user renamed their own account,
        /// so the caller can reissue the login cookie (it stores the username).
        /// </summary>
        public ServiceResult<bool> Update(int id, UserViewModel model, string currentUsername)
        {
            if (UsernameExists(model.Username, id))
                return ServiceResult<bool>.Fail(Resources.Labels.UsernameDuplicate, "Username");

            var user = db.Users.Find(id);
            if (user == null)
                return ServiceResult<bool>.NotFound();

            bool renamedSelf = user.Username == currentUsername && user.Username != model.Username;

            user.Username = model.Username;
            user.FullName = model.FullName;
            user.Email = model.Email;
            db.SaveChanges();

            return ServiceResult<bool>.Ok(renamedSelf);
        }

        /// <summary>The reset-password form, only for the signed-in user's own account.</summary>
        public ServiceResult<ResetPasswordViewModel> GetResetPasswordForm(int id, string currentUsername)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return ServiceResult<ResetPasswordViewModel>.NotFound();

            if (user.Username != currentUsername)
                return ServiceResult<ResetPasswordViewModel>.Fail(OwnPasswordOnlyError);

            return ServiceResult<ResetPasswordViewModel>.Ok(new ResetPasswordViewModel
            {
                Id = user.Id,
                Username = user.Username
            });
        }

        public ServiceResult ResetPassword(int id, ResetPasswordViewModel model, string currentUsername)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return ServiceResult.NotFound();

            if (user.Username != currentUsername)
                return ServiceResult.Fail(OwnPasswordOnlyError);

            user.PasswordEncrypted = PasswordHelper.Hash(model.NewPassword);
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        /// <summary>
        /// Activates or deactivates a user. The last active user can't be deactivated, and a
        /// deactivated user loses all sessions (the caller's own session too when they deactivate themselves).
        /// </summary>
        public ServiceResult<ToggleActiveOutcome> ToggleActive(int id, string currentUsername, string currentSessionToken)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return ServiceResult<ToggleActiveOutcome>.Fail("User not found.");

            if (user.IsActive && db.Users.Count(u => u.IsActive) <= 1)
                return ServiceResult<ToggleActiveOutcome>.Fail(Resources.Labels.CannotDeactivateLastAdmin);

            user.IsActive = !user.IsActive;

            // Deactivated users must be signed out everywhere (saved together with the status change)
            if (!user.IsActive)
                sessions.EndAllSessions(user.Id);

            db.SaveChanges();

            bool signOut = !user.IsActive && user.Username == currentUsername;
            if (signOut)
                sessions.EndSession(currentSessionToken);

            return ServiceResult<ToggleActiveOutcome>.Ok(
                new ToggleActiveOutcome { IsActive = user.IsActive, SignOut = signOut },
                user.IsActive ? Resources.Labels.SaveSuccess : Resources.Labels.UserDeactivated);
        }

        private bool UsernameExists(string username, int? excludeId)
        {
            return db.Users.Any(u =>
                u.Username.ToLower() == username.ToLower() &&
                (!excludeId.HasValue || u.Id != excludeId.Value));
        }
    }
}
