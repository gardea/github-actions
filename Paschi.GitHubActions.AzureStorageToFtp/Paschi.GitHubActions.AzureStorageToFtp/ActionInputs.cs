namespace Paschi.GitHubActions.AzureStorageToFtp;

public class ActionInputs
{
    [Option('a', "connection-string",
        Required = true,
        HelpText = "Azure Blob Storage connection string (Key or SAS).")]
    public string ConnectionString { get; set; } = null!;

    [Option('c', "container-name",
        Required = true,
        HelpText = "The Azure Blob Storage container name from where to copy the files.")]
    public string ContainerName { get; set; } = null!;

    [Option('p', "publish-profile",
        Required = true,
        HelpText = "Applies to Web Apps(Windows and Linux) and Web App Containers(Linux). Multi container scenario not supported. Publish profile (*.publishsettings) file contents with Web Deploy secrets")]
    public string PublishProfile { get; set; } = null!;

    /// <summary>
    /// The directory or file used to copy. Example, path/to/copy.
    /// </summary>
    [Option('s', "source",
        Required = true,
        HelpText = "The directory or file used to copy. Example, \"path/to/folder/\" or \"path/to/file.txt\".")]
    public string Source { get; set; } = null!;

    [Option('d', "destination", 
        Required = true,
        HelpText = "The directory where to copy files. Example, \"path/to/destination\".")]
    public string Destination { get; set; } = null!;
}

