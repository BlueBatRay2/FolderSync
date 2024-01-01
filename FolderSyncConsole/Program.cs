using FolderSyncLib;
using FolderSyncLib.CompareStrategies;
using FolderSyncLib.Config;
using FolderSyncLib.FileAndFolder;
using FolderSyncLib.Syncing;

namespace FolderSyncConsole;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        if (!ValidateArgs(args))
            return;
        
        var folderSync = CreateAndConfigureFolderSync(args);

        var cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            var syncTask = folderSync.StartSync(cancellationTokenSource.Token);

            Console.WriteLine("Press any key to stop synchronization...");
            Console.ReadKey();

            // Signal the cancellation
            cancellationTokenSource.Cancel();

            // Wait for the sync task to complete
            await syncTask;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FolderSync failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static bool ValidateInterval(string intervalArg)
    {
        int interval = 0;
        try
        {
            interval = Int32.Parse(intervalArg);
        }
        catch(Exception)
        {
            Console.WriteLine($"Interval time invalid. must be a value between 1 and " + Int32.MaxValue);
            return false;
        }
        if (interval <= 0)
        {
            Console.WriteLine($"Interval time invalid. must be a value between 1 and " + Int32.MaxValue);
            return false;
        }

        return true;
    }


    
    private static bool ValidateDirectoryExists(string directory)
    {
        if(!Directory.Exists(directory))
        {
            Console.WriteLine("Error. Source directory " + directory + "doesn't exist");
            return false;
        }

        return true;
    }
    private static bool ValidateArgs(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Error. Args must be in the following format {Full source path} {Full replica path} {Sync time in minutes} {Log path with file name}");
            return false;
        }

        if(!ValidateInterval(args[2])) return false;
        
        string sourceDirectory = args[0];
        string replicaDirectory = args[1];
        string Logfile = args[3];
        
        var logDirectoryPath = Path.GetDirectoryName(Logfile);
        
        if (logDirectoryPath == null)
        {
            Console.WriteLine($"LogFile can't be null");
            return false;
        }

        if (!ValidateDirectoryExists(sourceDirectory)
            || !ValidateDirectoryExists(logDirectoryPath))
            return false;
        
        if (logDirectoryPath == args[0] || logDirectoryPath == args[1])
        {
            Console.WriteLine($"LogFile can't have same directory as source or replica directory");
            return false;
        }
        
        if (!CanWriteToLogFile(logDirectoryPath)
            || !CanWriteToLogFile(replicaDirectory))
            return false;
        
        return true;
    }

    private static FolderSync CreateAndConfigureFolderSync(string[] args)
    {
        var builder = new ConfigurationBuilder();
        BuildConfig(builder, args);
        
        string logPath = args[3];
        
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<FolderSync>();
                services.AddSingleton<IFolderSyncConfig>(new FolderSyncConfig(args));
                services.AddTransient<IFileOperations, FileOperations>(); 
                services.AddTransient<IFileCompareStrategy>(provider => FileComparisonStrategyFactory.CreateDefaultStrategy());
                services.AddTransient<ISyncOperations, SyncOperations>();
            })
            .UseSerilog((context, configuration) => 
            {
                configuration
                    .ReadFrom.Configuration(builder.Build())
                    .Enrich.FromLogContext()
                    .WriteTo.File(logPath)
                    .WriteTo.Console();
                    
            })
            .Build();
        
        FolderSync folderSync = host.Services.GetRequiredService<FolderSync>();

        return folderSync;
    }
    private static void BuildConfig(IConfigurationBuilder builder, string[] args)
    {
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    }
    
    public static bool CanWriteToLogFile(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            // Attempt to create (and delete) a temporary file as a test
            var testFilePath = Path.Combine(directory, Guid.NewGuid().ToString());
            File.WriteAllText(testFilePath, "Test");
            File.Delete(testFilePath);

            return true;
        }
        catch (Exception)
        {
            Console.WriteLine($"Error testing log file write. Check write permissions to the directory {directory}");
            return false;
        }
    }
}