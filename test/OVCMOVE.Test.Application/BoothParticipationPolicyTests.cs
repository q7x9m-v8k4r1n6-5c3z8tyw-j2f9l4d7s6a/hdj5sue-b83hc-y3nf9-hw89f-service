using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public class BoothParticipationPolicyTests
{
    [Fact]
    public void RegularBooth_AllowsEligibleTeam()
    {
        var error = BoothParticipationPolicy.GetEntryError(
            new Booth { IsHidden = false },
            Progress(regular: 0, hidden: 0));

        Assert.Null(error);
    }

    [Fact]
    public void CompletedBooth_IsRejected()
    {
        var error = BoothParticipationPolicy.GetEntryError(
            new Booth(),
            Progress(regular: 2, hidden: 0, completed: true));

        Assert.Equal(BoothParticipationPolicy.ChooseAnotherBoothMessage, error);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(2, 0, true)]
    [InlineData(2, 1, false)]
    [InlineData(4, 1, true)]
    [InlineData(4, 2, false)]
    [InlineData(6, 2, true)]
    [InlineData(6, 3, false)]
    public void HiddenBooth_AppliesTwoRegularForOneHiddenQuota(
        int regular,
        int hidden,
        bool expectedAllowed)
    {
        var error = BoothParticipationPolicy.GetEntryError(
            new Booth { IsHidden = true },
            Progress(regular, hidden));

        Assert.Equal(expectedAllowed, error is null);
    }

    [Fact]
    public void TeamOutsideRace_IsRejected()
    {
        var error = BoothParticipationPolicy.GetEntryError(
            new Booth(),
            Progress(0, 0, isTeamInRace: false));

        Assert.Equal("Đội không tham gia giải đua của trạm này.", error);
    }

    private static BoothProgressResultModel Progress(
        int regular,
        int hidden,
        bool completed = false,
        bool isTeamInRace = true) =>
        new()
        {
            IsTeamInRace = isTeamInRace,
            HasCompletedBooth = completed,
            CompletedRegularBooths = regular,
            CompletedHiddenBooths = hidden
        };
}
