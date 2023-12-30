namespace FolderSyncLib.CompareStrategies;

public static class FileComparisonStrategyFactory
{
    public static IFileCompareStrategy CreateDefaultStrategy()
    {
        var defaultStrategies = new List<IFileCompareStrategy>
        {
            new SizeComparisonStrategy(),
            new ModifiedTimeComparisonStrategy(),
            new Sha256ComparisonStrategy()
        };
        return new CompositeFileComparisonStrategy(defaultStrategies);
    }
}
