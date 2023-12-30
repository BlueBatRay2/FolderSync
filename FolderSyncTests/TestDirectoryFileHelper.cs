using System.Security.Cryptography;

namespace FolderSyncTests;

public class TestDirectoryFileHelper : IDisposable
{
    private readonly List<string> _files = new();
    private readonly List<string> _directories = new();

    public TestDirectoryFileHelper(string sourceDirectory, string replicaDirectory)
    {
        CreateTestDirectory(sourceDirectory);
        CreateTestDirectory(replicaDirectory);
    }

    public void CreateTestDirectory(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        _directories.Add(directoryPath);
    }

    private void CreateFile(string filePath, long fileSizeInBytes)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                byte[] buffer = new byte[8192]; //8kb chunks is standard
                long bytesRemaining = fileSizeInBytes;

                Random random = new Random();
                while (bytesRemaining > 0)
                {
                    int bytesToWrite = (int)Math.Min(buffer.Length, bytesRemaining);
                    random.NextBytes(buffer);
                    binaryWriter.Write(buffer, 0, bytesToWrite);
                    bytesRemaining -= bytesToWrite;
                }
            }
        }
        
        _files.Add(filePath);
    }
    public string CreateTestFile(string directory, long fileSizeInBytes = 10000, string fileName = "")
    {
        if (fileName.Length == 0)
             fileName = Guid.NewGuid() + ".txt";
       
        string filePath = Path.Combine(directory, fileName);
        CreateFile(filePath, fileSizeInBytes);
        
        return fileName;
    }
    
    public void Dispose()
    {
        Parallel.ForEach(_files, (filePath, cancellationToken) =>
        {
            if(File.Exists(filePath))
                File.Delete(filePath);
        });
        
        Parallel.ForEach(_directories, (directoryPath, cancellationToken) =>
        {
            if(Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
        });
    }

    public static void TestDirectoryEqualness(string sourceDirectory, string replicaDirectory)
    {
        Assert.True(Directory.Exists(replicaDirectory));

        foreach (var fileName in Directory.GetFiles(sourceDirectory))
        {
            string sourceFilePath = Path.Combine(sourceDirectory, fileName);
            string replicaFilePath = Path.Combine(replicaDirectory, fileName);

            FileInfo sourceFileInfo = new FileInfo(sourceFilePath);
            FileInfo replicaFileInfo = new FileInfo(replicaFilePath);

            Assert.True(sourceFileInfo.Exists);
            Assert.True(replicaFileInfo.Exists);

            Assert.Equal(sourceFileInfo.Length, replicaFileInfo.Length);
            Assert.Equal(sourceFileInfo.LastWriteTime, replicaFileInfo.LastWriteTime);
    
            using (var sha256 = SHA256.Create())
            {
                using (var sourceStream = File.OpenRead(sourceFilePath))
                using (var replicaStream = File.OpenRead(replicaFilePath))
                {
                    var hashSource = sha256.ComputeHash(sourceStream);
                    var hashReplica = sha256.ComputeHash(replicaStream);
                    Assert.True(hashSource.SequenceEqual(hashReplica));
                }
            }
        }

        var sourceSubdirectories = Directory.GetDirectories(sourceDirectory);
        Assert.True(sourceSubdirectories.SequenceEqual(Directory.GetDirectories(replicaDirectory)));
        
        foreach (var sourceSubdirectory in sourceSubdirectories)
        {
            var newSourcePath = Path.Combine(sourceDirectory, sourceSubdirectory);
            var newReplicaPath = Path.Combine(replicaDirectory, sourceSubdirectory);
            
            TestDirectoryEqualness(newSourcePath, newReplicaPath);
        }
    }
    
    public List<string> SetupTestFilesSameDirectory(string sourceDirectory, int filesToCreate, long sizeLower, long sizeUpper)
    {
        List<string> fileNames = new();
        Random random = new Random(123); // Seed for reproducibility
    
        for (int i = 0; i < filesToCreate; i++)
        {
            var randomFileSize = random.NextInt64(sizeLower, sizeUpper);
            var fileName = CreateTestFile(sourceDirectory, randomFileSize);
            fileNames.Add(fileName);
        }

        return fileNames;
    }
}