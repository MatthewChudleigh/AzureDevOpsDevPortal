using System.Collections.Concurrent;
using System.Threading.Channels;
using AzureDevOps.Models;
using AzureDevOps.Services;

namespace AzureDevOps;

public class AzureDevOpsQueryProxy(IHostApplicationLifetime lifetime, Worker worker) : IAzureDevOpsQuery, IAzureDevOpsCommand
{
    public async Task<ReleaseEnvironmentDetails> GetEnvironmentDetailsAsync(string pipelineId,
        string releaseId, CancellationToken cancel)
    {
        return await worker.GetEnvironmentDetailsAsync(pipelineId, releaseId, cancel);
    }

    public EnvironmentDetails? GetEnvironmentDetails(string environmentId)
    {
        return worker.GetEnvironmentDetails(environmentId);
    }
    
    public async Task<ReleasePipelinesResponse> GetReleasePipelinesAsync(CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<ReleasePipelinesResponse>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            GetReleasePipelines: new Worker.GetReleasePipelines(channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task<ReleasePipeline?> GetReleasePipelineAsync(int pipelineId, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<ReleasePipeline?>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            GetReleasePipeline: new Worker.GetReleasePipeline(pipelineId, channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task<ReleasesResponse> GetReleasesAsync(int pipelineId, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<ReleasesResponse>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            GetReleases: new Worker.GetReleases(pipelineId, channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task<Release?> GetReleaseAsync(int releaseId, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<Release?>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            GetRelease: new Worker.GetRelease(releaseId, channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task<List<EnvironmentAgentInfo>> GetEnvironmentAgentSpecificationsAsync(CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<List<EnvironmentAgentInfo>>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
            GetEnvironmentAgentSpecs: new Worker.GetEnvironmentAgentSpecs(channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task<ReleasePipeline?> GetReleasePipelineDefinitionAsync(int pipelineId, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        var channel = Channel.CreateUnbounded<ReleasePipeline?>();
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
            GetReleasePipelineDefinition: new Worker.GetReleasePipelineDefinition(pipelineId, channel)), cts.Token);
        return await channel.Reader.ReadAsync(cts.Token);
    }

    public async Task StartRelease(IAzureDevOpsCommand.StartReleaseRequest releaseRequest, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            StartRelease: new Worker.StartRelease(releaseRequest)), cts.Token);
    }

    public async Task StartReleases(IEnumerable<IAzureDevOpsCommand.StartReleaseRequest> requests,
        CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        foreach (var request in requests)
        {
            await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
                StartRelease: new Worker.StartRelease(request)), cts.Token);
        }
    }
    public async Task CancelRelease(IAzureDevOpsCommand.CancelReleaseRequest cancelReleaseRequest, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            CancelRelease: new Worker.CancelRelease(cancelReleaseRequest)), cts.Token);
    }

    public async Task CancelReleases(IEnumerable<IAzureDevOpsCommand.CancelReleaseRequest> requests,
        CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        foreach (var request in requests)
        {
            await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
                CancelRelease: new Worker.CancelRelease(request)), cts.Token);
        }
    }
    public async Task ApproveRelease(int approvalId, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token, 
            ApproveRelease: new Worker.ApproveRelease(approvalId)), cts.Token);
    }

    public async Task ApproveReleases(IEnumerable<int> approvalIds, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        foreach (var approvalId in approvalIds)
        {
            await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
                ApproveRelease: new Worker.ApproveRelease(approvalId)), cts.Token);
        }
    }

    public async Task UpdateAgentSpecification(IAzureDevOpsCommand.UpdateAgentSpecRequest request, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
            UpdateAgentSpec: new Worker.UpdateAgentSpec(request)), cts.Token);
    }

    public async Task UpdateAgentSpecifications(IEnumerable<IAzureDevOpsCommand.UpdateAgentSpecRequest> requests, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping, lifetime.ApplicationStopped);
        foreach (var request in requests)
        {
            await worker.Messages.Writer.WriteAsync(new Worker.Message(cts.Token,
                UpdateAgentSpec: new Worker.UpdateAgentSpec(request)), cts.Token);
        }
    }
}

public enum ReleaseEnvironmentAction
{
    Release,
    Schedule,
    Approve,
    Cancel
}

public record EnvironmentDetails(
    int Id,
    string Name,
    string Release,
    string Status,
    Release.Approval? Approval,
    DateTime? ScheduledTime,
    ReleaseEnvironmentAction[] Actions);
public class Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime, IServiceProvider services) : BackgroundService
{
    public ConcurrentDictionary<int, EnvironmentDetails> EnvironmentDetails { get; } = new();

    public async Task<ReleaseEnvironmentDetails> GetEnvironmentDetailsAsync(string pipelineId, string releaseId, CancellationToken cancel)
    {
        await using var scope = services.CreateAsyncScope();
        var azure = scope.ServiceProvider.GetRequiredService<AzureDevOpsService>();
        
        Release? release = null;
        ReleasePipeline? pipeline = null;
        if (int.TryParse(pipelineId, out var intPipelineId) && int.TryParse(releaseId, out var intReleaseId))
        {
            pipeline = (await azure.GetReleasePipelineAsync(intPipelineId, cancel));
            release = (await azure.GetReleaseAsync(intReleaseId, cancel));
        }

        if (pipeline == null || release == null) return new ReleaseEnvironmentDetails(null, []);

        Dictionary<int, EnvironmentDetails> environmentDetails = [];
        foreach (var env in release.Environments)
        {
            var envRelease = pipeline.Environments.FirstOrDefault(e => env.Name == e.Name)?.CurrentRelease.Id;
            var releaseVersion = envRelease?.ToString() ?? "N/A";

            var approval = env.PreDeployApprovals.FirstOrDefault();
            var details = new EnvironmentDetails(env.Id, env.Name, releaseVersion, env.Status, approval, null, []);
            
            var deploySteps = env.DeploySteps ?? [];
            var queuedOn = deploySteps
                .OrderByDescending(d => d.QueuedOn)
                .FirstOrDefault()?.QueuedOn;
            if (queuedOn != null && DateTime.TryParse(queuedOn, out var dateTime))
            {
                details = details with { ScheduledTime = dateTime };
            }

            environmentDetails[env.Id] = details;
            EnvironmentDetails[env.Id] = details;
        }

        return new ReleaseEnvironmentDetails(release, environmentDetails);
    }
    
    public EnvironmentDetails? GetEnvironmentDetails(string environmentId)
    {
        return !int.TryParse(environmentId, out var intEnvironmentId) ? null 
            : EnvironmentDetails.GetValueOrDefault(intEnvironmentId);
    }
    
    public record GetReleasePipelines(Channel<ReleasePipelinesResponse> Response);
    public record GetReleasePipeline(int PipelineId, Channel<ReleasePipeline?> Response);
    public record GetReleases(int PipelineId, Channel<ReleasesResponse> Response);
    public record GetRelease(int ReleaseId, Channel<Release?> Response);
    public record GetEnvironmentAgentSpecs(Channel<List<EnvironmentAgentInfo>> Response);
    public record GetReleasePipelineDefinition(int PipelineId, Channel<ReleasePipeline?> Response);
    public record ApproveRelease(int ApprovalId);
    public record StartRelease(IAzureDevOpsCommand.StartReleaseRequest StartReleaseRequest);
    public record CancelRelease(IAzureDevOpsCommand.CancelReleaseRequest CancelReleaseRequest);
    public record UpdateAgentSpec(IAzureDevOpsCommand.UpdateAgentSpecRequest UpdateAgentSpecRequest);
    
    public record Message(
        CancellationToken Cancel,
        GetReleasePipelines? GetReleasePipelines = null,
        GetReleasePipeline? GetReleasePipeline = null,
        GetReleases? GetReleases = null,
        GetRelease? GetRelease = null,
        GetEnvironmentAgentSpecs? GetEnvironmentAgentSpecs = null,
        GetReleasePipelineDefinition? GetReleasePipelineDefinition = null,
        ApproveRelease? ApproveRelease = null,
        StartRelease? StartRelease = null,
        CancelRelease? CancelRelease = null,
        UpdateAgentSpec? UpdateAgentSpec = null);
    
    public readonly Channel<Message> Messages = Channel.CreateUnbounded<Message>();
    
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.ApplicationStopping,
                lifetime.ApplicationStopped);
        
        await foreach (var message in Messages.Reader.ReadAllAsync(cts.Token))
        {
            try
            {
                await HandleMessage(message, cts.Token);
            }
            catch (Exception)
            {
                lifetime.StopApplication();
                return;
            }
        }
    }

    private async Task HandleMessage(Message message, CancellationToken cancel)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, message.Cancel);
        await using var scope = services.CreateAsyncScope();
        var azure = scope.ServiceProvider.GetRequiredService<AzureDevOpsService>();

        try
        {
            if (message.GetReleasePipelines is not null)
            {
                var response = await azure.GetReleasePipelinesAsync(cts.Token);
                await message.GetReleasePipelines.Response.Writer.WriteAsync(response, cts.Token);
            } 
            else if (message.GetReleasePipeline is not null)
            {
                var response = await azure.GetReleasePipelineAsync(message.GetReleasePipeline.PipelineId, cts.Token);    
                await message.GetReleasePipeline.Response.Writer.WriteAsync(response, cts.Token);
            }
            else if (message.GetReleases is not null)
            {
                var response = await azure.GetReleasesAsync(message.GetReleases.PipelineId, cts.Token);    
                await message.GetReleases.Response.Writer.WriteAsync(response, cts.Token);
            }
            else if (message.GetRelease is not null)
            {
                var response = await azure.GetReleaseAsync(message.GetRelease.ReleaseId, cts.Token);    
                await message.GetRelease.Response.Writer.WriteAsync(response, cts.Token);
            }
            else if (message.GetEnvironmentAgentSpecs is not null)
            {
                var response = await azure.GetEnvironmentAgentSpecificationsAsync(cts.Token);
                await message.GetEnvironmentAgentSpecs.Response.Writer.WriteAsync(response, cts.Token);
            }
            else if (message.GetReleasePipelineDefinition is not null)
            {
                var response = await azure.GetReleasePipelineDefinitionAsync(message.GetReleasePipelineDefinition.PipelineId, cts.Token);
                await message.GetReleasePipelineDefinition.Response.Writer.WriteAsync(response, cts.Token);
            }
            else if (message.ApproveRelease is not null)
            {
                await azure.ApproveRelease(message.ApproveRelease.ApprovalId, cts.Token);
            }
            else if (message.StartRelease is not null)
            {
                await azure.StartRelease(message.StartRelease.StartReleaseRequest, cts.Token);
            }
            else if (message.UpdateAgentSpec is not null)
            {
                await azure.UpdateAgentSpecification(message.UpdateAgentSpec.UpdateAgentSpecRequest, cts.Token);
            }
            else if (message.CancelRelease is not null)
            {
                await azure.CancelRelease(message.CancelRelease.CancelReleaseRequest, cts.Token);
            }
        }
        catch (OperationCanceledException) when (message.Cancel.IsCancellationRequested)
        {
            logger.LogInformation("Message was cancelled by the client");
        }
    }
}