using System.ComponentModel.DataAnnotations;

namespace SupportDashboard.Infrastructure.ServiceNow;

/// <summary>
/// Bound from the "ServiceNow" configuration section. Credentials must come from user secrets or
/// Key Vault, never appsettings.json.
/// </summary>
public sealed class ServiceNowOptions
{
    public const string SectionName = "ServiceNow";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    public Uri BaseUri => new(BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/");
}
