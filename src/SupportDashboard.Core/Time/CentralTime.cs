using System.Globalization;

namespace SupportDashboard.Core.Time;

public static class CentralTime
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public static TimeZoneInfo Zone { get; } = Resolve();

    public static string Abbreviation(DateTimeOffset utc) => Zone.IsDaylightSavingTime(utc) ? "CDT" : "CST";

    public static string FormatDate(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, Zone).ToString("MMM d, yyyy", English);

    public static string FormatTime(DateTimeOffset utc) =>
        $"{TimeZoneInfo.ConvertTime(utc, Zone).ToString("h:mm tt", English)} {Abbreviation(utc)}";

    public static string Format(DateTimeOffset utc) => $"{FormatDate(utc)}, {FormatTime(utc)}";

    public static DateTimeOffset StartOfDayUtc(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localMidnight, Zone.GetUtcOffset(localMidnight)).ToUniversalTime();
    }

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows hosts without ICU only know the Windows zone id.
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
    }
}
