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

    private static bool ValidateArgs(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Error. Args must be in the following format {Full source path} {Full replica path} {Sync time in minutes} {Log path with file name}");
            return false;
        }
        
        if(!Directory.Exists(args[0]))
        {
            Console.WriteLine("Error. Source directory " + args[0] + "doesn't exist");
            return false;
        }
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
}