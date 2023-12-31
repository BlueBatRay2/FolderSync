using FolderSyncLib;
using FolderSyncLib.CompareStrategies;
using FolderSyncLib.Config;
using FolderSyncLib.FileAndFolder;
using FolderSyncLib.Syncing;
using Microsoft.Extensions.Logging;
using Moq;

namespace FolderSyncTests.IntegrationTests;

public class FolderSyncIntegTests
{
    private static readonly string BaseTempDirectory = Path.GetTempPath();
    private static readonly string SourceDirectory = Path.Combine(BaseTempDirectory, "source");
    private static readonly string ReplicaDirectory = Path.Combine(BaseTempDirectory, "replica");

    [Theory]
    [InlineData(1_000, 8_192, 102_400, 10)]              // 1000 files small  8-100 kb
    [InlineData(100, 8_192_000, 102_400_000, 100)]        // 100 files medium 8-100 mb
    //[InlineData(2, 8_192_000_000, 20_400_000_000, 1000)]  // 2 files big 8-20 gb
    public void FilesBaseFolderDifferentSizes_ShouldSyncCorrectly(int filesToCreate, long sizeLower, long sizeUpper, int cancelTimeSeconds)
    {
        // Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        testDirectoryFileHelper.SetupTestFilesSameDirectory(SourceDirectory, filesToCreate, sizeLower, sizeUpper);

        var config = FolderSyncConfig(0, cancelTimeSeconds, 
            out var mockLogger, out var cancellationToken, out var syncOperations);
        IFolderSync folderSync = new FolderSync(mockLogger.Object, config, syncOperations);

        // Act
        var syncTask = folderSync.StartSync(cancellationToken);

        // Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        TestDirectoryFileHelper.TestDirectoryEqualness(SourceDirectory, ReplicaDirectory);
    }
    
    [Fact]
    public void ExtraFilesAndFoldersInDestination_ShouldRemove()
    {
        // Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        string subdirectoryPath = Path.Combine(ReplicaDirectory, "Subdirectory");
        testDirectoryFileHelper.CreateTestFile(ReplicaDirectory);
        testDirectoryFileHelper.CreateTestDirectory(subdirectoryPath);
        testDirectoryFileHelper.CreateTestFile(subdirectoryPath);

        int cancelTimeSeconds = 10;
        var config = FolderSyncConfig(0, cancelTimeSeconds,
            out var mockLogger, out var cancellationToken, out var syncOperations);
        var folderSync = new FolderSync(mockLogger.Object, config, syncOperations);

        // Act
        var syncTask = folderSync.StartSync(cancellationToken);

        // Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        Assert.Empty(Directory.GetFileSystemEntries(ReplicaDirectory));
    }

    [Fact]
    public void NestedFoldersWithFiles_EmptyReplica_ShouldCopy()
    {
        //Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        const string subdirectoryName = "Subdirectory";
        string subdirectoryPath = Path.Combine(SourceDirectory, subdirectoryName);
        testDirectoryFileHelper.CreateTestDirectory(subdirectoryPath);
        var fileName = testDirectoryFileHelper.CreateTestFile(subdirectoryPath, 10000);
        
        int cancelTimeSeconds = 10000;
        var config = FolderSyncConfig(0, cancelTimeSeconds, out var mockLogger, 
            out var cancellationToken, out var syncOperations);
        var folderSync = new FolderSync(mockLogger.Object, config, syncOperations);
        
        // Act
        var syncTask = folderSync.StartSync(cancellationToken);

        //Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        Assert.True(Directory.Exists(Path.Combine(ReplicaDirectory, subdirectoryName)));
        Assert.True(File.Exists(Path.Combine(ReplicaDirectory, subdirectoryName, fileName)));
    }
    
    //Test for ensuring source file is always copied, no matter if replica is older or newer modified dates
    [Theory]
    [InlineData(0, 1)]    
    [InlineData(1, 0)]    
    public void ModifiedDates_ShouldSyncToSourceDate(int sourceFileSubtractedDays, int replicaFileSubtractedDays)
    {
        //Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        string sourceFileName = testDirectoryFileHelper.CreateTestFile(SourceDirectory);
        testDirectoryFileHelper.CreateTestFile(ReplicaDirectory, 10000, sourceFileName);
        DateTime sourceFileModifiedTime = new DateTime(2023, 1, 1, 12, 0, 0); // Fixed current time for test
        File.SetLastWriteTime(Path.Combine(SourceDirectory, sourceFileName), sourceFileModifiedTime.AddDays(-sourceFileSubtractedDays));
        File.SetLastWriteTime(Path.Combine(ReplicaDirectory, sourceFileName), sourceFileModifiedTime.AddDays(-replicaFileSubtractedDays));
        
        int cancelTimeSeconds = 10;
        var config = FolderSyncConfig(0, cancelTimeSeconds, out var mockLogger, out var cancellationToken, out var syncOperations);
        var folderSync = new FolderSync(mockLogger.Object, config, syncOperations);
        
        // Act
        var syncTask = folderSync.StartSync(cancellationToken);
        
        //Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        DateTime expected = File.GetLastWriteTime(Path.Combine(SourceDirectory, sourceFileName));;
        DateTime actual = File.GetLastWriteTime(Path.Combine(ReplicaDirectory, sourceFileName));
        Assert.True((actual - expected).Duration() <= TimeSpan.FromSeconds(5), "File last write times are not within expected range.");
    }
    
    [Fact]
    public void ModifiedNames_ShouldCopy()
    {
        //Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        string sourceFileName = testDirectoryFileHelper.CreateTestFile(SourceDirectory);
        
        File.Copy(Path.Combine(SourceDirectory, sourceFileName), Path.Combine(ReplicaDirectory, "differentName.txt"));
        DateTime sourceFileModifiedTime = new DateTime(2023, 1, 1, 12, 0, 0); // Fixed current time for test
        File.SetLastWriteTime(Path.Combine(SourceDirectory, sourceFileName), sourceFileModifiedTime);
        File.SetLastWriteTime(Path.Combine(ReplicaDirectory, "differentName.txt"), sourceFileModifiedTime);

        int cancelTimeSeconds = 10;
        var config = FolderSyncConfig(0, cancelTimeSeconds, out var mockLogger, out var cancellationToken, out var syncOperations);
        var folderSync = new FolderSync(mockLogger.Object, config, syncOperations);
        
        // Act
        var syncTask = folderSync.StartSync(cancellationToken);
        
        //Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        Assert.True(File.Exists(Path.Combine(ReplicaDirectory, sourceFileName)));
    }
    
    [Fact]
    public void ModifiedSizes_ShouldCopy()
    {
        //Arrange
        using var testDirectoryFileHelper = new TestDirectoryFileHelper(SourceDirectory, ReplicaDirectory);
        string sourceFileName = testDirectoryFileHelper.CreateTestFile(SourceDirectory);
        string sourceFileFullPath = Path.Combine(SourceDirectory, sourceFileName);
        string replicaFileFullPath = Path.Combine(ReplicaDirectory, sourceFileName);
        File.Copy(sourceFileFullPath, replicaFileFullPath);
        
        // Add 1 byte to the source file
        using (var fileStream = new FileStream(replicaFileFullPath, FileMode.Append))
        {
            fileStream.WriteByte(0); // Add a single byte
        }
        
        DateTime sourceFileModifiedTime = new DateTime(2023, 1, 1, 12, 0, 0); // Fixed current time for test
        File.SetLastWriteTime(sourceFileFullPath, sourceFileModifiedTime);
        File.SetLastWriteTime(replicaFileFullPath, sourceFileModifiedTime);
        
        int cancelTimeSeconds = 10000;
        var config = FolderSyncConfig(0, cancelTimeSeconds, out var mockLogger, out var cancellationToken, out var syncOperations);
        var folderSync = new FolderSync(mockLogger.Object, config, syncOperations);
        
        // Act
        var syncTask = folderSync.StartSync(cancellationToken);
        
        //Assert
        Assert.True(syncTask.Wait(TimeSpan.FromSeconds(cancelTimeSeconds)), "Operation timed out");
        Assert.Equal(new FileInfo(sourceFileFullPath).Length, new FileInfo(replicaFileFullPath).Length);
    }

    //init config
    private static IFolderSyncConfig FolderSyncConfig(int intervalTime, int cancelTimeSeconds,
        out Mock<ILogger<FolderSync>> mockLogger,
        out CancellationToken cancellationToken, out ISyncOperations syncOperations)
    {
        string[] args = { SourceDirectory, ReplicaDirectory, intervalTime.ToString() };
        IFolderSyncConfig config = new FolderSyncConfig(args);
        var fileCompareStrategy = FileComparisonStrategyFactory.CreateDefaultStrategy();
        mockLogger = new Mock<ILogger<FolderSync>>();
        cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(cancelTimeSeconds)).Token;
        var fileOperationsLogger = new Mock<ILogger<FileOperations>>();
        var fileOperations = new FileOperations(fileOperationsLogger.Object);
        syncOperations = new SyncOperations(fileOperations, fileCompareStrategy);
        return config;
    }
}