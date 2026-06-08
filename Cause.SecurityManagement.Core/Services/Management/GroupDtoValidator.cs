using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public class GroupDtoValidator : AbstractValidator<GroupDto>
    {
        public GroupDtoValidator()
        {
            RuleFor(group => group.Id).NotEmpty();
            RuleFor(group => group.Name).NotEmpty().MaximumLength(100);
            RuleForEach(group => group.Permissions).SetValidator(new GroupPermissionDtoValidator());
        }
    }

    public class GroupPermissionDtoValidator : AbstractValidator<GroupPermissionDto>
    {
        public GroupPermissionDtoValidator()
        {
            RuleFor(permission => permission.Id).NotEmpty();
            RuleFor(permission => permission.IdModulePermission).NotEmpty();
        }
    }
}
