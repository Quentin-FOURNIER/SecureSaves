using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using NLog;

namespace SecureSaves
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            Logger.Info("Démarage de la sauvegarde.");
        
            var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            var appSettings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            
            var excludedPaths = appSettings.GetRequiredSection("excludedPaths").Get<List<string>>();

            var dir = args.Length > 0  ? args[0] : "C:\\SecureSaves";
            var saveDirectory = dir + "/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".zip";
            var saveTempDirectory = dir + "/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "-temp";
            
            CopyDirectory(appDataDirectory, saveTempDirectory, excludedPaths);

            ZipFile.CreateFromDirectory(saveTempDirectory, saveDirectory);
            Directory.Delete(saveTempDirectory, true);
            
            
            Logger.Info($"Le dossier {appDataDirectory} a été sauvegardé : Fin de la sauvegarde");
        }
    
        private static void CopyDirectory(string sourceDir, string destDir, List<string>? excludedPaths = null)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                if (excludedPaths is null || 
                    !excludedPaths.Contains(new DirectoryInfo(directory).Name, StringComparer.OrdinalIgnoreCase))
                {
                    var destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                    Directory.CreateDirectory(destDirectory);
                    try
                    {
                        CopyDirectory(directory, destDirectory, excludedPaths);

                    }
                    catch (IOException exception)
                    {
                        Logger.Error(exception, $"Le dossier {directory} n'a pas pu être sauvegardé.");
                        Directory.Delete(destDirectory, true);
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        Logger.Error(exception, $"Le dossier {directory} n'a pas pu être sauvegardé.");
                        Directory.Delete(destDirectory, true);
                    }
                }
            }
        }
    }
}
