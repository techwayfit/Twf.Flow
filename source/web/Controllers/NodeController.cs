using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Repositories;

namespace Twf.Flow.Web.Controllers;

public class NodeController : Controller
{
    private readonly INodeTypeRepository _repository;
    private readonly ILogger<NodeController> _logger;

    private static readonly JsonSerializerOptions _prettyOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public NodeController(INodeTypeRepository repository, ILogger<NodeController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: /Node
    public async Task<IActionResult> Index(string? category)
    {
        var nodes = string.IsNullOrWhiteSpace(category)
            ? await _repository.GetAllAsync()
            : await _repository.GetByCategoryAsync(category);

        ViewBag.SelectedCategory = category;
        ViewBag.Categories = (await _repository.GetAllAsync())
            .Select(n => n.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return View(nodes);
    }

    // GET: /Node/Create
    public IActionResult Create()
    {
        var entity = new NodeTypeEntity
        {
            Color = "#888888",
            Icon  = "bi-box",
            SchemaJson = JsonSerializer.Serialize(new NodeParameterSchema(), _prettyOptions),
        };
        return View(entity);
    }

    // POST: /Node/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NodeTypeEntity entity)
    {
        // Validate SchemaJson is valid JSON
        if (!IsValidJson(entity.SchemaJson))
            ModelState.AddModelError(nameof(entity.SchemaJson), "Schema must be valid JSON.");

        // Check uniqueness of NodeType
        var existing = await _repository.GetByNodeTypeAsync(entity.NodeType);
        if (existing != null)
            ModelState.AddModelError(nameof(entity.NodeType), $"Node type '{entity.NodeType}' already exists.");

        if (!ModelState.IsValid)
            return View(entity);

        await _repository.CreateAsync(entity);
        TempData["Success"] = $"Node type '{entity.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Node/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return NotFound();

        // Pretty-print for editing
        entity.SchemaJson = PrettyPrintJson(entity.SchemaJson);
        return View(entity);
    }

    // POST: /Node/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NodeTypeEntity entity)
    {
        if (id != entity.Id) return BadRequest();

        if (!IsValidJson(entity.SchemaJson))
            ModelState.AddModelError(nameof(entity.SchemaJson), "Schema must be valid JSON.");

        // Ensure NodeType uniqueness (excluding self)
        var existing = await _repository.GetByNodeTypeAsync(entity.NodeType);
        if (existing != null && existing.Id != id)
            ModelState.AddModelError(nameof(entity.NodeType), $"Node type '{entity.NodeType}' is already used by another entry.");

        if (!ModelState.IsValid)
            return View(entity);

        try
        {
            await _repository.UpdateAsync(entity);
            TempData["Success"] = $"Node type '{entity.Name}' updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating node type {Id}", id);
            ModelState.AddModelError("", "An error occurred while saving.");
            return View(entity);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Node/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return NotFound();
        return View(entity);
    }

    // POST: /Node/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        var name = entity?.Name ?? id.ToString();
        await _repository.DeleteAsync(id);
        TempData["Success"] = $"Node type '{name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Node/ToggleEnabled/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEnabled(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.IsEnabled = !entity.IsEnabled;
        await _repository.UpdateAsync(entity);
        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }

    private static string PrettyPrintJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, _prettyOptions);
        }
        catch
        {
            return json;
        }
    }
}
