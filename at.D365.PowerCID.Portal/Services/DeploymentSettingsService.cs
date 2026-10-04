using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace at.D365.PowerCID.Portal.Services
{
    public sealed class DeploymentSettingDescriptor
    {
        public DeploymentSettingKind Kind { get; set; }
        public Guid MsId { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string ConnectorId { get; set; }
        public string EnvironmentVariableType { get; set; }
        public string DefaultValue { get; set; }
        public bool IsRequired { get; set; }
    }

    public sealed class DeploymentSettingImportValue
    {
        public DeploymentSettingKind Kind { get; set; }
        public Guid MsId { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string ConnectorId { get; set; }
        public string Value { get; set; }
    }

    public sealed class DeploymentSettingsStatus
    {
        public string ManifestStatus { get; set; }
        public string ConfigurationStatus { get; set; }
        public int Total { get; set; }
        public int Configured { get; set; }
        public int Missing { get; set; }
        public int Inherited { get; set; }
        public IReadOnlyCollection<DeploymentSettingDescriptor> MissingSettings { get; set; }
    }

    public class DeploymentSettingsService
    {
        private const int EnvironmentVariableDefinitionComponentType = 380;
        private readonly atPowerCIDContext dbContext;
        private readonly IConfiguration configuration;
        private readonly ILogger<DeploymentSettingsService> logger;

        public DeploymentSettingsService(
            atPowerCIDContext dbContext,
            IConfiguration configuration,
            ILogger<DeploymentSettingsService> logger)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
            this.logger = logger;
        }

        public static string ComputeManifestHash(IEnumerable<DeploymentSettingDescriptor> descriptors)
        {
            var canonical = string.Join(
                "\n",
                descriptors
                    .OrderBy(e => e.Kind)
                    .ThenBy(e => e.MsId)
                    .Select(e => string.Join(
                        "|",
                        e.Kind,
                        e.MsId.ToString("D"),
                        e.LogicalName ?? string.Empty,
                        e.ConnectorId ?? string.Empty,
                        e.EnvironmentVariableType ?? string.Empty,
                        e.DefaultValue ?? string.Empty,
                        e.IsRequired ? "1" : "0")));

            using var sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
        }

        public async Task<SolutionDeploymentManifest> RefreshDeploymentManifest(int solutionId)
        {
            var solution = await GetTenantIndependentSolution(solutionId);
            var manifest = await dbContext.SolutionDeploymentManifests
                .Include(e => e.Settings)
                    .ThenInclude(e => e.Values)
                .SingleOrDefaultAsync(e => e.SolutionId == solutionId);

            if (manifest == null)
            {
                manifest = new SolutionDeploymentManifest
                {
                    SolutionId = solutionId,
                    DataverseSolutionId = solution.MsId,
                    Status = DeploymentManifestStatus.Draft,
                    CreatedOn = DateTime.UtcNow
                };
                dbContext.SolutionDeploymentManifests.Add(manifest);
                await dbContext.SaveChangesAsync();
            }

            manifest.Status = DeploymentManifestStatus.Syncing;
            manifest.LastSyncError = null;
            manifest.ModifiedOn = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            try
            {
                var descriptors = await DiscoverSettings(solution);
                var dataverseMetadata = await ReadSolutionMetadata(solution, descriptors);
                var newHash = ComputeManifestHash(descriptors);
                var existingSettings = manifest.Settings.ToList();
                var desiredKeys = descriptors.Select(GetSettingKey).ToHashSet();

                foreach (var descriptor in descriptors)
                {
                    var existing = existingSettings.SingleOrDefault(e =>
                        e.Kind == descriptor.Kind && e.MsId == descriptor.MsId);
                    if (existing == null)
                    {
                        existing = new SolutionDeploymentSetting
                        {
                            ManifestId = manifest.Id,
                            Kind = descriptor.Kind,
                            MsId = descriptor.MsId
                        };
                        manifest.Settings.Add(existing);
                    }

                    existing.LogicalName = descriptor.LogicalName;
                    existing.DisplayName = descriptor.DisplayName;
                    existing.ConnectorId = descriptor.ConnectorId;
                    existing.EnvironmentVariableType = descriptor.EnvironmentVariableType;
                    existing.DefaultValue = descriptor.DefaultValue;
                    existing.IsRequired = descriptor.IsRequired;
                    existing.ComponentHash = ComputeComponentHash(descriptor);
                }

                var removedSettings = existingSettings
                    .Where(e => !desiredKeys.Contains(GetSettingKey(e)))
                    .ToList();
                if (removedSettings.Count > 0)
                {
                    dbContext.SolutionDeploymentSettings.RemoveRange(removedSettings);
                    foreach (var removedSetting in removedSettings)
                        manifest.Settings.Remove(removedSetting);
                }

                manifest.DataverseSolutionId = solution.MsId;
                manifest.DataverseVersion = dataverseMetadata.Version ?? solution.Version;
                manifest.DataverseModifiedOn = dataverseMetadata.ModifiedOn;
                manifest.ManifestHash = newHash;
                manifest.Status = DeploymentManifestStatus.Ready;
                manifest.LastSyncedOn = DateTime.UtcNow;
                manifest.ModifiedOn = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                await MigrateLegacyValues(solution, manifest);
                logger.LogInformation(
                    "Deployment manifest {ManifestId} for solution {SolutionId} is ready with {SettingCount} settings.",
                    manifest.Id,
                    solutionId,
                    descriptors.Count);
                return manifest;
            }
            catch (Exception exception)
            {
                manifest.Status = DeploymentManifestStatus.SyncFailed;
                manifest.LastSyncError = exception.Message;
                manifest.ModifiedOn = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                logger.LogError(exception, "Could not refresh deployment manifest for solution {SolutionId}.", solutionId);
                throw;
            }
        }

        public async Task<SolutionDeploymentManifest> EnsureReadyForImport(int solutionId)
        {
            var manifest = await dbContext.SolutionDeploymentManifests
                .SingleOrDefaultAsync(e => e.SolutionId == solutionId);

            if (manifest == null)
                return await RefreshDeploymentManifest(solutionId);

            var status = await ValidateManifest(solutionId);
            if (status != DeploymentManifestStatus.Ready)
                return await RefreshDeploymentManifest(solutionId);

            return await dbContext.SolutionDeploymentManifests
                .Include(e => e.Settings)
                    .ThenInclude(e => e.Values)
                .SingleAsync(e => e.SolutionId == solutionId);
        }

        public async Task<SolutionDeploymentManifest> GetManifest(int solutionId, bool refreshIfNotReady = false)
        {
            if (refreshIfNotReady)
                await EnsureReadyForImport(solutionId);

            return await dbContext.SolutionDeploymentManifests
                .Include(e => e.Settings)
                    .ThenInclude(e => e.Values)
                .SingleOrDefaultAsync(e => e.SolutionId == solutionId);
        }

        public async Task<IReadOnlyCollection<DeploymentSettingImportValue>> GetSettingsForImport(
            int solutionId,
            int environmentId)
        {
            var manifest = await EnsureReadyForImport(solutionId);
            if (manifest.Status != DeploymentManifestStatus.Ready)
                throw new InvalidOperationException("Deployment settings manifest is not ready.");

            var values = await dbContext.SolutionDeploymentSettingValues
                .Where(e => e.EnvironmentId == environmentId && e.SettingNavigation.ManifestId == manifest.Id)
                .ToDictionaryAsync(e => e.SettingId);

            return manifest.Settings
                .Where(setting =>
                    values.TryGetValue(setting.Id, out var value) &&
                    value.IsConfigured &&
                    (setting.Kind != DeploymentSettingKind.ConnectionReference ||
                     !string.IsNullOrWhiteSpace(value.Value)))
                .OrderBy(e => e.Kind)
                .ThenBy(e => e.MsId)
                .Select(setting =>
                {
                    var value = values[setting.Id];
                    return new DeploymentSettingImportValue
                    {
                        Kind = setting.Kind,
                        MsId = setting.MsId,
                        LogicalName = setting.LogicalName,
                        DisplayName = setting.DisplayName,
                        ConnectorId = setting.ConnectorId,
                        Value = value.Value
                    };
                })
                .ToList();
        }

        public async Task CreateActionSnapshot(int actionId, int solutionId, int environmentId)
        {
            if (await dbContext.DeploymentSettingSnapshots.AnyAsync(e => e.ActionId == actionId))
                return;

            var manifest = await EnsureReadyForImport(solutionId);
            var values = await dbContext.SolutionDeploymentSettingValues
                .Where(e => e.EnvironmentId == environmentId && e.SettingNavigation.ManifestId == manifest.Id)
                .ToDictionaryAsync(e => e.SettingId);

            foreach (var setting in manifest.Settings)
            {
                values.TryGetValue(setting.Id, out var value);
                dbContext.DeploymentSettingSnapshots.Add(new DeploymentSettingSnapshot
                {
                    ActionId = actionId,
                    SolutionId = solutionId,
                    EnvironmentId = environmentId,
                    Kind = setting.Kind,
                    MsId = setting.MsId,
                    LogicalName = setting.LogicalName,
                    DisplayName = setting.DisplayName,
                    ConnectorId = setting.ConnectorId,
                    Value = value?.Value,
                    IsConfigured = value?.IsConfigured == true,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<DeploymentSettingImportValue>> GetSettingsForAction(int actionId)
        {
            var snapshots = await dbContext.DeploymentSettingSnapshots
                .Where(e => e.ActionId == actionId && e.IsConfigured)
                .OrderBy(e => e.Kind)
                .ThenBy(e => e.MsId)
                .ToListAsync();
            if (!await dbContext.DeploymentSettingSnapshots.AnyAsync(e => e.ActionId == actionId))
            {
                var manifestSettingCount = await dbContext.Actions
                    .Where(e => e.Id == actionId)
                    .SelectMany(e => e.SolutionNavigation.DeploymentManifests)
                    .SelectMany(e => e.Settings)
                    .CountAsync();
                if (manifestSettingCount != 0)
                    throw new InvalidOperationException("This import action has no deployment settings snapshot.");
                return Array.Empty<DeploymentSettingImportValue>();
            }

            return snapshots
                .Where(e => e.Kind != DeploymentSettingKind.ConnectionReference || !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => new DeploymentSettingImportValue
                {
                    Kind = e.Kind,
                    MsId = e.MsId,
                    LogicalName = e.LogicalName,
                    DisplayName = e.DisplayName,
                    ConnectorId = e.ConnectorId,
                    Value = e.Value
                })
                .ToList();
        }

        public async Task<DeploymentSettingsStatus> GetStatus(int solutionId, int environmentId)
        {
            var manifest = await GetManifest(solutionId);
            if (manifest?.Status == DeploymentManifestStatus.Ready)
            {
                await ValidateManifest(solutionId);
                manifest = await GetManifest(solutionId);
            }
            if (manifest == null)
            {
                return new DeploymentSettingsStatus
                {
                    ManifestStatus = DeploymentManifestStatus.Draft.ToString(),
                    ConfigurationStatus = "Unavailable",
                    MissingSettings = Array.Empty<DeploymentSettingDescriptor>()
                };
            }

            var values = await dbContext.SolutionDeploymentSettingValues
                .Where(e => e.EnvironmentId == environmentId && e.SettingNavigation.ManifestId == manifest.Id)
                .ToDictionaryAsync(e => e.SettingId);
            var configured = manifest.Settings.Count(e =>
                values.TryGetValue(e.Id, out var value) &&
                value.IsConfigured &&
                (e.Kind != DeploymentSettingKind.ConnectionReference || !string.IsNullOrWhiteSpace(value.Value)));
            var missing = manifest.Settings
                .Where(e => !values.TryGetValue(e.Id, out var value) ||
                    !value.IsConfigured ||
                    (e.Kind == DeploymentSettingKind.ConnectionReference && string.IsNullOrWhiteSpace(value.Value)))
                .Select(ToDescriptor)
                .ToList();

            return new DeploymentSettingsStatus
            {
                ManifestStatus = manifest.Status.ToString(),
                ConfigurationStatus = manifest.Status == DeploymentManifestStatus.Ready && missing.Count == 0
                    ? "Complete"
                    : "Incomplete",
                Total = manifest.Settings.Count,
                Configured = configured,
                Missing = missing.Count,
                Inherited = values.Values.Count(e => e.IsInherited),
                MissingSettings = missing
            };
        }

        private async Task<DeploymentManifestStatus> ValidateManifest(int solutionId)
        {
            var manifest = await dbContext.SolutionDeploymentManifests
                .SingleOrDefaultAsync(e => e.SolutionId == solutionId);
            if (manifest == null)
                return DeploymentManifestStatus.Draft;

            var solution = await GetTenantIndependentSolution(solutionId);
            try
            {
                var descriptors = await DiscoverSettings(solution);
                var currentHash = ComputeManifestHash(descriptors);
                if (manifest.Status != DeploymentManifestStatus.Ready ||
                    !string.Equals(manifest.ManifestHash, currentHash, StringComparison.OrdinalIgnoreCase))
                {
                    manifest.Status = DeploymentManifestStatus.Stale;
                    manifest.ModifiedOn = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                    return DeploymentManifestStatus.Stale;
                }

                return DeploymentManifestStatus.Ready;
            }
            catch (Exception exception)
            {
                manifest.Status = DeploymentManifestStatus.SyncFailed;
                manifest.LastSyncError = exception.Message;
                manifest.ModifiedOn = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                logger.LogError(exception, "Could not validate deployment manifest for solution {SolutionId}.", solutionId);
                return DeploymentManifestStatus.SyncFailed;
            }
        }

        public async Task<SolutionDeploymentSettingValue> UpdateValue(
            int solutionId,
            int settingId,
            int environmentId,
            string value,
            bool isConfigured,
            byte[] expectedRowVersion = null)
        {
            var manifest = await dbContext.SolutionDeploymentManifests
                .Include(e => e.SolutionNavigation)
                .SingleOrDefaultAsync(e => e.SolutionId == solutionId);
            if (manifest == null)
                throw new KeyNotFoundException("The selected solution does not have a deployment settings manifest.");
            var setting = await dbContext.SolutionDeploymentSettings
                .SingleOrDefaultAsync(e => e.Id == settingId && e.ManifestId == manifest.Id);
            if (setting == null)
                throw new KeyNotFoundException("The deployment setting does not belong to the selected solution.");

            if (setting.Kind == DeploymentSettingKind.ConnectionReference &&
                isConfigured &&
                string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A configured connection reference requires a connection id.");

            var environment = await dbContext.Environments
                .SingleOrDefaultAsync(e => e.Id == environmentId);
            if (environment == null)
                throw new KeyNotFoundException("The target environment does not exist.");

            var settingValue = await dbContext.SolutionDeploymentSettingValues
                .SingleOrDefaultAsync(e => e.SettingId == settingId && e.EnvironmentId == environmentId);
            if (settingValue == null)
            {
                settingValue = new SolutionDeploymentSettingValue
                {
                    SettingId = settingId,
                    EnvironmentId = environmentId
                };
                dbContext.SolutionDeploymentSettingValues.Add(settingValue);
            }
            else if (expectedRowVersion == null)
            {
                throw new InvalidOperationException("A row version is required when updating an existing deployment setting.");
            }
            else
            {
                dbContext.Entry(settingValue).Property(e => e.RowVersion).OriginalValue = expectedRowVersion;
            }

            settingValue.Value = isConfigured ? value : null;
            settingValue.IsConfigured = isConfigured;
            settingValue.IsInherited = false;
            settingValue.InheritedFromSolutionId = null;
            settingValue.ModifiedBy = await dbContext.Users
                .Where(e => e.MsId == dbContext.MsIdCurrentUser)
                .Select(e => (int?)e.Id)
                .SingleOrDefaultAsync();
            settingValue.ModifiedOn = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return settingValue;
        }

        public async Task<SolutionDeploymentSettingValue> ResetValue(
            int solutionId,
            int settingId,
            int environmentId)
        {
            var solution = await GetTenantIndependentSolution(solutionId);
            var setting = await dbContext.SolutionDeploymentSettings
                .SingleOrDefaultAsync(e => e.Id == settingId &&
                    e.ManifestNavigation.SolutionId == solutionId);
            if (setting == null)
                throw new KeyNotFoundException("The deployment setting does not belong to the selected solution.");

            var predecessor = await GetPredecessor(solution);
            var predecessorSetting = predecessor == null
                ? null
                : await dbContext.SolutionDeploymentSettings
                    .SingleOrDefaultAsync(e => e.ManifestNavigation.SolutionId == predecessor.Id &&
                        e.Kind == setting.Kind &&
                        e.MsId == setting.MsId);
            var predecessorValue = predecessorSetting == null
                ? null
                : await dbContext.SolutionDeploymentSettingValues
                    .SingleOrDefaultAsync(e => e.SettingId == predecessorSetting.Id && e.EnvironmentId == environmentId);
            var currentValue = await dbContext.SolutionDeploymentSettingValues
                .SingleOrDefaultAsync(e => e.SettingId == settingId && e.EnvironmentId == environmentId);

            return await UpdateValue(
                solutionId,
                settingId,
                environmentId,
                predecessorValue?.Value,
                predecessorValue?.IsConfigured == true,
                currentValue?.RowVersion);
        }

        public async Task InitializeManifestAndInherit(int solutionId)
        {
            var solution = await GetTenantIndependentSolution(solutionId);
            var manifest = await RefreshDeploymentManifest(solutionId);
            var predecessor = await GetPredecessor(solution);
            if (predecessor == null)
                return;

            var predecessorManifest = await dbContext.SolutionDeploymentManifests
                .Include(e => e.Settings)
                    .ThenInclude(e => e.Values)
                .SingleOrDefaultAsync(e => e.SolutionId == predecessor.Id);
            if (predecessorManifest == null || predecessorManifest.Status != DeploymentManifestStatus.Ready)
                return;

            var currentSettings = manifest.Settings.ToDictionary(e => (e.Kind, e.MsId));
            var currentValues = await dbContext.SolutionDeploymentSettingValues
                .Where(e => e.SettingNavigation.ManifestId == manifest.Id)
                .ToListAsync();
            var existingValueKeys = currentValues
                .Select(e => (e.SettingId, e.EnvironmentId))
                .ToHashSet();

            // Refreshing a newly created version can migrate legacy application values.
            // A concrete predecessor is authoritative for version creation, so discard
            // those non-versioned values before copying the predecessor snapshot.
            var legacyValues = currentValues
                .Where(e => e.IsInherited && e.InheritedFromSolutionId == null)
                .ToList();
            if (legacyValues.Count > 0)
            {
                dbContext.SolutionDeploymentSettingValues.RemoveRange(legacyValues);
                existingValueKeys = currentValues
                    .Except(legacyValues)
                    .Select(e => (e.SettingId, e.EnvironmentId))
                    .ToHashSet();
            }

            foreach (var predecessorSetting in predecessorManifest.Settings)
            {
                if (!currentSettings.TryGetValue((predecessorSetting.Kind, predecessorSetting.MsId), out var currentSetting))
                    continue;

                foreach (var predecessorValue in predecessorSetting.Values.Where(e => e.IsConfigured))
                {
                    if (existingValueKeys.Contains((currentSetting.Id, predecessorValue.EnvironmentId)))
                        continue;

                    dbContext.SolutionDeploymentSettingValues.Add(new SolutionDeploymentSettingValue
                    {
                        SettingId = currentSetting.Id,
                        EnvironmentId = predecessorValue.EnvironmentId,
                        Value = predecessorValue.Value,
                        IsConfigured = predecessorValue.IsConfigured,
                        IsInherited = true,
                        InheritedFromSolutionId = predecessor.Id,
                        ModifiedBy = predecessorValue.ModifiedBy,
                        ModifiedOn = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task<List<DeploymentSettingDescriptor>> DiscoverSettings(Solution solution)
        {
            using var dataverseClient = CreateDataverseClient(solution.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl);
            var descriptors = new List<DeploymentSettingDescriptor>();

            var environmentVariableQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid")
            };
            environmentVariableQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solution.MsId);
            environmentVariableQuery.Criteria.AddCondition("componenttype", ConditionOperator.Equal, EnvironmentVariableDefinitionComponentType);
            var environmentVariableComponents = await dataverseClient.RetrieveMultipleAsync(environmentVariableQuery);
            foreach (var component in environmentVariableComponents.Entities)
            {
                var msId = component.GetAttributeValue<Guid>("objectid");
                var response = await dataverseClient.RetrieveAsync(
                    "environmentvariabledefinition",
                    msId,
                    new ColumnSet("displayname", "schemaname", "defaultvalue", "type", "isrequired"));
                descriptors.Add(new DeploymentSettingDescriptor
                {
                    Kind = DeploymentSettingKind.EnvironmentVariable,
                    MsId = msId,
                    LogicalName = response.GetAttributeValue<string>("schemaname"),
                    DisplayName = response.GetAttributeValue<string>("displayname"),
                    DefaultValue = response.GetAttributeValue<string>("defaultvalue"),
                    EnvironmentVariableType = response.GetAttributeValue<OptionSetValue>("type")?.Value.ToString(),
                    IsRequired = response.GetAttributeValue<bool?>("isrequired") ?? false
                });
            }

            var connectionReferenceQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid")
            };
            connectionReferenceQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solution.MsId);
            connectionReferenceQuery.AddLink(
                "connectionreference",
                "objectid",
                "connectionreferenceid",
                JoinOperator.Inner);
            var connectionReferenceComponents = await dataverseClient.RetrieveMultipleAsync(connectionReferenceQuery);
            foreach (var component in connectionReferenceComponents.Entities)
            {
                var msId = component.GetAttributeValue<Guid>("objectid");
                var response = await dataverseClient.RetrieveAsync(
                    "connectionreference",
                    msId,
                    new ColumnSet("connectionreferencedisplayname", "connectionreferencelogicalname", "connectorid"));
                descriptors.Add(new DeploymentSettingDescriptor
                {
                    Kind = DeploymentSettingKind.ConnectionReference,
                    MsId = msId,
                    LogicalName = response.GetAttributeValue<string>("connectionreferencelogicalname"),
                    DisplayName = response.GetAttributeValue<string>("connectionreferencedisplayname"),
                    ConnectorId = response.GetAttributeValue<string>("connectorid"),
                    IsRequired = true
                });
            }

            return descriptors
                .GroupBy(e => (e.Kind, e.MsId))
                .Select(e => e.First())
                .OrderBy(e => e.Kind)
                .ThenBy(e => e.MsId)
                .ToList();
        }

        private async Task<(string Version, DateTime? ModifiedOn)> ReadSolutionMetadata(
            Solution solution,
            IReadOnlyCollection<DeploymentSettingDescriptor> descriptors)
        {
            using var dataverseClient = CreateDataverseClient(solution.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl);
            var response = await dataverseClient.RetrieveAsync(
                "solution",
                solution.MsId,
                new ColumnSet("version", "modifiedon"));
            return (response.GetAttributeValue<string>("version"), response.GetAttributeValue<DateTime?>("modifiedon"));
        }

        private async Task MigrateLegacyValues(Solution solution, SolutionDeploymentManifest manifest)
        {
            var settingIds = manifest.Settings.ToDictionary(e => (e.Kind, e.MsId), e => e.Id);
            var environmentVariables = await dbContext.EnvironmentVariableEnvironments
                .Include(e => e.EnvironmentVariableNavigation)
                .Where(e => e.EnvironmentVariableNavigation.Application == solution.Application)
                .ToListAsync();
            foreach (var legacyValue in environmentVariables)
            {
                AddLegacyValueIfMissing(
                    settingIds,
                    (DeploymentSettingKind.EnvironmentVariable, legacyValue.EnvironmentVariableNavigation.MsId),
                    legacyValue.Environment,
                    legacyValue.Value);
            }

            var connectionReferences = await dbContext.ConnectionReferenceEnvironments
                .Include(e => e.ConnectionReferenceNavigation)
                .Where(e => e.ConnectionReferenceNavigation.Application == solution.Application)
                .ToListAsync();
            foreach (var legacyValue in connectionReferences)
            {
                AddLegacyValueIfMissing(
                    settingIds,
                    (DeploymentSettingKind.ConnectionReference, legacyValue.ConnectionReferenceNavigation.MsId),
                    legacyValue.Environment,
                    legacyValue.ConnectionId);
            }

            await dbContext.SaveChangesAsync();
        }

        private void AddLegacyValueIfMissing(
            IReadOnlyDictionary<(DeploymentSettingKind Kind, Guid MsId), int> settingIds,
            (DeploymentSettingKind Kind, Guid MsId) key,
            int environmentId,
            string value)
        {
            if (!settingIds.TryGetValue(key, out var settingId) ||
                dbContext.SolutionDeploymentSettingValues.Any(e =>
                    e.SettingId == settingId && e.EnvironmentId == environmentId))
                return;

            dbContext.SolutionDeploymentSettingValues.Add(new SolutionDeploymentSettingValue
            {
                SettingId = settingId,
                EnvironmentId = environmentId,
                Value = value,
                IsConfigured = value != null,
                IsInherited = true,
                ModifiedOn = DateTime.UtcNow
            });
        }

        private async Task<Solution> GetTenantIndependentSolution(int solutionId)
        {
            var solution = await dbContext.Solutions
                .Include(e => e.ApplicationNavigation)
                    .ThenInclude(e => e.DevelopmentEnvironmentNavigation)
                .SingleOrDefaultAsync(e => e.Id == solutionId);
            if (solution == null)
                throw new KeyNotFoundException("The selected solution does not exist.");
            return solution;
        }

        private async Task<Solution> GetPredecessor(Solution solution)
        {
            return await dbContext.Solutions
                .Where(e => e.Application == solution.Application &&
                    e.Id != solution.Id &&
                    e.CreatedOn < solution.CreatedOn)
                .OrderByDescending(e => e.CreatedOn)
                .FirstOrDefaultAsync();
        }

        private ServiceClient CreateDataverseClient(string basicUrl)
        {
            return new ServiceClient(
                new Uri(basicUrl),
                configuration["AzureAd:ClientId"],
                configuration["AzureAd:ClientSecret"],
                true);
        }

        private static string GetSettingKey(DeploymentSettingDescriptor descriptor)
            => $"{descriptor.Kind}:{descriptor.MsId:D}";

        private static string GetSettingKey(SolutionDeploymentSetting setting)
            => $"{setting.Kind}:{setting.MsId:D}";

        private static string ComputeComponentHash(DeploymentSettingDescriptor descriptor)
            => ComputeManifestHash(new[] { descriptor });

        private static DeploymentSettingDescriptor ToDescriptor(SolutionDeploymentSetting setting)
        {
            return new DeploymentSettingDescriptor
            {
                Kind = setting.Kind,
                MsId = setting.MsId,
                LogicalName = setting.LogicalName,
                DisplayName = setting.DisplayName,
                ConnectorId = setting.ConnectorId,
                EnvironmentVariableType = setting.EnvironmentVariableType,
                DefaultValue = setting.DefaultValue,
                IsRequired = setting.IsRequired
            };
        }
    }
}
