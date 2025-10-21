using System.Text.Json.Serialization;
using AzureDevOps.Services;
using AzureDevOps.Web.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using RazorComponentHelpers;

namespace AzureDevOps.Web.Pages;

public partial class Main
{
    public const string ApiPipelines = "/main/pipelines";
    public static string ApiPipeline(string id = "{id}") => $"/main/pipelines/{id}";
    public static string ApiReleases(string id = "{id}") => $"/main/pipelines/{id}/releases";
    public static string ApiRelease(string pipelineId = "{pipelineId}", string releaseId = "{releaseId}") => 
        $"/main/pipelines/{pipelineId}/releases/{releaseId}";
    
    public static string ApiReleaseEnvironment(
        string pipelineId = "{pipelineId}", 
        string releaseId = "{releaseId}",
        string environmentId = "{environmentId}") => 
        $"/main/pipelines/{pipelineId}/releases/{releaseId}/environment/{environmentId}";

    public static string ApiReleaseApprove(
        string pipelineId = "{pipelineId}", 
        string releaseId = "{releaseId}") =>
        $"/main/pipelines/{pipelineId}/releases/{releaseId}/approve";

    private record ReleaseApproveRequest(
        [property: JsonPropertyName("release-id")] string ReleaseId,
        [property: JsonPropertyName("release-datetime")] string? ReleaseDatetime,
        [property: JsonPropertyName("release-env")] string[] EnvironmentIds,
        [property: JsonPropertyName("release-status")] string[] EnvironmentStatus,
        [property: JsonPropertyName("release-approval")] string[] ApprovalIds,
        [property: JsonPropertyName("timezone-offset")] string TimeZoneOffset);
    
    public static void AddEndpoints(WebApplication app)
    {
        app.MapPost(ApiReleaseEnvironment(), async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsQuery azure,
            string environmentId) =>
        {
            var environment = azure.GetEnvironmentDetails(environmentId);
            return await render.Fragment(ReleaseSelection(environment)).ToResultAsync();
        });

        app.MapPost(ApiReleaseApprove(), async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsCommand azure,
            HttpContext http,
            [FromBody] ReleaseApproveRequest request) =>
        {
            var envIds = new List<IAzureDevOpsCommand.StartReleaseRequest>();
            var approvalIds = new List<int>();

            int.TryParse(request.ReleaseId, out var releaseId);
            int.TryParse(request.TimeZoneOffset, out var timeZoneOffset);
            var hasReleaseDate = DateTime.TryParse(request.ReleaseDatetime, out var releaseDate);

            if (hasReleaseDate)
            {
                var utc = releaseDate.AddMinutes(timeZoneOffset);
                releaseDate = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            for (var idx = 0; idx < request.EnvironmentIds.Length; idx++)
            {
                if (!hasReleaseDate && int.TryParse(request.ApprovalIds[idx], out var appId))
                {   // Approvals apply if a release date isn't being set (that needs to update the release settings first)
                    approvalIds.Add(appId);
                }
                else if (int.TryParse(request.EnvironmentIds[idx], out var envId))
                {
                    var status = request.EnvironmentStatus[idx];
                    envIds.Add(new IAzureDevOpsCommand.StartReleaseRequest(releaseId, envId, status, releaseDate));
                }
            }

            if (envIds.Count > 0)
            {
                await azure.StartReleases(envIds, http.RequestAborted);
            } 
            else if (approvalIds.Count > 0)
            {
                await azure.ApproveReleases(approvalIds, http.RequestAborted);
            }

            return await render.Fragment(ReleaseApproved()).ToResultAsync();
        });
        
        app.MapGet(ApiPipelines, async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsQuery azure,
            CancellationToken cancel) =>
        {
            var pipelines = (await azure.GetReleasePipelinesAsync(cancel)).Value;
            await Task.Delay(TimeSpan.FromSeconds(1), cancel);
            return await render.Fragment(PipelinesTable(pipelines)).ToResultAsync();
        });

        app.MapGet(ApiPipeline(), async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsQuery azure,
            HttpContext http,
            string id,
            CancellationToken cancel) =>
        {
            Models.ReleasePipeline? pipeline = null;
            if (int.TryParse(id, out var pipelineId))
            {
                pipeline = (await azure.GetReleasePipelineAsync(pipelineId, cancel));
            }

            var fragment = pipeline != null ? MainPipeline(pipeline) : ErrorView(http);
            
            return await RenderMain(http, render, fragment);
        });

        app.MapGet(ApiReleases(), async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsQuery azure,
            HttpContext http,
            string id,
            CancellationToken cancel) =>
        {
            Models.ReleasesResponse? response = null;
            if (int.TryParse(id, out var pipelineId))
            {
                response = (await azure.GetReleasesAsync(pipelineId, cancel));
            }

            var fragment = response != null ? ReleasesList(pipelineId, response) : ErrorView(http);

            return await render.Fragment(fragment).ToResultAsync();
        });

        app.MapGet(ApiRelease(), async (
            [FromServices] Renderer render,
            [FromServices] IAzureDevOpsQuery azure,
            HttpContext http,
            string pipelineId, string releaseId,
            CancellationToken cancel) =>
        {

            var release = await azure.GetEnvironmentDetailsAsync(pipelineId, releaseId, cancel);
            var fragment = MainRelease(pipelineId, releaseId, null, release);
            
            return await RenderMain(http, render, fragment);
        });
    }

    private static async Task<IResult> RenderMain(HttpContext http, Renderer render, RenderFragment main)
    {
        if (http.Request.Headers.TryGetValue("HX-Request", out var hxRequest) && hxRequest.Equals("true"))
        {
            return await render.Fragment(main).ToResultAsync();
        }
        else
        {
            return await render.Fragment(main).WithLayout<MainLayout>().ToResultAsync();
        }
    }
}