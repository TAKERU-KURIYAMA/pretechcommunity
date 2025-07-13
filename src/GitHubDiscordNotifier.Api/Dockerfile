FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/GitHubDiscordNotifier.Api/GitHubDiscordNotifier.Api.csproj", "GitHubDiscordNotifier.Api/"]
COPY ["src/GitHubDiscordNotifier.Application/GitHubDiscordNotifier.Application.csproj", "GitHubDiscordNotifier.Application/"]
COPY ["src/GitHubDiscordNotifier.Domain/GitHubDiscordNotifier.Domain.csproj", "GitHubDiscordNotifier.Domain/"]
COPY ["src/GitHubDiscordNotifier.Infrastructure/GitHubDiscordNotifier.Infrastructure.csproj", "GitHubDiscordNotifier.Infrastructure/"]
RUN dotnet restore "GitHubDiscordNotifier.Api/GitHubDiscordNotifier.Api.csproj"
COPY src/ .
WORKDIR "/src/GitHubDiscordNotifier.Api"
RUN dotnet build "GitHubDiscordNotifier.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GitHubDiscordNotifier.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "GitHubDiscordNotifier.Api.dll"]