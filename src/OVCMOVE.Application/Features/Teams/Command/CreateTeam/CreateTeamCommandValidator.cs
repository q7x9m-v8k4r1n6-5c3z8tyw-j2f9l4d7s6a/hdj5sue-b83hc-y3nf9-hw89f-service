using FluentValidation;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .NotEmpty().WithMessage("Team display name is required.")
            .MaximumLength(255);

        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("Team username is required.")
            .MaximumLength(100)
            .Matches("^[a-z0-9]+$")
            .WithMessage("Team username must contain only lowercase letters and digits.");

        RuleFor(command => command.LeaderEmail)
            .NotEmpty().WithMessage("Leader email is required.")
            .MaximumLength(255)
            .EmailAddress();
    }
}
