#!/bin/bash

# Production deployment script for VPS
# 使用方法: ./deploy.sh

set -e

PROJECT_DIR="/home/github-discord-notifier"
BACKUP_DIR="/home/github-discord-notifier/backups"
DATE=$(date +%Y%m%d_%H%M%S)

echo "Starting deployment..."

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

# Navigate to project directory
cd $PROJECT_DIR

# Pull latest changes from git
echo "Pulling latest changes..."
git pull origin main

# Create database backup before deployment
echo "Creating database backup..."
docker exec github-discord-notifier-sqlserver-prod /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$SQL_SERVER_PASSWORD" \
    -Q "BACKUP DATABASE [GitHubDiscordNotifier] TO DISK = N'/var/opt/mssql/backups/GitHubDiscordNotifier_$DATE.bak'"

# Pull latest Docker images
echo "Pulling Docker images..."
docker-compose -f deployment/vps/docker-compose.prod.yml pull

# Stop services (except database to avoid data loss)
echo "Stopping application services..."
docker-compose -f deployment/vps/docker-compose.prod.yml stop api frontend

# Start updated services
echo "Starting updated services..."
docker-compose -f deployment/vps/docker-compose.prod.yml up -d

# Wait for services to be healthy
echo "Waiting for services to be healthy..."
sleep 30

# Check service health
echo "Checking service health..."
if docker-compose -f deployment/vps/docker-compose.prod.yml ps | grep -q "unhealthy"; then
    echo "ERROR: Some services are unhealthy!"
    docker-compose -f deployment/vps/docker-compose.prod.yml ps
    exit 1
fi

# Clean up old Docker images
echo "Cleaning up old Docker images..."
docker system prune -f

# Clean up old backups (keep last 10)
echo "Cleaning up old backups..."
cd $BACKUP_DIR
ls -t *.bak | tail -n +11 | xargs -r rm

echo "Deployment completed successfully!"
echo "Services status:"
docker-compose -f deployment/vps/docker-compose.prod.yml ps