#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_DIR="$ROOT_DIR/.run"
LOG_DIR="$RUN_DIR/logs"
mkdir -p "$LOG_DIR"

AGENTS_DIR="$ROOT_DIR/pathfinder-ai-agents"
BACKEND_DIR="$ROOT_DIR/PathfinderAI"
FRONTEND_DIR="$ROOT_DIR/frontend_hackathon"

DOTNET_ROOT="${DOTNET_ROOT:-/Users/dkap/.local/share/dotnet}"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

BUNDLED_PYTHON="/Users/dkap/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3"
PYTHON_BIN="${PYTHON_BIN:-$BUNDLED_PYTHON}"

AGENTS_HOST="${AGENTS_HOST:-127.0.0.1}"
AGENTS_PORT="${AGENTS_PORT:-8088}"
BACKEND_HOST="${BACKEND_HOST:-127.0.0.1}"
BACKEND_PORT="${BACKEND_PORT:-5136}"
FRONTEND_HOST="${FRONTEND_HOST:-127.0.0.1}"
FRONTEND_PORT="${FRONTEND_PORT:-5174}"

OPENAI_BASE_URL="${OPENAI_BASE_URL:-https://example.services.ai.azure.com/openai/v1/}"
OPENAI_API_KEY="${OPENAI_API_KEY:-local-placeholder-key}"
AZURE_OPENAI_DEPLOYMENT_NAME="${AZURE_OPENAI_DEPLOYMENT_NAME:-local-placeholder-model}"
MICROSOFT_LEARN_MCP_ENDPOINT="${MICROSOFT_LEARN_MCP_ENDPOINT:-https://learn.microsoft.com/api/mcp}"
PATHFINDER_APP_ENV="${PATHFINDER_APP_ENV:-local}"
PATHFINDER_LOG_LEVEL="${PATHFINDER_LOG_LEVEL:-INFO}"

CONNECTION_STRING="${CONNECTION_STRING:-Host=localhost;Port=5432;Database=pathfinderai;Username=$USER}"
export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"
export Agents__BaseUrl="http://$AGENTS_HOST:$AGENTS_PORT"
export ASPNETCORE_URLS="http://$BACKEND_HOST:$BACKEND_PORT"

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

ensure_port_free() {
  local port="$1"
  if lsof -iTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1; then
    echo "Port $port is already in use." >&2
    lsof -iTCP:"$port" -sTCP:LISTEN >&2 || true
    exit 1
  fi
}

wait_for_http() {
  local url="$1"
  local name="$2"
  for _ in $(seq 1 60); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo "$name is ready at $url"
      return 0
    fi
    sleep 1
  done
  echo "$name did not become ready: $url" >&2
  return 1
}

start_process() {
  local name="$1"
  local workdir="$2"
  local pidfile="$3"
  local logfile="$4"
  local command="$5"

  if [[ -f "$pidfile" ]]; then
    local old_pid
    old_pid="$(cat "$pidfile")"
    if kill -0 "$old_pid" >/dev/null 2>&1; then
      echo "$name is already running with pid $old_pid"
      return 0
    fi
    rm -f "$pidfile"
  fi

  nohup /bin/bash -lc "cd \"$workdir\" && exec $command" </dev/null >"$logfile" 2>&1 &
  echo $! >"$pidfile"
}

if ! command_exists curl || ! command_exists psql || ! command_exists createdb || ! command_exists pnpm; then
  echo "Missing one of required tools: curl, psql, createdb, pnpm" >&2
  exit 1
fi

if [[ ! -x "$PYTHON_BIN" ]]; then
  echo "Python 3.11 runtime not found at $PYTHON_BIN" >&2
  exit 1
fi

if ! command_exists dotnet; then
  echo "dotnet is not available on PATH even after exporting DOTNET_ROOT=$DOTNET_ROOT" >&2
  exit 1
fi

ensure_port_free "$AGENTS_PORT"
ensure_port_free "$BACKEND_PORT"
ensure_port_free "$FRONTEND_PORT"

if ! psql -lqt | cut -d '|' -f 1 | grep -qw pathfinderai; then
  createdb -h localhost -U "$USER" pathfinderai
fi

if [[ ! -d "$AGENTS_DIR/.venv" ]]; then
  "$PYTHON_BIN" -m venv "$AGENTS_DIR/.venv"
fi

"$AGENTS_DIR/.venv/bin/pip" install -e "$AGENTS_DIR[dev]" >/dev/null
dotnet restore "$BACKEND_DIR/PathfinderAI.slnx" >/dev/null
pnpm --dir "$FRONTEND_DIR" install >/dev/null

start_process \
  "agents" \
  "$AGENTS_DIR" \
  "$RUN_DIR/agents.pid" \
  "$LOG_DIR/agents.log" \
  "env OPENAI_BASE_URL=\"$OPENAI_BASE_URL\" OPENAI_API_KEY=\"$OPENAI_API_KEY\" AZURE_OPENAI_DEPLOYMENT_NAME=\"$AZURE_OPENAI_DEPLOYMENT_NAME\" MICROSOFT_LEARN_MCP_ENDPOINT=\"$MICROSOFT_LEARN_MCP_ENDPOINT\" PATHFINDER_APP_ENV=\"$PATHFINDER_APP_ENV\" PATHFINDER_LOG_LEVEL=\"$PATHFINDER_LOG_LEVEL\" \"$AGENTS_DIR/.venv/bin/uvicorn\" pathfinder_ai_agents.main:app --host \"$AGENTS_HOST\" --port \"$AGENTS_PORT\""

start_process \
  "backend" \
  "$BACKEND_DIR" \
  "$RUN_DIR/backend.pid" \
  "$LOG_DIR/backend.log" \
  "env ASPNETCORE_URLS=\"$ASPNETCORE_URLS\" Agents__BaseUrl=\"$Agents__BaseUrl\" ConnectionStrings__DefaultConnection=\"$ConnectionStrings__DefaultConnection\" dotnet run --no-launch-profile --project PathfinderAPI/PathfinderAPI.csproj"

start_process \
  "frontend" \
  "$FRONTEND_DIR" \
  "$RUN_DIR/frontend.pid" \
  "$LOG_DIR/frontend.log" \
  "pnpm dev --host \"$FRONTEND_HOST\" --port \"$FRONTEND_PORT\""

wait_for_http "http://$AGENTS_HOST:$AGENTS_PORT/healthz" "Agents"
wait_for_http "http://$BACKEND_HOST:$BACKEND_PORT/health" "Backend"
wait_for_http "http://$FRONTEND_HOST:$FRONTEND_PORT" "Frontend"

cat <<EOF

All local services are running:

- Frontend: http://$FRONTEND_HOST:$FRONTEND_PORT
- Backend:  http://$BACKEND_HOST:$BACKEND_PORT
- Agents:   http://$AGENTS_HOST:$AGENTS_PORT

Logs:
- $LOG_DIR/frontend.log
- $LOG_DIR/backend.log
- $LOG_DIR/agents.log

Notes:
- Placeholder Azure OpenAI values are used unless you export real credentials before starting.
- With placeholder credentials, roadmap generation will fall back to the backend fallback pathway.
EOF
