/// <summary>Stats of the current/last run, carried from the game scene to the win screen.</summary>
public static class RunStats
{
    public static float Seconds;
    public static int Moves;
    public static void Reset() { Seconds = 0f; Moves = 0; }
}
