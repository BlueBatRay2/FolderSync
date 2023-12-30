namespace FolderSyncLib.Config;

public interface IFolderSyncConfig
{
    string SourcePath { get; }
    string ReplicaPath { get; }
    int SyncInterval { get; }
}