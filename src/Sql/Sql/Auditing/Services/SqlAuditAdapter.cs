// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Common.Authentication.Abstractions;
using Microsoft.Azure.Commands.Sql.Auditing.Model;
using Microsoft.Azure.Commands.Sql.Common;
using Microsoft.Azure.Commands.Sql.Database.Services;
using Microsoft.Azure.Management.Monitor.Version2018_09_01.Models;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProxyResource = Microsoft.Azure.Management.Sql.Models.ProxyResource;

namespace Microsoft.Azure.Commands.Sql.Auditing.Services
{
    /// <summary>
    /// The SqlAuditClient class is responsible for transforming the data that was received form the endpoints to the cmdlets model of auditing policy and vice versa
    /// </summary>
    public abstract class SqlAuditAdapter<AuditPolicyType, AuditModelType> where AuditPolicyType : ProxyResource
                                                                           where AuditModelType : ServerDevOpsAuditModel
    {
        /// <summary>
        /// Gets or sets the Azure profile
        /// </summary>
        public IAzureContext Context { get; set; }

        /// <summary>
        /// The auditing endpoints communicator used by this adapter
        /// </summary>
        protected AuditingEndpointsCommunicator Communicator { get; set; }

        /// <summary>
        /// The Azure endpoints communicator used by this adapter
        /// </summary>
        protected AzureEndpointsCommunicator AzureCommunicator { get; set; }

        /// <summary>
        /// Gets or sets the Azure subscription
        /// </summary>
        private IAzureSubscription Subscription { get; set; }

        private Guid RoleAssignmentId { get; }

        public SqlAuditAdapter(IAzureContext context, Guid roleAssignmentId = default(Guid))
        {
            Context = context;
            Subscription = context?.Subscription;
            Communicator = new AuditingEndpointsCommunicator(Context);
            AzureCommunicator = new AzureEndpointsCommunicator(Context);
            RoleAssignmentId = roleAssignmentId;
        }

        internal virtual IList<DiagnosticSettingsResource> GetDiagnosticsEnablingAuditCategory(
            string resourceGroup, string serverName,
            out string nextDiagnosticSettingsName)
        {
            return Communicator.GetDiagnosticsEnablingAuditCategory(out nextDiagnosticSettingsName,
                GetDiagnosticsEnablingAuditCategoryName(), GetNextDiagnosticSettingsNamePrefix(),
                resourceGroup, serverName);
        }

        internal void GetAuditingSettings(string resourceGroup, string serverName, AuditModelType model)
        {
            AuditPolicyType policy = GetAuditingPolicy(resourceGroup, serverName);

            model.DiagnosticsEnablingAuditCategory = GetDiagnosticsEnablingAuditCategory(resourceGroup, 
                serverName, out string nextDiagnosticSettingsName);

            model.NextDiagnosticSettingsName = nextDiagnosticSettingsName;

            ModelizeAuditPolicy(model, policy);
        }

        internal abstract AuditPolicyType GetAuditingPolicy(string resourceGroup, string serverName);

        internal abstract bool SetAudit(AuditModelType model);

        internal abstract string GetDiagnosticsEnablingAuditCategoryName();

        internal abstract string GetNextDiagnosticSettingsNamePrefix();

        internal abstract void ModelizeAuditPolicy(AuditModelType model, AuditPolicyType policy);

        internal abstract StorageKeyKind GetStorageKeyKind(AuditModelType model);

        internal void ModelizeAuditPolicy(AuditModelType model,
            BlobAuditingPolicyState state,
            string storageEndpoint,
            bool? isSecondary,
            Guid? storageAccountSubscriptionId,
            bool? isAzureMonitorTargetEnabled,
            int? retentionDays)
        {
            model.IsAzureMonitorTargetEnabled = isAzureMonitorTargetEnabled;

            ModelizeStorageInfo(model, storageEndpoint, isSecondary, storageAccountSubscriptionId, IsAuditEnabled(state), retentionDays);
            DetermineTargetsState(model, state);
        }

        internal AuditActionGroups[] ExtractAuditActionGroups(IEnumerable<string> auditActionsAndGroups)
        {
            var groups = new List<AuditActionGroups>();
            if (auditActionsAndGroups != null)
            {
                auditActionsAndGroups.ForEach(item =>
                {
                    if (Enum.TryParse(item, true, out AuditActionGroups group))
                    {
                        groups.Add(group);
                    }
                });
            }

            return groups.ToArray();
        }

        internal string[] ExtractAuditActions(IEnumerable<string> auditActionsAndGroups)
        {
            var actions = new List<string>();
            if (auditActionsAndGroups != null)
            {
                auditActionsAndGroups.ForEach(item =>
                {
                    if (!Enum.TryParse(item, true, out AuditActionGroups group))
                    {
                        actions.Add(item);
                    }
                });
            }

            return actions.ToArray();
        }

        internal virtual void ModelizeStorageKeyType(AuditModelType model, bool? isSecondary) { }

        internal virtual void ModelizeRetentionInfo(AuditModelType model, int? retentionDays) { }

        private void ModelizeStorageInfo(AuditModelType model,
            string storageEndpoint, bool? isSecondary, Guid? storageAccountSubscriptionId, 
            bool isAuditEnabled, int? retentionDays)
        {
            if (string.IsNullOrEmpty(storageEndpoint))
            {
                return;
            }

            ModelizeStorageKeyType(model, isSecondary);

            if (isAuditEnabled)
            {
                if (storageAccountSubscriptionId == null || Guid.Empty.Equals(storageAccountSubscriptionId))
                {
                    storageAccountSubscriptionId = Subscription.GetId();
                }

                model.StorageAccountResourceId = AzureCommunicator.RetrieveStorageAccountIdAsync(
                    storageAccountSubscriptionId.Value,
                    GetStorageAccountName(storageEndpoint)).GetAwaiter().GetResult();

                ModelizeRetentionInfo(model, retentionDays);
            }
        }

        private static string GetStorageAccountName(string storageEndpoint)
        {
            int accountNameStartIndex = storageEndpoint.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ? 8 : 7; // https:// or http://
            int accountNameEndIndex = storageEndpoint.IndexOf(".blob", StringComparison.InvariantCultureIgnoreCase);
            return storageEndpoint.Substring(accountNameStartIndex, accountNameEndIndex - accountNameStartIndex);
        }

        private bool IsAuditEnabled(BlobAuditingPolicyState state)
        {
            return state == BlobAuditingPolicyState.Enabled;
        }

        private static void DetermineTargetsState(
            AuditModelType model,
            BlobAuditingPolicyState policyState)
        {
            if (policyState == BlobAuditingPolicyState.Disabled)
            {
                model.BlobStorageTargetState = AuditStateType.Disabled;
                model.EventHubTargetState = AuditStateType.Disabled;
                model.LogAnalyticsTargetState = AuditStateType.Disabled;
            }
            else
            {
                if (string.IsNullOrEmpty(model.StorageAccountResourceId))
                {
                    model.BlobStorageTargetState = AuditStateType.Disabled;
                }
                else
                {
                    model.BlobStorageTargetState = AuditStateType.Enabled;
                }

                if (model.IsAzureMonitorTargetEnabled == null ||
                    model.IsAzureMonitorTargetEnabled == false ||
                    model.DiagnosticsEnablingAuditCategory == null)
                {
                    model.EventHubTargetState = AuditStateType.Disabled;
                    model.LogAnalyticsTargetState = AuditStateType.Disabled;
                }
                else
                {
                    DiagnosticSettingsResource eventHubSettings = model.DiagnosticsEnablingAuditCategory.FirstOrDefault(
                        settings => !string.IsNullOrEmpty(settings.EventHubAuthorizationRuleId));
                    if (eventHubSettings == null)
                    {
                        model.EventHubTargetState = AuditStateType.Disabled;
                    }
                    else
                    {
                        model.EventHubTargetState = AuditStateType.Enabled;
                        model.EventHubName = eventHubSettings.EventHubName;
                        model.EventHubAuthorizationRuleResourceId = eventHubSettings.EventHubAuthorizationRuleId;
                    }

                    DiagnosticSettingsResource logAnalyticsSettings = model.DiagnosticsEnablingAuditCategory.FirstOrDefault(
                        settings => !string.IsNullOrEmpty(settings.WorkspaceId));
                    if (logAnalyticsSettings == null)
                    {
                        model.LogAnalyticsTargetState = AuditStateType.Disabled;
                    }
                    else
                    {
                        model.LogAnalyticsTargetState = AuditStateType.Enabled;
                        model.WorkspaceResourceId = logAnalyticsSettings.WorkspaceId;
                    }
                }
            }
        }

        internal bool CreateDiagnosticSettings(
            string eventHubName, string eventHubAuthorizationRuleId, string workspaceId,
            AuditModelType model)
        {
            DiagnosticSettingsResource settings;
            string categoryName = GetDiagnosticsEnablingAuditCategoryName();

            if (model is DatabaseAuditModel databaseAuditModel)
            {
                settings = Communicator.CreateDiagnosticSettings(categoryName, databaseAuditModel.NextDiagnosticSettingsName,
                    eventHubName, eventHubAuthorizationRuleId, workspaceId,
                    databaseAuditModel.ResourceGroupName, databaseAuditModel.ServerName, databaseAuditModel.DatabaseName);
            }
            else
            {
                settings = Communicator.CreateDiagnosticSettings(categoryName, model.NextDiagnosticSettingsName,
                    eventHubName, eventHubAuthorizationRuleId, workspaceId,
                    model.ResourceGroupName, model.ServerName);
            }

            if (settings == null)
            {
                return false;
            }

            IList<DiagnosticSettingsResource> diagnosticsEnablingAuditCategory = model.DiagnosticsEnablingAuditCategory;
            if (diagnosticsEnablingAuditCategory == null)
            {
                diagnosticsEnablingAuditCategory = new List<DiagnosticSettingsResource>();
            }

            diagnosticsEnablingAuditCategory.Add(settings);
            model.DiagnosticsEnablingAuditCategory = diagnosticsEnablingAuditCategory;
            return true;
        }

        internal bool UpdateDiagnosticSettings(
            DiagnosticSettingsResource settings,
            AuditModelType model)
        {
            DiagnosticSettingsResource modifiedSettings;
            if (model is DatabaseAuditModel databaseAuditModel)
            {
                modifiedSettings = Communicator.UpdateDiagnosticSettings(settings,
                    databaseAuditModel.ResourceGroupName, databaseAuditModel.ServerName, databaseAuditModel.DatabaseName);
            }
            else
            {
                modifiedSettings = Communicator.UpdateDiagnosticSettings(settings,
                    model.ResourceGroupName, model.ServerName);
            }

            if (modifiedSettings == null)
            {
                return false;
            }

            List<DiagnosticSettingsResource> diagnosticsEnablingAuditCategory = new List<DiagnosticSettingsResource>();
            foreach (DiagnosticSettingsResource existingSettings in model.DiagnosticsEnablingAuditCategory)
            {
                if (!string.Equals(modifiedSettings.Id, existingSettings.Id))
                {
                    diagnosticsEnablingAuditCategory.Add(existingSettings);
                }
                else if (AuditingEndpointsCommunicator.IsAuditCategoryEnabled(modifiedSettings, GetDiagnosticsEnablingAuditCategoryName()))
                {
                    diagnosticsEnablingAuditCategory.Add(modifiedSettings);
                }
            }

            model.DiagnosticsEnablingAuditCategory = diagnosticsEnablingAuditCategory.Any() ? diagnosticsEnablingAuditCategory : null;
            return true;
        }

        internal bool RemoveFirstDiagnosticSettings(dynamic model)
        {
            IList<DiagnosticSettingsResource> diagnosticsEnablingAuditCategory = model.DiagnosticsEnablingAuditCategory;
            DiagnosticSettingsResource settings = diagnosticsEnablingAuditCategory.FirstOrDefault();
            if (settings == null ||
                (model is DatabaseAuditModel dbModel ?
                Communicator.RemoveDiagnosticSettings(settings.Name, dbModel.ResourceGroupName, dbModel.ServerName, dbModel.DatabaseName) :
                Communicator.RemoveDiagnosticSettings(settings.Name, model.ResourceGroupName, model.ServerName)) == false)
            {
                return false;
            }

            diagnosticsEnablingAuditCategory.RemoveAt(0);
            model.DiagnosticsEnablingAuditCategory = diagnosticsEnablingAuditCategory.Any() ? diagnosticsEnablingAuditCategory : null;
            return true;
        }

        internal virtual void PolicizeAuditModel(AuditModelType model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            dynamicPolicy.State = model.BlobStorageTargetState == AuditStateType.Enabled ||
                model.EventHubTargetState == AuditStateType.Enabled ||
                model.LogAnalyticsTargetState == AuditStateType.Enabled ?
                BlobAuditingPolicyState.Enabled : BlobAuditingPolicyState.Disabled;

            dynamicPolicy.IsAzureMonitorTargetEnabled = model.IsAzureMonitorTargetEnabled;

            if (model.BlobStorageTargetState == AuditStateType.Enabled)
            {
                PolicizeStorageInfo(model, policy);
            }
        }

        internal void ValidateDatabaseInServiceTierForPolicy(string resourceGroupName, string serverName, string databaseName)
        {
            var dbCommunicator = new AzureSqlDatabaseCommunicator(Context);
            var database = dbCommunicator.Get(resourceGroupName, serverName, databaseName);

            if (!Enum.TryParse(database.Edition, true, out Database.Model.DatabaseEdition edition))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture,
                    Properties.Resources.UnsupportedDatabaseEditionForAuditingPolicy, database.Edition));
            }

            if (edition == Database.Model.DatabaseEdition.None || edition == Database.Model.DatabaseEdition.Free)
            {
                throw new Exception(Properties.Resources.DatabaseNotInServiceTierForAuditingPolicy);
            }
        }

        internal virtual void PolicizePublicStorageInfo(AuditModelType model, ProxyResource policy) 
        {
            dynamic dynamicPolicy = (dynamic)policy;

            dynamicPolicy.StorageAccountAccessKey = AzureCommunicator.RetrieveStorageKeysAsync(
                model.StorageAccountResourceId).GetAwaiter().GetResult()[GetStorageKeyKind(model) == StorageKeyKind.Secondary ? StorageKeyKind.Secondary : StorageKeyKind.Primary];
        }

        internal virtual void PolicizeStorageInfo(AuditModelType model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            ExtractStorageAccountProperties(model.StorageAccountResourceId, out string storageAccountName, out Guid storageAccountSubscriptionId);

            dynamicPolicy.StorageEndpoint = GetStorageAccountEndpoint(storageAccountName);
            dynamicPolicy.StorageAccountSubscriptionId = storageAccountSubscriptionId;

            if (AzureCommunicator.IsStorageAccountInVNet(model.StorageAccountResourceId))
            {
                Guid? principalId = Communicator.AssignServerIdentityIfNotAssigned(model.ResourceGroupName, model.ServerName);
                AzureCommunicator.AssignRoleForServerIdentityOnStorageIfNotAssigned(model.StorageAccountResourceId, principalId.Value, RoleAssignmentId);
            }
            else
            {
                PolicizePublicStorageInfo(model, policy);
            }
        }

        private void ExtractStorageAccountProperties(string storageAccountResourceId, out string storageAccountName, out Guid storageAccountSubscriptionId)
        {
            const string separator = "subscriptions/";
            storageAccountResourceId = storageAccountResourceId.Substring(storageAccountResourceId.IndexOf(separator) + separator.Length);
            string[] segments = storageAccountResourceId.Split('/');
            storageAccountSubscriptionId = Guid.Parse(segments[0]);
            storageAccountName = segments[6];
        }

        internal static IList<string> ExtractAuditActionsAndGroups(AuditActionGroups[] auditActionGroup, string[] auditAction = null)
        {
            var actionsAndGroups = new List<string>();
            if (auditAction != null)
            {
                actionsAndGroups.AddRange(auditAction);
            }

            auditActionGroup.ToList().ForEach(aag => actionsAndGroups.Add(aag.ToString()));
            if (actionsAndGroups.Count == 0) // default audit actions and groups in case nothing was defined by the user
            {
                actionsAndGroups.Add("SUCCESSFUL_DATABASE_AUTHENTICATION_GROUP");
                actionsAndGroups.Add("FAILED_DATABASE_AUTHENTICATION_GROUP");
                actionsAndGroups.Add("BATCH_COMPLETED_GROUP");
            }

            return actionsAndGroups;
        }

        /// <summary>
        /// Extracts the storage account name from the given model
        /// </summary>
        private string GetStorageAccountEndpoint(string storageAccountName)
        {
            return string.Format("https://{0}.blob.{1}", storageAccountName, Context.Environment.GetEndpoint(AzureEnvironment.Endpoint.StorageEndpointSuffix));
        }

        internal void PersistAuditChanges(AuditModelType model)
        {
            VerifyAuditBeforePersistChanges(model);

            DiagnosticSettingsResource currentSettings = model.DiagnosticsEnablingAuditCategory?.FirstOrDefault();
            if (currentSettings == null)
            {
                ChangeAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);
            }
            else
            {
                ChangeAuditWhenDiagnosticsEnablingAuditCategoryExist(model, currentSettings);
            }
        }

        internal void RemoveAuditingSettings(AuditModelType model)
        {
            model.BlobStorageTargetState = AuditStateType.Disabled;
            model.EventHubTargetState = AuditStateType.Disabled;
            model.LogAnalyticsTargetState = AuditStateType.Disabled;

            DisableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);

            Exception exception = null;
            while (model.DiagnosticsEnablingAuditCategory != null &&
                model.DiagnosticsEnablingAuditCategory.Any())
            {
                DiagnosticSettingsResource settings = model.DiagnosticsEnablingAuditCategory.First();
                if (IsAnotherCategoryEnabled(settings))
                {
                    if (DisableAuditCategory(model, settings) == false)
                    {
                        exception = DefinitionsCommon.UpdateDiagnosticSettingsException;
                    }
                }
                else
                {
                    if (RemoveFirstDiagnosticSettings(model) == false)
                    {
                        exception = DefinitionsCommon.RemoveDiagnosticSettingsException;
                    }
                }
            }

            if (exception != null)
            {
                throw exception;
            }
        }

        private void ChangeAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(
            AuditModelType model)
        {
            if (model.EventHubTargetState == AuditStateType.Enabled ||
                model.LogAnalyticsTargetState == AuditStateType.Enabled)
            {
                EnableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);
            }
            else
            {
                DisableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);
            }
        }

        private void EnableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(
            AuditModelType model)
        {
            if (CreateDiagnosticSettings(
                model.EventHubTargetState == AuditStateType.Enabled ?
                model.EventHubName : null,
                model.EventHubTargetState == AuditStateType.Enabled ?
                model.EventHubAuthorizationRuleResourceId : null,
                model.LogAnalyticsTargetState == AuditStateType.Enabled ?
                model.WorkspaceResourceId : null,
                model) == false)
            {
                throw DefinitionsCommon.CreateDiagnosticSettingsException;
            }

            try
            {
                model.IsAzureMonitorTargetEnabled = true;

                if (SetAudit(model) == false)
                {
                    throw DefinitionsCommon.SetAuditingSettingsException;
                }
            }
            catch (Exception)
            {
                try
                {
                    RemoveFirstDiagnosticSettings(model);
                }
                catch (Exception) { }

                throw;
            }
        }

        private void DisableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(
            AuditModelType model)
        {
            model.IsAzureMonitorTargetEnabled = null;

            if (SetAudit(model) == false)
            {
                throw DefinitionsCommon.SetAuditingSettingsException;
            }
        }

        private void ChangeAuditWhenDiagnosticsEnablingAuditCategoryExist(
            AuditModelType model,
            DiagnosticSettingsResource settings)
        {
            if (IsAnotherCategoryEnabled(settings))
            {
                ChangeAuditWhenMultipleCategoriesAreEnabled(model, settings);
            }
            else
            {
                ChangeAuditWhenOnlyAuditCategoryIsEnabled(model, settings);
            }
        }

        private void ChangeAuditWhenMultipleCategoriesAreEnabled(
            AuditModelType model,
            DiagnosticSettingsResource settings)
        {
            if (DisableAuditCategory(model, settings) == false)
            {
                throw DefinitionsCommon.UpdateDiagnosticSettingsException;
            }

            try
            {
                ChangeAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);
            }
            catch (Exception)
            {
                try
                {
                    EnableAuditCategory(model, settings);
                }
                catch (Exception) { }

                throw;
            }
        }

        private void ChangeAuditWhenOnlyAuditCategoryIsEnabled(
            AuditModelType model,
            DiagnosticSettingsResource settings)
        {
            string oldEventHubName = settings.EventHubName;
            string oldEventHubAuthorizationRuleId = settings.EventHubAuthorizationRuleId;
            string oldWorkspaceId = settings.WorkspaceId;

            if (model.EventHubTargetState == AuditStateType.Enabled ||
                model.LogAnalyticsTargetState == AuditStateType.Enabled)
            {
                EnableDiagnosticsAuditWhenOnlyAuditCategoryIsEnabled(model, settings, oldEventHubName, oldEventHubAuthorizationRuleId, oldWorkspaceId);
            }
            else
            {
                DisableDiagnosticsAuditWhenOnlyAuditCategoryIsEnabled(model, settings, oldEventHubName, oldEventHubAuthorizationRuleId, oldWorkspaceId);
            }
        }

        private void EnableDiagnosticsAuditWhenOnlyAuditCategoryIsEnabled(
            AuditModelType model,
            DiagnosticSettingsResource settings,
            string oldEventHubName,
            string oldEventHubAuthorizationRuleId,
            string oldWorkspaceId)
        {
            settings.EventHubName = model.EventHubTargetState == AuditStateType.Enabled ?
                model.EventHubName : null;
            settings.EventHubAuthorizationRuleId = model.EventHubTargetState == AuditStateType.Enabled ?
                model.EventHubAuthorizationRuleResourceId : null;
            settings.WorkspaceId = model.LogAnalyticsTargetState == AuditStateType.Enabled ?
                model.WorkspaceResourceId : null;

            if (UpdateDiagnosticSettings(settings, model) == false)
            {
                throw DefinitionsCommon.UpdateDiagnosticSettingsException;
            }

            try
            {
                model.IsAzureMonitorTargetEnabled = true;

                if (SetAudit(model) == false)
                {
                    throw DefinitionsCommon.SetAuditingSettingsException;
                }
            }
            catch (Exception)
            {
                try
                {
                    settings.EventHubName = oldEventHubName;
                    settings.EventHubAuthorizationRuleId = oldEventHubAuthorizationRuleId;
                    settings.WorkspaceId = oldWorkspaceId;
                    UpdateDiagnosticSettings(settings, model);
                }
                catch (Exception) { }

                throw;
            }
        }

        private void DisableDiagnosticsAuditWhenOnlyAuditCategoryIsEnabled(
            AuditModelType model,
            DiagnosticSettingsResource settings,
            string oldEventHubName,
            string oldEventHubAuthorizationRuleId,
            string oldWorkspaceId)
        {
            if (RemoveFirstDiagnosticSettings(model) == false)
            {
                throw DefinitionsCommon.RemoveDiagnosticSettingsException;
            }

            try
            {
                DisableDiagnosticsAuditWhenDiagnosticsEnablingAuditCategoryDoNotExist(model);
            }
            catch (Exception)
            {
                try
                {
                    CreateDiagnosticSettings(oldEventHubName, oldEventHubAuthorizationRuleId, oldWorkspaceId, model);
                }
                catch (Exception) { }

                throw;
            }
        }

        private bool SetAuditCategoryState(
            dynamic model,
            DiagnosticSettingsResource settings,
            bool isEnabled)
        {
            var log = settings?.Logs?.FirstOrDefault(l => string.Equals(l.Category, GetDiagnosticsEnablingAuditCategoryName()));
            if (log != null)
            {
                log.Enabled = isEnabled;
            }

            return UpdateDiagnosticSettings(settings, model);
        }

        internal bool EnableAuditCategory(
            dynamic model,
            DiagnosticSettingsResource settings)
        {
            return SetAuditCategoryState(model, settings, true);
        }

        internal bool DisableAuditCategory(
            dynamic model,
            DiagnosticSettingsResource settings)
        {
            return SetAuditCategoryState(model, settings, false);
        }

        private void VerifyAuditBeforePersistChanges(AuditModelType model)
        {
            if (model.BlobStorageTargetState == AuditStateType.Enabled &&
                string.IsNullOrEmpty(model.StorageAccountResourceId))
            {
                throw DefinitionsCommon.StorageAccountNameParameterException;
            }

            if (model.EventHubTargetState == AuditStateType.Enabled &&
                string.IsNullOrEmpty(model.EventHubAuthorizationRuleResourceId))
            {
                throw DefinitionsCommon.EventHubAuthorizationRuleResourceIdParameterException;
            }

            if (model.LogAnalyticsTargetState == AuditStateType.Enabled &&
                string.IsNullOrEmpty(model.WorkspaceResourceId))
            {
                throw DefinitionsCommon.WorkspaceResourceIdParameterException;
            }

            if (model.DiagnosticsEnablingAuditCategory != null && model.DiagnosticsEnablingAuditCategory.Count > 1)
            {
                throw new Exception($"Operation is not supported when multiple Diagnostic Settings enable {GetDiagnosticsEnablingAuditCategoryName()}");
            }
        }

        internal bool IsAnotherCategoryEnabled(DiagnosticSettingsResource settings)
        {
            return settings.Logs.FirstOrDefault(l => l.Enabled &&
                !string.Equals(l.Category, GetDiagnosticsEnablingAuditCategoryName())) != null ||
                settings.Metrics.FirstOrDefault(m => m.Enabled) != null;
        }
    }

    public abstract class SqlUserAuditAdapter<AuditPolicyType, ExtendedAuditPolicyType, AuditModelType> : SqlAuditAdapter<ExtendedAuditPolicyType, AuditModelType>
        where AuditPolicyType : ProxyResource, new()
        where ExtendedAuditPolicyType : ProxyResource, new()
        where AuditModelType : ServerAuditModel
    {
        public SqlUserAuditAdapter(IAzureContext context, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
        {
        }

        internal override string GetDiagnosticsEnablingAuditCategoryName()
        {
            return DefinitionsCommon.SQLSecurityAuditCategory;
        }

        internal override string GetNextDiagnosticSettingsNamePrefix()
        {
            return DefinitionsCommon.DiagnosticSettingsNamePrefixSQLSecurityAuditEvents;
        }

        internal abstract bool SetAuditingPolicy(string resourceGroup, string serverName, AuditPolicyType policy);

        internal abstract bool SetExtendedAuditingPolicy(string resourceGroup, string serverName, ExtendedAuditPolicyType policy);

        internal override void ModelizeAuditPolicy(AuditModelType model, ExtendedAuditPolicyType policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            ModelizeAuditPolicy(model,
                dynamicPolicy.State, dynamicPolicy.StorageEndpoint, dynamicPolicy.IsStorageSecondaryKeyInUse,
                dynamicPolicy.StorageAccountSubscriptionId, dynamicPolicy.IsAzureMonitorTargetEnabled,
                dynamicPolicy.RetentionDays);

            model.PredicateExpression = dynamicPolicy.PredicateExpression;
            model.AuditActionGroup = ExtractAuditActionGroups(dynamicPolicy.AuditActionsAndGroups);            
        }

        internal override void ModelizeStorageKeyType(AuditModelType model, bool? isSecondary) 
        {
            model.StorageKeyType = isSecondary.HasValue && isSecondary.Value ? StorageKeyKind.Secondary : StorageKeyKind.Primary;
        }

        internal override void ModelizeRetentionInfo(AuditModelType model, int? retentionDays) 
        {
            model.RetentionInDays = Convert.ToUInt32(retentionDays);
        }

        internal override bool SetAudit(AuditModelType model)
        {
            if (string.IsNullOrEmpty(model.PredicateExpression))
            {
                AuditPolicyType policy = new AuditPolicyType();

                PolicizeAuditModel(model, policy);

                return SetAuditingPolicy(model.ResourceGroupName, model.ServerName, policy);
            }

            ExtendedAuditPolicyType extendedPolicy = new ExtendedAuditPolicyType();
            dynamic dynamicExtendedPolicy = (dynamic)extendedPolicy;

            PolicizeAuditModel(model, extendedPolicy);

            dynamicExtendedPolicy.PredicateExpression = model.PredicateExpression;
            
            return SetExtendedAuditingPolicy(model.ResourceGroupName, model.ServerName, extendedPolicy);
        }

        internal override void PolicizeStorageInfo(AuditModelType model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            base.PolicizeStorageInfo(model, policy);

            if (model.RetentionInDays != null)
            {
                dynamicPolicy.RetentionDays = (int)model.RetentionInDays;
            }
        }

        internal override void PolicizePublicStorageInfo(AuditModelType model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            base.PolicizePublicStorageInfo(model, policy);

            dynamicPolicy.IsStorageSecondaryKeyInUse = model.StorageKeyType == StorageKeyKind.Secondary;
        }

        internal override StorageKeyKind GetStorageKeyKind(AuditModelType model)
        {
            return model.StorageKeyType;
        }
    }

    public sealed class SqlServerAuditAdapter : SqlUserAuditAdapter<ServerBlobAuditingPolicy, ExtendedServerBlobAuditingPolicy, ServerAuditModel>
    {
        public SqlServerAuditAdapter(IAzureContext context, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
        {
        }

        internal override ExtendedServerBlobAuditingPolicy GetAuditingPolicy(string resourceGroup, string serverName)
        {
            return Communicator.GetAuditingPolicy(resourceGroup, serverName);
        }

        internal override bool SetAuditingPolicy(string resourceGroup, string serverName, ServerBlobAuditingPolicy policy)
        {
            return Communicator.SetAuditingPolicy(resourceGroup, serverName, policy);
        }

        internal override bool SetExtendedAuditingPolicy(string resourceGroup, string serverName, ExtendedServerBlobAuditingPolicy policy)
        {
            return Communicator.SetExtendedAuditingPolicy(resourceGroup, serverName, policy);
        }

        internal override void PolicizeAuditModel(ServerAuditModel model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            base.PolicizeAuditModel(model, policy);

            dynamicPolicy.AuditActionsAndGroups = ExtractAuditActionsAndGroups(model.AuditActionGroup);
        }
    }

    public sealed class SqlDatabaseAuditAdapter : SqlUserAuditAdapter<DatabaseBlobAuditingPolicy, ExtendedDatabaseBlobAuditingPolicy, DatabaseAuditModel>
    {
        public SqlDatabaseAuditAdapter(IAzureContext context, string databaseName, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
        {
            this.DatabaseName = databaseName;
        }

        internal override ExtendedDatabaseBlobAuditingPolicy GetAuditingPolicy(string resourceGroup, string serverName)
        {
            return Communicator.GetAuditingPolicy(resourceGroup, serverName, DatabaseName);
        }

        internal override bool SetAuditingPolicy(string resourceGroup, string serverName, DatabaseBlobAuditingPolicy policy)
        {
            return Communicator.SetAuditingPolicy(resourceGroup, serverName, DatabaseName, policy);
        }

        internal override bool SetExtendedAuditingPolicy(string resourceGroup, string serverName, ExtendedDatabaseBlobAuditingPolicy policy)
        {
            return Communicator.SetExtendedAuditingPolicy(resourceGroup, serverName, DatabaseName, policy);
        }

        internal override IList<DiagnosticSettingsResource> GetDiagnosticsEnablingAuditCategory(
            string resourceGroupName, string serverName,
            out string nextDiagnosticSettingsName)
        {
            return Communicator.GetDiagnosticsEnablingAuditCategory(out nextDiagnosticSettingsName,
                GetDiagnosticsEnablingAuditCategoryName(), GetNextDiagnosticSettingsNamePrefix(),
                resourceGroupName, serverName, DatabaseName);
        }

        internal override void ModelizeAuditPolicy(DatabaseAuditModel model, ExtendedDatabaseBlobAuditingPolicy policy)
        {
            base.ModelizeAuditPolicy(model, policy);

            model.AuditAction = ExtractAuditActions(policy.AuditActionsAndGroups);
            model.DatabaseName = DatabaseName;
        }

        internal override bool SetAudit(DatabaseAuditModel model)
        {
            ValidateDatabaseInServiceTierForPolicy(model.ResourceGroupName, model.ServerName, model.DatabaseName);

            return base.SetAudit(model);
        }

        internal override void PolicizeAuditModel(DatabaseAuditModel model, ProxyResource policy)
        {
            dynamic dynamicPolicy = (dynamic)policy;

            base.PolicizeAuditModel(model, policy);

            dynamicPolicy.AuditActionsAndGroups = ExtractAuditActionsAndGroups(model.AuditActionGroup, model.AuditAction);
        }

        internal string DatabaseName { get; set; }
    }

    public sealed class SqlDevOpsAuditAdapter : SqlAuditAdapter<ServerDevOpsAuditingPolicy, ServerDevOpsAuditModel>
    {
        public SqlDevOpsAuditAdapter(IAzureContext context, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
        {
        }

        internal override ServerDevOpsAuditingPolicy GetAuditingPolicy(string resourceGroup, string serverName)
        {
            return Communicator.GetDevOpsAuditingPolicy(resourceGroup, serverName);
        }

        internal override bool SetAudit(ServerDevOpsAuditModel model)
        {
            var policy = new ServerDevOpsAuditingPolicy();

            PolicizeAuditModel(model, policy);

            return Communicator.SetDevOpsAuditingPolicy(model.ResourceGroupName, model.ServerName, policy);
        }

        internal override string GetDiagnosticsEnablingAuditCategoryName()
        {
            return DefinitionsCommon.DevOpsAuditCategory;
        }

        internal override string GetNextDiagnosticSettingsNamePrefix()
        {
            return DefinitionsCommon.DiagnosticSettingsNamePrefixDevOpsOperationsAudit;
        }

        internal override void ModelizeAuditPolicy(ServerDevOpsAuditModel model, ServerDevOpsAuditingPolicy policy)
        {
            ModelizeAuditPolicy(model,
                policy.State, policy.StorageEndpoint, null, policy.StorageAccountSubscriptionId,
                policy.IsAzureMonitorTargetEnabled, null);
        }

        internal override StorageKeyKind GetStorageKeyKind(ServerDevOpsAuditModel model)
        {
            return StorageKeyKind.Primary;
        }
    }


















    //public sealed class SqlAuditAdapterRegular : SqlAuditAdapter<ExtendedServerBlobAuditingPolicy, ServerAuditModel>
    //{
    //    public SqlAuditAdapterRegular(IAzureContext context, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
    //    {
    //    }

    //    internal override ExtendedServerBlobAuditingPolicy GetAuditingPolicy(string resourceGroup, string serverName)
    //    {
    //        return Communicator.GetAuditingPolicy(resourceGroup, serverName);
    //    }

    //    internal override bool SetAudit(ServerAuditModel model)
    //    {
    //        if (string.IsNullOrEmpty(model.PredicateExpression))
    //        {
    //            var policy = new ServerBlobAuditingPolicy();
    //            PolicizeAuditModel(model, policy);
    //            return Communicator.SetAuditingPolicy(model.ResourceGroupName, model.ServerName, policy);
    //        }
    //        else
    //        {
    //            var policy = new ExtendedServerBlobAuditingPolicy
    //            {
    //                PredicateExpression = model.PredicateExpression
    //            };
    //            PolicizeAuditModel(model, policy);
    //            return Communicator.SetExtendedAuditingPolicy(model.ResourceGroupName, model.ServerName, policy);
    //        }
    //    }

    //    internal override string GetDiagnosticsEnablingAuditCategoryName()
    //    {
    //        return DefinitionsCommon.SQLSecurityAuditCategory;
    //    }

    //    internal override string GetNextDiagnosticSettingsNamePrefix()
    //    {
    //        return DefinitionsCommon.DiagnosticSettingsNamePrefixSQLSecurityAuditEvents;
    //    }

    //    internal override void ModelizeAuditPolicy(ServerAuditModel model, ExtendedServerBlobAuditingPolicy policy)
    //    {
    //        ModelizeAuditPolicy(model, 
    //            policy.State, policy.StorageEndpoint, policy.StorageAccountSubscriptionId, 
    //            policy.IsAzureMonitorTargetEnabled);

    //        model.StorageKeyType = policy.IsStorageSecondaryKeyInUse.GetValueOrDefault(false) ? StorageKeyKind.Secondary : StorageKeyKind.Primary;
    //        model.PredicateExpression = policy.PredicateExpression;
    //        model.AuditActionGroup = ExtractAuditActionGroups(policy.AuditActionsAndGroups);
    //        model.RetentionInDays = Convert.ToUInt32(policy.RetentionDays);
    //    }
    //}

    //public sealed class SqlAuditAdapterDevOps : SqlAuditAdapter<ServerDevOpsAuditingPolicy, ServerDevOpsAuditModel>
    //{
    //    public SqlAuditAdapterDevOps(IAzureContext context, Guid roleAssignmentId = default(Guid)) : base(context, roleAssignmentId)
    //    {
    //    }

    //    internal override ServerDevOpsAuditingPolicy GetAuditingPolicy(string resourceGroup, string serverName)
    //    {
    //        return Communicator.GetDevOpsAuditingPolicy(resourceGroup, serverName);
    //    }

    //    internal override bool SetAudit(ServerDevOpsAuditModel model)
    //    {
    //        var policy = new ServerDevOpsAuditingPolicy();

    //        PolicizeAuditModel(model, policy);

    //        return Communicator.SetDevOpsAuditingPolicy(model.ResourceGroupName, model.ServerName, policy);
    //    }

    //    internal override string GetDiagnosticsEnablingAuditCategoryName()
    //    {
    //        return DefinitionsCommon.DevOpsAuditCategory;
    //    }

    //    internal override string GetNextDiagnosticSettingsNamePrefix()
    //    {
    //        return DefinitionsCommon.DiagnosticSettingsNamePrefixDevOpsOperationsAudit;
    //    }

    //    internal override void ModelizeAuditPolicy(ServerDevOpsAuditModel model, ServerDevOpsAuditingPolicy policy)
    //    {
    //        ModelizeAuditPolicy(model, 
    //            policy.State, policy.StorageEndpoint, policy.StorageAccountSubscriptionId,
    //            policy.IsAzureMonitorTargetEnabled);
    //    }
    //}
}
