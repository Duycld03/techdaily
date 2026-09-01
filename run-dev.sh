#!/usr/bin/env bash

# Exit immediately if a command exits with a non-zero status
set -e

# Cleanup child processes on exit (Ctrl+C)
trap 'echo -e "\n🛑 Stopping TechDaily services..."; kill $(jobs -p) 2>/dev/null || true; fuser -k 5000/tcp 3000/tcp 2>/dev/null || true; exit 0' SIGINT SIGTERM EXIT

echo "=========================================================="
echo "⚡ TechDaily - Senior Engineering Micro-Learning Platform"
echo "=========================================================="

# Clean up any lingering processes on ports 5000 and 3000
fuser -k 5000/tcp 3000/tcp 2>/dev/null || true

# 1. Check if database container is running
if ! docker ps --format '{{.Names}}' | grep -q 'techdaily_postgres'; then
  echo "📦 Starting PostgreSQL 17 (pgvector) container..."
  docker compose up -d db
fi

# Get Local IP address for mobile access
LOCAL_IP=$(hostname -I 2>/dev/null | awk '{print $1}')
if [ -z "$LOCAL_IP" ]; then
  LOCAL_IP="localhost"
fi

# 2. Start Backend API with Hot Reload
echo "🚀 Starting ASP.NET Core API on http://0.0.0.0:5000..."
dotnet watch --project backend/src/TechDaily.Api --urls "http://0.0.0.0:5000" &
BACKEND_PID=$!

# Wait for backend port to be open
echo "⏳ Waiting for API to become available..."
until curl -s http://localhost:5000/health > /dev/null 2>&1 || [ ! -e /proc/$BACKEND_PID ]; do
  sleep 1
done

# 3. Start Frontend Nuxt 4 with HMR (binding to 0.0.0.0)
echo "✨ Starting Nuxt 4 Frontend on http://0.0.0.0:3000..."
echo "📱 Mobile Access URL: http://${LOCAL_IP}:3000"
npm --prefix frontend run dev -- --host 0.0.0.0

# Wait for background jobs
wait
