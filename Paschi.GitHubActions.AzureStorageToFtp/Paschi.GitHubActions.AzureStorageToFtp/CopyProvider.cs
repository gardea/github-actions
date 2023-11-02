using System.Net;
using System;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using static System.Net.WebRequestMethods;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection.Metadata;
using Azure.Storage.Blobs;
using FluentFTP.Helpers;
using Azure.Storage.Blobs.Models;
using Azure;

namespace Paschi.GitHubActions.AzureStorageToFtp;

sealed class CopyProvider
{
    ILogger<CopyProvider> _logger;
    public CopyProvider(ILogger<CopyProvider> logger)
    {
        _logger = logger;
    }
    internal async Task<ConcurrentBag<CopyResult>> CopyAsync(ActionInputs inputs)
    {
        var results = new ConcurrentBag<CopyResult>();
        XElement? name;
        try
        {
            var xdoc = XDocument.Parse(inputs.PublishProfile);
            name = xdoc.XPathSelectElement("/publishData/publishProfile[@publishMethod='FTP']");
        }
        catch (Exception ex)
        {
            results.Add(new CopyResult
            {
                Copied = false,
                Error = $"Unable to parse publishing profile: {ex.Message}"
            });
            return results;
        }
        if (name == null)
        {
            results.Add(new CopyResult
            {
                Copied = false,
                Error = "Unable to find FTP publishing method."
            });
            return results;
        }
        var urlValue = name.Attribute("publishUrl")?.Value;
        if (urlValue is not { Length: > 0 })
        {
            results.Add(new CopyResult
            {
                Copied = false,
                Error = "Unable to find FTP publish URL."
            });
            return results;
        }
        var url = new Uri(urlValue);

        var user = name.Attribute("userName")?.Value;
        var password = name.Attribute("userPWD")?.Value;
        var ftpClients = new ConcurrentBag<AsyncFtpClient>();
        var blobClients = new ConcurrentBag<BlobContainerClient>();
        var sourceContainer = new BlobContainerClient(inputs.ConnectionString, inputs.ContainerName);
        
        var files = await ListFiles(sourceContainer, inputs.Source);
        blobClients.Add(sourceContainer);

        var targetFolder = inputs.Destination is not { Length: > 0 } || inputs.Destination == "/" ? 
                string.Empty : (inputs.Destination.StartsWith('/') ? inputs.Destination[1..] : inputs.Destination);
        if (targetFolder is { Length: > 0 } && !targetFolder.EndsWith('/'))
            targetFolder = $"{targetFolder}/";
        var targetRoot = $"{url.LocalPath}{(url.LocalPath.EndsWith('/')?string.Empty:'/')}{targetFolder}";
        
        await Parallel.ForEachAsync(files, new ParallelOptions(), async (sourceFile, token) =>
        {
            var file = sourceFile;
            if(file == inputs.Source)
            {
                file = file.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            }
            else
            {
                file = file.Substring(inputs.Source.Length);
            }
            string thread = $"Thread {Environment.CurrentManagedThreadId}";
            if (!ftpClients.TryTake(out var client))
            {
                Console.WriteLine($"{thread} Opening FTP connection...");
                client = new AsyncFtpClient(url.Host, user, password);
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                client.Config.ValidateAnyCertificate = true;
                await client.Connect(token);
                Console.WriteLine($"{thread} Opened FTP connection {client.GetHashCode()}.");
            }

            if (!blobClients.TryTake(out var containerClient))
            {
                containerClient = new BlobContainerClient(inputs.ConnectionString, inputs.ContainerName);
            }
            // TODO fix the path
            string remotePath = $"{targetRoot}{file}";

            string desc =
                $"{thread}, Connection {client.GetHashCode()}, " +
                $"File {sourceFile} => {remotePath}";
            Console.WriteLine($"{desc} - Starting...");
            var startTime = DateTime.Now;
            try
            {
                var blobClient = containerClient.GetBlobClient(sourceFile);
                var info = await blobClient.DownloadAsync(token);
                var result = await client.UploadStream(info.Value.Content, remotePath, token: token);
                results.Add(new CopyResult
                {
                    Copied = result.IsSuccess()
                });
            }
            catch (Azure.RequestFailedException af)
            {
                _logger.LogError($"Unable to retrieve Azure blob [{af.Status}] - [{file}]: {af.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to retrieve Azure blob [{file}]: {ex.Message}");
            }
            Console.WriteLine($"{desc} - Done in {(DateTime.Now - startTime).TotalMilliseconds}[ms].");
            ftpClients.Add(client);
        });
        Console.WriteLine($"Closing {ftpClients.Count} connections");
        foreach (var client in ftpClients)
        {
            Console.WriteLine($"Closing connection {client.GetHashCode()}");
            client.Dispose();
        }
        return results;

    }

    private static async Task<IEnumerable<string>> ListFiles(BlobContainerClient sourceContainer, string source)
    {
        try
        {
            var files = new List<string>();
            // Call the listing operation and return pages of the specified size.
            var resultSegment = sourceContainer.GetBlobsAsync(prefix: source)
                .AsPages(default);

            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    if(source == blobItem.Name || source.EndsWith('/') && blobItem.Name.StartsWith(source))
                    {
                        files.Add(blobItem.Name);
                    }
                }
            }
            return files;
        }
        catch (RequestFailedException e)
        {
            throw new OperationException("Unable to list source files.", e);
        }
    }
}

sealed class CopyResult
{
    public bool Copied { get; set; }
    public string Error { get; set; }
}
