// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenTelemetry.Resources;
using System.Diagnostics;

namespace OpenTelemetry.Exporter.AzureMonitor.Demo.Tracing
{
    public static class DemoTrace
    {
        public static readonly ActivitySource source = new ActivitySource("DemoSource");

        public static void Main()
        {
            var resource = OpenTelemetry.Resources.Resources.CreateServiceResource("my-service", "roleinstance1", "my-namespace");
            using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                .SetResource(resource)
                .AddSource("Samples.SampleServer")
                .AddSource("Samples.SampleClient")
                .AddAzureMonitorTraceExporter(o => {
                    o.ConnectionString = $"InstrumentationKey=6c49c07c-e95c-48fe-8a7b-eff230955cc5;IngestionEndpoint=https://westus2-0.in.applicationinsights.azure.com/";
                })
                .Build();

            using (var sample = new InstrumentationWithActivitySource())
            {
                sample.Start();

                System.Console.WriteLine("Press ENTER to stop.");
                System.Console.ReadLine();
            }
        }
    }
}
