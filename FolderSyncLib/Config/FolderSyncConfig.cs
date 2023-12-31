namespace FolderSyncLib.Config;

public class FolderSyncConfig(string[] args) : IFolderSyncConfig
{ 
    public string SourcePath { get; } = args[0];
    public string ReplicaPath { get; } = args[1];
    public int SyncInterval { get; } = int.Parse((string)args[2]);
}