using FluentValidation;
using System;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommandValidator : AbstractValidator<SubmitBoothScoreCommand>
{
    public SubmitBoothScoreCommandValidator()
    {
        RuleFor(x => x.BoothID)
            .NotEmpty()
            .WithMessage("ID Trạm thi đấu không được để trống.");

        RuleFor(x => x.TeamID)
            .NotEmpty()
            .WithMessage("ID Đội chơi không được để trống.");

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 100)
            .WithMessage("Số điểm cộng mỗi lần phải nằm trong khoảng từ 1 đến 100 điểm.");
    }
}