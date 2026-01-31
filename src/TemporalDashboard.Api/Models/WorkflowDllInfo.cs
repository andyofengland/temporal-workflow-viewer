namespace TemporalDashboard.Api.Models;

/// <summary>
/// Identifies a DLL that contains workflow types and its location for upload processing.
/// </summary>
public class WorkflowDllInfo
{
    public string DllPath { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public string SourceDirectory { get; set; } = string.Empty;
}
