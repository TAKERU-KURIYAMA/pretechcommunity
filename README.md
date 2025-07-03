# GitHub Discord Notifier

A multi-tenant system that notifies GitHub repository events to Discord servers.

## Project Structure

```
.
├── src/
│   ├── GitHubDiscordNotifier.Api/       # Azure Functions API project
│   ├── GitHubDiscordNotifier.Common/    # Common library (entities, utilities)
│   └── GitHubDiscordNotifier.Tests/     # Test project
├── deployment/                           # Deployment configurations
├── db-scripts/                          # Database scripts
├── docker-compose.yml                   # Docker Compose configuration
└── .github/workflows/                   # GitHub Actions CI/CD

```

## Technology Stack

- **Backend**: .NET 6.0, Azure Functions v4
- **Database**: SQL Server 2019, Entity Framework Core 6.0
- **Cache**: Redis
- **Container**: Docker, Docker Compose
- **CI/CD**: GitHub Actions
- **Cloud**: ConohaVPS (planned)

## Development Environment Setup

### Prerequisites

- .NET 6.0 SDK
- Docker Desktop
- Azure Functions Core Tools v4
- Visual Studio 2022 / VS Code

### Initial Setup

1. Clone the repository
```bash
git clone https://github.com/yourusername/pretechcommunity.git
cd pretechcommunity
```

2. Start dependency services with Docker Compose
```bash
docker-compose up -d sqlserver redis azurite
```

3. Run database migrations (to be created)
```bash
dotnet ef database update -p src/GitHubDiscordNotifier.Common -s src/GitHubDiscordNotifier.Api
```

4. Start local development server
```bash
cd src/GitHubDiscordNotifier.Api
func start
```

## API Endpoints

- `GET /api/health` - Health check

## Environment Variables

See `src/GitHubDiscordNotifier.Api/local.settings.json` for reference.

## Deployment

Automatic deployment to VPS is triggered by pushing to the main branch.

## License

TBD
