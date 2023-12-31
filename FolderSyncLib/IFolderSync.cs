using System.Timers;

namespace FolderSyncLib;

public interface IFolderSync
{
    Task StartSync(CancellationToken cancellationToken);
    Task WaitForCancellation();
    Task PerformSync();
    void StartTimer();
    void StopTimer();
    void SyncInterval(object? sender, ElapsedEventArgs e);
}