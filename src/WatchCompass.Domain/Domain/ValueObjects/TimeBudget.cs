namespace WatchCompass.Domain.ValueObjects;

public sealed record TimeBudget
{
    public int Minutes { get; }

    public TimeBudget(int minutes)
    {
        if (minutes < 1 || minutes > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Time budget must be between 1 and 600 minutes.");
        }

        Minutes = minutes;
    }
}
