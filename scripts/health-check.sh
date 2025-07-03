#!/bin/bash

# Health Check Script for GitHub Discord Notifier
# 使用方法: ./scripts/health-check.sh

set -e

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
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[⚠]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

echo "🏥 GitHub Discord Notifier ヘルスチェック"
echo "=========================================="
echo ""

# システム基本情報
log_info "システム情報"
echo "Date: $(date)"
echo "Hostname: $(hostname)"
echo "Uptime: $(uptime)"
echo ""

# ディスク使用量チェック
log_info "ディスク使用量チェック"
df -h
echo ""

# メモリ使用量チェック
log_info "メモリ使用量チェック"
free -h
echo ""

# Docker サービス状態チェック
log_info "Docker サービス状態チェック"
if systemctl is-active --quiet docker; then
    log_success "Docker サービスが稼働中"
else
    log_error "Docker サービスが停止中"
    exit 1
fi

# Docker Compose サービス状態チェック
log_info "Docker Compose サービス状態チェック"
if [ -f "deployment/vps/docker-compose.prod.yml" ]; then
    cd /home/github-discord-notifier
    
    SERVICES=(sqlserver redis api frontend)
    ALL_HEALTHY=true
    
    for service in "${SERVICES[@]}"; do
        if docker-compose -f deployment/vps/docker-compose.prod.yml ps | grep -q "$service.*Up"; then
            log_success "$service サービスが稼働中"
        else
            log_error "$service サービスが停止中または異常"
            ALL_HEALTHY=false
        fi
    done
    
    if [ "$ALL_HEALTHY" = false ]; then
        log_warning "一部のサービスに問題があります"
        docker-compose -f deployment/vps/docker-compose.prod.yml ps
    fi
else
    log_warning "Docker Compose設定ファイルが見つかりません"
fi

echo ""

# Nginx 状態チェック
log_info "Nginx 状態チェック"
if systemctl is-active --quiet nginx; then
    log_success "Nginx が稼働中"
    
    # Nginx設定テスト
    if nginx -t 2>/dev/null; then
        log_success "Nginx 設定が正常"
    else
        log_error "Nginx 設定にエラーがあります"
        nginx -t
    fi
else
    log_error "Nginx が停止中"
fi

echo ""

# SSL証明書チェック
log_info "SSL証明書チェック"
if [ -f "/etc/letsencrypt/live/pretechcommunity.com/fullchain.pem" ]; then
    EXPIRY_DATE=$(openssl x509 -enddate -noout -in /etc/letsencrypt/live/pretechcommunity.com/fullchain.pem | cut -d= -f2)
    EXPIRY_TIMESTAMP=$(date -d "$EXPIRY_DATE" +%s)
    CURRENT_TIMESTAMP=$(date +%s)
    DAYS_LEFT=$(( (EXPIRY_TIMESTAMP - CURRENT_TIMESTAMP) / 86400 ))
    
    if [ $DAYS_LEFT -gt 30 ]; then
        log_success "SSL証明書は有効です (残り ${DAYS_LEFT} 日)"
    elif [ $DAYS_LEFT -gt 7 ]; then
        log_warning "SSL証明書の期限が近づいています (残り ${DAYS_LEFT} 日)"
    else
        log_error "SSL証明書の期限が切れそうです (残り ${DAYS_LEFT} 日)"
    fi
else
    log_error "SSL証明書が見つかりません"
fi

echo ""

# ネットワーク接続チェック
log_info "ネットワーク接続チェック"

# HTTP接続チェック
if curl -s -I http://pretechcommunity.com | grep -q "301\|302"; then
    log_success "HTTP リダイレクトが正常"
else
    log_warning "HTTP リダイレクトに問題があります"
fi

# HTTPS接続チェック
if curl -s -I https://pretechcommunity.com | grep -q "200"; then
    log_success "HTTPS 接続が正常"
else
    log_error "HTTPS 接続に問題があります"
fi

# API ヘルスチェック
log_info "API ヘルスチェック"
API_RESPONSE=$(curl -s https://pretechcommunity.com/api/health || echo "ERROR")

if echo "$API_RESPONSE" | grep -q "healthy"; then
    log_success "API が正常に応答"
    echo "Response: $API_RESPONSE"
else
    log_error "API に問題があります"
    echo "Response: $API_RESPONSE"
fi

echo ""

# データベース接続チェック
log_info "データベース接続チェック"
if docker exec github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "wKkBG-um@7v94c!" -Q "SELECT 1" &>/dev/null; then
    log_success "データベース接続が正常"
else
    log_error "データベース接続に問題があります"
fi

# Redis接続チェック
log_info "Redis接続チェック"
if docker exec github-discord-notifier-redis-prod redis-cli ping | grep -q "PONG"; then
    log_success "Redis接続が正常"
else
    log_error "Redis接続に問題があります"
fi

echo ""

# ログファイルサイズチェック
log_info "ログファイルサイズチェック"
LOG_SIZE=$(du -sh /var/lib/docker/containers/ 2>/dev/null | cut -f1 || echo "不明")
echo "Dockerログ総サイズ: $LOG_SIZE"

if [ -d "/var/log/nginx" ]; then
    NGINX_LOG_SIZE=$(du -sh /var/log/nginx/ | cut -f1)
    echo "Nginxログサイズ: $NGINX_LOG_SIZE"
fi

echo ""

# ポート使用状況チェック
log_info "ポート使用状況"
echo "開放ポート:"
netstat -tulnp | grep LISTEN | grep -E ":(80|443|1433|6379|7071|8080)\s"

echo ""

# 最近のエラーログチェック
log_info "最近のエラーログ (過去1時間)"
if [ -f "/var/log/nginx/error.log" ]; then
    RECENT_ERRORS=$(tail -n 100 /var/log/nginx/error.log | grep "$(date +'%Y/%m/%d %H')" | wc -l)
    if [ $RECENT_ERRORS -eq 0 ]; then
        log_success "Nginxエラーログに新しいエラーはありません"
    else
        log_warning "Nginxエラーログに $RECENT_ERRORS 件のエラーがあります"
    fi
fi

# Docker コンテナエラーチェック
CONTAINER_ERRORS=$(docker ps -a | grep -v "Up" | wc -l)
if [ $CONTAINER_ERRORS -le 1 ]; then  # ヘッダー行を除く
    log_success "すべてのコンテナが正常に稼働中"
else
    log_warning "一部のコンテナに問題があります"
    docker ps -a | grep -v "Up"
fi

echo ""

# リソース使用率警告
log_info "リソース使用率チェック"

# CPU使用率
CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
if (( $(echo "$CPU_USAGE > 80" | bc -l) )); then
    log_warning "CPU使用率が高くなっています: ${CPU_USAGE}%"
else
    log_success "CPU使用率: ${CPU_USAGE}%"
fi

# メモリ使用率
MEMORY_USAGE=$(free | grep Mem | awk '{printf "%.1f", ($3/$2) * 100.0}')
if (( $(echo "$MEMORY_USAGE > 85" | bc -l) )); then
    log_warning "メモリ使用率が高くなっています: ${MEMORY_USAGE}%"
else
    log_success "メモリ使用率: ${MEMORY_USAGE}%"
fi

# ディスク使用率
DISK_USAGE=$(df / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
if [ $DISK_USAGE -gt 85 ]; then
    log_warning "ディスク使用率が高くなっています: ${DISK_USAGE}%"
else
    log_success "ディスク使用率: ${DISK_USAGE}%"
fi

echo ""
echo "=========================================="
echo "🏥 ヘルスチェック完了"
echo "詳細なログは各サービスのログファイルを確認してください"
echo ""
echo "問題がある場合は以下のコマンドで詳細を確認:"
echo "  docker-compose -f deployment/vps/docker-compose.prod.yml logs"
echo "  tail -f /var/log/nginx/error.log"
echo "  systemctl status nginx"