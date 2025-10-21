using System.Text.Json.Serialization;

namespace AzureDevOps.Models;

public record AgentSpecification(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("demands")] string[] Demands
);

public record DeploymentInput(
    [property: JsonPropertyName("parallelExecution")] ParallelExecution ParallelExecution,
    [property: JsonPropertyName("agentSpecification")] AgentSpecification AgentSpecification,
    [property: JsonPropertyName("skipArtifactsDownload")] bool SkipArtifactsDownload,
    [property: JsonPropertyName("artifactsDownloadInput")] ArtifactsDownloadInput? ArtifactsDownloadInput,
    [property: JsonPropertyName("queueId")] int QueueId,
    [property: JsonPropertyName("demands")] string[] Demands,
    [property: JsonPropertyName("enableAccessToken")] bool EnableAccessToken,
    [property: JsonPropertyName("timeoutInMinutes")] int TimeoutInMinutes,
    [property: JsonPropertyName("jobCancelTimeoutInMinutes")] int JobCancelTimeoutInMinutes,
    [property: JsonPropertyName("condition")] string Condition,
    [property: JsonPropertyName("overrideInputs")] Dictionary<string, object> OverrideInputs
);

public record ParallelExecution(
    [property: JsonPropertyName("parallelExecutionType")] string ParallelExecutionType
);

public record ArtifactsDownloadInput(
    [property: JsonPropertyName("downloadInputs")] DownloadInput[] DownloadInputs
);

public record DownloadInput(
    [property: JsonPropertyName("artifactItems")] string ArtifactItems,
    [property: JsonPropertyName("alias")] string Alias
);

public record EnvironmentDefinition(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("variables")] Dictionary<string, Variable> Variables,
    [property: JsonPropertyName("deploymentInput")] DeploymentInput DeploymentInput,
    [property: JsonPropertyName("conditions")] Condition[] Conditions,
    [property: JsonPropertyName("executionPolicy")] ExecutionPolicy ExecutionPolicy
);

public record Condition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("conditionType")] string ConditionType,
    [property: JsonPropertyName("value")] string Value
);

public record ExecutionPolicy(
    [property: JsonPropertyName("concurrencyCount")] int ConcurrencyCount,
    [property: JsonPropertyName("queueDepthCount")] int QueueDepthCount
);

public record UpdateAgentSpecificationRequest(
    [property: JsonPropertyName("environments")] EnvironmentDefinition[] Environments
);

public record EnvironmentAgentInfo(
    int PipelineId,
    string PipelineName,
    int EnvironmentId,
    string EnvironmentName,
    string CurrentAgentSpec,
    bool CanUpdate
);
