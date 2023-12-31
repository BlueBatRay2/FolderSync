namespace FolderSyncLib.FileAndFolder;

public interface IFileOperations
{
    Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken);
    Task DeleteFileAsync(string filePath);
    Task DeleteDirectoryAsync(string directoryPath);
    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateDirectories(string path);
    void CreateDirectory(string directory);
    string[] GetDirectories(string path);
    public string[] GetSubdirectoriesNames(string path);
}