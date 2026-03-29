#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
    echo ""
    echo "Shutting down..."

    if [[ -n "$FRONTEND_PID" ]] && kill -0 "$FRONTEND_PID" 2>/dev/null; then
        kill "$FRONTEND_PID" 2>/dev/null
        wait "$FRONTEND_PID" 2>/dev/null || true
        echo "Frontend stopped."
    fi

    if [[ -n "$BACKEND_PID" ]] && kill -0 "$BACKEND_PID" 2>/dev/null; then
        kill "$BACKEND_PID" 2>/dev/null
        wait "$BACKEND_PID" 2>/dev/null || true
        echo "Backend stopped."
    fi

    echo "Done."
}

trap cleanup EXIT INT TERM

echo "=== Starting backend (dotnet) ==="
cd "$ROOT_DIR/backend"
dotnet run --project src/TacBlog.Api &
BACKEND_PID=$!

echo "Waiting for backend on port 5063..."
for i in $(seq 1 30); do
    if curl -sf http://localhost:5063/api/tags > /dev/null 2>&1; then
        echo "Backend ready."
        break
    fi
    if ! kill -0 "$BACKEND_PID" 2>/dev/null; then
        echo "Backend process died."
        exit 1
    fi
    sleep 1
done

echo ""
echo "=== Starting frontend (astro) ==="
cd "$ROOT_DIR/frontend"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "Press Ctrl+C to stop both."
wait
