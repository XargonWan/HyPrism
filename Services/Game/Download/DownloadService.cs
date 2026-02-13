using HyPrism.Services.Core.Infrastructure;

namespace HyPrism.Services.Game.Download;

/// <summary>
/// Provides file download functionality with progress tracking and resume support.
/// Used for downloading game files, patches, and other assets.
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for downloading files.</param>
    public DownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task DownloadFileAsync(
        string url, 
        string destinationPath, 
        Action<int, long, long> progressCallback, 
        CancellationToken cancellationToken = default)
    {
        long existingLength = 0;
        if (File.Exists(destinationPath))
        {
            existingLength = new FileInfo(destinationPath).Length;
        }

        // 1. Get total size (HEAD)
        long totalBytes = -1;
        try 
        {
            totalBytes = await GetFileSizeAsync(url, cancellationToken);
        }
        catch 
        {
             // Ignore, fallback to normal download if HEAD fails
        }

        bool canResume = false;
        if (existingLength > 0 && totalBytes > 0 && existingLength < totalBytes)
        {
            canResume = true;
            Logger.Info("Download", $"Resuming download from byte {existingLength} of {totalBytes}");
        }
        else if (existingLength >= totalBytes && totalBytes > 0)
        {
             // Already done?
             Logger.Info("Download", "File already downloaded fully.");
             progressCallback?.Invoke(100, totalBytes, totalBytes);
             return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (canResume)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
        }
        else
        {
            // Reset if we can't resume
            existingLength = 0; 
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        // If server doesn't support range, it sends 200 OK instead of 206 Partial Content
        if (canResume && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
        {
            Logger.Warning("Download", "Server did not accept Range header, restarting download.");
            canResume = false;
            existingLength = 0;
        }
        else if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.Error("Download", $"Download failed from {url}: HTTP {(int)response.StatusCode} {response.StatusCode}. Response: {errorBody?.Substring(0, Math.Min(500, errorBody?.Length ?? 0))}");
            throw new HttpRequestException($"Download failed: HTTP {(int)response.StatusCode} {response.StatusCode}");
        }
        
        // If we didn't get totalBytes from HEAD earlier (e.g. -1), try getting it from response
        if (totalBytes <= 0)
        {
            totalBytes = response.Content.Headers.ContentLength ?? -1;
            // If resumes, add existing length to content length (since content-length is just the part)
            if (canResume && totalBytes != -1) totalBytes += existingLength;
        }

        // File Mode
        FileMode fileMode = canResume ? FileMode.Append : FileMode.Create;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, fileMode, FileAccess.Write, FileShare.None, 8192, true);
        
        var buffer = new byte[8192];
        long totalRead = existingLength; // Start counter at existing
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;
            
            if (totalBytes > 0)
            {
                var progress = (int)((totalRead * 100) / totalBytes);
                progressCallback?.Invoke(progress, totalRead, totalBytes);
            }
        }
        
        Logger.Info("Download", $"Download finished. {totalRead / 1024 / 1024} MB to {destinationPath}");
    }

    /// <summary>
    /// Check file size without downloading.
    /// </summary>
    public async Task<long> GetFileSizeAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return -1;
            }
            
            return response.Content.Headers.ContentLength ?? -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Check if file exists on server.
    /// </summary>
    public async Task<bool> FileExistsAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
