FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy solution and project files
COPY GitHubDiscordNotifier.sln ./
COPY src/GitHubDiscordNotifier.Common/*.csproj ./src/GitHubDiscordNotifier.Common/
COPY src/GitHubDiscordNotifier.Api/*.csproj ./src/GitHubDiscordNotifier.Api/
COPY src/GitHubDiscordNotifier.Tests/*.csproj ./src/GitHubDiscordNotifier.Tests/

# Restore packages
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish src/GitHubDiscordNotifier.Api/GitHubDiscordNotifier.Api.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/azure-functions/dotnet:4
WORKDIR /home/site/wwwroot
COPY --from=build-env /app/out .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true