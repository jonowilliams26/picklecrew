namespace PickleCrew.Api.Games.RecordGame;

internal static class GameScoreRules
{
    public static bool IsValidFinalScore(int firstScore, int secondScore)
    {
        if (firstScore < 0 || secondScore < 0 || firstScore == secondScore)
        {
            return false;
        }

        var winningScore = Math.Max(firstScore, secondScore);
        var losingScore = Math.Min(firstScore, secondScore);

        if (winningScore == 11)
        {
            return losingScore <= 9;
        }

        return losingScore >= 10 && winningScore - losingScore == 2;
    }
}
