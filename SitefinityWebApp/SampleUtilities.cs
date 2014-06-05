using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Configuration;
using Telerik.Sitefinity.Security.Model;

namespace SitefinityWebApp
{
    public static class SampleUtilities
    {
        private const string DefaultUserName = "admin";
        private const string DefaultUserPassword = "password";
        private const string DefaultUserFirstName = "Admin";
        private const string DefaultUserLastName = "User";

        public static Guid CreateUsersAndRoles()
        {
            var userManager = UserManager.GetManager();
            var userId = Guid.Empty;
            using (new ElevatedModeRegion(userManager))
            {
                string username = SampleUtilities.DefaultUserName;
                string password = SampleUtilities.DefaultUserPassword;
                if (!userManager.UserExists(username))
                {
                    userManager.Provider.SuppressSecurityChecks = true;
                    MembershipCreateStatus status;
                    User user = userManager.CreateUser(username, password, "admin@sample.com", "test", "yes", true, null, out status);
                    if (status != MembershipCreateStatus.Success)
                    {
                        throw new InvalidOperationException("User cannot be created" + status.ToString());
                    }
                    userId = user.Id;
                    userManager.SaveChanges();
                    userManager.Provider.SuppressSecurityChecks = false;

                    CreateProfileForUser(username, SampleUtilities.DefaultUserFirstName, SampleUtilities.DefaultUserLastName);

                    var roleManager = RoleManager.GetManager("AppRoles");
                    using (new ElevatedModeRegion(roleManager))
                    {
                        roleManager.Provider.SuppressSecurityChecks = true;
                        Guid id;
                        foreach (var a in Config.Get<SecurityConfig>().ApplicationRoles.Keys)
                        {
                            var info = Config.Get<SecurityConfig>().ApplicationRoles[a];
                            id = info.Id;
                            var role = roleManager.GetRoles().FirstOrDefault(r => r.Id == id);
                            if (role == null)
                            {
                                roleManager.CreateRole(info.Id, info.Name);
                            }
                        }
                        roleManager.SaveChanges();

                        var adminRole = roleManager.GetRole("Administrators");
                        roleManager.AddUserToRole(user, adminRole);
                        roleManager.SaveChanges();
                        roleManager.Provider.SuppressSecurityChecks = false;
                    }
                }
            }
            return userId;
        }

        public static void CreateProfileForUser(string username, string firstName, string lastName)
        {
            UserManager userManager = UserManager.GetManager();
            using (new ElevatedModeRegion(userManager))
            {
                User user = userManager.GetUsers().SingleOrDefault(u => u.UserName == username);
                if (user != null)
                {
                    UserProfileManager profileManager = UserProfileManager.GetManager();
                    using (new ElevatedModeRegion(profileManager))
                    {
                        var userProfile = (SitefinityProfile)profileManager.GetUserProfile(user.Id, typeof(SitefinityProfile).FullName);
                        if (userProfile == null)
                        {
                            CreateSitefinityProfile(firstName, lastName, profileManager, user);
                        }
                    }
                }
            }
        }

        private static void CreateSitefinityProfile(string firstName, string lastName, UserProfileManager profileManager, User user)
        {
            var userProfile = profileManager.CreateProfile(user, user.Id, typeof(SitefinityProfile)) as SitefinityProfile;
            if (userProfile != null)
            {
                userProfile.FirstName = firstName;
                userProfile.LastName = lastName;
                profileManager.SaveChanges();
            }
        }
    }
}