namespace FolderSyncLib.Config;

public class FolderSyncConfig : IFolderSyncConfig
{ 
    public string SourcePath { get; }
    public string ReplicaPath { get; }
    public int SyncInterval { get; }
    
    public FolderSyncConfig(string[] args)
    {
        SourcePath = args[0];
        ReplicaPath = args[1];
        SyncInterval = int.Parse(args[2]);
    }
}