using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancer.License.Models;

namespace Avancer.License.Wrapper
{
    public sealed class LicenseValidation : ILicenseValidation
    {
        private static readonly HttpClient SharedHttpClient = new();

        private static readonly JsonSerializerOptions ResponseSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions CacheSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly SemaphoreSlim CacheLock = new(1, 1);

        private const int MaxOfflineEntries = 100;

        private readonly HttpClient _httpClient;
        private readonly LicenseValidationOptions _options;

        public LicenseValidation()
            : this(SharedHttpClient)
        {
        }

        public LicenseValidation(HttpClient httpClient, LicenseValidationOptions? options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? new LicenseValidationOptions();
        }

        public LicenseValidation(LicenseValidationOptions options)
            : this(SharedHttpClient, options)
        {
        }

        public LicenseSecurity ValidateLicense(string domain, string licenseKey)
        {
            return ValidateLicenseAsync(domain, licenseKey).GetAwaiter().GetResult();
        }

        public async Task<LicenseSecurity> ValidateLicenseAsync(
            string domain,
            string licenseKey,
            CancellationToken cancellationToken = default)
        {
            domain = domain?.Trim() ?? string.Empty;
            licenseKey = licenseKey?.Trim() ?? string.Empty;
            if(domain.Equals("localhost", StringComparison.OrdinalIgnoreCase) || domain.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                domain = "ims.avancerapp.com";
            }

            if (domain.Length == 0 || licenseKey.Length == 0)
                return Invalid("The domain and license key are required.");

            var requestUrl = BuildRequestUrl(domain, licenseKey);

            try
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (_options.Timeout > TimeSpan.Zero)
                    timeoutSource.CancelAfter(_options.Timeout);

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return Invalid("The license key was not found for this domain.");

                if (!response.IsSuccessStatusCode)
                    return Invalid($"The license server returned {(int)response.StatusCode} ({response.ReasonPhrase}).");

                await using var payload = await response.Content
                    .ReadAsStreamAsync(timeoutSource.Token)
                    .ConfigureAwait(false);

                var license = await JsonSerializer
                    .DeserializeAsync<LicenseResponse>(payload, ResponseSerializerOptions, timeoutSource.Token)
                    .ConfigureAwait(false);

                if (license is null)
                    return Invalid("The license server returned an empty response.");

                var result = Evaluate(license, domain, licenseKey);
                result.Response = license;

                if (result.IsLicenseValid)
                    await SaveOfflineAsync(domain, licenseKey, result, cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return await GetOfflineResultAsync(domain, licenseKey, cancellationToken).ConfigureAwait(false)
                    ?? OfflineFailure("The license server did not respond in time.");
            }
            catch (HttpRequestException exception)
            {
                return await GetOfflineResultAsync(domain, licenseKey, cancellationToken).ConfigureAwait(false)
                    ?? OfflineFailure($"The license server is unreachable: {exception.Message}");
            }
            catch (JsonException)
            {
                return Invalid("The license server returned an invalid response.");
            }
        }

        private string BuildRequestUrl(string domain, string licenseKey)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? KeySetting.DefaultApiUrl : _options.BaseUrl.Trim();
            baseUrl = baseUrl.TrimEnd('/');

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{baseUrl}/api/License/{Uri.EscapeDataString(domain)}/{Uri.EscapeDataString(licenseKey)}");
        }

        private static LicenseSecurity Evaluate(LicenseResponse response, string requestedDomain, string requestedKey)
        {
            var status = ParseStatus(response.Status);
            var subscription = BuildSubscription(response, status);

            if (!string.Equals(response.ApplicationDomain?.Trim(), requestedDomain, StringComparison.OrdinalIgnoreCase))
                return Invalid("The license was issued for a different domain.", subscription);

            if (!string.Equals(response.LicenseKey?.Trim(), requestedKey, StringComparison.Ordinal))
                return Invalid("The license key does not match the issued license.", subscription);

            if (status is not (SubscriptionStatus.Active or SubscriptionStatus.Trial))
                return Invalid($"The license is not active (status: {response.Status}).", subscription);

            var now = DateTime.UtcNow;

            if (response.ValidFrom is { } validFrom)
            {
                var validFromUtc = validFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                if (now < validFromUtc)
                    return Invalid($"The license is not valid before {validFromUtc:yyyy-MM-dd}.", subscription);
            }

            if (response.ValidUntil is { } validUntil)
            {
                var validUntilUtc = validUntil.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                var graceDays = Math.Max(0, response.GracePeriod);
                if (now > validUntilUtc.AddDays(graceDays))
                    return Invalid($"The license expired on {validUntilUtc:yyyy-MM-dd}.", subscription);
            }

            return new LicenseSecurity
            {
                IsLicenseValid = true,
                IsOffline = false,
                Message = "The license is valid.",
                CheckedAtUtc = now,
                Subscription = subscription
            };
        }

        private static SubscriptionStatus ParseStatus(RecordStatus status)
        {
            return status switch
            {
                RecordStatus.Pending => SubscriptionStatus.Pending,
                RecordStatus.Active => SubscriptionStatus.Active,
                RecordStatus.InActive => SubscriptionStatus.Inactive,
                RecordStatus.Expired => SubscriptionStatus.Expired,
                RecordStatus.Suspended => SubscriptionStatus.Suspended,
                RecordStatus.Revoked => SubscriptionStatus.Revoked,
                RecordStatus.Cancelled => SubscriptionStatus.Cancelled,
                _ => SubscriptionStatus.Expired
            };
        }

        private static Subscription BuildSubscription(LicenseResponse response, SubscriptionStatus status)
        {
            var activatedAt = NormalizeUtc(response.ActivatedAtUtc);

            var activeDate = activatedAt
                ?? (response.ValidFrom is { } validFrom
                    ? validFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    : DateTime.MinValue);

            var validUntilUtc = response.ValidUntil is { } validUntil
                ? validUntil.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
                : DateTime.MaxValue;

            var graceDays = Math.Max(0, response.GracePeriod);
            var expiryDate = validUntilUtc == DateTime.MaxValue
                ? DateTime.MaxValue
                : validUntilUtc.AddDays(graceDays);

            return new Subscription
            {
                Id = response.SubscriptionId ?? response.LicenseId,
                LicenseId = response.LicenseId,
                SubscriptionId = response.SubscriptionId,
                SubscriptionNumber = response.SubscriptionNumber,
                SubscriptionName = response.SubscriptionName,
                ProjectId = response.ProjectId,
                ProjectName = response.ProjectName,
                DomainName = response.ApplicationDomain,
                Plan = response.Type.ToString(),
                BillingCycle = response.BillingCycle,
                Type = response.Type,
                RecordStatus = response.Status,
                Amount = response.Amount,
                GracePeriod = graceDays,
                ActiveDate = activeDate,
                ValidUntil = validUntilUtc,
                ExpiryDate = expiryDate,
                Status = status
            };
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue || value.Value == default)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private async Task<LicenseSecurity?> GetOfflineResultAsync(
            string domain,
            string licenseKey,
            CancellationToken cancellationToken)
        {
            if (!_options.AllowOfflineValidation ||
                _options.OfflineGracePeriod <= TimeSpan.Zero ||
                string.IsNullOrWhiteSpace(_options.OfflineCachePath))
            {
                return null;
            }

            await CacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var entries = await ReadCacheFileAsync(cancellationToken).ConfigureAwait(false);
                if (entries.Count == 0)
                    return null;

                PruneOfflineEntries(entries);

                if (!entries.TryGetValue(BuildCacheKey(domain, licenseKey), out var entry) ||
                    entry?.License is null ||
                    !entry.License.IsLicenseValid)
                {
                    return null;
                }

                var now = DateTime.UtcNow;
                if (now - entry.CachedAtUtc > _options.OfflineGracePeriod)
                    return null;

                if (entry.License.Subscription is { } cachedSubscription &&
                    cachedSubscription.ExpiryDate != DateTime.MaxValue &&
                    cachedSubscription.ExpiryDate < now)
                {
                    return null;
                }

                return new LicenseSecurity
                {
                    IsLicenseValid = true,
                    IsOffline = true,
                    Message = "The license was validated from the offline cache.",
                    CheckedAtUtc = entry.CachedAtUtc,
                    Subscription = entry.License.Subscription
                };
            }
            finally
            {
                CacheLock.Release();
            }
        }

        private async Task SaveOfflineAsync(
            string domain,
            string licenseKey,
            LicenseSecurity license,
            CancellationToken cancellationToken)
        {
            if (!_options.AllowOfflineValidation || string.IsNullOrWhiteSpace(_options.OfflineCachePath))
                return;

            await CacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_options.OfflineCachePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var entries = await ReadCacheFileAsync(cancellationToken).ConfigureAwait(false);
                PruneOfflineEntries(entries);

                entries[BuildCacheKey(domain, licenseKey)] = new OfflineCacheEntry
                {
                    Domain = domain,
                    LicenseKey = licenseKey,
                    CachedAtUtc = DateTime.UtcNow,
                    License = license
                };

                while (entries.Count > MaxOfflineEntries)
                {
                    var oldestKey = entries
                        .OrderBy(pair => pair.Value.CachedAtUtc)
                        .Select(pair => pair.Key)
                        .First();
                    entries.Remove(oldestKey);
                }

                var temporaryPath = _options.OfflineCachePath + ".tmp";
                await using (var stream = File.Create(temporaryPath))
                {
                    await JsonSerializer
                        .SerializeAsync(stream, entries, CacheSerializerOptions, cancellationToken)
                        .ConfigureAwait(false);
                }

                File.Move(temporaryPath, _options.OfflineCachePath, overwrite: true);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or OperationCanceledException)
            {
            }
            finally
            {
                CacheLock.Release();
            }
        }

        private async Task<Dictionary<string, OfflineCacheEntry>> ReadCacheFileAsync(
            CancellationToken cancellationToken)
        {
            var empty = new Dictionary<string, OfflineCacheEntry>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(_options.OfflineCachePath) ||
                !File.Exists(_options.OfflineCachePath))
            {
                return empty;
            }

            try
            {
                await using var stream = File.OpenRead(_options.OfflineCachePath);
                var entries = await JsonSerializer
                    .DeserializeAsync<Dictionary<string, OfflineCacheEntry>>(
                        stream,
                        CacheSerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);

                return entries is null
                    ? empty
                    : new Dictionary<string, OfflineCacheEntry>(entries, StringComparer.Ordinal);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                return empty;
            }
        }

        private void PruneOfflineEntries(Dictionary<string, OfflineCacheEntry> entries)
        {
            var now = DateTime.UtcNow;

            foreach (var key in entries.Keys.ToList())
            {
                var entry = entries[key];

                if (entry?.License is null ||
                    now - entry.CachedAtUtc > _options.OfflineGracePeriod ||
                    (entry.License.Subscription is { } subscription &&
                     subscription.ExpiryDate != DateTime.MaxValue &&
                     subscription.ExpiryDate < now))
                {
                    entries.Remove(key);
                }
            }

            while (entries.Count > MaxOfflineEntries)
            {
                var oldestKey = entries
                    .OrderBy(pair => pair.Value.CachedAtUtc)
                    .Select(pair => pair.Key)
                    .First();
                entries.Remove(oldestKey);
            }
        }

        private static string BuildCacheKey(string domain, string licenseKey) =>
            $"{domain.ToLowerInvariant()}\n{licenseKey}";

        private static LicenseSecurity Invalid(string message, Subscription? subscription = null)
        {
            return new LicenseSecurity
            {
                IsLicenseValid = false,
                IsOffline = false,
                Message = message,
                CheckedAtUtc = DateTime.UtcNow,
                Subscription = subscription
            };
        }

        private static LicenseSecurity OfflineFailure(string message)
        {
            return new LicenseSecurity
            {
                IsLicenseValid = false,
                IsOffline = true,
                Message = message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }

        private sealed class OfflineCacheEntry
        {
            public string Domain { get; set; } = string.Empty;
            public string LicenseKey { get; set; } = string.Empty;
            public DateTime CachedAtUtc { get; set; }
            public LicenseSecurity License { get; set; } = new();
        }
    }
}
