using Microsoft.AspNetCore.Mvc;
using TemporalDashboard.Api.Models;
using TemporalDashboard.Api.Services;

namespace TemporalDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly WorkflowDiscoveryService _discoveryService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(WorkflowDiscoveryService discoveryService, ILogger<WorkflowsController> logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a list of all workflows discovered from uploaded DLLs
    /// </summary>
    [HttpGet]
    public ActionResult<List<WorkflowInfo>> GetWorkflows()
    {
        try
        {
            var workflows = _discoveryService.DiscoverWorkflows();
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering workflows");
            return StatusCode(500, new { error = "Failed to discover workflows", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all workflows from a specific DLL with their Mermaid diagrams
    /// </summary>
    [HttpGet("{dllName}/diagrams")]
    public ActionResult<List<WorkflowTypeInfo>> GetWorkflowDiagrams(string dllName)
    {
        try
        {
            var workflows = _discoveryService.GetWorkflowsFromDll(dllName);
            return Ok(workflows);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = "DLL not found", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow diagrams for {DllName}", dllName);
            return StatusCode(500, new { error = "Failed to get workflow diagrams", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a single workflow's Mermaid diagram by DLL name and workflow type name
    /// </summary>
    [HttpGet("{dllName}/diagrams/{workflowName}")]
    public ActionResult<WorkflowTypeInfo> GetWorkflowDiagram(string dllName, string workflowName)
    {
        try
        {
            var diagram = _discoveryService.GetWorkflowDiagram(dllName, workflowName);
            return Ok(diagram);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = "Workflow or DLL not found", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow diagram for {WorkflowName} in {DllName}", workflowName, dllName);
            return StatusCode(500, new { error = "Failed to get workflow diagram", message = ex.Message });
        }
    }
}
