using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Repositories;

namespace Twf.Flow.Web.Controllers;

public class WorkflowController : Controller
{
    private readonly IWorkflowRepository _repository;
    private readonly INodeTypeRepository _nodeTypeRepository;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowRepository repository,
        INodeTypeRepository nodeTypeRepository,
        ILogger<WorkflowController> logger)
    {
        _repository = repository;
        _nodeTypeRepository = nodeTypeRepository;
        _logger = logger;
    }

    // GET: /Workflow
    public async Task<IActionResult> Index(string? search)
    {
        IEnumerable<WorkflowDefinition> workflows;

        if (!string.IsNullOrWhiteSpace(search))
        {
            workflows = await _repository.SearchAsync(search);
            ViewBag.SearchQuery = search;
        }
        else
        {
            workflows = await _repository.GetAllAsync();
        }

        return View(workflows);
    }

    // GET: /Workflow/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        return View(workflow);
    }

    // GET: /Workflow/Create
    public IActionResult Create()
    {
        return View(new WorkflowDefinition { Name = "New Workflow" });
    }

    // POST: /Workflow/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkflowDefinition workflow)
    {
        if (ModelState.IsValid)
        {
            await _repository.CreateAsync(workflow);
            return RedirectToAction(nameof(Designer), new { id = workflow.Id });
        }

        return View(workflow);
    }

    // GET: /Workflow/Designer/{id}
    // GET: /{id}/{subWorkflowId}
    public async Task<IActionResult> Designer(Guid id, Guid? subWorkflowId = null)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        if (subWorkflowId.HasValue)
        {
            var exists = workflow.SubWorkflows.Any(sw => sw.Id == subWorkflowId.Value);
            if (exists)
            {
                ViewBag.InitialSubWorkflowId = subWorkflowId.Value;
            }
        }

        return View(workflow);
    }

    // GET: /Workflow/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        return View(workflow);
    }

    // POST: /Workflow/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WorkflowDefinition workflow)
    {
        if (id != workflow.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _repository.UpdateAsync(workflow);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
                ModelState.AddModelError("", "An error occurred while updating the workflow.");
            }
        }

        return View(workflow);
    }

    // GET: /Workflow/Delete/5
    public async Task<IActionResult> Delete(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        return View(workflow);
    }

    // POST: /Workflow/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _repository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // API endpoints for the designer

    // GET: /Workflow/GetWorkflow/5
    [HttpGet]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        var workflow = await _repository.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        return Json(workflow);
    }

    // POST: /Workflow/SaveWorkflow
    [HttpPost]
    public async Task<IActionResult> SaveWorkflow([FromBody] WorkflowDefinition? workflow)
    {
        if (workflow == null)
            return Json(new { success = false, error = "Invalid workflow payload. Node and connection IDs must be valid GUIDs." });

        try
        {
            if (workflow.Id == Guid.Empty)
            {
                await _repository.CreateAsync(workflow);
            }
            else
            {
                await _repository.UpdateAsync(workflow);
            }

            return Json(new { success = true, id = workflow.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workflow");
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: /Workflow/GetAvailableNodes
    [HttpGet]
    public async Task<IActionResult> GetAvailableNodes()
    {
        var nodes = await _nodeTypeRepository.GetAllAsync();
        var result = nodes
            .Where(n => n.IsEnabled)
            .Select(n =>
            {
                // Derive default parameters from the schema's parameter definitions
                var schema = DeserializeSchema(n);
                var defaultParams = schema?.Parameters
                    ?.Where(p => p.DefaultValue is not null)
                    ?.ToDictionary(p => p.Name, p => p.DefaultValue)
                    ?? new Dictionary<string, object?>();

                return new
                {
                    type          = n.NodeType,
                    category      = n.Category,
                    name          = n.Name,
                    description   = n.Description,
                    color         = n.Color,
                    icon          = n.Icon,
                    idPrefix      = n.IdPrefix,
                    fullTypeName  = n.FullTypeName,
                    defaultParams = defaultParams,
                };
            });
        return Json(result);
    }

    // GET: /Workflow/GetNodeSchema/{nodeType}
    [HttpGet]
    public async Task<IActionResult> GetNodeSchema(string nodeType)
    {
        var entity = await _nodeTypeRepository.GetByNodeTypeAsync(nodeType);
        if (entity == null)
            return NotFound(new { error = $"Schema for node type '{nodeType}' not found" });

        // Deserialize stored schema JSON back to the typed object so the response
        // shape is identical to the old static provider.
        var schema = DeserializeSchema(entity);
        return Json(schema);
    }

    // GET: /Workflow/GetAllNodeSchemas
    [HttpGet]
    public async Task<IActionResult> GetAllNodeSchemas()
    {
        var nodes = await _nodeTypeRepository.GetAllAsync();
        var schemas = nodes.ToDictionary(n => n.NodeType, DeserializeSchema);
        return Json(schemas);
    }

    private static readonly JsonSerializerOptions _schemaReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static NodeParameterSchema? DeserializeSchema(Data.NodeTypeEntity entity)
    {
        try
        {
            return JsonSerializer.Deserialize<NodeParameterSchema>(entity.SchemaJson, _schemaReadOptions);
        }
        catch
        {
            return null;
        }
    }
}

