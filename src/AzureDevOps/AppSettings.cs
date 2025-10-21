namespace AzureDevOps;

public class AppSettings
{
    public string PAT { get; set; } = string.Empty; // store as base64(username:PAT)
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string? ApiVersion { get; set; }
    public string ApiVersionForPatchRelease { get; set; } = null!;
    public string ApiVersionForPatchApproval { get; set; } = null!;
}
