using System.Management;
using System.Runtime.Versioning;
using HardwareVault.Core.Models;
using System.Security.Principal;

namespace HardwareVault.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class ActiveUserProvider
    {
        public async Task<ActiveUserInfo> GetActiveUserAsync()
        {
            return await Task.FromResult(GetActiveUser()).ConfigureAwait(false);
        }

        public ActiveUserInfo GetActiveUser()
        {
            var activeUser = new ActiveUserInfo();

            try
            {
                var currentUser = WindowsIdentity.GetCurrent();
                var userName = currentUser.Name;
                
                if (userName.Contains('\\'))
                {
                    var parts = userName.Split('\\');
                    activeUser.Domain = parts[0];
                    activeUser.Name = parts[1];
                }
                else
                {
                    activeUser.Name = userName;
                    activeUser.Domain = Environment.MachineName;
                }

                activeUser.FullName = Environment.UserName;
                activeUser.HomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                activeUser.SessionType = Environment.UserInteractive ? "Interactive" : "Service";

                // Try to get login time from WMI
                try
                {
                    using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_LogonSession WHERE LogonType=2"))
                    {
                        foreach (ManagementObject session in searcher.Get())
                        {
                            var startTime = session["StartTime"];
                            if (startTime != null)
                            {
                                activeUser.LoginTime = ManagementDateTimeConverter.ToDateTime(startTime.ToString());
                                break; // Get the first interactive session
                            }
                        }
                    }
                }
                catch
                {
                    // If we can't get login time, use system boot time as approximation
                    activeUser.LoginTime = DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(Environment.TickCount));
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve active user information: {ex.Message}", ex);
            }

            return activeUser;
        }
    }
}