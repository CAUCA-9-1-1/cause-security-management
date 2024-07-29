using System;
using System.Collections.Generic;

namespace Cause.SecurityManagement.Models;

public class User : BaseModel, IAuthenticableEntity
{		
    public virtual string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }
    public virtual string Email { get; set; }

    public bool IsActive { get; set; } = true;
    public bool PasswordMustBeResetAfterLogin { get; set; }
    public bool TwoFactorAuthenticatorEnabled { get; set; } = true;

    public ICollection<UserGroup> Groups { get; set; } = new List<UserGroup>();
    public ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
    public ICollection<UserToken> Tokens { get; set; }
    public ICollection<UserValidationCode> ValidationCodes { get; set; }
}

public interface IAuthenticableEntity
{
    Guid Id { get; set; }
    string UserName { get; set; }
    string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    bool IsActive { get; set; }
    bool PasswordMustBeResetAfterLogin { get; set; }
    bool TwoFactorAuthenticatorEnabled { get; set; }
}