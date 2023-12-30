namespace FolderSyncLib.FileAndFolder;

public interface IFileOperations
{
    Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken);
    Task DeleteFileAsync(string filePath);
    Task DeleteDirectoryAsync(string directoryPath);
}