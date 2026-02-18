// Transports/ITransport.cs
using System.Threading;
using System.Threading.Tasks;

namespace Ashutosh.AnalyticsSdk.Transports
{
    public interface ITransport
    {
        Task<TransportResult> SendAsync(
            byte[] payload,
            string contentType,
            CancellationToken ct);
    }

    public readonly struct TransportResult
    {
        public bool IsSuccess { get; }
        public bool IsRetryable { get; }
        public int StatusCode { get; }
        public string Error { get; }

        public TransportResult(bool isSuccess, bool isRetryable, int statusCode, string error)
        {
            IsSuccess = isSuccess;
            IsRetryable = isRetryable;
            StatusCode = statusCode;
            Error = error;
        }

        public static TransportResult Success(int statusCode = 200)
            => new TransportResult(true, false, statusCode, null);

        public static TransportResult Retryable(int statusCode, string error)
            => new TransportResult(false, true, statusCode, error);

        public static TransportResult Fatal(int statusCode, string error)
            => new TransportResult(false, false, statusCode, error);
    }
}
