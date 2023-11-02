# Set the base image as the .NET 6.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./Paschi.GitHubActions.AzureStorageToFtp/Paschi.GitHubActions.AzureStorageToFtp.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Petru Gardea <petru.gardea@paschidev.com>"
LABEL repository="https://github.com/gardea/github-actions"
LABEL homepage="https://github.com/gardea/github-actions"

# Label as GitHub action
LABEL com.github.actions.name="Azure Blob Storage to FTPS Copy"
LABEL com.github.actions.description="A Github action that copies files from Azure blob storage into a folder on an FTPS server."
LABEL com.github.actions.icon="download-cloud"
LABEL com.github.actions.color="blue"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:6.0
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/Paschi.GitHubActions.AzureStorageToFtp.dll" ]
