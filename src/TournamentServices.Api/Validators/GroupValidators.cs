using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
{
    public CreateGroupDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateGroupDtoValidator : AbstractValidator<UpdateGroupDto>
{
    public UpdateGroupDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class AssignTeamsDtoValidator : AbstractValidator<AssignTeamsDto>
{
    public AssignTeamsDtoValidator()
    {
        // Solo forma (400). Las reglas de negocio (repetidos, cupo,
        // equipos inexistentes) las revisa el Delegate y dan 422.
        RuleFor(x => x.TeamIds).NotEmpty();
        RuleForEach(x => x.TeamIds)
            .Must(IdFormat.IsValid)
            .WithMessage("Cada teamId debe tener un formato valido.");
    }
}
