using System.Text.Json.Serialization;

namespace AzureDevOps.Models;

public record Link(string Href);
public record Variable(string? Value, bool IsSecret);

public record Release(
    int Id,
    string Name,
    string Status,
    Release.Environment[] Environments,
    Dictionary<string, Variable> Variables,
    [property: JsonPropertyName("_links")] Dictionary<string, Link> Links
)
{
    public record Environment(
        int Id,
        int ReleaseId,
        string Name,
        string Status,
        Approval[] PreDeployApprovals,
        DeployStep[] DeploySteps
    );

    public record Approval(
        int Id,
        string ApprovalType,
        string Status
    );

    public record DeployStep(
        int Id,
        int DeploymentId,
        int Attempt,
        bool HasStarted,
        string Reason,
        string Status,
        string OperationStatus,
        string QueuedOn
        );
}

public record ReleasePipeline(
    int Id,
    string Name,
    [property: JsonPropertyName("_links")] Dictionary<string, Link> Links,
    ReleasePipeline.Environment[] Environments,
    Dictionary<string, Variable> Variables
)
{
    public record Environment(
        int Id,
        string Name,
        Dictionary<string, Variable> Variables,
        Environment.Release CurrentRelease)
    {
        public record Release(int Id, string Url);
    }

}

public record ReleasesResponse(int Count, ReleasesResponse.Release[] Value)
{
    public record Release(int Id, string Name, string Status, DateTimeOffset CreatedOn, string LogsContainerUrl, 
        [property: JsonPropertyName("_links")]
        Dictionary<string, Link> Links);
}

public record ReleasePipelinesResponse(int Count, ReleasePipelinesResponse.ReleasePipeline[] Value)
{
    public record ReleasePipeline(
        int Id,
        string Name,
        bool IsDeleted,
        bool IsDisabled,
        [property: JsonPropertyName("_links")]
        Dictionary<string, Link> Links
    );
}

public record PatchReleaseEnvironmentRequest(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("scheduledDeploymentTime")] string? ScheduledDeploymentTime
);

public record PatchReleaseApprovalRequest(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("comments")] string? Comments);