// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using global::OpenTelemetry;
using global::OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace Azure.Monitor.OpenTelemetry.Exporter.Demo.Tracing
{
    public class DemoTrace
    {
        public static readonly ActivitySource source = new ActivitySource("DemoSource");

        public static void Main()
        {
            var resourceAttributes = new Dictionary<string, object> { { "service.name", "my-service" }, { "service.namespace", "my-namespace" }, { "service.instance.id", "my-instance" } };
            var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Demo.DemoServer")
                .AddSource("Demo.DemoClient")
                .AddAzureMonitorTraceExporter(o => {
                    o.ConnectionString = $"InstrumentationKey=Ikey;";
                })
                .Build();

                using var loggerFactory = LoggerFactory.Create(builder => builder
                .AddOpenTelemetry(loggerOptions =>
                {
                    loggerOptions.SetResourceBuilder(resourceBuilder);
                    loggerOptions.AddAzureMonitorLogExporter(exporterOptions =>
                    {
                        exporterOptions.ConnectionString = $"InstrumentationKey=Ikey;";
                    });
                }));

            var logger = loggerFactory.CreateLogger<DemoTrace>();
            logger.LogInformation("Hello from {name} {price}.", "tomato", 2.99);
            try
            {
                CallFun1();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "My test message");
            }

            using (var sample = new InstrumentationWithActivitySource())
            {
                sample.Start();

                System.Console.WriteLine("Press ENTER to stop.");
                System.Console.ReadLine();
            }
        }

        private static void CallFun1()
        {
            throw new InvalidOperationException("test exception");
        }
    }
}
