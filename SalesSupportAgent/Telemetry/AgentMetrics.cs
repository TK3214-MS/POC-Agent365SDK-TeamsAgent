using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SalesSupportAgent.Telemetry;

/// <summary>
/// Agent365 メトリクスとトレーシング
/// </summary>
public static class AgentMetrics
{
    public const string SourceName = "SalesSupportAgent";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> OperationCounter = Meter.CreateCounter<long>(
        "agent.operations.count",
        description: "Number of agent operations");

    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "agent.operations.duration",
        unit: "ms",
        description: "Duration of agent operations");

    /// <summary>
    /// HTTP 操作を観測可能にラップ
    /// </summary>
    public static async Task InvokeObservedHttpOperation(string operationName, Func<Task> operation)
    {
        using var activity = ActivitySource.StartActivity(operationName, ActivityKind.Server);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await operation();
            OperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("status", "success"));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            OperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            OperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operationName));
        }
    }

    /// <summary>
    /// 非同期操作を観測可能にラップ（戻り値あり）
    /// </summary>
    public static async Task<T> InvokeObservedHttpOperation<T>(string operationName, Func<Task<T>> operation)
    {
        using var activity = ActivitySource.StartActivity(operationName, ActivityKind.Server);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation();
            OperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("status", "success"));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            OperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            OperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operationName));
        }
    }
}
