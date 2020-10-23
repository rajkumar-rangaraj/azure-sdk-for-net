// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    internal class AzureHelper
    {
        private static readonly IReadOnlyDictionary<string, string> azNamespaceToDescription = new Dictionary<string, string>
        {
            ["Microsoft.AAD"] = "Azure Active Directory",
            ["Microsoft.AppConfiguration"] = "App Configuration",
            ["Microsoft.CognitiveServices"] = "Azure Cognitive Services",
            ["Microsoft.Compute"] = "Azure Compute",
            ["Microsoft.DataLakeStore"] = "Azure Data Lake",
            ["Microsoft.DigitalTwins"] = "Azure Digital Twins",
            ["Microsoft.Dns"] = "Azure DNS",
            ["Microsoft.DocumentDB"] = "Azure DocumentDB",
            ["Microsoft.EventGrid"] = "Azure Event Grid",
            ["Microsoft.EventHub"] = "Azure Event Hubs",
            ["Microsoft.KeyVault"] = "Azure Key Vault",
            ["Microsoft.Network"] = "Azure Networking",
            ["Microsoft.ResourceGraph"] = "Azure Resource Graph",
            ["Microsoft.Resources"] = "Azure resources",
            ["Microsoft.ServiceBus"] = "Azure Service Bus",
            ["Microsoft.Sql"] = "Azure SQL Database",
            ["Microsoft.Storage"] = "Azure Storage",
            ["Microsoft.Synapse"] = "Azure Synapse Analytics",
            ["Microsoft.Tables"] = "Azure Table"
        };

        internal static void ExtractProperties(Dictionary<string, string> tags, out string type, out string sourceOrTarget)
        {
            sourceOrTarget = null;
            type = null;

            if (!tags.TryGetValue(SemanticConventions.AttributeAzureNameSpace, out var azNamespace))
            {
                return;
            }

            azNamespaceToDescription.TryGetValue(azNamespace, out type);

            switch (azNamespace)
            {
                case "Microsoft.EventHub":

                    if (tags.TryGetValue(SemanticConventions.AttributeEndpointAddress, out var endpoint))
                    {
                        endpoint = endpoint.Last() == '/' ? endpoint : endpoint + '/';

                        if (tags.TryGetValue(SemanticConventions.AttributeMessageBusDestination, out var eventHubQueueName))
                        {
                            sourceOrTarget = $"{endpoint}{eventHubQueueName}";
                        }
                        else
                        {
                            sourceOrTarget = endpoint;
                        }
                    }

                    break;

                case "Microsoft.KeyVault":

                    if (tags.TryGetValue(SemanticConventions.AttributeMessageBusDestination, out var certificate))
                    {
                        sourceOrTarget = certificate;
                    }

                    break;

                case "Microsoft.ServiceBus":

                    if (tags.TryGetValue(SemanticConventions.AttributeEndpointAddress, out endpoint))
                    {
                        sourceOrTarget = endpoint;
                    }

                    break;

                case "Microsoft.CognitiveServices":

                    if (tags.TryGetValue(SemanticConventions.AttributeDocument, out var document))
                    {
                        sourceOrTarget = document;
                    }

                    break;

                case "Microsoft.AppConfiguration":

                    if (tags.TryGetValue(SemanticConventions.AttributeKey, out var key))
                    {
                        sourceOrTarget = key;
                    }

                    break;

                case "Microsoft.Storage":
                case "Microsoft.Tables":
                case "Microsoft.DataLakeStore":

                    if (tags.TryGetValue(SemanticConventions.AttributeUrl, out var url))
                    {
                        sourceOrTarget = url;
                    }

                    break;
            }
        }

    }
}
