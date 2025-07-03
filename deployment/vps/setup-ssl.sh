#!/bin/bash

# SSL証明書設定スクリプト for pretechcommunity.com
# 使用方法: sudo bash setup-ssl.sh

set -e

DOMAIN="pretechcommunity.com"
EMAIL="admin@pretechcommunity.com"  # 管理者メールアドレスを設定してください

echo "Setting up SSL certificate for $DOMAIN..."

# Certbotをインストール
if ! command -v certbot &> /dev/null; then
    echo "Installing Certbot..."
    if command -v apt-get &> /dev/null; then
        # Ubuntu/Debian
        apt-get update
        apt-get install -y certbot python3-certbot-nginx
    elif command -v yum &> /dev/null; then
        # CentOS/RHEL
        yum install -y certbot python3-certbot-nginx
    else
        echo "Unsupported OS. Please install certbot manually."
        exit 1
    fi
fi

# Nginxが起動していることを確認
if ! systemctl is-active --quiet nginx; then
    echo "Starting Nginx..."
    systemctl start nginx
fi

# 証明書を取得
echo "Obtaining SSL certificate..."
certbot certonly --nginx \
    --email $EMAIL \
    --agree-tos \
    --no-eff-email \
    --domains $DOMAIN,www.$DOMAIN

# Nginx設定をリロード
echo "Reloading Nginx configuration..."
systemctl reload nginx

# 自動更新のcronジョブを設定
echo "Setting up automatic renewal..."
CRON_JOB="0 3 * * * /usr/bin/certbot renew --quiet --post-hook 'systemctl reload nginx'"

# 既存のcronジョブをチェック
if ! crontab -l 2>/dev/null | grep -q "certbot renew"; then
    (crontab -l 2>/dev/null; echo "$CRON_JOB") | crontab -
    echo "Automatic renewal cron job added."
else
    echo "Automatic renewal cron job already exists."
fi

# SSL証明書の状態を確認
echo "Checking SSL certificate status..."
certbot certificates

echo "SSL setup completed successfully!"
echo ""
echo "Certificate locations:"
echo "  Certificate: /etc/letsencrypt/live/$DOMAIN/fullchain.pem"
echo "  Private Key: /etc/letsencrypt/live/$DOMAIN/privkey.pem"
echo ""
echo "Automatic renewal is configured to run daily at 3 AM."
echo "You can test renewal with: certbot renew --dry-run"