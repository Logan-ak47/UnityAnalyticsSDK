namespace Ashutosh.AnalyticsSdk.Internal.Serialization
{
    internal interface IEventSerializer
    {
        string ContentType { get; }
        byte[] Serialize(AnalyticsPayload payload);
    }
}