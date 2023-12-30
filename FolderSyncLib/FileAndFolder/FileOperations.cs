using Microsoft.Extensions.Logging;

namespace FolderSyncLib.FileAndFolder;

public class FileOperations : IFileOperations
{
    private readonly ILogger<FileOperations> _logger;

    public FileOperations(ILogger<FileOperations> logger)
    {
        _logger = logger;
    }

    public async Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken)
    {
        using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);
        }
        
        CopyModifiedTime(sourceFile, destinationFile);
        _logger.LogInformation("Copied: " + sourceFile + " to " + destinationFile);
    }
    private void CopyModifiedTime(string sourceFile, string destinationFile)
    {
        DateTime lastWriteTime = File.GetLastWriteTime(sourceFile);
                
        File.SetLastWriteTime(destinationFile, lastWriteTime);
    }
    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            await Task.Run(() => File.Delete(filePath));
            _logger.LogInformation("Deleted: " + filePath);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error deleting: " + filePath + " " + ex.Message);
        }
    }
    public async Task DeleteDirectoryAsync(string directoryPath)
    {
        try
        {
            await Task.Run(() => Directory.Delete(directoryPath, true));
            _logger.LogInformation("Deleted: " + directoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error deleting: " + directoryPath + " " + ex.Message);
        }
    }
}