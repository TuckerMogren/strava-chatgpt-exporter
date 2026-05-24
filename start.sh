#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$ROOT_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing .env. Create it from .env.example and add your Strava credentials."
  echo "  cp .env.example .env"
  exit 1
fi

set -a
source "$ENV_FILE"
set +a

if [[ -z "${STRAVA_CLIENT_ID:-}" || -z "${STRAVA_CLIENT_SECRET:-}" ]]; then
  echo "STRAVA_CLIENT_ID and STRAVA_CLIENT_SECRET must be set in .env."
  exit 1
fi

export Strava__ClientId="$STRAVA_CLIENT_ID"
export Strava__ClientSecret="$STRAVA_CLIENT_SECRET"
export Strava__RedirectUri="${Strava__RedirectUri:-http://localhost:5080/api/strava/callback}"
export Frontend__Origin="${Frontend__Origin:-http://localhost:5173}"
export Frontend__RedirectAfterConnect="${Frontend__RedirectAfterConnect:-http://localhost:5173/connected}"
export ConnectionStrings__AppDb="${ConnectionStrings__AppDb:-Data Source=$ROOT_DIR/strava-exporter.db}"
export VITE_API_BASE_URL="${VITE_API_BASE_URL:-http://localhost:5080}"

cleanup() {
  if [[ -n "${API_PID:-}" ]]; then
    kill "$API_PID" 2>/dev/null || true
  fi
  if [[ -n "${WEB_PID:-}" ]]; then
    kill "$WEB_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "Starting API on http://localhost:5080"
dotnet run --project "$ROOT_DIR/backend/src/StravaExporter.Api/StravaExporter.Api.csproj" &
API_PID=$!

echo "Starting frontend on http://localhost:5173"
(
  cd "$ROOT_DIR/frontend"
  if [[ ! -d node_modules ]]; then
    npm install
  fi
  npm run dev
) &
WEB_PID=$!

while kill -0 "$API_PID" 2>/dev/null && kill -0 "$WEB_PID" 2>/dev/null; do
  sleep 1
done
