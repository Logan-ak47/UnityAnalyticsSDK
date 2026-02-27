using System;
using System.Threading;
using System.Threading.Tasks;
using Ashutosh.AnalyticsSdk.Transports;

public sealed class SimulatedNetworkTransport : ITransport
{
    private readonly ITransport _inner;
    private readonly Func<bool> _isOffline;

    public int AttemptCount { get; private set; }

    public SimulatedNetworkTransport(ITransport inner, Func<bool> isOffline)
    {
        _inner = inner;
        _isOffline = isOffline;
    }

    public Task<TransportResult> SendAsync(byte[] payload, string contentType, CancellationToken ct)
    {
        AttemptCount++;

        if (_isOffline())
            return Task.FromResult(TransportResult.Retryable(0, "Simulated offline"));

        return _inner.SendAsync(payload, contentType, ct);
    }
}