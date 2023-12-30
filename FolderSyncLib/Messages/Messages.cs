using Microsoft.Extensions.Logging;

namespace FolderSyncLib;

public class Messages
{
    public const string ReplicaDirectoryUnmatchedDelete = "Replica Directory {replicaDirectory} deleted along with its contents";
    public const string SourceDirectoryCreation = "Source Directory {sourceDirectory} created";
    public const string SyncStart = "Syncing started - source {source} to {destination} every {seconds} seconds";
    public const string SyncError = "Error during synchronization";
    public const string SyncCancelled = "Synchronization cancelled";
}