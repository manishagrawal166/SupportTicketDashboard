using System.Globalization;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Web.Formatting;

public static class DisplayFormat
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public static string Relative(DateTimeOffset utc, DateTimeOffset now)
    {
        var elapsed = now - utc;
        if (elapsed < TimeSpan.FromMinutes(1)) return "just now";
        if (elapsed < TimeSpan.FromHours(1)) return $"{(int)elapsed.TotalMinutes} min ago";
        if (elapsed < TimeSpan.FromDays(1)) return $"{(int)elapsed.TotalHours} hr ago";
        var days = (int)elapsed.TotalDays;
        return days == 1 ? "1 day ago" : $"{days} days ago";
    }

    public static string Age(DateTimeOffset openedUtc, DateTimeOffset now)
    {
        var age = now - openedUtc;
        if (age < TimeSpan.Zero) age = TimeSpan.Zero;
        if (age < TimeSpan.FromHours(1)) return $"{(int)age.TotalMinutes}m";
        if (age < TimeSpan.FromDays(1)) return $"{(int)age.TotalHours}h {age.Minutes}m";
        return $"{(int)age.TotalDays}d {age.Hours}h";
    }

    public static string Duration(TimeSpan? duration)
    {
        if (duration is not { } d) return "—";
        if (d < TimeSpan.FromSeconds(1)) return $"{(int)d.TotalMilliseconds} ms";
        if (d < TimeSpan.FromMinutes(1)) return $"{d.TotalSeconds.ToString("0.0", English)} s";
        if (d < TimeSpan.FromHours(1)) return $"{(int)d.TotalMinutes}m {d.Seconds:00}s";
        return $"{(int)d.TotalHours}h {d.Minutes:00}m";
    }

    private static readonly TimeSpan NewTicketWindow = TimeSpan.FromMinutes(15);

    public static bool IsNew(DateTimeOffset openedUtc, DateTimeOffset now) => now - openedUtc < NewTicketWindow;

    /// <summary>Compact target label, e.g. 15m, 2h, 3d.</summary>
    public static string ShortSpan(TimeSpan span) =>
        span.TotalDays >= 1 && span.TotalDays % 1 == 0 ? $"{(int)span.TotalDays}d"
        : span.TotalHours >= 1 ? $"{(int)span.TotalHours}h"
        : $"{(int)span.TotalMinutes}m";

    public static string StateTone(string state) => state switch
    {
        IncidentStates.Open => "info",
        IncidentStates.InProgress => "caution",
        IncidentStates.Resolved or IncidentStates.Closed => "success",
        _ => "neutral",
    };

    public static string StatusTone(RunStatus status) => status switch
    {
        RunStatus.Succeeded => "success",
        RunStatus.Failed => "danger",
        _ => "info",
    };

    public static string PriorityTone(IncidentPriority priority) => priority switch
    {
        IncidentPriority.P1 => "danger",
        IncidentPriority.P2 => "warning",
        IncidentPriority.P3 => "caution",
        IncidentPriority.P4 => "indigo",
        _ => "neutral",
    };
}
