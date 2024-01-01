namespace FolderSyncLib.CompareStrategies;

public class ModifiedTimeComparisonStrategy : IFileCompareStrategy
{
    //this tolerance is added because of small differences between modified times
    private readonly TimeSpan _tolerance = TimeSpan.FromSeconds(2);
    
    public Task<bool> AreEquivalentAsync(string file1, string file2)
    {
        var fileInfo1 = new FileInfo(file1);
        var fileInfo2 = new FileInfo(file2);

        var timeDifference = fileInfo1.LastWriteTime - fileInfo2.LastWriteTime;
        bool areEquiv = Math.Abs(timeDifference.TotalSeconds) <= _tolerance.TotalSeconds;
        return Task.FromResult(areEquiv);
    }
}