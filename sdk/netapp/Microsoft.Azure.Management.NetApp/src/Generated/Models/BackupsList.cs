// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.NetApp.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// List of Backups
    /// </summary>
    public partial class BackupsList
    {
        /// <summary>
        /// Initializes a new instance of the BackupsList class.
        /// </summary>
        public BackupsList()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the BackupsList class.
        /// </summary>
        /// <param name="value">A list of Backups</param>
        public BackupsList(IList<Backup> value = default(IList<Backup>))
        {
            Value = value;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets a list of Backups
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public IList<Backup> Value { get; set; }

    }
}
