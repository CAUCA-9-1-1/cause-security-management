using System;
using System.Threading.Tasks;
using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Services;

public abstract class BaseAuthenticator<TEntity, TEntityToken>(
    ICurrentUserService currentUserService,
    IAuthenticableEntityRepository<TEntity> repository,
    IAuthenticationMultiFactorHandler<TEntity> multiFactorHandler,
    IEntityTokenGenerator<TEntityToken> entityTokenGenerator,
    IOptions<SecurityConfiguration> configuration
) : IEntityAuthenticator
    where TEntity : class, IAuthenticableEntity
    where TEntityToken : BaseToken
{
    private readonly SecurityConfiguration configuration = configuration.Value;

    public virtual async Task<LoginResult> LoginAsync(string userName, string password)
    {
        var (entityFound, roles) = GetEntityWithTemporaryPassword(userName, password) ?? GetEntity(userName, password);
        var (token, entity) = await GenerateTokenIfEntityCanLogInAsync(entityFound, roles);
        if (entity == null || token == null)
            return null;
        return new LoginResult
        {
            IdUser = entity.Id,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            AuthorizationType = "Bearer",
            ExpiredOn = token.ExpiresOn,
            MustVerifyCode = MustValidateCode(entity),
            MustChangePassword = entity.PasswordMustBeResetAfterLogin,
            Name = entity.FirstName + " " + entity.LastName,
        };
    }

    protected virtual (TEntity entity, string role)? GetEntityWithTemporaryPassword(string userName, string password)
    {
        var tempUser = repository.GetEntityWithTemporaryPassword(userName, password);
        if (tempUser != null && CanLogIn(tempUser))
        {
            return (tempUser, SecurityRoles.UserPasswordSetup);
        }
        return null;
    }

    protected virtual async Task<(TEntityToken token, TEntity entity)> GenerateTokenIfEntityCanLogInAsync(TEntity entityFound, string role)
    {
        var entity = CanLogIn(entityFound) ? (await entityTokenGenerator.GenerateEntityTokenAsync(entityFound, role), entityFound) : (null, null);
        await multiFactorHandler.SendValidationCodeWhenNeededAsync(entity.entityFound);
        return entity;
    }

    public async Task<LoginResult> ValidateMultiFactorCodeAsync(ValidationInformation validationInformation)
    {
        var (entityFound, roles) = GetEntity(currentUserService.GetUserId());
        if (entityFound != null && await multiFactorHandler.CodeIsValidAsync(entityFound, validationInformation.ValidationCode, ValidationCodeType.MultiFactorLogin))
        {
            var (token, entity) = (await entityTokenGenerator.GenerateEntityTokenAsync(entityFound, roles), entityFound);
            if (token == null)
                return null;
            return new LoginResult
            {
                IdUser = entity.Id,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                AuthorizationType = "Bearer",
                ExpiredOn = token.ExpiresOn,
                MustVerifyCode = false,
                MustChangePassword = entity.PasswordMustBeResetAfterLogin,
                Name = entity.FirstName + " " + entity.LastName,
            };
        }
        throw new InvalidValidationCodeException($"Validation code {validationInformation.ValidationCode} is invalid for this entity.");
    }

    public virtual async Task<BaseToken> GenerateRecoveryTokenAsync(Guid entityId)
    {
        var entityFound = repository.GetEntityById(entityId);
        if (entityFound != null)
        {
            return await entityTokenGenerator.GenerateEntityTokenAsync(entityFound, SecurityRoles.UserRecovery);
        }
        return null;
    }

    protected virtual (TEntity entity, string rolesToGive) GetEntity(Guid entityId)
    {
        var entityFound = repository.GetEntityById(entityId);
        return (entityFound, GetSecurityRole(entityFound));
    }

    protected virtual (TEntity user, string rolesToGive) GetEntity(string userName, string password)
    {
        var encodedPassword = new PasswordGenerator().EncodePassword(password, configuration.PackageName);
        var entityFound = repository.GetEntity(userName, encodedPassword);
        if (entityFound == null)
            return (null, null);
        return (entityFound, GetRoleFromUser(entityFound));
    }

    private string GetRoleFromUser(TEntity entity)
    {
        if (entity.PasswordMustBeResetAfterLogin)
            return SecurityRoles.UserPasswordSetup;

        return MustValidateCode(entity)
            ? SecurityRoles.UserLoginWithMultiFactor
            : GetSecurityRole(entity);
    }

    private static string GetSecurityRole(TEntity userFound)
    {
        return userFound.PasswordMustBeResetAfterLogin ?
            SecurityRoles.UserPasswordSetup :
            SecurityRoles.User;
    }

    public virtual bool MustValidateCode(TEntity entity)
    {
        return SecurityManagementOptions.MultiFactorAuthenticationIsActivated && entity.TwoFactorAuthenticatorEnabled;
    }

    public bool IsLoggedIn(string refreshToken)
    {
        return repository.HasToken(currentUserService.GetUserId(), refreshToken);
    }

    protected virtual bool CanLogIn(TEntity entity)
    {
        return entity != null && HasRequiredPermissionToLogIn(entity);
    }

    protected virtual bool HasRequiredPermissionToLogIn(TEntity entity)
    {
        return true;
    }

    public async Task SendNewCodeAsync()
    {
        var user = repository.GetEntityById(currentUserService.GetUserId());
        await multiFactorHandler.SendNewValidationCodeAsync(user);
    }
}