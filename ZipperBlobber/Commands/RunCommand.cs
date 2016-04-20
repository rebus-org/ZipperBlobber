using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using GoCommando;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;

namespace ZipperBlobber.Commands
{
    [Command("run")]
    [Description("Zips & Blobs a directory of files")]
    public class RunCommand : ICommand
    {
        [Parameter("storage", allowConnectionString: true)]
        [Description("Storage account connection string")]
        public string ConnectionString { get; set; }

        [Parameter("dir")]
        [Description("Path to directory to ZIP and upload")]
        public string Directory { get; set; }

        [Parameter("container")]
        [Description("Name of the blob container to store backups in")]
        public string ContainerName { get; set; }

        public void Run()
        {
            var cloudBlobContainer = GetContainerReference();
            var directoryInfo = GetDirectoryInfo();
            var zipFilePath = GetTempFileName();

            Zip(directoryInfo, zipFilePath);

            Upload(zipFilePath, cloudBlobContainer);
        }

        static void Zip(DirectoryInfo directoryInfo, string zipFilePath)
        {
            Log.Information("Creating ZIP file {ZipFilePath}", zipFilePath);

            var totalFileSizeBytes = 0L;

            using (var destination = File.OpenWrite(zipFilePath))
            {
                using (var zipArchive = new ZipArchive(destination, ZipArchiveMode.Create))
                {
                    Log.Information("Zipping directory {DirectoryPath}", directoryInfo.FullName);

                    var basePath = new Uri(directoryInfo.FullName);

                    foreach (var file in directoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        var filePath = new Uri(file.FullName);

                        totalFileSizeBytes += file.Length;

                        var entryName = basePath.MakeRelativeUri(filePath).ToString()
                            .Replace('/', Path.DirectorySeparatorChar);

                        Log.Debug("Adding {EntryName}", entryName);

                        var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);

                        using (var entryStream = entry.Open())
                        using (var source = file.OpenRead())
                        {
                            WriteTo(source, entryStream);
                        }
                    }
                }
            }

            var compressedFileSizeBytes = new FileInfo(zipFilePath).Length;

            Log.Information("Done zipping! (size {PreviousFileSizeMB} MB => {CompressedFileSizeMB} MB)", 
                GetMb(totalFileSizeBytes), GetMb(compressedFileSizeBytes));
        }

        static void Upload(string filePath, CloudBlobContainer cloudBlobContainer)
        {
            var now = DateTime.Now;
            var blobName = $"{now:yyyyMMdd}-{now:HHmmss}.zip";
            var stopwatch = Stopwatch.StartNew();

            Log.Information("Uploading file {FilePath} to blob {BlobName} in container {ContainerName}", 
                filePath, blobName, cloudBlobContainer.Name);

            var blob = cloudBlobContainer.GetBlockBlobReference(blobName);

            blob.UploadFromFile(filePath);

            Log.Information("Done uploading! (elapsed {ElapsedSeconds} s)", stopwatch.Elapsed.TotalSeconds);
        }

        static double GetMb(long byteCount)
        {
            return Math.Round(byteCount / (1024.0 * 1024.0), 2);
        }

        static string GetTempFileName()
        {
            var tempFileName = Path.GetTempFileName();
            var tempPath = Path.GetTempPath();

            var zipFileName = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(tempFileName)}.zip");

            File.Move(tempFileName, zipFileName);

            return zipFileName;
        }

        static void WriteTo(Stream source, Stream destination)
        {
            var buffer = new byte[8192];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, count);
            }
            destination.Close();
        }

        DirectoryInfo GetDirectoryInfo()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                throw new GoCommandoException($"Directory '{Directory}' does not exist");
            }

            return new DirectoryInfo(Directory);
        }

        CloudBlobContainer GetContainerReference()
        {
            try
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(ConnectionString);
                var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(ContainerName);
                cloudBlobContainer.CreateIfNotExists();
                return cloudBlobContainer;
            }
            catch (Exception)
            {
                throw new GoCommandoException($"Could not get container {ContainerName} from storage account connection string '{ConnectionString}'");
            }
        }
    }
}