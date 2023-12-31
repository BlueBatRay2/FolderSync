using FolderSyncLib.CompareStrategies;
using FolderSyncLib.FileAndFolder;
using FolderSyncLib.Syncing;
using Moq;

namespace FolderSyncTests.UnitTests.Syncing;

public class SyncOperationsTests
{
    private static readonly string BaseTempDirectory = Path.GetTempPath();
    private static readonly string SourceDirectory = Path.Combine(BaseTempDirectory, "source");
    private static readonly string ReplicaDirectory = Path.Combine(BaseTempDirectory, "replica");

    [Fact]
    public async Task TestDeleteUnmatched()
    {
        //arrange
        var mockFileOperations = new Mock<IFileOperations>();
        var fileCompareStrategy = new Mock<IFileCompareStrategy>();

        mockFileOperations.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<string> { "file1.txt", "file2.txt" });
        mockFileOperations.Setup(fs => fs.EnumerateDirectories(It.IsAny<string>()))
            .Returns(new List<string> { "testdir1", "testdir2" });

        var syncOps = new SyncOperations(mockFileOperations.Object, fileCompareStrategy.Object);

        // act
        await syncOps.DeleteUnmatched(SourceDirectory, ReplicaDirectory);

        // assert
        mockFileOperations.Verify(fs => fs.EnumerateFiles(SourceDirectory), Times.Once);
        mockFileOperations.Verify(fs => fs.EnumerateDirectories(SourceDirectory), Times.Once);
    }
    
    [Fact]
    public async Task SyncFilesAndDirectories_ShouldSyncCorrectly()
    {
        //arrange
        var fileSystemMock = new Mock<IFileOperations>();
        var fileCompareStrategy = new Mock<IFileCompareStrategy>();
        var sourceDirectory = "sourcePath";
        var replicaDirectory = "replicaPath";
        var subDirectories = new[] { "subDir1", "subDir2" };

        fileSystemMock.Setup(fs => fs.CreateDirectory(replicaDirectory));
        fileSystemMock.Setup(fs => fs.GetDirectories(sourceDirectory)).Returns(subDirectories);
        fileSystemMock.Setup(fs => fs.GetSubdirectoriesNames(sourceDirectory)).Returns(subDirectories);
        
        var syncOps = new SyncOperations(fileSystemMock.Object, fileCompareStrategy.Object);

        // act
        await syncOps.SyncFilesAndDirectories(sourceDirectory, replicaDirectory, CancellationToken.None);
        
        //assert
        fileSystemMock.Verify(fs => fs.CreateDirectory(replicaDirectory), Times.Once);
       
        foreach (var subDir in subDirectories)
        {
            var newReplicaPath = Path.Combine(replicaDirectory, subDir);
            fileSystemMock.Verify(fo => fo.CreateDirectory(newReplicaPath), Times.Once);
        }
    }
}