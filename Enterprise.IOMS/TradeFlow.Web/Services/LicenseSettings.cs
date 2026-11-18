using Avancer.License.Models;

namespace TradeFlow.Web.Services;

public sealed class LicenseSettings
{
    public const string SectionName = "License";
    public const string LicenseKeyCookie = "ioms_license_key";
    public const string TrialCookie = "ioms_license_trial";
    public const string StatusCookie = "ioms_license_status";

    public bool Enabled { get; set; } = true;
    public string ApiUrl { get; set; } = KeySetting.DefaultApiUrl;
    public string Domain { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public int CacheMinutes { get; set; } = 10;
    public int TrialDays { get; set; } = 15;
}
