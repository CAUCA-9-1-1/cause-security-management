namespace Cause.SecurityManagement
{
    public static class SecurityRoles
    {
        public const string ExternalSystem = "ExternalSystem";
        public const string User = "RegularUser";
        public const string UserCreation = "UserCreation";
        public const string UserRecovery = "UserRecovery";
        public const string UserPasswordSetup = "UserPasswordSetup";

        public const string UserAndUserCreation = User + "," + UserCreation;
        public const string UserAndUserRecovery = User + "," + UserRecovery;
        public const string UserAndRecoveryAndCreation = User + "," + UserCreation + "," + UserRecovery;
    }
}
