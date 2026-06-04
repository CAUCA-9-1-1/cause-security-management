using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace Cause.SecurityManagement.Wolverine.Features.Management;

public class SaveGroupEndpoint
{
    [WolverinePost("/GroupManagement")]
    public static async Task<IResult> Handle(
        GroupDto group,
        IGroupManagementApiService groupService,
        IValidator<GroupDto> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(group, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        return Results.Ok(await groupService.SaveGroupAsync(group, cancellationToken));
    }
}
