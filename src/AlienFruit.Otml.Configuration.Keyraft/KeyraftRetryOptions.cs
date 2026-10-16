using System;

namespace AlienFruit.Otml.Configuration.Keyraft
{
    /// <summary>
    /// Retry policy for transient Keyraft failures, configured through
    /// <see cref="KeyraftSecretProviderOptions.Retry"/>.
    /// </summary>
    /// <remarks>
    /// Retried: HTTP <c>408</c>, <c>429</c>, <c>500</c>, <c>502</c>, <c>503</c>, <c>504</c> responses,
    /// transport failures (<see cref="System.Net.Http.HttpRequestException"/>) and per-request timeouts.
    /// Never retried: <c>401</c>/<c>403</c>, <c>404</c> (a miss for this storage, not a failure),
    /// other status codes and malformed payloads. Cancellation requested by the caller token aborts
    /// resolution immediately, without retries.
    /// The delay before retry N (0-based) is <c>InitialDelay * BackoffMultiplier^N</c>, capped at
    /// <see cref="MaxDelay"/>; there is no jitter.
    /// </remarks>
    public class KeyraftRetryOptions
    {
        /// <summary>
        /// Number of retries after the initial request, so the total request budget is
        /// <c>MaxRetryAttempts + 1</c>. Default: 2. Negative values fail validation.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 2;

        /// <summary>
        /// Delay before the first retry. Default: 200 ms. Zero disables pausing between attempts.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Factor by which the delay grows before every next retry. Default: 2.0
        /// (200 ms, 400 ms, 800 ms, ...). Must be at least 1.0.
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Upper bound for the delay between retries. Default: 5 seconds.
        /// Must not be smaller than <see cref="InitialDelay"/>.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Computes the pause before retry number <paramref name="retryIndex"/> (0-based):
        /// <see cref="InitialDelay"/> scaled by <see cref="BackoffMultiplier"/>^retryIndex,
        /// capped at <see cref="MaxDelay"/>.
        /// </summary>
        internal TimeSpan GetDelay(int retryIndex)
        {
            var milliseconds = this.InitialDelay.TotalMilliseconds * Math.Pow(this.BackoffMultiplier, retryIndex);

            if (double.IsInfinity(milliseconds) || milliseconds >= this.MaxDelay.TotalMilliseconds)
                return this.MaxDelay;

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        internal KeyraftRetryOptions Clone()
            => (KeyraftRetryOptions)MemberwiseClone();

        internal void Validate()
        {
            if (this.MaxRetryAttempts < 0)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(KeyraftSecretProviderOptions.Retry)}." +
                    $"{nameof(this.MaxRetryAttempts)}' must not be negative");

            if (this.InitialDelay < TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(KeyraftSecretProviderOptions.Retry)}." +
                    $"{nameof(this.InitialDelay)}' must not be negative");

            if (double.IsNaN(this.BackoffMultiplier) || this.BackoffMultiplier < 1.0)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(KeyraftSecretProviderOptions.Retry)}." +
                    $"{nameof(this.BackoffMultiplier)}' must be at least 1.0");

            if (this.MaxDelay < this.InitialDelay)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(KeyraftSecretProviderOptions.Retry)}." +
                    $"{nameof(this.MaxDelay)}' must not be smaller than {nameof(this.InitialDelay)}");
        }
    }
}
