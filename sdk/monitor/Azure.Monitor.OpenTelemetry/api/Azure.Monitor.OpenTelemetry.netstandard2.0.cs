namespace Azure.Monitor.OpenTelemetry
{
    public static partial class AzureMonitorOpenTelemetryExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAzureMonitorOpenTelemetry(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Azure.Monitor.OpenTelemetry.AzureMonitorOpenTelemetryOptions>? configureAzureMonitorOpenTelemetry = null, string? name = null) { throw null; }
    }
    public partial class AzureMonitorOpenTelemetryOptions : Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterOptions
    {
        public AzureMonitorOpenTelemetryOptions() { }
        public bool EnableLogs { get { throw null; } set { } }
        public bool EnableMetrics { get { throw null; } set { } }
        public bool EnableTraces { get { throw null; } set { } }
    }
}
