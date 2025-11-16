namespace AzureDevOps.Constants;

public static class ApprovalStatus
{
    public const string Approved = "approved";
    public const string Canceled = "canceled";
    public const string Pending = "pending";
    public const string Reassigned = "reassigned";
    public const string Rejected = "rejected";
    public const string Skipped = "skipped";
    public const string Undefined = "undefined";
    
    /* Approval Status
    approved - Indicates the approval is approved.
    canceled - Indicates the approval is canceled.
    pending - Indicates the approval is pending.
    reassigned - Indicates the approval is reassigned.
    rejected - Indicates the approval is rejected.
    skipped - Indicates the approval is skipped.
    undefined - Indicates the approval does not have the status set.
    */
}