// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Commands.Sql.Auditing.DevOps
{
    using Microsoft.Azure.Management.Sql.Models;
    using Microsoft.Rest;
    using Newtonsoft.Json;

    /// <summary>
    /// A server DevOps auditing policy.
    /// </summary>
    [Rest.Serialization.JsonTransformation]
    public partial class ServerDevOpsAuditingPolicy : ProxyResource
    {
        /// <summary>
        /// Initializes a new instance of the ServerDevOpsAuditingPolicy class.
        /// </summary>
        public ServerDevOpsAuditingPolicy()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ServerDevOpsAuditingPolicy class.
        /// </summary>
        /// <param name="state">Specifies the state of the policy. If state is
        /// Enabled, storageEndpoint or isAzureMonitorTargetEnabled are
        /// required. Possible values include: 'Enabled', 'Disabled'</param>
        /// <param name="id">Resource ID.</param>
        /// <param name="name">Resource name.</param>
        /// <param name="type">Resource type.</param>
        /// <param name="isAzureMonitorTargetEnabled">Specifies whether DevOps
        /// audit events are sent to Azure Monitor.
        /// In order to send the events to Azure Monitor, specify 'State' as
        /// 'Enabled' and 'IsAzureMonitorTargetEnabled' as true.
        ///
        /// When using REST API to configure DevOps audit, Diagnostic Settings
        /// with 'DevOpsOperationsAudit' diagnostic logs category on the master
        /// database should be also created.
        ///
        /// Diagnostic Settings URI format:
        /// PUT
        /// https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Sql/servers/{serverName}/databases/master/providers/microsoft.insights/diagnosticSettings/{settingsName}?api-version=2017-05-01-preview
        ///
        /// For more information, see [Diagnostic Settings REST
        /// API](https://go.microsoft.com/fwlink/?linkid=2033207)
        /// or [Diagnostic Settings
        /// PowerShell](https://go.microsoft.com/fwlink/?linkid=2033043)
        /// </param>
        /// <param name="storageEndpoint">Specifies the blob storage endpoint
        /// (e.g. https://MyAccount.blob.core.windows.net). If state is
        /// Enabled, storageEndpoint or isAzureMonitorTargetEnabled is
        /// required.</param>
        /// <param name="storageAccountAccessKey">Specifies the identifier key
        /// of the auditing storage account.
        /// If state is Enabled and storageEndpoint is specified, not
        /// specifying the storageAccountAccessKey will use SQL server
        /// system-assigned managed identity to access the storage.
        /// Prerequisites for using managed identity authentication:
        /// 1. Assign SQL Server a system-assigned managed identity in Azure
        /// Active Directory (AAD).
        /// 2. Grant SQL Server identity access to the storage account by
        /// adding 'Storage Blob Data Contributor' RBAC role to the server
        /// identity.
        /// For more information, see [Auditing to storage using Managed
        /// Identity
        /// authentication](https://go.microsoft.com/fwlink/?linkid=2114355)</param>
        /// <param name="storageAccountSubscriptionId">Specifies the blob
        /// storage subscription Id.</param>
        public ServerDevOpsAuditingPolicy(BlobAuditingPolicyState state, string id = default(string), string name = default(string), string type = default(string), bool? isAzureMonitorTargetEnabled = default(bool?), string storageEndpoint = default(string), string storageAccountAccessKey = default(string), System.Guid? storageAccountSubscriptionId = default(System.Guid?))
            : base(id, name, type)
        {
            IsAzureMonitorTargetEnabled = isAzureMonitorTargetEnabled;
            State = state;
            StorageEndpoint = storageEndpoint;
            StorageAccountAccessKey = storageAccountAccessKey;
            StorageAccountSubscriptionId = storageAccountSubscriptionId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets specifies whether DevOps audit events are sent to
        /// Azure Monitor.
        /// In order to send the events to Azure Monitor, specify 'State' as
        /// 'Enabled' and 'IsAzureMonitorTargetEnabled' as true.
        ///
        /// When using REST API to configure DevOps audit, Diagnostic Settings
        /// with 'DevOpsOperationsAudit' diagnostic logs category on the master
        /// database should be also created.
        ///
        /// Diagnostic Settings URI format:
        /// PUT
        /// https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Sql/servers/{serverName}/databases/master/providers/microsoft.insights/diagnosticSettings/{settingsName}?api-version=2017-05-01-preview
        ///
        /// For more information, see [Diagnostic Settings REST
        /// API](https://go.microsoft.com/fwlink/?linkid=2033207)
        /// or [Diagnostic Settings
        /// PowerShell](https://go.microsoft.com/fwlink/?linkid=2033043)
        ///
        /// </summary>
        [JsonProperty(PropertyName = "properties.isAzureMonitorTargetEnabled")]
        public bool? IsAzureMonitorTargetEnabled { get; set; }

        /// <summary>
        /// Gets or sets specifies the state of the policy. If state is
        /// Enabled, storageEndpoint or isAzureMonitorTargetEnabled are
        /// required. Possible values include: 'Enabled', 'Disabled'
        /// </summary>
        [JsonProperty(PropertyName = "properties.state")]
        public BlobAuditingPolicyState State { get; set; }

        /// <summary>
        /// Gets or sets specifies the blob storage endpoint (e.g.
        /// https://MyAccount.blob.core.windows.net). If state is Enabled,
        /// storageEndpoint or isAzureMonitorTargetEnabled is required.
        /// </summary>
        [JsonProperty(PropertyName = "properties.storageEndpoint")]
        public string StorageEndpoint { get; set; }

        /// <summary>
        /// Gets or sets specifies the identifier key of the auditing storage
        /// account.
        /// If state is Enabled and storageEndpoint is specified, not
        /// specifying the storageAccountAccessKey will use SQL server
        /// system-assigned managed identity to access the storage.
        /// Prerequisites for using managed identity authentication:
        /// 1. Assign SQL Server a system-assigned managed identity in Azure
        /// Active Directory (AAD).
        /// 2. Grant SQL Server identity access to the storage account by
        /// adding 'Storage Blob Data Contributor' RBAC role to the server
        /// identity.
        /// For more information, see [Auditing to storage using Managed
        /// Identity
        /// authentication](https://go.microsoft.com/fwlink/?linkid=2114355)
        /// </summary>
        [JsonProperty(PropertyName = "properties.storageAccountAccessKey")]
        public string StorageAccountAccessKey { get; set; }

        /// <summary>
        /// Gets or sets specifies the blob storage subscription Id.
        /// </summary>
        [JsonProperty(PropertyName = "properties.storageAccountSubscriptionId")]
        public System.Guid? StorageAccountSubscriptionId { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
        }
    }
}
