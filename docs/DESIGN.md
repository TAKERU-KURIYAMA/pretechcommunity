# GitHub Discord Notifier 設計書

## 更新履歴
- 2025/07/03: 初版作成

## 1. システム概要

GitHubリポジトリのイベントをDiscordサーバーに通知するマルチテナント型システム。

## 2. アーキテクチャ

### 2.1 技術スタック
- **Backend**: .NET 6.0, Azure Functions v4
- **Frontend**: Vue.js 3.x (Composition API)
- **Database**: SQL Server 2019
- **Cache**: Redis
- **Discord Integration**: Discord.NET (Bot Framework)
- **Container**: Docker, Docker Compose
- **VPS**: ConohaVPS (163.44.122.139)
- **Domain**: pretechcommunity.com

### 2.2 認証方式
- **独自認証システム**を採用
- JWT (JSON Web Token) によるトークンベース認証
- パスワードはbcryptでハッシュ化して保存
- RefreshTokenによるトークン更新機能

### 2.3 Discord連携
- **Discord Bot**を採用
- Discord.NET ライブラリを使用
- Botトークンで認証し、WebhookよりリッチなUI/UXを提供

## 3. データベース設計

### 3.1 エンティティ

#### Users
- Id (GUID, PK)
- Email (varchar(255), Unique)
- PasswordHash (varchar(255))
- GitHubId (varchar(255), Nullable)
- GitHubUsername (varchar(255), Nullable)
- AvatarUrl (varchar(500), Nullable)
- RefreshToken (varchar(500), Nullable)
- RefreshTokenExpiry (DateTime, Nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

#### Systems
- Id (GUID, PK)
- Name (varchar(255))
- Description (varchar(1000), Nullable)
- DiscordServerId (varchar(255))
- DiscordServerName (varchar(255), Nullable)
- DiscordBotToken (varchar(500))
- OwnerId (GUID, FK -> Users.Id)
- WebhookSecret (varchar(255))
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

#### SystemMembers
- SystemId (GUID, FK -> Systems.Id)
- UserId (GUID, FK -> Users.Id)
- Role (varchar(50)) // owner, admin, member
- JoinedAt (DateTime)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)
- PK: (SystemId, UserId)

#### Repositories
- Id (GUID, PK)
- SystemId (GUID, FK -> Systems.Id)
- GitHubRepositoryId (varchar(255))
- GitHubRepositoryName (varchar(255))
- GitHubRepositoryUrl (varchar(500))
- IsActive (bit)
- AddedByUserId (GUID, FK -> Users.Id)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

#### NotificationChannels
- Id (GUID, PK)
- SystemId (GUID, FK -> Systems.Id)
- RepositoryId (GUID, FK -> Repositories.Id)
- DiscordChannelId (varchar(255))
- DiscordChannelName (varchar(255), Nullable)
- EventTypes (nvarchar(max)) // JSON array
- NotificationTemplate (nvarchar(max), Nullable) // カスタムテンプレート
- IsActive (bit)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

#### EventTypes (新規追加)
- Id (int, PK)
- Name (varchar(100))
- DisplayName (varchar(100))
- Description (varchar(500))
- DefaultEnabled (bit)

## 4. API設計

### 4.1 認証API
- POST /api/auth/register - ユーザー登録
- POST /api/auth/login - ログイン
- POST /api/auth/refresh - トークン更新
- POST /api/auth/logout - ログアウト
- GET /api/auth/me - 現在のユーザー情報

### 4.2 システム管理API
- GET /api/systems - システム一覧取得
- POST /api/systems - システム作成
- GET /api/systems/{id} - システム詳細取得
- PUT /api/systems/{id} - システム更新
- DELETE /api/systems/{id} - システム削除

### 4.3 メンバー管理API
- GET /api/systems/{systemId}/members - メンバー一覧
- POST /api/systems/{systemId}/members - メンバー招待
- PUT /api/systems/{systemId}/members/{userId} - メンバーロール更新
- DELETE /api/systems/{systemId}/members/{userId} - メンバー削除

### 4.4 リポジトリ管理API
- GET /api/systems/{systemId}/repositories - リポジトリ一覧
- POST /api/systems/{systemId}/repositories - リポジトリ追加
- PUT /api/systems/{systemId}/repositories/{id} - リポジトリ更新
- DELETE /api/systems/{systemId}/repositories/{id} - リポジトリ削除

### 4.5 通知設定API
- GET /api/systems/{systemId}/notifications - 通知設定一覧
- POST /api/systems/{systemId}/notifications - 通知設定追加
- PUT /api/systems/{systemId}/notifications/{id} - 通知設定更新
- DELETE /api/systems/{systemId}/notifications/{id} - 通知設定削除

### 4.6 Webhook受信API
- POST /api/webhook/{systemId} - GitHub Webhook受信

## 5. 権限管理

### 5.1 ロール定義
- **Owner**: システム作成者、全権限
- **Admin**: システム管理権限（メンバー招待、リポジトリ追加、通知設定変更）
- **Member**: 閲覧権限のみ

### 5.2 権限マトリックス
| 機能 | Owner | Admin | Member |
|------|-------|-------|--------|
| システム削除 | ○ | × | × |
| システム設定変更 | ○ | ○ | × |
| メンバー招待 | ○ | ○ | × |
| メンバーロール変更 | ○ | ○ | × |
| メンバー削除 | ○ | ○ | × |
| リポジトリ追加 | ○ | ○ | × |
| リポジトリ削除 | ○ | ○ | × |
| 通知設定変更 | ○ | ○ | × |
| 閲覧 | ○ | ○ | ○ |

## 6. GitHub Webhook検証

### 6.1 署名検証フロー
1. WebhookリクエストヘッダーからX-Hub-Signature-256を取得
2. SystemのWebhookSecretを使用してHMAC-SHA256を計算
3. 計算結果とヘッダーの値を比較
4. 一致しない場合は401 Unauthorizedを返す

### 6.2 実装例
```csharp
private bool VerifyWebhookSignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedSignature = $"sha256={BitConverter.ToString(computedHash).Replace("-", "").ToLower()}";
    return computedSignature == signature;
}
```

## 7. 通知カスタマイズ

### 7.1 イベントタイプ
- push
- pull_request
- issues
- issue_comment
- pull_request_review
- pull_request_review_comment
- release
- create (branch/tag)
- delete (branch/tag)

### 7.2 通知テンプレート
Handlebars.jsライクなテンプレートエンジンを使用：
```
{{repository.name}}: {{event.type}}
{{#if event.pusher}}
💡 {{event.pusher.name}} pushed {{event.commits.length}} commits
{{/if}}
```

## 8. VPS設定

### 8.1 Nginx設定
```nginx
server {
    listen 80;
    server_name pretechcommunity.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name pretechcommunity.com;

    ssl_certificate /etc/letsencrypt/live/pretechcommunity.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/pretechcommunity.com/privkey.pem;

    location /api {
        proxy_pass http://localhost:7071;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### 8.2 SSL証明書自動更新
Certbotのcronジョブ設定：
```bash
0 3 * * * /usr/bin/certbot renew --quiet --post-hook "systemctl reload nginx"
```

## 9. セキュリティ考慮事項

### 9.1 認証・認可
- JWTトークンの有効期限: 15分
- RefreshTokenの有効期限: 7日
- パスワード要件: 8文字以上、大文字・小文字・数字・記号を含む

### 9.2 データ保護
- すべての通信はHTTPS
- センシティブデータ（トークン、パスワード）は暗号化
- SQLインジェクション対策: Entity Framework使用
- XSS対策: 入力値のサニタイズ

### 9.3 レート制限
- API: 1分間に60リクエスト/IP
- Webhook: 1分間に100リクエスト/システム

## 10. 今後の拡張予定

- GitHub Apps対応
- Slack連携
- メール通知
- 通知履歴の保存と検索
- ダッシュボード機能