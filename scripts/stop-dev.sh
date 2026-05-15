#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_DIR="$ROOT_DIR/.run"

stop_pid() {
  local name="$1"
  local pidfile="$2"

  if [[ ! -f "$pidfile" ]]; then
    return 0
  fi

  local pid
  pid="$(cat "$pidfile")"

  if kill -0 "$pid" >/dev/null 2>&1; then
    kill "$pid" >/dev/null 2>&1 || true
    wait "$pid" 2>/dev/null || true
    echo "Stopped $name ($pid)"
  fi

  rm -f "$pidfile"
}

stop_pid "frontend" "$RUN_DIR/frontend.pid"
stop_pid "backend" "$RUN_DIR/backend.pid"
stop_pid "agents" "$RUN_DIR/agents.pid"
