using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class AuthenticationMultiFactorHandler<TUser> : IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        private readonly IUserValidationCodeRepository repository;
        private readonly IAuthenticationValidationCodeSender<TUser> sender;

        public AuthenticationMultiFactorHandler(
            IUserValidationCodeRepository repository,
            IAuthenticationValidationCodeSender<TUser> sender = null)
        {
            this.repository = repository;
            this.sender = sender;
        }

        public async Task SendValidationCodeWhenNeededAsync(TUser user)
        {
            if (MustSendValidationCode(user))
            {
                repository.DeleteExistingValidationCode(user.Id);
                await SendValidationCodeAsync(user, ValidationCodeType.MultiFactorLogin);
            }
        }

        private static bool MustSendValidationCode(TUser userFound)
        {
            return userFound != null
                && !userFound.PasswordMustBeResetAfterLogin
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
            await sender.SendCodeAsync(user, code.Code, code.ExpiresOn);
        }

        private static string GenerateValidationCode()
        {
            Random generator = new();
            return generator.Next(0, 1000000).ToString("D6");
        }

        public virtual bool CodeIsValid(Guid idUser, string validationCode, ValidationCodeType type)
        {
            var code = repository.GetExistingValidCode(idUser, validationCode, type);

            if (code != null)
            {
                repository.DeleteCode(code);
                return true;
            }
            return false;
        }

        public async Task SendNewValidationCodeAsync(TUser user)
        {
            var existingCode = repository.GetLastCode(user.Id);
            ThrowExceptionIfNoCodeHasBeenFound(existingCode);
            repository.DeleteExistingValidationCode(user.Id);
            await SendValidationCodeAsync(user, existingCode.Type);
        }

        private static void ThrowExceptionIfNoCodeHasBeenFound(UserValidationCode existingCode)
        {
            if (existingCode == null)
                throw new UserValidationCodeNotFoundException();
        }
    }
}