using FolderSyncLib.CompareStrategies;
using FolderSyncLib.FileAndFolder;

namespace FolderSyncLib.Syncing;

public class SyncOperations : ISyncOperations
{
    private readonly IFileOperations _fileOperations;
    private readonly IFileCompareStrategy _fileCompareStrategy;

    public SyncOperations(IFileOperations fileOperations, IFileCompareStrategy fileCompareStrategy)
    {
        _fileOperations = fileOperations;
        _fileCompareStrategy = fileCompareStrategy;
    }

    public async Task<IEnumerable<Task>> DeleteFilesUnmatched(string sourceDirectory, string replicaDirectory)
    {
        // Asynchronously retrieve the full paths of files in both directories
        var sourceFilesTask = Task.Run(() => Directory.EnumerateFiles(sourceDirectory));
        var replicaFilesTask = Task.Run(() => Directory.EnumerateFiles(replicaDirectory));

        var sourceFiles = await sourceFilesTask;
        var replicaFiles = await replicaFilesTask;

        // Convert full paths to relative paths
        var sourceRelativePaths = sourceFiles.Select(path => Path.GetRelativePath(sourceDirectory, path)).ToHashSet();
        var replicaRelativePaths = replicaFiles.Select(path => Path.GetRelativePath(replicaDirectory, path));

        // Identify files in the replica directory that don't exist in the source directory
        var filesToDelete = replicaRelativePaths.Where(replicaPath => !sourceRelativePaths.Contains(replicaPath))
            .Select(replicaPath => Path.Combine(replicaDirectory, replicaPath));

        // Return tasks to delete these files
        return filesToDelete.Select(file => _fileOperations.DeleteFileAsync(file));
    }

    public async Task<IEnumerable<Task>> DeleteDirectoriesUnmatched(string sourceDirectory, string replicaDirectory)
    {
        // Asynchronously retrieve the full paths of directories in both source and replica directories
        var sourceDirectoriesTask = Task.Run(() => Directory.EnumerateDirectories(sourceDirectory));
        var replicaDirectoriesTask = Task.Run(() => Directory.EnumerateDirectories(replicaDirectory));

        var sourceDirectories = await sourceDirectoriesTask;
        var replicaDirectories = await replicaDirectoriesTask;

        // Convert full paths to relative paths
        var sourceRelativePaths = sourceDirectories.Select(path => Path.GetRelativePath(sourceDirectory, path)).ToHashSet();
        var replicaRelativePaths = replicaDirectories.Select(path => Path.GetRelativePath(replicaDirectory, path));

        // Identify directories in the replica directory that don't exist in the source directory
        var directoriesToDelete = replicaRelativePaths.Where(replicaPath => !sourceRelativePaths.Contains(replicaPath))
            .Select(replicaPath => Path.Combine(replicaDirectory, replicaPath));

        // Return tasks to delete these directories
        return directoriesToDelete.Select(directoryPath => _fileOperations.DeleteDirectoryAsync(directoryPath));
    }

    public async Task<IEnumerable<Task>> DeleteFilesUnmatched2(string sourceDirectory, string replicaDirectory)
    {
        var sourceFilesTask = Task.Run(() => Directory.EnumerateFiles(sourceDirectory));
        var replicaFilesTask = Task.Run(() => Directory.EnumerateFiles(replicaDirectory));

        var sourceFiles = await sourceFilesTask;
        var replicaFiles = await replicaFilesTask;
        
        var filesToDelete = replicaFiles.Where(entry => !sourceFiles.Contains(entry));

        return filesToDelete.Select(file => _fileOperations.DeleteFileAsync(file));
    }

    public async Task<IEnumerable<Task>> DeleteDirectoriesUnmatched2(string sourceDirectory, string replicaDirectory)
    {
        var sourceDirectoriesTask = Task.Run(() => Directory.EnumerateDirectories(sourceDirectory));
        var replicaDirectoriesTask = Task.Run(() => Directory.EnumerateDirectories(replicaDirectory));

        var sourceFiles = await sourceDirectoriesTask;
        var replicaFiles = await replicaDirectoriesTask;
        
        var directoriesToDelete = replicaFiles.Where(entry => !sourceFiles.Contains(entry));

        return directoriesToDelete.Select(directoryPath => _fileOperations.DeleteDirectoryAsync(directoryPath));
    }

    public async Task SyncFilesAndDirectories(string sourceDirectory, string replicaDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(replicaDirectory);
        
        //first let's delete all files and directories not in source
        await DeleteUnmatched(sourceDirectory, replicaDirectory);

        await SyncExistingFiles(sourceDirectory, replicaDirectory, cancellationToken);
            
        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);

        foreach (var directory in sourceDirectoryInfo.GetDirectories())
        {
            var newSourcePath = Path.Combine(sourceDirectory, directory.Name);
            var newReplicaPath = Path.Combine(replicaDirectory, directory.Name);
            
            await SyncFilesAndDirectories(newSourcePath, newReplicaPath, cancellationToken);
        }
    }

    public async Task SyncExistingFiles(string sourceDirectory, string replicaDirectory, CancellationToken cancellationToken)
    {
        var sourceFiles = Directory.EnumerateFiles(sourceDirectory);
        var replicaFiles = Directory.EnumerateFiles(replicaDirectory);

        foreach (var file in sourceFiles)
        {
            var fileName = Path.GetFileName(file);
            var replicaFilePath = Path.Combine(replicaDirectory, fileName);

            if (!File.Exists(replicaFilePath) || await FileNeedsUpdate(file, replicaFilePath))
            {
                await _fileOperations.CopyFileAsync(file, replicaFilePath, cancellationToken);
            }
        }
    }

    public async Task<bool> FileNeedsUpdate(string sourceFile, string replicaFile)
    {
        return !await _fileCompareStrategy.AreEquivalentAsync(sourceFile, replicaFile);
    }

    public async Task DeleteUnmatched(string sourceDirectory, string replicaDirectory)
    {
        // Start the tasks for file and directory deletion, but don't await them yet
        var fileDeleteTask = DeleteFilesUnmatched(sourceDirectory, replicaDirectory);
        var directoriesDeleteTask = DeleteDirectoriesUnmatched(sourceDirectory, replicaDirectory);

        // Await the tasks to get IEnumerable<Task>
        var fileDeletionTasks = await fileDeleteTask;
        var directoryDeletionTasks = await directoriesDeleteTask;

        // Flatten all deletion tasks into a single array
        var allDeletionTasks = fileDeletionTasks.Concat(directoryDeletionTasks).ToArray();

        // Await all deletion tasks
        await Task.WhenAll(allDeletionTasks);
    }
}