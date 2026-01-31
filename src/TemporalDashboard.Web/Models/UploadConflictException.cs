namespace TemporalDashboard.Web.Models;

/// <summary>
/// Thrown when upload would overwrite existing workflow assemblies and overwrite was not requested.
/// </summary>
public class UploadConflictException : Exception
{
    public IReadOnlyList<string> ExistingAssemblies { get; }

    public UploadConflictException(IReadOnlyList<string> existingAssemblies, string? message = null)
        : base(message ?? "One or more workflow assemblies already exist.")
    {
        ExistingAssemblies = existingAssemblies;
    }
}
