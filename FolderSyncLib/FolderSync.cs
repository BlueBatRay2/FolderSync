using System.Timers;
using FolderSyncLib.Config;
using FolderSyncLib.Syncing;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace FolderSyncLib;

public class FolderSync : IFolderSync
{
    private readonly ILogger<FolderSync> _logger;
    private readonly IFolderSyncConfig _config;
    private readonly ISyncOperations _syncOperations;
    private readonly Timer? _syncTimer;
    private CancellationToken _cancellationToken;
    private bool _isSyncing;
    
    const int MillisecondsPerSecond = 1000;
    
    public FolderSync(ILogger<FolderSync> logger, IFolderSyncConfig config, ISyncOperations syncOperations)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _syncOperations = syncOperations;
        ;

        if (_config.SyncInterval > 0)
        {
            _syncTimer = new Timer(_config.SyncInterval * MillisecondsPerSecond)
            {
                AutoReset = true,
                Enabled = false,
            };
            _syncTimer.Elapsed += ((IFolderSync)this).SyncInterval;
        }
    }

    public async Task StartSync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        if (_config.SyncInterval > 0)
        {
            _logger.LogInformation("Syncing started - source {source} to {destination} every {seconds} seconds", _config.SourcePath, _config.ReplicaPath, _config.SyncInterval);
            StartTimer();
            await WaitForCancellation();
            StopTimer();
        }
        else
        {
            _logger.LogInformation("Syncing started - source {source} to {destination}", _config.SourcePath, _config.ReplicaPath);
            await PerformSync();
        }
    }

    public async Task WaitForCancellation()
    {
        try
        {
            await Task.Delay(Timeout.Infinite, _cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Synchronization cancelled");
        }
    }

    public async Task PerformSync()
    {
        try
        {
            await _syncOperations.SyncFilesAndDirectories(_config.SourcePath, _config.ReplicaPath, _cancellationToken);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error during synchronization");
        }
    }

    public void StartTimer() => _syncTimer?.Start();
    public void StopTimer() => _syncTimer?.Stop();

    void IFolderSync.SyncInterval(object? sender, ElapsedEventArgs e)
    {
        if (_isSyncing) return;
        
        _isSyncing = true;
        PerformSync().Wait(_cancellationToken);
        _isSyncing = false;
    }
}