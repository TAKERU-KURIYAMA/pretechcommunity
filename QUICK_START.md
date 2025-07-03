# GitHub Discord Notifier クイックスタートガイド

## 🚀 5分でデプロイ

### 前提条件
- VPS: 163.44.122.139 (ConohaVPS)
- ドメイン: pretechcommunity.com
- SSH公開鍵認証済み

### ステップ1: VPS接続

```bash
ssh root@163.44.122.139
```

### ステップ2: 一括セットアップスクリプト実行

```bash
# セットアップスクリプトをダウンロード & 実行
curl -fsSL https://raw.githubusercontent.com/yourusername/pretechcommunity/main/scripts/quick-setup.sh | bash
```

### ステップ3: 環境変数設定

```bash
cd /home/github-discord-notifier
cp .env.example .env

# JWTシークレット生成
JWT_SECRET=$(openssl rand -base64 64)
sed -i "s/JWT_SECRET=.*/JWT_SECRET=$JWT_SECRET/" .env

# 設定確認
cat .env
```

### ステップ4: サービス起動

```bash
# データベース初期化
docker-compose -f deployment/vps/docker-compose.prod.yml up -d sqlserver
sleep 60

# スキーマ作成
docker exec -i github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "wKkBG-um@7v94c!" \
    < db-scripts/001_initial_schema.sql

# 全サービス起動
docker-compose -f deployment/vps/docker-compose.prod.yml up -d
```

### ステップ5: SSL証明書設定

```bash
# 自動SSL設定
sudo bash deployment/vps/setup-ssl.sh
```

### ステップ6: 動作確認

```bash
# ヘルスチェック
curl https://pretechcommunity.com/api/health

# ユーザー登録テスト
curl -X POST https://pretechcommunity.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@pretechcommunity.com",
    "password": "Admin123!@#",
    "confirmPassword": "Admin123!@#"
  }'
```

## ✅ 成功時の確認項目

- [ ] https://pretechcommunity.com にアクセス可能
- [ ] APIヘルスチェックが正常
- [ ] ユーザー登録が可能
- [ ] SSL証明書が有効

## 🔧 主要コマンド

### サービス管理
```bash
# サービス状態確認
docker-compose -f deployment/vps/docker-compose.prod.yml ps

# ログ確認
docker-compose -f deployment/vps/docker-compose.prod.yml logs -f

# サービス再起動
docker-compose -f deployment/vps/docker-compose.prod.yml restart
```

### メンテナンス
```bash
# 最新版デプロイ
./deployment/vps/deploy.sh

# データベースバックアップ
./backup-db.sh

# システムクリーンアップ
docker system prune -f
```

## 🆘 トラブル時のチェック

1. **サービス確認**
   ```bash
   docker ps
   systemctl status nginx
   ```

2. **ログ確認**
   ```bash
   docker logs github-discord-notifier-api-prod
   tail -f /var/log/nginx/error.log
   ```

3. **接続確認**
   ```bash
   curl -I https://pretechcommunity.com
   nslookup pretechcommunity.com
   ```

## 📞 サポート

問題が発生した場合は `DEPLOYMENT_GUIDE.md` の詳細手順を参照してください。