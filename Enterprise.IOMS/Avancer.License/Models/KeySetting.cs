namespace Avancer.License.Models
{
    public class KeySetting
    {
        public const string DefaultApiUrl = "https://license.avancerapp.com";
        public const string DefaultDomain = "avancerapp.com";
        public string ApiUrl { get; set; } = DefaultApiUrl;
        public string Domain { get; set; } = DefaultDomain;
        public string LicenseKey { get; set; } = string.Empty;

        [Obsolete("Use ApiUrl instead.")]
        public string ApiUlr => ApiUrl;
    }
}
