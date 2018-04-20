using System.Security.Principal;

namespace TechBrain
{
    public class AppEnvironment
    {
        public static bool HasAdminRights
        {
            get
            {
                var pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                var adminRights = pricipal.IsInRole(WindowsBuiltInRole.Administrator);
                return false;
            }
        }
    }
}
