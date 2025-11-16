namespace AzureDevOps.Constants;

public static class EnvReleaseStatus
{
    public const string Canceled = "canceled";
    public const string InProgress = "inProgress";
    public const string NotStarted = "notStarted";
    public const string PartiallySucceeded = "partiallySucceeded";
    public const string Queued = "queued";
    public const string Rejected = "rejected";
    public const string Scheduled = "scheduled";
    public const string Succeeded = "succeeded";
    public const string Undefined = "undefined";
    
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
}
