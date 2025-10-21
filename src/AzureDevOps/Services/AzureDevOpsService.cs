using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureDevOps.Models;
using Microsoft.Extensions.Options;

namespace AzureDevOps.Services;

public interface IAzureDevOpsQuery
{
    Task<ReleasePipelinesResponse> GetReleasePipelinesAsync(CancellationToken cancel);
    Task<ReleasePipeline?> GetReleasePipelineAsync(int pipelineId, CancellationToken cancel);
    Task<ReleasesResponse> GetReleasesAsync(int pipelineId, CancellationToken cancel);
    Task<Release?> GetReleaseAsync(int releaseId, CancellationToken cancel);
    Task<List<EnvironmentAgentInfo>> GetEnvironmentAgentSpecificationsAsync(CancellationToken cancel);
    Task<ReleasePipeline?> GetReleasePipelineDefinitionAsync(int pipelineId, CancellationToken cancel);

    Task<ReleaseEnvironmentDetails> GetEnvironmentDetailsAsync(string pipelineId, string releaseId, CancellationToken cancel);
    EnvironmentDetails? GetEnvironmentDetails(string environmentId);
}

public record ReleaseEnvironmentDetails(Release? Release, Dictionary<int, EnvironmentDetails> EnvironmentDetails);

public interface IAzureDevOpsCommand
{
    public record StartReleaseRequest(int ReleaseId, int EnvId, string Status, DateTime? ScheduledTime);
    public record UpdateAgentSpecRequest(int PipelineId, int EnvironmentId, string NewAgentSpec);
    Task StartRelease(StartReleaseRequest request, CancellationToken cancel);
    Task StartReleases(IEnumerable<StartReleaseRequest> requests, CancellationToken cancel);
    Task ApproveRelease(int approvalId, CancellationToken cancel);
    Task ApproveReleases(IEnumerable<int> approvalIds, CancellationToken cancel);
    Task UpdateAgentSpecification(UpdateAgentSpecRequest request, CancellationToken cancel);
    Task UpdateAgentSpecifications(IEnumerable<UpdateAgentSpecRequest> requests, CancellationToken cancel);
}

public class AzureDevOpsService : IAzureDevOpsQuery, IAzureDevOpsCommand
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDevOpsService> _logger;

    private readonly string? _apiVersion;
    private readonly string? _apiVersionForPatchRelease;
    private readonly string? _apiVersionForPatchApproval;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public AzureDevOpsService(
        HttpClient httpClient, 
        IOptions<AppSettings> appSettings,
        ILogger<AzureDevOpsService> logger)
    {
        _httpClient = httpClient;
        _apiVersion = appSettings.Value.ApiVersion;
        _apiVersionForPatchRelease = appSettings.Value.ApiVersionForPatchRelease;
        _apiVersionForPatchApproval = appSettings.Value.ApiVersionForPatchApproval;
        _logger = logger;
        
        // Configure the HttpClient with the base URL and authentication header
        _httpClient.BaseAddress = new Uri($"https://vsrm.dev.azure.com/{appSettings.Value.Organization}/{appSettings.Value.Project}/");
        
        // Create Basic Authentication header with PAT
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", appSettings.Value.PAT);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public async Task<ReleasePipelinesResponse> GetReleasePipelinesAsync(CancellationToken cancel)
    {
        try
        {
            var response = await _httpClient.GetAsync($"_apis/release/definitions{(_apiVersion != null ? $"?api-version={_apiVersion}" : "")}", cancel);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancel);
            var pipelinesResponse = JsonSerializer.Deserialize<ReleasePipelinesResponse>(content, JsonOptions);

            return pipelinesResponse ?? new ReleasePipelinesResponse(0, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipelines from Azure DevOps");
            throw;
        }
    }

    public async Task<ReleasePipeline?> GetReleasePipelineAsync(int pipelineId, CancellationToken cancel)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"_apis/release/definitions/{pipelineId}{(_apiVersion != null ? $"?api-version={_apiVersion}" : "")}",
                cancel);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancel);
            var pipelinesResponse = JsonSerializer.Deserialize<ReleasePipeline>(content, JsonOptions);
            return pipelinesResponse;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipelines from Azure DevOps");
            throw;
        }
    }

    public async Task<ReleasePipeline?> GetReleasePipelineDefinitionAsync(int pipelineId, CancellationToken cancel)
    {
        try
        {
            // Get full pipeline definition with environments details
            var response = await _httpClient.GetAsync($"_apis/release/definitions/{pipelineId}?$expand=environments{(_apiVersion != null ? $"&api-version={_apiVersion}" : "")}", cancel);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancel);
            var pipelineDefinition = JsonSerializer.Deserialize<ReleasePipeline>(content, JsonOptions);
            return pipelineDefinition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipeline definition from Azure DevOps");
            throw;
        }
    }

    public Task<ReleaseEnvironmentDetails> GetEnvironmentDetailsAsync(string pipelineId, string releaseId, CancellationToken cancel)
    { // TODO...
        return Task.FromResult(new ReleaseEnvironmentDetails(null, new Dictionary<int, EnvironmentDetails>()));
    }

    public EnvironmentDetails? GetEnvironmentDetails(string environmentId)
    { // TODO...
        return null;
    }

    public async Task<List<EnvironmentAgentInfo>> GetEnvironmentAgentSpecificationsAsync(CancellationToken cancel)
    {
        try
        {
            var pipelines = await GetReleasePipelinesAsync(cancel);
            var environmentAgentInfos = new List<EnvironmentAgentInfo>();

            foreach (var pipeline in pipelines.Value)
            {
                if (pipeline.IsDeleted || pipeline.IsDisabled)
                    continue;

                try
                {
                    // Get full pipeline definition with expanded environments
                    var response = await _httpClient.GetAsync(
                        $"_apis/release/definitions/{pipeline.Id}?$expand=environments{(_apiVersion != null ? $"&api-version={_apiVersion}" : "")}",
                        cancel);
                        
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to get pipeline definition for pipeline {PipelineId}: {StatusCode}", 
                            pipeline.Id, response.StatusCode);
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync(cancel);
                    var pipelineJson = JsonDocument.Parse(content);
                    
                    if (pipelineJson.RootElement.TryGetProperty("environments", out var environmentsArray))
                    {
                        foreach (var envElement in environmentsArray.EnumerateArray())
                        {
                            var envId = envElement.GetProperty("id").GetInt32();
                            var envName = envElement.GetProperty("name").GetString() ?? "Unknown";
                            
                            // Extract agent specification from deployment input
                            var agentSpec = ExtractAgentSpecification(envElement);
                            var canUpdate = DetermineIfCanUpdate(envElement);
                            
                            var envInfo = new EnvironmentAgentInfo(
                                pipeline.Id,
                                pipeline.Name,
                                envId,
                                envName,
                                agentSpec,
                                canUpdate
                            );
                            environmentAgentInfos.Add(envInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get environment details for pipeline {PipelineId}", pipeline.Id);
                }
            }

            return environmentAgentInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching environment agent specifications from Azure DevOps");
            throw;
        }
    }

    private string ExtractAgentSpecification(JsonElement environmentElement)
    {
        try
        {
            // Try to find agent specification in various possible locations
            // Path 1: environments[].deployPhases[].deploymentInput.agentSpecification.name
            if (environmentElement.TryGetProperty("deployPhases", out var deployPhases))
            {
                foreach (var phase in deployPhases.EnumerateArray())
                {
                    if (!phase.TryGetProperty("deploymentInput", out var deploymentInput)) continue;
                    if (deploymentInput.TryGetProperty("agentSpecification", out var agentSpec))
                    {
                        if (agentSpec.TryGetProperty("name", out var nameProperty))
                        {
                            return nameProperty.GetString() ?? "unknown";
                        }
                    }
                        
                    // Alternative: Check for demands that might indicate Windows version
                    if (!deploymentInput.TryGetProperty("demands", out var demands)) continue;
                    foreach (var demand in demands.EnumerateArray())
                    {
                        var demandStr = demand.GetString() ?? "";
                        if (!demandStr.Contains("Agent.OS") || !demandStr.Contains("Windows")) continue;
                        if (demandStr.Contains("2019")) return "windows-2019";
                        if (demandStr.Contains("2022")) return "windows-2022";
                        if (demandStr.Contains("latest")) return "windows-latest";
                    }
                }
            }
            
            // Path 2: Look in conditions or other properties
            if (!environmentElement.TryGetProperty("conditions", out var conditions)) return "unknown";
            foreach (var condition in conditions.EnumerateArray())
            {
                if (!condition.TryGetProperty("value", out var conditionValue)) continue;
                var valueStr = conditionValue.GetString() ?? "";
                if (valueStr.Contains("windows-2019")) return "windows-2019";
                if (valueStr.Contains("windows-2022")) return "windows-2022";
                if (valueStr.Contains("windows-latest")) return "windows-latest";
                if (valueStr.Contains("ubuntu")) return "ubuntu-latest";
                if (valueStr.Contains("macos")) return "macos-latest";
            }

            return "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting agent specification from environment JSON");
            return "error";
        }
    }

    private static bool DetermineIfCanUpdate(JsonElement environmentElement)
    {
        try
        {
            // Check if environment has deployment phases that can be updated
            if (!environmentElement.TryGetProperty("deployPhases", out var deployPhases)) return true;
            foreach (var phase in deployPhases.EnumerateArray())
            {
                if (!phase.TryGetProperty("phaseType", out var phaseType)) continue;
                var phaseTypeStr = phaseType.GetString();
                // Agent-based deployment phases can typically be updated
                if (phaseTypeStr is "agentBasedDeployment" or "deploymentGroup")
                {
                    return true;
                }
            }

            // Default to allowing updates for most environments
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<ReleasesResponse> GetReleasesAsync(int pipelineId, CancellationToken cancel)
    {
        try
        {
            var response = await _httpClient.GetAsync($"_apis/release/releases?definitionId={pipelineId}{(_apiVersion != null ? $"&api-version={_apiVersion}" : "")}", cancel);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancel);
            var releaseResponse = JsonSerializer.Deserialize<ReleasesResponse>(content, JsonOptions);

            return releaseResponse ?? new ReleasesResponse(0, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipelines from Azure DevOps");
            throw;
        }
    }
    
    public async Task<Release?> GetReleaseAsync(int releaseId, CancellationToken cancel)
    {
        try
        {
            var response = await _httpClient.GetAsync($"_apis/release/releases/{releaseId}{(_apiVersion != null ? $"?api-version={_apiVersion}" : "")}", cancel);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancel);
            var releaseResponse = JsonSerializer.Deserialize<Release>(content, JsonOptions);
            return releaseResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipelines from Azure DevOps");
            throw;
        }
    }
    
    
    public async Task ApproveRelease(int approvalId, CancellationToken cancel)
    {
        try
        {
            // PATCH https://vsrm.dev.azure.com/{organization}/{project}/_apis/release/approvals?api-version=7.2-preview.4
            var request = new PatchReleaseApprovalRequest("approved", "Approved via Azure API");
            var json = JsonSerializer.Serialize(request);
            var jsonBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var parameter = (_apiVersionForPatchApproval != null ? $"?api-version={_apiVersionForPatchApproval}" : "");
            var response = await _httpClient.PatchAsync($"_apis/release/approvals/{approvalId}{parameter}", jsonBody, cancel);
            
            await response.Content.ReadAsStringAsync(cancel);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching release pipelines from Azure DevOps");
            throw;
        }
    }

    public async Task StartReleases(IEnumerable<IAzureDevOpsCommand.StartReleaseRequest> requests, CancellationToken cancel)
    {
        var tasks = requests.Select(request => StartRelease(request, cancel));
        await Task.WhenAll(tasks);
    }
    
    public async Task ApproveReleases(IEnumerable<int> approvalIds, CancellationToken cancel)
    {
        var tasks = approvalIds.Select(approvalId => ApproveRelease(approvalId, cancel));
        await Task.WhenAll(tasks);
    }

    public async Task UpdateAgentSpecification(IAzureDevOpsCommand.UpdateAgentSpecRequest request, CancellationToken cancel)
    {
        try
        {
            // Get the current pipeline definition
            var response = await _httpClient.GetAsync(
                $"_apis/release/definitions/{request.PipelineId}?$expand=environments{(_apiVersion != null ? $"&api-version={_apiVersion}" : "")}",
                cancel);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancel);
            var pipelineDoc = JsonDocument.Parse(content);
            
            // Create a mutable copy of the pipeline definition
            var updatedPipeline = UpdateAgentSpecInPipelineDefinition(pipelineDoc.RootElement, request.EnvironmentId, request.NewAgentSpec);
            
            // PUT the updated definition back
            var jsonContent = JsonSerializer.Serialize(updatedPipeline, JsonOptions);
            var jsonBody = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var putResponse = await _httpClient.PutAsync(
                $"_apis/release/definitions/{request.PipelineId}{(_apiVersion != null ? $"?api-version={_apiVersion}" : "")}",
                jsonBody, cancel);
            putResponse.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Successfully updated agent specification for pipeline {PipelineId}, environment {EnvironmentId} to {NewAgentSpec}",
                request.PipelineId, request.EnvironmentId, request.NewAgentSpec);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent specification for pipeline {PipelineId}, environment {EnvironmentId}",
                request.PipelineId, request.EnvironmentId);
            throw;
        }
    }

    public async Task UpdateAgentSpecifications(IEnumerable<IAzureDevOpsCommand.UpdateAgentSpecRequest> requests, CancellationToken cancel)
    {
        // Process requests in parallel with some concurrency control
        var semaphore = new SemaphoreSlim(3, 3); // Limit to 3 concurrent requests
        var tasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync(cancel);
            try
            {
                await UpdateAgentSpecification(request, cancel);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
    }

    private Dictionary<string, object> UpdateAgentSpecInPipelineDefinition(JsonElement pipelineDefinition, int environmentId, string newAgentSpec)
    {
        try
        {
            // Convert JsonElement to a mutable dictionary structure
            var pipelineDict = JsonSerializer.Deserialize<Dictionary<string, object>>(pipelineDefinition.GetRawText(), JsonOptions);

            if (pipelineDict?.TryGetValue("environments", out var environmentsObj) != true || environmentsObj is not JsonElement environmentsElement)
                return pipelineDict ?? new Dictionary<string, object>();
            var environmentsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(environmentsElement.GetRawText(), JsonOptions);

            if (environmentsList == null) return pipelineDict;
            foreach (var environment in environmentsList)
            {
                if (!environment.TryGetValue("id", out var envIdObj) ||
                    envIdObj is not JsonElement envIdElement ||
                    envIdElement.GetInt32() != environmentId) continue;
                // Update the agent specification for this environment
                UpdateEnvironmentAgentSpec(environment, newAgentSpec);
                break;
            }
                        
            pipelineDict["environments"] = environmentsList;

            return pipelineDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent specification in pipeline definition");
            throw;
        }
    }

    private void UpdateEnvironmentAgentSpec(Dictionary<string, object> environment, string newAgentSpec)
    {
        try
        {
            if (!environment.TryGetValue("deployPhases", out var deployPhasesObj)) return;
            if (deployPhasesObj is not JsonElement deployPhasesElement) return;
            var deployPhasesList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(deployPhasesElement.GetRawText(), JsonOptions);

            if (deployPhasesList == null) return;
            foreach (var phase in deployPhasesList)
            {
                if (!phase.TryGetValue("deploymentInput", out var deploymentInputObj)) continue;
                if (deploymentInputObj is not JsonElement deploymentInputElement) continue;
                var deploymentInput = JsonSerializer.Deserialize<Dictionary<string, object>>(deploymentInputElement.GetRawText(), JsonOptions);

                if (deploymentInput == null) continue;
                // Update agent specification
                if (!deploymentInput.ContainsKey("agentSpecification"))
                {
                    deploymentInput["agentSpecification"] = new Dictionary<string, object>();
                }
                                        
                switch (deploymentInput["agentSpecification"])
                {
                    case JsonElement agentSpecElement:
                    {
                        var agentSpec = JsonSerializer.Deserialize<Dictionary<string, object>>(agentSpecElement.GetRawText(), JsonOptions) 
                                        ?? new Dictionary<string, object>();
                        agentSpec["name"] = newAgentSpec;
                        deploymentInput["agentSpecification"] = agentSpec;
                        break;
                    }
                    case Dictionary<string, object> agentSpecDict:
                        agentSpecDict["name"] = newAgentSpec;
                        break;
                }
                                        
                // Also update demands if they reference specific Windows versions
                if (!deploymentInput.TryGetValue("demands", out var demandsObj)) continue;
                
                if (demandsObj is JsonElement demandsElement)
                {
                    var demands = JsonSerializer.Deserialize<List<string>>(demandsElement.GetRawText(), JsonOptions);
                    if (demands == null) continue;
                    
                    for (var i = 0; i < demands.Count; i++)
                    {
                        // Update Windows version demands
                        if (demands[i].Contains("windows-2019"))
                        {
                            demands[i] = demands[i].Replace("windows-2019", newAgentSpec);
                        }
                        else if (demands[i].Contains("windows-2022"))
                        {
                            demands[i] = demands[i].Replace("windows-2022", newAgentSpec);
                        }
                    }
                    deploymentInput["demands"] = demands;
                    
                }
                
                                        
                phase["deploymentInput"] = deploymentInput;
            }
                        
            environment["deployPhases"] = deployPhasesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating environment agent specification");
            throw;
        }
    }

    public async Task StartRelease(IAzureDevOpsCommand.StartReleaseRequest releaseRequest, CancellationToken cancel)
    {
        try
        {
            var request = (status: releaseRequest.Status, scheduledTime: releaseRequest.ScheduledTime) switch
            {
                ("notStarted", _) =>
                    new PatchReleaseEnvironmentRequest("inProgress", null),
                ("inProgress", { } scheduledTime) =>
                    new PatchReleaseEnvironmentRequest(null, scheduledTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")),
                ("inProgress", null) =>
                    new PatchReleaseEnvironmentRequest(null, 
                        DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ")),
                _ => throw new NotImplementedException($"Combination of status '{releaseRequest.Status}' and scheduledTime '{releaseRequest.ScheduledTime}' is not supported")
            };
            
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var jsonBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var parameter = (_apiVersionForPatchRelease != null ? $"?api-version={_apiVersionForPatchRelease}" : "");
            var response = await _httpClient.PatchAsync($"_apis/release/releases/{releaseRequest.ReleaseId}/environments/{releaseRequest.EnvId}{parameter}", jsonBody, cancel);
            
            await response.Content.ReadAsStringAsync(cancel);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting/scheduling release environment in Azure DevOps");
            throw;
        }
    }
}

/* Approval Status
approved - Indicates the approval is approved.
canceled - Indicates the approval is canceled.
pending - Indicates the approval is pending.
reassigned - Indicates the approval is reassigned.
rejected - Indicates the approval is rejected.
skipped - Indicates the approval is skipped.
undefined - Indicates the approval does not have the status set.
 */

/* Env Release Status
canceled - Environment is in canceled state.
inProgress - Environment is in progress state.
notStarted - Environment is in not started state.
partiallySucceeded - Environment is in partially succeeded state.
queued - Environment is in queued state.
rejected - Environment is in rejected state.
scheduled - Environment is in scheduled state.
succeeded - Environment is in succeeded state.
undefined - Environment status not set.
*/