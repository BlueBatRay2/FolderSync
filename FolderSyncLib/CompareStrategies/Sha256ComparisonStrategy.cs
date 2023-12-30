using System.Security.Cryptography;

namespace FolderSyncLib.CompareStrategies;

public class Sha256ComparisonStrategy : IFileCompareStrategy
{
    public async Task<bool> AreEquivalentAsync(string file1, string file2)
    {
        using (var sha256 = SHA256.Create())
        {
            using (var sourceStream = File.OpenRead(file1))
            using (var replicaStream = File.OpenRead(file2))
            {
                var hashSource = sha256.ComputeHash(sourceStream);
                var hashReplica = sha256.ComputeHash(replicaStream);
                return hashSource.SequenceEqual(hashReplica);
            }
        }
    }
}