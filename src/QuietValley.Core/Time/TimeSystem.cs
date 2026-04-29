namespace QuietValley.Core.Time;

public sealed class TimeSystem
{
    private double _minuteAccumulator;

    public int Day { get; private set; } = 1;
    public string Season { get; private set; } = "Spring";
    public int MinutesSinceMidnight { get; private set; } = 6 * 60;

    public string ClockText
    {
        get
        {
            int hour24 = MinutesSinceMidnight / 60;
            int minute = MinutesSinceMidnight % 60;
            string suffix = hour24 >= 12 ? "PM" : "AM";
            int hour12 = hour24 % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            return $"{hour12}:{minute:00} {suffix}";
        }
    }

    public void Update(double elapsedSeconds)
    {
        _minuteAccumulator += elapsedSeconds;
        while (_minuteAccumulator >= 1.0)
        {
            _minuteAccumulator -= 1.0;
            MinutesSinceMidnight++;
            if (MinutesSinceMidnight >= 24 * 60)
            {
                MinutesSinceMidnight = 0;
            }
        }
    }

    public void AdvanceDay()
    {
        Day++;
        MinutesSinceMidnight = 6 * 60;
        _minuteAccumulator = 0;
    }

    public void SetState(int day, string season, int minutesSinceMidnight)
    {
        Day = Math.Max(1, day);
        Season = string.IsNullOrWhiteSpace(season) ? "Spring" : season;
        MinutesSinceMidnight = Math.Clamp(minutesSinceMidnight, 0, 24 * 60 - 1);
    }
}
