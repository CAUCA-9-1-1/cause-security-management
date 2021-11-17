using Cause.SecurityManagement.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Services
{
    public class AuthenticationMultiFactorHandler<TUser> : IAuthenticationMultiFactorHandler<TUser>
        where TUser : User, new()
    {
        private readonly ISecurityContext<TUser> context;
        private readonly IAuthenticationValidationCodeSender sender;

        public AuthenticationMultiFactorHandler(
            ISecurityContext<TUser> context,
            IAuthenticationValidationCodeSender sender = null)
        {
            this.context = context;
            this.sender = sender;
        }

        public async Task SendValidationCodeWhenNeededAsync(TUser user)
        {
            if (MustSendValidationCode(user))
            {
                DeleteExistingValidationCode(user.Id, ValidationCodeType.MultiFactorLogin);
                await SendValidationCodeAsync(user, ValidationCodeType.MultiFactorLogin);
            }
        }

        private static bool MustSendValidationCode(TUser userFound)
        {
            return userFound != null
                && !userFound.PasswordMustBeResetAfterLogin
                && SecurityManagementOptions.MultiFactorAuthenticationIsActivated;
        }

        private void DeleteExistingValidationCode(Guid idUser, ValidationCodeType type)
        {
            var existingCode = context.UserValidationCodes
                .Where(code => code.IdUser == idUser && code.Type == type).ToList();
            context.UserValidationCodes.RemoveRange(existingCode);
            context.SaveChanges();
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
            context.UserValidationCodes.Add(code);
            context.SaveChanges();
            await sender.SendCodeAsync(user.Email, code.Code);
        }

        private static string GenerateValidationCode()
        {
            Random generator = new();
            return generator.Next(0, 1000000).ToString("D6");
        }

        public virtual bool CodeIsValid(Guid idUser, string validationCode, ValidationCodeType type)
        {
            var code = context.UserValidationCodes
                .Where(code => code.IdUser == idUser && code.ExpiresOn >= DateTime.Now && code.Code == validationCode && code.Type == type)
                .FirstOrDefault();

            if (code != null)
            {
                context.UserValidationCodes.Remove(code);
                context.SaveChanges();
                return true;
            }
            return false;
        }
    }
}