public static class GameSettings
{
    public static Difficulty AIDifficulty = Difficulty.Medium;
    public static bool FromMenu = false;      // set true by the menu when BEGIN DUEL is pressed
    public static bool Player1IsAI = false;
    public static bool Player2IsAI = true;
    public static bool SinglePlayer => FromMenu && (Player1IsAI != Player2IsAI);
}