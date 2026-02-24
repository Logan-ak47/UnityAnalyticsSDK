// Transports/ITransport.cs
using System.Threading;
using System.Threading.Tasks;

namespace Ashutosh.AnalyticsSdk.Transports
{
    /// <summary>
    /// Sends serialized analytics payloads to a backend endpoint.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends one serialized payload.
        /// </summary>
        /// <param name="payload">Serialized request body.</param>
        /// <param name="contentType">Request content type header.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Transport result describing success and retry behavior.</returns>
        Task<TransportResult> SendAsync(
            byte[] payload,
            string contentType,
            CancellationToken ct);
    }

    /// <summary>
    /// Result returned by an <see cref="ITransport"/> send operation.
    /// </summary>
    public readonly struct TransportResult
    {
        /// <summary>
        /// True when the payload was accepted successfully.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// True when the failure should be retried later.
        /// </summary>
        public bool IsRetryable { get; }

        /// <summary>
        /// HTTP status code or transport-specific status.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Error message for failed requests, if available.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Creates a transport result.
        /// </summary>
        /// <param name="isSuccess">Whether the send succeeded.</param>
        /// <param name="isRetryable">Whether a failure is retryable.</param>
        /// <param name="statusCode">HTTP or transport status code.</param>
        /// <param name="error">Error message for failures.</param>
        public TransportResult(bool isSuccess, bool isRetryable, int statusCode, string error)
        {
            IsSuccess = isSuccess;
            IsRetryable = isRetryable;
            StatusCode = statusCode;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        public static TransportResult Success(int statusCode = 200)
            => new TransportResult(true, false, statusCode, null);

        /// <summary>
        /// Creates a retryable failure result.
        /// </summary>
        /// <param name="statusCode">HTTP or transport status code.</param>
        /// <param name="error">Failure reason.</param>
        public static TransportResult Retryable(int statusCode, string error)
            => new TransportResult(false, true, statusCode, error);

        /// <summary>
        /// Creates a non-retryable failure result.
        /// </summary>
        /// <param name="statusCode">HTTP or transport status code.</param>
        /// <param name="error">Failure reason.</param>
        public static TransportResult Fatal(int statusCode, string error)
            => new TransportResult(false, false, statusCode, error);
    }
}
