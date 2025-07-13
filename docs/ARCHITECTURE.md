# GitHub Discord Notifier アーキテクチャ図

## 1. システム全体アーキテクチャ

```mermaid
graph TB
    subgraph "External Services"
        GH[GitHub]
        Discord[Discord API]
        Email[Email Service]
    end
    
    subgraph "Client Layer"
        Browser[Web Browser]
        Mobile[Mobile Browser]
    end
    
    subgraph "Frontend - Vue.js SPA"
        Vue[Vue.js 3 + TypeScript]
        Router[Vue Router]
        Store[Pinia Store]
        UI[UI Components]
    end
    
    subgraph "API Gateway"
        Nginx[Nginx Reverse Proxy]
    end
    
    subgraph "Backend - .NET 8"
        subgraph "API Layer"
            WebAPI[ASP.NET Core Web API]
            Auth[JWT Authentication]
            MW[Middleware Pipeline]
        end
        
        subgraph "Business Logic Layer"
            AS[Auth Service]
            SS[System Service]
            NS[Notification Service]
            WS[Webhook Service]
            DS[Discord Service]
        end
        
        subgraph "Data Access Layer"
            EF[Entity Framework Core 8]
            Repo[Repository Pattern]
            UOW[Unit of Work]
        end
    end
    
    subgraph "Data Layer"
        PG[(PostgreSQL)]
        Redis[(Redis Cache)]
    end
    
    subgraph "Infrastructure"
        Docker[Docker Containers]
        VPS[Conoha VPS]
    end
    
    Browser --> Nginx
    Mobile --> Nginx
    Nginx --> Vue
    Vue --> Store
    Store --> Router
    Router --> UI
    Vue --> WebAPI
    
    WebAPI --> Auth
    Auth --> MW
    MW --> AS
    MW --> SS
    MW --> NS
    MW --> WS
    
    AS --> Repo
    SS --> Repo
    NS --> DS
    WS --> Repo
    DS --> Discord
    
    Repo --> UOW
    UOW --> EF
    EF --> PG
    AS --> Redis
    
    GH --> WS
    NS --> Email
    
    WebAPI --> Docker
    PG --> Docker
    Redis --> Docker
    Vue --> Docker
    Docker --> VPS
```

## 2. レイヤードアーキテクチャ詳細

```mermaid
graph LR
    subgraph "Presentation Layer"
        API[REST API Controllers]
        DTO[DTOs]
        Valid[Validation]
    end
    
    subgraph "Application Layer"
        Service[Application Services]
        Command[Commands]
        Query[Queries]
        Handler[Handlers]
    end
    
    subgraph "Domain Layer"
        Entity[Domain Entities]
        VObj[Value Objects]
        DService[Domain Services]
        IRepo[Repository Interfaces]
    end
    
    subgraph "Infrastructure Layer"
        Impl[Repository Implementations]
        DbContext[EF DbContext]
        External[External Services]
        Cache[Cache Service]
    end
    
    API --> Service
    Service --> Command
    Service --> Query
    Command --> Handler
    Query --> Handler
    Handler --> DService
    DService --> Entity
    Entity --> VObj
    Handler --> IRepo
    IRepo --> Impl
    Impl --> DbContext
    Service --> External
    Service --> Cache
```

## 3. .NET 8 プロジェクト構造

```
GitHubDiscordNotifier/
├── src/
│   ├── GitHubDiscordNotifier.Api/          # Web API プロジェクト
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── SystemsController.cs
│   │   │   ├── RepositoriesController.cs
│   │   │   ├── NotificationsController.cs
│   │   │   └── WebhookController.cs
│   │   ├── Middleware/
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   ├── JwtMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── Filters/
│   │   │   └── AuthorizeAttribute.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── GitHubDiscordNotifier.Application/  # アプリケーション層
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   ├── Mappings/
│   │   │   └── Behaviours/
│   │   ├── Auth/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── DTOs/
│   │   ├── Systems/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── DTOs/
│   │   └── Notifications/
│   │       ├── Commands/
│   │       ├── Queries/
│   │       └── DTOs/
│   │
│   ├── GitHubDiscordNotifier.Domain/       # ドメイン層
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── System.cs
│   │   │   ├── SystemMember.cs
│   │   │   ├── Repository.cs
│   │   │   └── NotificationChannel.cs
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── Role.cs
│   │   │   └── WebhookSecret.cs
│   │   ├── Interfaces/
│   │   │   ├── IUserRepository.cs
│   │   │   ├── ISystemRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Events/
│   │       └── DomainEvents.cs
│   │
│   ├── GitHubDiscordNotifier.Infrastructure/ # インフラ層
│   │   ├── Persistence/
│   │   │   ├── NotifierDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── DiscordService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── JwtService.cs
│   │   │   └── CacheService.cs
│   │   └── DependencyInjection.cs
│   │
│   └── GitHubDiscordNotifier.Tests/       # テストプロジェクト
│       ├── Unit/
│       ├── Integration/
│       └── E2E/
│
├── frontend/                               # Vue.jsフロントエンド
├── docker-compose.yml
├── .github/workflows/
└── README.md
```

## 4. データフロー図

```mermaid
graph LR
    subgraph "GitHub Webhook Flow"
        GH[GitHub Event] --> WHE[Webhook Endpoint]
        WHE --> SV[Signature Verification]
        SV --> EP[Event Parser]
        EP --> NP[Notification Processor]
        NP --> TM[Template Manager]
        TM --> DQ[Discord Queue]
        DQ --> DS[Discord Sender]
        DS --> DC[Discord Channel]
    end
    
    subgraph "Authentication Flow"
        U[User] --> LG[Login]
        LG --> VC[Validate Credentials]
        VC --> GT[Generate Token]
        GT --> ST[Store Token]
        ST --> RT[Return Token]
    end
    
    subgraph "System Management Flow"
        AD[Admin] --> CS[Create System]
        CS --> VD[Validate Discord]
        VD --> SS[Save System]
        SS --> CM[Create Member]
        CM --> NF[Notify]
    end
```

## 5. デプロイメントアーキテクチャ

```mermaid
graph TB
    subgraph "Internet"
        User[Users]
        GitHub[GitHub Webhooks]
    end
    
    subgraph "Conoha VPS"
        subgraph "Docker Network"
            Nginx[Nginx Container<br/>:80 :443]
            
            subgraph "Application Containers"
                API[.NET 8 API<br/>:5000]
                Frontend[Vue.js SPA<br/>:8080]
            end
            
            subgraph "Data Containers"
                Postgres[PostgreSQL<br/>:5432]
                Redis[Redis<br/>:6379]
            end
        end
        
        subgraph "Host System"
            Docker[Docker Engine]
            Certbot[Let's Encrypt]
            Logs[Log Files]
        end
    end
    
    User --> Nginx
    GitHub --> Nginx
    Nginx --> API
    Nginx --> Frontend
    API --> Postgres
    API --> Redis
    Frontend --> API
    Certbot --> Nginx
    Docker --> Logs
```

## 6. セキュリティアーキテクチャ

```mermaid
graph TB
    subgraph "Security Layers"
        subgraph "Network Security"
            HTTPS[HTTPS/TLS]
            FW[Firewall Rules]
            RL[Rate Limiting]
        end
        
        subgraph "Application Security"
            JWT[JWT Authentication]
            RBAC[Role-Based Access Control]
            VS[Input Validation]
            CSRF[CSRF Protection]
        end
        
        subgraph "Data Security"
            ENC[Encryption at Rest]
            HASH[Password Hashing]
            SEC[Secrets Management]
        end
        
        subgraph "Infrastructure Security"
            ISO[Container Isolation]
            UPD[Security Updates]
            MON[Security Monitoring]
        end
    end
    
    HTTPS --> JWT
    FW --> RL
    JWT --> RBAC
    RBAC --> VS
    VS --> CSRF
    HASH --> ENC
    ENC --> SEC
    ISO --> UPD
    UPD --> MON
```

## 7. スケーラビリティ考慮事項

### 7.1 水平スケーリング対応
- ステートレスなAPI設計
- Redisによるセッション管理
- ロードバランサー対応設計

### 7.2 パフォーマンス最適化
- 非同期処理（async/await）
- キャッシュ戦略
- データベースインデックス最適化
- N+1問題の回避

### 7.3 可用性
- ヘルスチェックエンドポイント
- グレースフルシャットダウン
- 自動リスタート設定
- バックアップ戦略

## 8. 監視とロギング

```mermaid
graph LR
    subgraph "Application"
        API[API Logs]
        ERR[Error Logs]
        PERF[Performance Metrics]
    end
    
    subgraph "Infrastructure"
        SYS[System Logs]
        DOCK[Docker Logs]
        NGX[Nginx Logs]
    end
    
    subgraph "Monitoring Stack"
        PROM[Prometheus]
        GRAF[Grafana]
        ALERT[AlertManager]
    end
    
    API --> PROM
    ERR --> PROM
    PERF --> PROM
    SYS --> PROM
    DOCK --> PROM
    NGX --> PROM
    PROM --> GRAF
    PROM --> ALERT
```

## 9. CI/CDパイプライン

```mermaid
graph LR
    subgraph "Development"
        DEV[Developer] --> GIT[Git Push]
    end
    
    subgraph "GitHub Actions"
        GIT --> BUILD[Build]
        BUILD --> TEST[Test]
        TEST --> SCAN[Security Scan]
        SCAN --> DOCK[Docker Build]
        DOCK --> PUSH[Push to Registry]
    end
    
    subgraph "Deployment"
        PUSH --> PULL[Pull Image]
        PULL --> STOP[Stop Container]
        STOP --> START[Start New Container]
        START --> HEALTH[Health Check]
        HEALTH --> NOTIFY[Notify Status]
    end
```

## 10. 技術選定理由

### 10.1 .NET 8を選択した理由
- 最新のLTS版で長期サポート
- 優れたパフォーマンス
- クロスプラットフォーム対応
- 豊富なエコシステム

### 10.2 PostgreSQLを選択した理由
- オープンソース
- JSON型サポート（通知設定保存に活用）
- 高い信頼性とパフォーマンス
- Entity Framework Coreとの相性

### 10.3 Redisを選択した理由
- 高速なキャッシュ
- セッション管理
- リアルタイム機能の実装基盤
- Pub/Sub機能

### 10.4 Vue.js 3を選択した理由
- Composition APIによる型安全性
- 軽量で高速
- 豊富なエコシステム
- 学習曲線が緩やか