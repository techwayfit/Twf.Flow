using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Twf.Flow.Core;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Repositories;
using Twf.Flow.Web.Services;

namespace Twf.Flow.Web.Controllers;

[Route("Workflow")]
public class WorkflowRunnerController : Controller
{
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly WorkflowDefinitionRunner _runner;
    private readonly ILogger<WorkflowRunnerController> _logger;

    public WorkflowRunnerController(
        IWorkflowRepository repository,
        IWorkflowInstanceRepository instanceRepository,
        WorkflowDefinitionRunner runner,
        ILogger<WorkflowRunnerController> logger)
    {
        _repository = repository;
        _instanceRepository = instanceRepository;
        _runner = runner;
        _logger = logger;
    }

    // GET: /Workflow/Runner/{id}
    [HttpGet("Runner/{id:guid}")]
    public async Task<IActionResult> Runner(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow is null) return NotFound();
        return View("~/Views/Workflow/Runner.cshtml", workflow);
    }

    // GET: /Workflow/Runs/{id}
    [HttpGet("Runs/{id:guid}")]
    public async Task<IActionResult> Runs(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow is null) return NotFound();

        var instances = await _instanceRepository.GetByWorkflowIdAsync(id);
        ViewBag.WorkflowName = workflow.Name;
        ViewBag.WorkflowId   = id;
        return View("~/Views/Workflow/Runs.cshtml", instances);
    }

    // GET: /Workflow/RunDetail/{instanceId}
    [HttpGet("RunDetail/{instanceId:guid}")]
    public async Task<IActionResult> RunDetail(Guid instanceId)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId);
        if (instance is null) return NotFound();
        return View("~/Views/Workflow/RunDetail.cshtml", instance);
    }

    // POST: /Workflow/Run/{id}
    // Body (optional JSON): { "initialData": { "key": "value" } }
    //
    // Loads the saved WorkflowDefinition, creates a WorkflowInstance, runs it,
    // persists the outcome, and returns a WorkflowRunResult.
    [HttpPost("Run/{id:guid}")]
    public async Task<IActionResult> Run(Guid id, [FromBody] WorkflowRunRequest? request = null)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow is null)
            return NotFound(new { error = $"Workflow {id} not found." });

        var seedDict = request?.InitialData ?? new Dictionary<string, object?>();
        var initialData = new WorkflowData();
        foreach (var (k, v) in seedDict)
            initialData.Set(k, v);

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = workflow.Id,
            WorkflowName         = workflow.Name,
            Status               = WorkflowRunStatus.Running,
            StartedAt            = DateTime.UtcNow,
            InitialData          = seedDict
        };
        await _instanceRepository.CreateAsync(instance);
        _logger.LogInformation("▶ WorkflowInstance {InstanceId} created for '{Name}'", instance.Id, workflow.Name);

        try
        {
            var result = await _runner.RunWithCallbackAsync(
                workflow,
                initialData,
                step =>
                {
                    _logger.LogInformation(
                        "{EventType} — {NodeType} '{NodeName}'{Error}",
                        step.EventType, step.NodeType, step.NodeName,
                        step.ErrorMessage is null ? "" : $" | {step.ErrorMessage}");

                    instance.Steps.Add(step);
                    return Task.CompletedTask;
                });

            instance.Status         = result.IsSuccess ? WorkflowRunStatus.Completed : WorkflowRunStatus.Failed;
            instance.CompletedAt    = DateTime.UtcNow;
            instance.OutputData     = result.OutputData;
            instance.ErrorMessage   = result.ErrorMessage;
            instance.FailedNodeName = result.FailedNodeName;
            await _instanceRepository.UpdateAsync(instance);

            _logger.LogInformation(
                "{Status} WorkflowInstance {InstanceId} for '{Name}'",
                instance.Status, instance.Id, workflow.Name);

            var statusCode = result.IsSuccess ? 200 : 422;
            return StatusCode(statusCode, new { result, instanceId = instance.Id });
        }
        catch (Exception ex)
        {
            instance.Status       = WorkflowRunStatus.Failed;
            instance.CompletedAt  = DateTime.UtcNow;
            instance.ErrorMessage = ex.Message;
            await _instanceRepository.UpdateAsync(instance);

            _logger.LogError(ex, "Unhandled error running workflow {WorkflowId}", id);
            return StatusCode(500, new { error = ex.Message, instanceId = instance.Id });
        }
    }

    // POST: /Workflow/RunStream/{id}
    // Streams per-node execution events as Server-Sent Events (text/event-stream).
    // Creates a WorkflowInstance before running and updates it on completion.
    [HttpPost("RunStream/{id:guid}")]
    public async Task RunStream(Guid id, [FromBody] WorkflowRunRequest? request = null)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow is null)
        {
            Response.StatusCode = 404;
            return;
        }

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var ct = HttpContext.RequestAborted;

        var seedDict = request?.InitialData ?? new Dictionary<string, object?>();
        var initialData = new WorkflowData();
        foreach (var (k, v) in seedDict)
            initialData.Set(k, v);

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = workflow.Id,
            WorkflowName         = workflow.Name,
            Status               = WorkflowRunStatus.Pending,
            StartedAt            = DateTime.UtcNow,
            InitialData          = seedDict
        };
        await _instanceRepository.CreateAsync(instance);

        instance.Status = WorkflowRunStatus.Running;
        await _instanceRepository.UpdateAsync(instance);
        _logger.LogInformation("▶ WorkflowInstance {InstanceId} created for '{Name}'", instance.Id, workflow.Name);

        var sseJsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        async Task WriteEvent(string eventName, object payload)
        {
            if (ct.IsCancellationRequested) return;
            var json = JsonSerializer.Serialize(payload, sseJsonOptions);
            await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var result = await _runner.RunWithCallbackAsync(
                workflow,
                initialData,
                async step =>
                {
                    var level = step.EventType == "node_error" ? "WARN" : "INFO";
                    _logger.LogInformation(
                        "[{Level}] {EventType} — {NodeType} '{NodeName}'{Error}",
                        level, step.EventType, step.NodeType, step.NodeName,
                        step.ErrorMessage is null ? "" : $" | {step.ErrorMessage}");

                    instance.Steps.Add(step);
                    await WriteEvent(step.EventType, step);
                });

            instance.Status         = result.IsSuccess ? WorkflowRunStatus.Completed : WorkflowRunStatus.Failed;
            instance.CompletedAt    = DateTime.UtcNow;
            instance.OutputData     = result.OutputData;
            instance.ErrorMessage   = result.ErrorMessage;
            instance.FailedNodeName = result.FailedNodeName;
            await _instanceRepository.UpdateAsync(instance);

            _logger.LogInformation(
                "{Status} WorkflowInstance {InstanceId} for '{Name}'",
                instance.Status, instance.Id, workflow.Name);

            var finalEvent = result.IsSuccess ? "workflow_done" : "workflow_error";
            await WriteEvent(finalEvent, new { result, instanceId = instance.Id });
        }
        catch (OperationCanceledException)
        {
            instance.Status       = WorkflowRunStatus.Failed;
            instance.CompletedAt  = DateTime.UtcNow;
            instance.ErrorMessage = "Client disconnected";
            await _instanceRepository.UpdateAsync(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error streaming workflow {WorkflowId}", id);

            instance.Status       = WorkflowRunStatus.Failed;
            instance.CompletedAt  = DateTime.UtcNow;
            instance.ErrorMessage = ex.Message;
            await _instanceRepository.UpdateAsync(instance);

            try
            {
                await WriteEvent(
                    "workflow_error",
                    WorkflowRunResult.Failure(workflow.Name, new WorkflowData(), ex.Message, null));
            }
            catch
            {
            }
        }
    }
}