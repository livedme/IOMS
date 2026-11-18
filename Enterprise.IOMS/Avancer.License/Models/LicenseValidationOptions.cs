namespace Avancer.License.Models
{
    public sealed class LicenseValidationOptions
    {
        public string BaseUrl { get; set; } = KeySetting.DefaultApiUrl;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

        public bool AllowOfflineValidation { get; set; } = true;

        public TimeSpan OfflineGracePeriod { get; set; } = TimeSpan.FromDays(3);

        public string OfflineCachePath { get; set; } = DefaultOfflineCachePath();

        public static string DefaultOfflineCachePath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = AppContext.BaseDirectory;

            return Path.Combine(root, "Avancer", "license-cache.json");
        }
    }
}
