namespace Cause.SecurityManagement.Models.ValidationCode
{
    public enum ValidationCodeType
    {
		MultiFactorLogin = 0,
		AccountRecovery,
		AccountCreation,
		PasswordReset,
    }
}