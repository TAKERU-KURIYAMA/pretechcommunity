# GitHub Discord Notifier デプロイ手順書

## 目次
1. [前提条件](#前提条件)
2. [初回セットアップ](#初回セットアップ)
3. [環境変数設定](#環境変数設定)
4. [データベース初期化](#データベース初期化)
5. [Docker イメージビルド](#docker-イメージビルド)
6. [VPS デプロイ](#vps-デプロイ)
7. [SSL証明書設定](#ssl証明書設定)
8. [動作確認](#動作確認)
9. [トラブルシューティング](#トラブルシューティング)

## 前提条件

### 必要なソフトウェア
- Docker 20.10+
- Docker Compose 2.0+
- Git
- .NET 6.0 SDK (ローカル開発時)
- Node.js 18+ (フロントエンド開発時)

### VPS情報
- **ホスト**: 163.44.122.139
- **ドメイン**: pretechcommunity.com
- **OS**: Ubuntu 20.04 LTS (推奨)
- **ユーザー**: root
- **認証**: SSH公開鍵認証

## 初回セットアップ

### 1. VPSへのSSH接続設定

```bash
# ローカルマシンで公開鍵をVPSにコピー
ssh-copy-id root@163.44.122.139

# SSH接続テスト
ssh root@163.44.122.139
```

### 2. VPS基本環境構築

```bash
# VPSにSSH接続後、以下を実行

# システム更新
apt update && apt upgrade -y

# 必要なパッケージをインストール
apt install -y curl wget git nginx certbot python3-certbot-nginx

# Dockerをインストール
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Docker Composeをインストール
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Dockerサービスを開始
systemctl start docker
systemctl enable docker
```

### 3. プロジェクトのクローン

```bash
# プロジェクトディレクトリを作成
mkdir -p /home/github-discord-notifier
cd /home/github-discord-notifier

# Gitリポジトリをクローン
git clone https://github.com/yourusername/pretechcommunity.git .

# 実行権限を付与
chmod +x deployment/vps/*.sh
```

## 環境変数設定

### 1. 本番環境用 .env ファイル作成

```bash
# VPS上で .env ファイルを作成
cd /home/github-discord-notifier
cat > .env << 'EOF'
# Database Configuration
SQL_SERVER_PASSWORD=wKkBG-um@7v94c!
MSSQL_SA_PASSWORD=wKkBG-um@7v94c!

# JWT Configuration
JWT_SECRET=your-super-secure-256-bit-jwt-secret-key-here-please-change-this

# Redis Configuration
REDIS_PASSWORD=your-redis-password-here

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
FUNCTIONS_WORKER_RUNTIME=dotnet

# Connection Strings
SqlConnectionString=Server=sqlserver;Database=GitHubDiscordNotifier;User Id=sa;Password=wKkBG-um@7v94c!;TrustServerCertificate=true
RedisConnectionString=redis:6379

# GitHub Configuration (各ユーザーがシステム作成時に設定)
GITHUB_CLIENT_ID=
GITHUB_CLIENT_SECRET=

# Application Insights (オプション)
APPLICATIONINSIGHTS_CONNECTION_STRING=

# Domain Configuration
DOMAIN=pretechcommunity.com
EMAIL=admin@pretechcommunity.com
EOF
```

### 2. セキュリティ設定

```bash
# .envファイルの権限を制限
chmod 600 .env

# 所有者をrootに設定
chown root:root .env
```

### 3. JWT シークレットキー生成

```bash
# 安全なJWTシークレットキーを生成
openssl rand -base64 64

# 生成されたキーを .env ファイルのJWT_SECRETに設定
```

## データベース初期化

### 1. SQL Server コンテナ起動

```bash
cd /home/github-discord-notifier

# SQL Serverのみ先に起動
docker-compose -f deployment/vps/docker-compose.prod.yml up -d sqlserver

# コンテナが健全になるまで待機（約1-2分）
docker-compose -f deployment/vps/docker-compose.prod.yml logs -f sqlserver
```

### 2. データベーススキーマ作成

```bash
# SQL Serverコンテナ内でスキーマ作成スクリプトを実行
docker exec -i github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "wKkBG-um@7v94c!" \
    < db-scripts/001_initial_schema.sql

# 実行結果を確認
echo "Database initialization completed."
```

## Docker イメージビルド

### 1. GitHub Container Registry設定

```bash
# GitHubトークンでログイン（ローカルマシンで実行）
echo $GITHUB_TOKEN | docker login ghcr.io -u yourusername --password-stdin
```

### 2. イメージビルドとプッシュ

```bash
# APIイメージをビルド
docker build -f src/GitHubDiscordNotifier.Api/Dockerfile -t ghcr.io/yourusername/github-discord-notifier:latest .

# フロントエンドイメージをビルド
docker build -f frontend/Dockerfile -t ghcr.io/yourusername/github-discord-notifier-frontend:latest ./frontend

# イメージをプッシュ
docker push ghcr.io/yourusername/github-discord-notifier:latest
docker push ghcr.io/yourusername/github-discord-notifier-frontend:latest
```

## VPS デプロイ

### 1. 全サービス起動

```bash
cd /home/github-discord-notifier

# 全サービスを起動
docker-compose -f deployment/vps/docker-compose.prod.yml up -d

# サービス状態確認
docker-compose -f deployment/vps/docker-compose.prod.yml ps

# ログ確認
docker-compose -f deployment/vps/docker-compose.prod.yml logs -f
```

### 2. Nginx設定適用

```bash
# Nginx設定ファイルをコピー
cp deployment/vps/nginx.conf /etc/nginx/nginx.conf

# 設定をテスト
nginx -t

# Nginxを再起動
systemctl restart nginx
systemctl enable nginx
```

## SSL証明書設定

### 1. Let's Encrypt証明書取得

```bash
cd /home/github-discord-notifier

# SSL設定スクリプトを実行
sudo bash deployment/vps/setup-ssl.sh
```

### 2. 証明書の確認

```bash
# 証明書の状態を確認
certbot certificates

# SSL接続テスト
curl -I https://pretechcommunity.com/health
```

## 動作確認

### 1. ヘルスチェック

```bash
# APIヘルスチェック
curl https://pretechcommunity.com/api/health

# 期待される応答:
# {"status":"healthy","timestamp":"2025-07-03T...","checks":{"database":{"status":"healthy"},"redis":{"status":"healthy"}}}
```

### 2. サービス状態確認

```bash
# 全サービスの状態確認
docker-compose -f deployment/vps/docker-compose.prod.yml ps

# 個別サービスのログ確認
docker-compose -f deployment/vps/docker-compose.prod.yml logs api
docker-compose -f deployment/vps/docker-compose.prod.yml logs frontend
```

### 3. ユーザー登録テスト

```bash
# ユーザー登録API テスト
curl -X POST https://pretechcommunity.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#"
  }'
```

## 継続的デプロイ

### 1. 自動デプロイスクリプト使用

```bash
# デプロイスクリプトを実行
cd /home/github-discord-notifier
./deployment/vps/deploy.sh
```

### 2. GitHub Actions経由でのデプロイ

GitHub Actions が main ブランチへのプッシュで自動実行されます：

```yaml
# .github/workflows/main.yml が以下を実行:
# 1. ビルド & テスト
# 2. Dockerイメージビルド & プッシュ
# 3. VPSへの自動デプロイ
```

## 環境変数リファレンス

### 必須環境変数

| 変数名 | 説明 | 例 |
|--------|------|-----|
| `SQL_SERVER_PASSWORD` | SQL Serverのsaパスワード | `wKkBG-um@7v94c!` |
| `JWT_SECRET` | JWT署名用秘密鍵 | `openssl rand -base64 64で生成` |
| `SqlConnectionString` | データベース接続文字列 | `Server=sqlserver;Database=...` |
| `RedisConnectionString` | Redis接続文字列 | `redis:6379` |

### オプション環境変数

| 変数名 | 説明 | デフォルト値 |
|--------|------|-------------|
| `REDIS_PASSWORD` | Redis認証パスワード | なし |
| `GITHUB_CLIENT_ID` | GitHub OAuth Client ID | なし |
| `GITHUB_CLIENT_SECRET` | GitHub OAuth Client Secret | なし |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights | なし |

## ファイアウォール設定

```bash
# UFW有効化
ufw enable

# 必要なポートを開放
ufw allow 22    # SSH
ufw allow 80    # HTTP
ufw allow 443   # HTTPS

# 状態確認
ufw status
```

## バックアップ設定

### 1. データベースバックアップ

```bash
# 自動バックアップスクリプト作成
cat > /home/github-discord-notifier/backup-db.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/home/github-discord-notifier/backups"
mkdir -p $BACKUP_DIR

docker exec github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "wKkBG-um@7v94c!" \
    -Q "BACKUP DATABASE [GitHubDiscordNotifier] TO DISK = N'/var/opt/mssql/backups/GitHubDiscordNotifier_$DATE.bak'"

# 古いバックアップを削除（30日以上前）
find $BACKUP_DIR -name "*.bak" -mtime +30 -delete
EOF

chmod +x /home/github-discord-notifier/backup-db.sh
```

### 2. Cronジョブ設定

```bash
# 毎日3時にバックアップ実行
crontab -e

# 以下を追加:
0 3 * * * /home/github-discord-notifier/backup-db.sh
```

## 監視設定

### 1. ログローテーション

```bash
# Docker ログローテーション設定
cat > /etc/docker/daemon.json << 'EOF'
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
EOF

systemctl restart docker
```

### 2. システム監視

```bash
# htop インストール
apt install -y htop

# ディスク使用量監視
df -h

# メモリ使用量監視
free -h

# Docker統計情報
docker stats
```

## トラブルシューティング

### よくある問題と解決方法

#### 1. SQL Server接続エラー
```bash
# コンテナ状態確認
docker logs github-discord-notifier-sqlserver-prod

# ポート確認
netstat -tulnp | grep 1433

# 接続テスト
docker exec -it github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd -S localhost -U sa
```

#### 2. SSL証明書エラー
```bash
# 証明書ステータス確認
certbot certificates

# Nginx設定テスト
nginx -t

# 証明書更新テスト
certbot renew --dry-run
```

#### 3. Docker Compose エラー
```bash
# サービス再起動
docker-compose -f deployment/vps/docker-compose.prod.yml restart

# ログ確認
docker-compose -f deployment/vps/docker-compose.prod.yml logs --tail=100

# 完全再構築
docker-compose -f deployment/vps/docker-compose.prod.yml down
docker-compose -f deployment/vps/docker-compose.prod.yml up -d
```

#### 4. ディスク容量不足
```bash
# Dockerイメージクリーンアップ
docker system prune -a

# ログファイルクリーンアップ
find /var/lib/docker/containers/ -name "*.log" -exec truncate -s 0 {} \;
```

## セキュリティチェックリスト

- [ ] SSH公開鍵認証の設定
- [ ] 不要なポートの閉鎖
- [ ] SSL証明書の有効性確認
- [ ] 環境変数ファイルの権限設定 (600)
- [ ] Docker コンテナの最新化
- [ ] データベースバックアップの動作確認
- [ ] ログローテーションの設定
- [ ] 証明書自動更新の設定

## サポート情報

問題が発生した場合は以下を確認してください：

1. **ログファイル**: `/var/log/nginx/`, Docker logs
2. **設定ファイル**: `/etc/nginx/nginx.conf`, `.env`
3. **サービス状態**: `systemctl status nginx`, `docker ps`
4. **ネットワーク**: `netstat -tulnp`, `ufw status`

---

**⚠️ 重要**: 本番環境では定期的なセキュリティ更新とバックアップの確認を行ってください。