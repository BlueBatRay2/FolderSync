namespace FolderSyncLib.CompareStrategies;

public interface IFileCompareStrategy
{
    Task<bool> AreEquivalentAsync(string file1, string file2);
}