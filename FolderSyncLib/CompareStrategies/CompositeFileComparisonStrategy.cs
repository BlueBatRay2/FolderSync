namespace FolderSyncLib.CompareStrategies;

public class CompositeFileComparisonStrategy : IFileCompareStrategy
{
    private readonly List<IFileCompareStrategy> _strategies;

    public CompositeFileComparisonStrategy(List<IFileCompareStrategy> strategies)
    {
        _strategies = strategies;
    }

    public async Task<bool> AreEquivalentAsync(string file1, string file2)
    {
        foreach (var strategy in _strategies)
        {
            if (!await strategy.AreEquivalentAsync(file1, file2))
            {
                return false;
            }
        }
        return true;
    }
}
