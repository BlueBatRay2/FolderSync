namespace FolderSyncLib.CompareStrategies;

public class SizeComparisonStrategy : IFileCompareStrategy
{
    public async Task<bool> AreEquivalentAsync(string file1, string file2)
    {
        var fileInfo1 = new FileInfo(file1);
        var fileInfo2 = new FileInfo(file2);
        return fileInfo1.Length == fileInfo2.Length;
    }
}