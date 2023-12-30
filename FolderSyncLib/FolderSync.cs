using System.Timers;
using FolderSyncLib.Config;
using FolderSyncLib.Syncing;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace FolderSyncLib;

public class FolderSync
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
            _syncTimer.Elapsed += SyncInterval;
        }
    }

    public async Task StartSync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        if (_config.SyncInterval > 0)
        {
            _logger.LogInformation(Messages.SyncStart, _config.SourcePath, _config.ReplicaPath, _config.SyncInterval);
            StartTimer();
            await WaitForCancellation();
            StopTimer();
        }
        else
        {
            _logger.LogInformation(Messages.SyncStart, _config.SourcePath, _config.ReplicaPath, _config.SyncInterval);
            await PerformSync();
        }
    }
    private async Task WaitForCancellation()
    {
        try
        {
            await Task.Delay(Timeout.Infinite, _cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(Messages.SyncCancelled);
        }
    }
    private async Task PerformSync()
    {
        try
        {
            await _syncOperations.SyncFilesAndDirectories(_config.SourcePath, _config.ReplicaPath, _cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Messages.SyncError);
        }
    }
    
    private void StartTimer() => _syncTimer?.Start();
    private void StopTimer() => _syncTimer?.Stop();
    
    private void SyncInterval(object? sender, ElapsedEventArgs e)
    {
        if (_isSyncing) return;
        
        _isSyncing = true;
        PerformSync().Wait(_cancellationToken);
        _isSyncing = false;
    }
}