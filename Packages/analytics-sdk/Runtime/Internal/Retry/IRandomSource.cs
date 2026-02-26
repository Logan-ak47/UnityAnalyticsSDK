namespace Ashutosh.AnalyticsSdk.Internal.Retry
{
    internal interface IRandomSource
    {
        double NextDouble(); // 0..1
    }
}