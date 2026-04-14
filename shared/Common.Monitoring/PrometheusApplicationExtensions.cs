using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace Common.Monitoring;

public static class PrometheusApplicationExtensions
{
    private static int _dotNetRuntimeCollectorStarted;

    /// <summary>
    /// Starts .NET runtime metrics (GC, thread pool, etc.) once per process.
    /// </summary>
    public static IServiceCollection AddDotNetRuntimeMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IHostedService, DotNetRuntimeMetricsHostedService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="SignalRPresenceMetrics"/> for hubs to update online-user gauges.
    /// </summary>
    public static IServiceCollection AddSignalRPresenceMetrics(this IServiceCollection services)
    {
        services.AddSingleton<SignalRPresenceMetrics>();
        return services;
    }

    /// <summary>
    /// Captures HTTP metrics. Call after <c>UseRouting</c> and before middleware that rewrites HTTP status codes.
    /// </summary>
    public static IApplicationBuilder UsePrometheusHttpMetrics(
        this IApplicationBuilder app,
        string serviceLabel)
    {
        return app.UseHttpMetrics(options =>
        {
            options.AddCustomLabel("service", _ => serviceLabel);
        });
    }

    /// <summary>
    /// Exposes Prometheus scrape endpoint without authorization.
    /// </summary>
    public static WebApplication MapPrometheusScrapeEndpoint(this WebApplication app, string path = "/metrics")
    {
        app.MapMetrics(path).AllowAnonymous();
        return app;
    }

    private sealed class DotNetRuntimeMetricsHostedService : IHostedService
    {
        private IDisposable? _collector;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _dotNetRuntimeCollectorStarted, 1) == 0)
                _collector = DotNetRuntimeStatsBuilder.Default().StartCollecting();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _collector?.Dispose();
            _collector = null;
            return Task.CompletedTask;
        }
    }
}
