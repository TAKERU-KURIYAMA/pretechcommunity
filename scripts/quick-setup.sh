#!/bin/bash

# GitHub Discord Notifier Quick Setup Script
# 使用方法: curl -fsSL https://raw.githubusercontent.com/yourusername/pretechcommunity/main/scripts/quick-setup.sh | bash

set -e

echo "🚀 GitHub Discord Notifier セットアップを開始します..."

# 色付きログ出力
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# システム情報確認
log_info "システム情報を確認中..."
uname -a
df -h
free -h

# 必要なパッケージのインストール
log_info "必要なパッケージをインストール中..."
apt update
apt install -y curl wget git nginx certbot python3-certbot-nginx htop

# Dockerのインストール確認
if ! command -v docker &> /dev/null; then
    log_info "Dockerをインストール中..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl start docker
    systemctl enable docker
    log_success "Docker インストール完了"
else
    log_success "Docker は既にインストール済み"
fi

# Docker Composeのインストール確認
if ! command -v docker-compose &> /dev/null; then
    log_info "Docker Composeをインストール中..."
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    log_success "Docker Compose インストール完了"
else
    log_success "Docker Compose は既にインストール済み"
fi

# プロジェクトディレクトリの作成
PROJECT_DIR="/home/github-discord-notifier"
log_info "プロジェクトディレクトリを作成中: $PROJECT_DIR"
mkdir -p $PROJECT_DIR
cd $PROJECT_DIR

# Gitリポジトリのクローン
if [ ! -d ".git" ]; then
    log_info "Gitリポジトリをクローン中..."
    git clone https://github.com/yourusername/pretechcommunity.git .
    log_success "リポジトリクローン完了"
else
    log_info "既存のリポジトリを更新中..."
    git pull origin main
    log_success "リポジトリ更新完了"
fi

# 実行権限の付与
log_info "スクリプトファイルに実行権限を付与中..."
chmod +x deployment/vps/*.sh
chmod +x scripts/*.sh

# 環境変数ファイルの作成
if [ ! -f ".env" ]; then
    log_info "環境変数ファイルを作成中..."
    cat > .env << 'EOF'
# Database Configuration
SQL_SERVER_PASSWORD=wKkBG-um@7v94c!
MSSQL_SA_PASSWORD=wKkBG-um@7v94c!

# JWT Configuration (will be generated automatically)
JWT_SECRET=

# Redis Configuration
REDIS_PASSWORD=

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
FUNCTIONS_WORKER_RUNTIME=dotnet

# Connection Strings
SqlConnectionString=Server=sqlserver;Database=GitHubDiscordNotifier;User Id=sa;Password=wKkBG-um@7v94c!;TrustServerCertificate=true
RedisConnectionString=redis:6379

# Domain Configuration
DOMAIN=pretechcommunity.com
EMAIL=admin@pretechcommunity.com

# GitHub Configuration (optional)
GITHUB_CLIENT_ID=
GITHUB_CLIENT_SECRET=

# Application Insights (optional)
APPLICATIONINSIGHTS_CONNECTION_STRING=
EOF

    # JWTシークレットを生成
    log_info "JWTシークレットキーを生成中..."
    JWT_SECRET=$(openssl rand -base64 64)
    sed -i "s/JWT_SECRET=.*/JWT_SECRET=$JWT_SECRET/" .env
    
    # ファイル権限を設定
    chmod 600 .env
    chown root:root .env
    
    log_success "環境変数ファイル作成完了"
else
    log_warning "環境変数ファイルが既に存在します"
fi

# バックアップディレクトリの作成
log_info "バックアップディレクトリを作成中..."
mkdir -p backups
mkdir -p logs

# バックアップスクリプトの作成
log_info "バックアップスクリプトを作成中..."
cat > backup-db.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/home/github-discord-notifier/backups"
mkdir -p $BACKUP_DIR

echo "Creating database backup: GitHubDiscordNotifier_$DATE.bak"
docker exec github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "wKkBG-um@7v94c!" \
    -Q "BACKUP DATABASE [GitHubDiscordNotifier] TO DISK = N'/var/opt/mssql/backups/GitHubDiscordNotifier_$DATE.bak'"

# 古いバックアップを削除（30日以上前）
find $BACKUP_DIR -name "*.bak" -mtime +30 -delete
echo "Backup completed: $BACKUP_DIR/GitHubDiscordNotifier_$DATE.bak"
EOF

chmod +x backup-db.sh

# ファイアウォール設定
log_info "ファイアウォールを設定中..."
if command -v ufw &> /dev/null; then
    ufw --force reset
    ufw default deny incoming
    ufw default allow outgoing
    ufw allow 22    # SSH
    ufw allow 80    # HTTP
    ufw allow 443   # HTTPS
    ufw --force enable
    log_success "ファイアウォール設定完了"
fi

# ログローテーション設定
log_info "Dockerログローテーションを設定中..."
cat > /etc/docker/daemon.json << 'EOF'
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
EOF

# システムサービスの確認
log_info "システムサービスを確認中..."
systemctl restart docker
systemctl enable nginx

# Dockerイメージの事前ダウンロード
log_info "Dockerイメージをダウンロード中..."
docker pull mcr.microsoft.com/mssql/server:2019-latest
docker pull redis:7-alpine
docker pull nginx:alpine

# セットアップ完了メッセージ
log_success "🎉 基本セットアップが完了しました！"
echo ""
echo "次の手順:"
echo "1. 環境変数を確認: cat .env"
echo "2. データベースを初期化: 以下のコマンドを順次実行"
echo "   docker-compose -f deployment/vps/docker-compose.prod.yml up -d sqlserver"
echo "   sleep 60"
echo "   docker exec -i github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"wKkBG-um@7v94c!\" < db-scripts/001_initial_schema.sql"
echo ""
echo "3. 全サービスを起動:"
echo "   docker-compose -f deployment/vps/docker-compose.prod.yml up -d"
echo ""
echo "4. SSL証明書を設定:"
echo "   sudo bash deployment/vps/setup-ssl.sh"
echo ""
echo "5. 動作確認:"
echo "   curl https://pretechcommunity.com/api/health"
echo ""
echo "詳細な手順は DEPLOYMENT_GUIDE.md を参照してください。"
echo ""
log_info "セットアップログは logs/setup.log に保存されました"

# ログファイルに記録
echo "Setup completed at $(date)" >> logs/setup.log
echo "System info: $(uname -a)" >> logs/setup.log
echo "Docker version: $(docker --version)" >> logs/setup.log
echo "Docker Compose version: $(docker-compose --version)" >> logs/setup.log