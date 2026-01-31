namespace TemporalDashboard.Web.Models;

public class UploadResponse
{
    public string Message { get; set; } = string.Empty;
    public string ExtractPath { get; set; } = string.Empty;
    public int DllCount { get; set; }
    public int WorkflowCount { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<string> SavedAssemblies { get; set; } = new();
}
