namespace FolderSyncLib.CompareStrategies;

public class CompositeFileComparisonStrategy(List<IFileCompareStrategy> strategies) : IFileCompareStrategy
{
    public async Task<bool> AreEquivalentAsync(string file1, string file2)
    {
        foreach (var strategy in strategies)
        {
            if (!await strategy.AreEquivalentAsync(file1, file2))
            {
                return false;
            }
        }
        return true;
    }
}
