using System.Linq;

namespace Cause.SecurityManagement
{
    public static class SecurityRoles
    {
        public const string ExternalSystem = "ExternalSystem";
        public const string User = "RegularUser";
        public const string UserCreation = "UserCreation";
        public const string UserRecovery = "UserRecovery";
        public const string UserPasswordSetup = "UserPasswordSetup";
        public const string UserLoginWithMultiFactor = "UserLoginWithMultiFactor";

        public const string UserAndUserCreation = User + "," + UserCreation;
        public const string UserAndUserRecovery = User + "," + UserRecovery;
        public const string UserAndRecoveryAndCreation = User + "," + UserCreation + "," + UserRecovery;
        
        internal static readonly string[] TemporaryRoles = [UserCreation, UserRecovery, UserPasswordSetup, UserLoginWithMultiFactor];

        public static bool IsTemporaryRole(string role)
        {
            return TemporaryRoles.Contains(role);
        }
    }
}
