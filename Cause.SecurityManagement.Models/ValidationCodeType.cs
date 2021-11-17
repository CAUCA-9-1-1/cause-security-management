namespace Cause.SecurityManagement.Models
{
    public enum ValidationCodeType
    {
		MultiFactorLogin = 0,
		AccountRecovery,
		AccountCreation,
		PasswordReset,
    }
}