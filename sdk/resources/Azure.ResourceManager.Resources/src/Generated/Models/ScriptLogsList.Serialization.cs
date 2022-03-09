// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Collections.Generic;
using System.Text.Json;
using Azure.Core;
using Azure.ResourceManager.Resources;

namespace Azure.ResourceManager.Resources.Models
{
    internal partial class ScriptLogsList
    {
        internal static ScriptLogsList DeserializeScriptLogsList(JsonElement element)
        {
            Optional<IReadOnlyList<ScriptLogData>> value = default;
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("value"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        property.ThrowNonNullablePropertyIsNull();
                        continue;
                    }
                    List<ScriptLogData> array = new List<ScriptLogData>();
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        array.Add(ScriptLogData.DeserializeScriptLogData(item));
                    }
                    value = array;
                    continue;
                }
            }
            return new ScriptLogsList(Optional.ToList(value));
        }
    }
}
