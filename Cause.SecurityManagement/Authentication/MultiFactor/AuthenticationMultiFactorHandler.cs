using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Authentication.MultiFactor
{
    public class AuthenticationMultiFactorHandler<TUser> : IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        private readonly IUserValidationCodeRepository repository;
        private readonly IAuthenticationValidationCodeSender<TUser> sender;
        private readonly IAuthenticationValidationCodeValidator<TUser> validator;

        public AuthenticationMultiFactorHandler(
            IUserValidationCodeRepository repository,
            IAuthenticationValidationCodeSender<TUser> sender = null,
            IAuthenticationValidationCodeValidator<TUser> validator = null)
        {
            this.repository = repository;
            this.sender = sender;
            this.validator = validator;
        }

        public async Task SendValidationCodeWhenNeededAsync(TUser user)
        {
            if (MustSendValidationCode(user))
            {
                if (!await SendWithExternalSenderWhenExternalValidatorIsActive(user))
                {
                    repository.DeleteExistingValidationCode(user.Id);
                    await SendValidationCodeAsync(user, ValidationCodeType.MultiFactorLogin);
                }
            }
        }

        private static bool MustSendValidationCode(TUser userFound)
        {
            return userFound != null
                && !userFound.PasswordMustBeResetAfterLogin
                && userFound.TwoFactorAuthenticatorEnabled
                && SecurityManagementOptions.MultiFactorAuthenticationIsActivated;
        }

        private async Task SendValidationCodeAsync(TUser user, ValidationCodeType type)
        {
            var code = new UserValidationCode
            {
                Code = GenerateValidationCode(),
                ExpiresOn = DateTime.Now.AddMinutes(5),
                IdUser = user.Id,
                Type = type
            };
            repository.SaveNewValidationCode(code);

            if (sender == null) throw new AuthenticationValidationCodeSenderNotFoundException();
            await sender.SendCodeAsync(user, code.Code, code.ExpiresOn);
        }

        private static string GenerateValidationCode()
        {
            Random generator = new();
            return generator.Next(0, 1000000).ToString("D6");
        }

        public virtual async Task<bool> CodeIsValidAsync(TUser user, string validationCode, ValidationCodeType type)
        {
            if (validator != null)
            {
                return await validator.CodeIsValidAsync(user, validationCode);
            }

            var code = repository.GetExistingValidCode(user.Id, validationCode, type);
            if (code != null)
            {
                repository.DeleteCode(code);
                return true;
            }
            return false;
        }

        public async Task SendNewValidationCodeAsync(TUser user)
        {
            if (!await SendWithExternalSenderWhenExternalValidatorIsActive(user))
            {
                var existingCode = user != null ? repository.GetLastCode(user.Id) : null;
                ThrowExceptionIfNoCodeHasBeenFound(existingCode);
                repository.DeleteExistingValidationCode(user.Id);
                await SendValidationCodeAsync(user, existingCode.Type);
            }
        }

        private async Task<bool> SendWithExternalSenderWhenExternalValidatorIsActive(TUser user)
        {
            if (sender != null && validator != null)
            {
                await sender.SendCodeAsync(user);
                return true;
            }

            return false;
        }

        private static void ThrowExceptionIfNoCodeHasBeenFound(UserValidationCode existingCode)
        {
            if (existingCode == null)
                throw new UserValidationCodeNotFoundException();
        }
    }
}