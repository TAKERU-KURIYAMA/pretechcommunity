#!/bin/bash

# Environment Variables Update Script
# 使用方法: ./scripts/update-env.sh

set -e

# 色付きログ出力
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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

ENV_FILE="/home/github-discord-notifier/.env"

echo "🔧 環境変数設定ツール"
echo "===================="
echo ""

# 現在の設定表示
if [ -f "$ENV_FILE" ]; then
    log_info "現在の環境変数設定:"
    echo ""
    cat "$ENV_FILE" | grep -v "PASSWORD\|SECRET\|TOKEN" | sed 's/=.*/=***/' 
    echo ""
else
    log_error "環境変数ファイルが見つかりません: $ENV_FILE"
    exit 1
fi

# インタラクティブ設定更新
echo "環境変数を更新しますか？ (y/N)"
read -r CONFIRM

if [[ $CONFIRM =~ ^[Yy]$ ]]; then
    log_info "環境変数を更新します..."
    
    # JWTシークレット更新
    echo ""
    echo "JWTシークレットキーを再生成しますか？ (y/N)"
    read -r JWT_CONFIRM
    
    if [[ $JWT_CONFIRM =~ ^[Yy]$ ]]; then
        NEW_JWT_SECRET=$(openssl rand -base64 64)
        sed -i "s/JWT_SECRET=.*/JWT_SECRET=$NEW_JWT_SECRET/" "$ENV_FILE"
        log_success "JWTシークレットキーを更新しました"
    fi
    
    # SQL Serverパスワード更新
    echo ""
    echo "SQL Serverパスワードを変更しますか？ (y/N)"
    echo "⚠️  注意: パスワード変更後はデータベースコンテナの再作成が必要です"
    read -r SQL_CONFIRM
    
    if [[ $SQL_CONFIRM =~ ^[Yy]$ ]]; then
        echo "新しいSQL Serverパスワードを入力してください:"
        read -s NEW_SQL_PASSWORD
        
        if [ ${#NEW_SQL_PASSWORD} -ge 8 ]; then
            sed -i "s/SQL_SERVER_PASSWORD=.*/SQL_SERVER_PASSWORD=$NEW_SQL_PASSWORD/" "$ENV_FILE"
            sed -i "s/MSSQL_SA_PASSWORD=.*/MSSQL_SA_PASSWORD=$NEW_SQL_PASSWORD/" "$ENV_FILE"
            sed -i "s/Password=[^;]*/Password=$NEW_SQL_PASSWORD/" "$ENV_FILE"
            log_success "SQL Serverパスワードを更新しました"
            log_warning "データベースコンテナの再作成が必要です"
        else
            log_error "パスワードは8文字以上である必要があります"
        fi
    fi
    
    # Redisパスワード設定
    echo ""
    echo "Redisパスワードを設定しますか？ (y/N)"
    read -r REDIS_CONFIRM
    
    if [[ $REDIS_CONFIRM =~ ^[Yy]$ ]]; then
        echo "Redisパスワードを入力してください (空の場合は自動生成):"
        read -s REDIS_PASSWORD
        
        if [ -z "$REDIS_PASSWORD" ]; then
            REDIS_PASSWORD=$(openssl rand -base64 32)
        fi
        
        sed -i "s/REDIS_PASSWORD=.*/REDIS_PASSWORD=$REDIS_PASSWORD/" "$ENV_FILE"
        log_success "Redisパスワードを設定しました"
        log_warning "Redisコンテナの再作成が必要です"
    fi
    
    # GitHub OAuth設定
    echo ""
    echo "GitHub OAuth設定を更新しますか？ (y/N)"
    read -r GITHUB_CONFIRM
    
    if [[ $GITHUB_CONFIRM =~ ^[Yy]$ ]]; then
        echo "GitHub Client IDを入力してください:"
        read GITHUB_CLIENT_ID
        
        echo "GitHub Client Secretを入力してください:"
        read -s GITHUB_CLIENT_SECRET
        
        sed -i "s/GITHUB_CLIENT_ID=.*/GITHUB_CLIENT_ID=$GITHUB_CLIENT_ID/" "$ENV_FILE"
        sed -i "s/GITHUB_CLIENT_SECRET=.*/GITHUB_CLIENT_SECRET=$GITHUB_CLIENT_SECRET/" "$ENV_FILE"
        log_success "GitHub OAuth設定を更新しました"
    fi
    
    # Application Insights設定
    echo ""
    echo "Application Insights接続文字列を設定しますか？ (y/N)"
    read -r APPINSIGHTS_CONFIRM
    
    if [[ $APPINSIGHTS_CONFIRM =~ ^[Yy]$ ]]; then
        echo "Application Insights接続文字列を入力してください:"
        read APPINSIGHTS_CONNECTION_STRING
        
        sed -i "s/APPLICATIONINSIGHTS_CONNECTION_STRING=.*/APPLICATIONINSIGHTS_CONNECTION_STRING=$APPINSIGHTS_CONNECTION_STRING/" "$ENV_FILE"
        log_success "Application Insights設定を更新しました"
    fi
    
    # ファイル権限を設定
    chmod 600 "$ENV_FILE"
    chown root:root "$ENV_FILE"
    
    log_success "環境変数の更新が完了しました"
    
    # 再起動の確認
    echo ""
    echo "設定を反映するためにサービスを再起動しますか？ (y/N)"
    read -r RESTART_CONFIRM
    
    if [[ $RESTART_CONFIRM =~ ^[Yy]$ ]]; then
        log_info "サービスを再起動中..."
        cd /home/github-discord-notifier
        
        if [[ $SQL_CONFIRM =~ ^[Yy]$ ]] || [[ $REDIS_CONFIRM =~ ^[Yy]$ ]]; then
            log_warning "データベース/Redisパスワードが変更されたため、コンテナを再作成します"
            docker-compose -f deployment/vps/docker-compose.prod.yml down
            docker-compose -f deployment/vps/docker-compose.prod.yml up -d
        else
            docker-compose -f deployment/vps/docker-compose.prod.yml restart api frontend
        fi
        
        log_success "サービス再起動が完了しました"
    fi
    
else
    log_info "環境変数の更新をキャンセルしました"
fi

echo ""
echo "=========================================="
echo "🔧 環境変数設定完了"
echo ""
echo "設定確認コマンド:"
echo "  cat $ENV_FILE | grep -v 'PASSWORD\\|SECRET\\|TOKEN'"
echo ""
echo "動作確認コマンド:"
echo "  curl https://pretechcommunity.com/api/health"