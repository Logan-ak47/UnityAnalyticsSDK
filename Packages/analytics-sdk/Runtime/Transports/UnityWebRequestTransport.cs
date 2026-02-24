using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Ashutosh.AnalyticsSdk.Transports
{
    /// <summary>
    /// UnityWebRequest-based transport for sending analytics payloads over HTTP.
    /// </summary>
    public sealed class UnityWebRequestTransport : ITransport
    {
        private readonly string _endpointUrl;
        private readonly int _timeoutSeconds;

        /// <summary>
        /// Creates a transport that POSTs payloads to an endpoint.
        /// </summary>
        /// <param name="endpointUrl">Destination endpoint URL.</param>
        /// <param name="timeoutSeconds">UnityWebRequest timeout in seconds.</param>
        public UnityWebRequestTransport(string endpointUrl, int timeoutSeconds = 10)
        {
            _endpointUrl = endpointUrl;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Sends a serialized analytics payload using HTTP POST.
        /// </summary>
        /// <param name="payload">Serialized request body.</param>
        /// <param name="contentType">Request content type header.</param>
        /// <param name="ct">Cancellation token (currently not wired to abort).</param>
        /// <returns>Task resolving to a mapped transport result.</returns>
        public Task<TransportResult> SendAsync(byte[] payload, string contentType, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TransportResult>();

            UnityWebRequest req = new UnityWebRequest(_endpointUrl, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", contentType);
            req.timeout = _timeoutSeconds;

            var op = req.SendWebRequest();

            // UnityWebRequestAsyncOperation has completed callback. :contentReference[oaicite:4]{index=4}
            op.completed += _ =>
            {
                try
                {
                    var result = MapResult(req);
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(TransportResult.Retryable(0, ex.Message));
                }
                finally
                {
                    req.Dispose();
                }
            };

            // Note: CancellationToken isn't wired to abort yet (we can add later).
            return tcs.Task;
        }

        private static TransportResult MapResult(UnityWebRequest req)
        {
            // result + error are the key APIs to interpret failures. :contentReference[oaicite:5]{index=5}
            long code = req.responseCode;
            string err = req.error;

            switch (req.result)
            {
                case UnityWebRequest.Result.Success:
                    return TransportResult.Success((int)code);

                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    return TransportResult.Retryable((int)code, err);

                case UnityWebRequest.Result.ProtocolError:
                    // ProtocolError means HTTP error response (4xx/5xx). :contentReference[oaicite:6]{index=6}
                    // Retry on 429 or 5xx; treat other 4xx as fatal (likely bad request/auth).
                    if (code == 429 || (code >= 500 && code <= 599))
                        return TransportResult.Retryable((int)code, err);

                    return TransportResult.Fatal((int)code, err);

                default:
                    return TransportResult.Retryable((int)code, err);
            }
        }
    }
}
