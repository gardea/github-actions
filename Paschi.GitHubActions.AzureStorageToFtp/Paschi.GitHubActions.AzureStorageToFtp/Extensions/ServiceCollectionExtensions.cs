namespace Paschi.GitHubActions.AzureStorageToFtp.Extensions;

static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddGitHubActionServices(
        this IServiceCollection services) => services.AddSingleton<CopyProvider>();
}
