using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Core.Services;

namespace Cause.SecurityManagement.Integration.Tests.TestData;

/// <summary>
/// Fluent builder for TestUser test data. Persists the user to the given context.
/// </summary>
public class UserBuilder
{
    private readonly TestSecurityContext _context;
    private readonly string _packageName;
    private string _userName = $"user_{Guid.NewGuid():N}";
    private string _password = "Password123!";
    private string _email = $"{Guid.NewGuid():N}@test.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private bool _isActive = true;
    private bool _passwordMustReset = false;
    private bool _twoFactorEnabled = false;
    private bool _useRawPassword = false;

    public UserBuilder(TestSecurityContext context, string packageName)
    {
        _context = context;
        _packageName = packageName;
    }

    public UserBuilder WithUserName(string userName) { _userName = userName; return this; }
    public UserBuilder WithPassword(string password) { _password = password; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder IsInactive() { _isActive = false; return this; }
    public UserBuilder WithPasswordMustReset() { _passwordMustReset = true; return this; }
    public UserBuilder WithTwoFactorEnabled() { _twoFactorEnabled = true; return this; }

    /// <summary>Stores the password as-is (unencoded) — used for temporary-password tests.</summary>
    public UserBuilder WithRawPassword() { _useRawPassword = true; return this; }

    public TestUser Build()
    {
        var storedPassword = _useRawPassword
            ? _password
            : new PasswordGenerator().EncodePassword(_password, _packageName);

        var user = new TestUser
        {
            UserName = _userName,
            Password = storedPassword,
            Email = _email,
            FirstName = _firstName,
            LastName = _lastName,
            IsActive = _isActive,
            PasswordMustBeResetAfterLogin = _passwordMustReset,
            TwoFactorAuthenticatorEnabled = _twoFactorEnabled,
        };

        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    /// <summary>Returns the plain-text password used to build this user.</summary>
    public string PlainPassword => _password;
}
