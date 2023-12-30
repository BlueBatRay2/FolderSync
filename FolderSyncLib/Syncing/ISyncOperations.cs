namespace FolderSyncLib.Syncing;

public interface ISyncOperations
{
    Task<IEnumerable<Task>> DeleteFilesUnmatched(string sourceDirectory, string replicaDirectory);
    Task<IEnumerable<Task>> DeleteDirectoriesUnmatched(string sourceDirectory, string replicaDirectory);
    Task SyncFilesAndDirectories(string sourceDirectory, string replicaDirectory, CancellationToken cancellationToken);
    Task SyncExistingFiles(string sourceDirectory, string replicaDirectory, CancellationToken cancellationToken);
    Task<bool> FileNeedsUpdate(string sourceFile, string replicaFile);
    Task DeleteUnmatched(string sourceDirectory, string replicaDirectory);
}