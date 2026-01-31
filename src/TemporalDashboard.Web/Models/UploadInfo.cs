namespace TemporalDashboard.Web.Models;

public class UploadInfo
{
    public string UploadsPath { get; set; } = string.Empty;
    public int FolderCount { get; set; }
    public int TotalDllCount { get; set; }
    public List<UploadFolder> Folders { get; set; } = new();
}
