using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class SaveGroupEndpoint
{
    [WolverinePost("/GroupManagement")]
    public static IResult Handle(
        GroupDto group,
        IGroupManagementApiService groupService,
        IValidator<GroupDto> validator)
    {
        var validation = validator.Validate(group);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        return Results.Ok(groupService.SaveGroup(group));
    }
}
