namespace PickleCrew.Api.Statistics.Services;

internal static class CrewScoreCalculator
{
    private const long WinPoints = 100;

    public static long Calculate(
        bool won,
        long pointsScored,
        long pointsConceded)
    {
        if (!won)
        {
            return 0;
        }

        return WinPoints + pointsScored - pointsConceded;
    }
}
