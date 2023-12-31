using Microsoft.Extensions.Logging;

namespace FolderSyncLib.FileAndFolder;

public class FileOperations(ILogger<FileOperations> logger) : IFileOperations
{
    public async Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken)
    {
        await ExecuteFileOperationAsync(async () =>
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);
            }
        },"Error Copying File: ");
        
        CopyModifiedTime(sourceFile, destinationFile);
        logger.LogInformation("Copied: " + sourceFile + " to " + destinationFile);
    }
    private void CopyModifiedTime(string sourceFile, string destinationFile)
    {
        DateTime lastWriteTime = File.GetLastWriteTime(sourceFile);
                
        File.SetLastWriteTime(destinationFile, lastWriteTime);
    }

    public async Task DeleteFileAsync(string filePath)
    {
        await ExecuteFileOperationAsync(async () =>
        {
            await Task.Run(() => File.Delete(filePath));
            logger.LogInformation("Deleted: " + filePath);
        },"Error Deleting File: ");
    }

    public async Task DeleteDirectoryAsync(string directoryPath)
    {
        await ExecuteFileOperationAsync(async () =>
        {
            await Task.Run(() => Directory.Delete(directoryPath, true));
            logger.LogInformation("Deleted: " + directoryPath);
        },"Error Deleting Directory: ");
    }

    private async Task ExecuteFileOperationAsync(Action fileOperation, string context)
    {
        try
        {
            fileOperation();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError($"{context} UnauthorizedAccessException: {ex.Message}");
        }
        catch (IOException ex)
        {
            logger.LogError($"{context} IOException: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError($"{context} Unexpected error: {ex.Message}");
            throw;
        }
    }
    
    public IEnumerable<string> EnumerateFiles(string path)
    {
        return Directory.EnumerateFiles(path);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return Directory.EnumerateDirectories(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }
    public string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }
    public string[] GetSubdirectoriesNames(string path)
    {
        var directoryInfo = new DirectoryInfo(path);
        return directoryInfo.GetDirectories().Select(dir => dir.Name).ToArray();
    }
}