using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class FormatInputDtoValidator : AbstractValidator<FormatInputDto>
{
    public FormatInputDtoValidator()
    {
        RuleFor(x => x.MaxTeamsPerGroup).GreaterThan(0);
        RuleFor(x => x.NumberOfGroups).GreaterThan(0);
        RuleFor(x => x.Type).NotNull().IsInEnum();
    }
}

public class CreateTournamentDtoValidator : AbstractValidator<CreateTournamentDto>
{
    public CreateTournamentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Format).NotNull().SetValidator(new FormatInputDtoValidator());
    }
}

public class UpdateTournamentDtoValidator : AbstractValidator<UpdateTournamentDto>
{
    public UpdateTournamentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Format).NotNull().SetValidator(new FormatInputDtoValidator());
    }
}

public class PatchTournamentDtoValidator : AbstractValidator<PatchTournamentDto>
{
    public PatchTournamentDtoValidator()
    {
        // Solo se valida lo que si viene en el body.
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);

        When(x => x.Format is not null, () =>
        {
            RuleFor(x => x.Format!.MaxTeamsPerGroup).GreaterThan(0).When(x => x.Format!.MaxTeamsPerGroup is not null);
            RuleFor(x => x.Format!.NumberOfGroups).GreaterThan(0).When(x => x.Format!.NumberOfGroups is not null);
            RuleFor(x => x.Format!.Type).IsInEnum().When(x => x.Format!.Type is not null);
        });
    }
}
