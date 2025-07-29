using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;
using System.DirectoryServices;
using System.Security.Principal;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class UserProvider
    {
        public async Task<List<UserInfo>> GetUsersAsync()
        {
            return await Task.FromResult(GetUsers()).ConfigureAwait(false);
        }

        public List<UserInfo> GetUsers()
        {
            var users = new List<UserInfo>();

            try
            {
                // Try multiple methods to get user information
                users = GetUsersFromWmi();
                
                // If WMI fails, try alternative method
                if (users.Count == 0)
                {
                    users = GetUsersAlternative();
                }
            }
            catch (Exception ex)
            {
                // If all methods fail, return basic current user info
                users = GetCurrentUserInfo();
            }

            return users;
        }

        private List<UserInfo> GetUsersFromWmi()
        {
            var users = new List<UserInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
                {
                    foreach (ManagementObject user in searcher.Get())
                    {
                        var userInfo = new UserInfo
                        {
                            Name = user["Name"]?.ToString(),
                            FullName = user["FullName"]?.ToString(),
                            Description = user["Description"]?.ToString(),
                            IsActive = !Convert.ToBoolean(user["Disabled"] ?? false),
                            IsLocked = Convert.ToBoolean(user["Lockout"] ?? false),
                            HomeDirectory = null // Will be populated separately if needed
                        };

                        users.Add(userInfo);
                    }
                }
            }
            catch
            {
                // If this fails, we'll try alternative methods
            }

            return users;
        }

        private List<UserInfo> GetUsersAlternative()
        {
            var users = new List<UserInfo>();

            try
            {
                // Try using DirectoryEntry for local users
                using (var machine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer"))
                {
                    foreach (DirectoryEntry child in machine.Children)
                    {
                        if (child.SchemaClassName == "User")
                        {
                            var userInfo = new UserInfo
                            {
                                Name = child.Name,
                                FullName = child.Properties["FullName"]?.Value?.ToString(),
                                Description = child.Properties["Description"]?.Value?.ToString(),
                                IsActive = !Convert.ToBoolean(child.Properties["AccountDisabled"]?.Value ?? false),
                                IsLocked = Convert.ToBoolean(child.Properties["IsAccountLocked"]?.Value ?? false),
                                HomeDirectory = child.Properties["HomeDirectory"]?.Value?.ToString()
                            };

                            users.Add(userInfo);
                        }
                    }
                }
            }
            catch
            {
                // If this also fails, fall back to current user
            }

            return users;
        }

        private List<UserInfo> GetCurrentUserInfo()
        {
            var users = new List<UserInfo>();

            try
            {
                var currentUser = WindowsIdentity.GetCurrent();
                var userInfo = new UserInfo
                {
                    Name = currentUser.Name?.Split('\\').LastOrDefault() ?? Environment.UserName,
                    FullName = Environment.UserName,
                    Description = "Current logged-in user",
                    IsActive = true,
                    IsLocked = false,
                    HomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    LastLogin = DateTime.UtcNow // Approximate since they're currently logged in
                };

                users.Add(userInfo);
            }
            catch
            {
                // Last resort - minimal info
                users.Add(new UserInfo
                {
                    Name = Environment.UserName,
                    FullName = Environment.UserName,
                    Description = "System user",
                    IsActive = true,
                    IsLocked = false
                });
            }

            return users;
        }
    }
}