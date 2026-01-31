using System.Net.Http.Json;
using TemporalDashboard.Web.Models;

namespace TemporalDashboard.Web.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<WorkflowInfo>> GetWorkflowsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<WorkflowInfo>>("api/workflows");
            return response ?? new List<WorkflowInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflows");
            throw;
        }
    }

    public async Task<List<WorkflowTypeInfo>> GetWorkflowDiagramsAsync(string dllName)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<WorkflowTypeInfo>>($"api/workflows/{Uri.EscapeDataString(dllName)}/diagrams");
            return response ?? new List<WorkflowTypeInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow diagrams for {DllName}", dllName);
            throw;
        }
    }

    public async Task<WorkflowTypeInfo?> GetWorkflowDiagramAsync(string dllName, string workflowName)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<WorkflowTypeInfo>($"api/workflows/{Uri.EscapeDataString(dllName)}/diagrams/{Uri.EscapeDataString(workflowName)}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow diagram for {WorkflowName} in {DllName}", workflowName, dllName);
            throw;
        }
    }

    public async Task<UploadResponse> UploadZipFileAsync(Stream fileStream, string fileName, bool overwrite = false)
    {
        try
        {
            var url = "api/upload/zip" + (overwrite ? "?overwrite=true" : "");
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var existingAssemblies = new List<string>();
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
                    if (errorObj.TryGetProperty("existingAssemblies", out var arr))
                    {
                        foreach (var item in arr.EnumerateArray())
                            existingAssemblies.Add(item.GetString() ?? "");
                    }
                }
                catch { /* use empty list */ }
                throw new UploadConflictException(existingAssemblies);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"Upload failed: {response.ReasonPhrase}";
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseBody);
                    if (errorObj.TryGetProperty("message", out var messageProp))
                        errorMessage = messageProp.GetString() ?? errorMessage;
                    else if (errorObj.TryGetProperty("error", out var errorProp))
                        errorMessage = errorProp.GetString() ?? errorMessage;
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(responseBody))
                        errorMessage = responseBody;
                }
                throw new HttpRequestException(errorMessage);
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<UploadResponse>(responseBody);
            return result ?? new UploadResponse { Message = "Upload completed" };
        }
        catch (HttpRequestException)
        {
            // Re-throw HTTP exceptions as-is (they contain the error message)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            throw;
        }
    }

    public async Task<UploadInfo> GetUploadInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<UploadInfo>("api/upload/info");
            return response ?? new UploadInfo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upload info");
            throw;
        }
    }
}
