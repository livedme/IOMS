using Avancer.License.Models;

namespace Avancer.License.Wrapper
{
    public interface ILicenseValidation
    {
        LicenseSecurity ValidateLicense(string domain, string licenseKey);

        Task<LicenseSecurity> ValidateLicenseAsync(string domain, string licenseKey, CancellationToken cancellationToken = default);
    }
}
