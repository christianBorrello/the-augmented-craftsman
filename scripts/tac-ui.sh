#!/usr/bin/env bash
# tac-ui.sh — Augmented Craftsman ASCII TUI
# Requires: gum (brew install gum)
# Compatible with bash 3.2+ (macOS default)

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
POSTS_DIR="$REPO_ROOT/docs/posts"
EDITOR_PORT=3456
EDITOR_URL="http://127.0.0.1:${EDITOR_PORT}"

# ── Load .env (if present) ────────────────────────────────────────────────

ENV_FILE="$REPO_ROOT/.env"
[ -f "$ENV_FILE" ] && set -a && . "$ENV_FILE" && set +a

# ── Backend Configuration ─────────────────────────────────────────────────

KOYEB_API_URL="https://api.theaugmentedcraftsman.christianborrello.dev"
LOCAL_API_URL="http://localhost:5063"
HEALTH_CHECK_INTERVAL=3
KOYEB_TIMEOUT=60
LOCAL_STARTUP_TIMEOUT=30
STATUS_FILE="/tmp/tac-backend-status"
HEALTH_PID_FILE="/tmp/tac-health-check.pid"
LOCAL_BACKEND_PID_FILE="/tmp/tac-local-backend.pid"

# CLI configuration
CLI_PROJECT="$REPO_ROOT/backend/src/TacBlog.Cli"
CLI_DLL=""
CLI_BUILD_LOG="/tmp/tac-cli-build.log"

# API key for admin endpoints (set via .env or shell environment)
TAC_API_KEY="${TAC_API_KEY:-}"

# Resolved at runtime by background probe
TAC_API_URL=""

# ── Helpers ────────────────────────────────────────────────────────────────

slugify() {
  echo "$1" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/-\+/-/g' | sed 's/^-\|-$//g'
}

require_gum() {
  if ! command -v gum &>/dev/null; then
    echo "Error: 'gum' is required. Install with: brew install gum" >&2
    exit 1
  fi
}

extract_title() {
  local t
  t=$(grep -m1 '^title:' "$1" 2>/dev/null | sed "s/^title:[[:space:]]*[\"']*//;s/[\"']*[[:space:]]*$//")
  [ -z "$t" ] && t=$(basename "$1" .md)
  echo "$t"
}

start_editor_server() {
  curl -sf "${EDITOR_URL}/health" > /dev/null 2>&1 && return 0
  cd "$REPO_ROOT/frontend" && node_modules/.bin/tsx editor-server.ts \
    > /tmp/tac-editor.log 2>&1 &
  echo $! > /tmp/tac-editor.pid
  local i=0
  while [ $i -lt 10 ]; do
    sleep 0.5
    curl -sf "${EDITOR_URL}/health" > /dev/null 2>&1 && return 0
    i=$((i + 1))
  done
  echo "Error: tac-editor did not start. Log: /tmp/tac-editor.log" >&2
  return 1
}

wait_for_enter() {
  gum input --placeholder "Press Enter to continue..." > /dev/null 2>&1 || true
}

pick_datetime() {
  local header="${1:-Publish at:}"
  local today_value
  today_value=$(date "+%Y-%m-%d")
  local today_display
  today_display="Today ($(date "+%a %d %b %Y"))"

  # Generate today + next 30 days
  local days=""
  local values=""
  days="$today_display"
  values="$today_value"
  local i=1
  while [ $i -le 30 ]; do
    local display
    display=$(date -v+${i}d "+%a %d %b %Y")
    local value
    value=$(date -v+${i}d "+%Y-%m-%d")
    days="$days"$'\n'"$display"
    values="$values"$'\n'"$value"
    i=$((i + 1))
  done

  local chosen_day
  chosen_day=$(printf "Skip\n%s" "$days" | gum choose --no-show-help --header "$header") || return 0
  [ "$chosen_day" = "Skip" ] && return 0

  # Resolve display to date value
  local day_value=""
  local idx=0
  while IFS= read -r d; do
    if [ "$d" = "$chosen_day" ]; then
      day_value=$(echo "$values" | sed -n "$((idx + 1))p")
      break
    fi
    idx=$((idx + 1))
  done <<< "$days"

  # Generate available hours — if today, only show future hours (next full hour onward)
  local min_hour=6
  if [ "$day_value" = "$today_value" ]; then
    local current_hour
    current_hour=$(date "+%H" | sed 's/^0//')
    min_hour=$((current_hour + 1))
    if [ $min_hour -lt 6 ]; then
      min_hour=6
    fi
    if [ $min_hour -gt 23 ]; then
      gum style --foreground 1 "No available hours left for today."
      return 0
    fi
  fi

  local hours=""
  local h=$min_hour
  while [ $h -le 23 ]; do
    local hour_str
    hour_str=$(printf '%02d:00' $h)
    if [ -z "$hours" ]; then
      hours="$hour_str"
    else
      hours="$hours"$'\n'"$hour_str"
    fi
    h=$((h + 1))
  done

  local chosen_hour
  chosen_hour=$(echo "$hours" | gum choose --no-show-help --header "Hour:") || return 0

  echo "${day_value}T${chosen_hour}:00Z"
}

open_url() {
  echo "Opening: $1"
  open "$1" 2>/dev/null || xdg-open "$1" 2>/dev/null || echo "Navigate to: $1"
}

pick_file_by_title() {
  local dir="$1"
  local header="$2"
  PICKED_FILE=""

  local files_list
  files_list=$(find "$dir" -name "*.md" 2>/dev/null | sort)
  if [ -z "$files_list" ]; then
    echo "No files found."
    wait_for_enter
    return 1
  fi

  local titles=""
  local paths=""
  local IFS_BAK="$IFS"
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    local t
    t=$(extract_title "$f")
    if [ -z "$titles" ]; then
      titles="$t"
      paths="$f"
    else
      titles="$titles"$'\n'"$t"
      paths="$paths"$'\n'"$f"
    fi
  done <<< "$files_list"
  IFS="$IFS_BAK"

  local chosen
  chosen=$(echo "$titles" | gum choose --no-show-help --header "$header") || return 1

  local idx=0
  while IFS= read -r t; do
    if [ "$t" = "$chosen" ]; then
      PICKED_FILE=$(echo "$paths" | sed -n "$((idx + 1))p")
      return 0
    fi
    idx=$((idx + 1))
  done <<< "$titles"

  return 1
}

# ── Backend Health Check (Background) ─────────────────────────────────────

write_status() {
  # Format: status|label|url|elapsed
  echo "$1|$2|$3|$4" > "$STATUS_FILE"
}

read_status() {
  # Returns: status|label|url|elapsed
  cat "$STATUS_FILE" 2>/dev/null || echo "checking|Connecting to Koyeb...||0"
}

get_status_field() {
  local data="$1"
  local field="$2"
  echo "$data" | cut -d'|' -f"$field"
}

probe_health() {
  local url="$1"
  curl -sf --connect-timeout 3 --max-time 5 "${url}/health/ready" > /dev/null 2>&1
}

background_health_check() {
  while true; do
    # Phase 1: Probe Koyeb with cold-start patience
    local elapsed=0
    write_status "warming" "Connecting to Koyeb..." "" "$elapsed"

    while [ "$elapsed" -lt "$KOYEB_TIMEOUT" ]; do
      if probe_health "$KOYEB_API_URL"; then
        write_status "ready" "Connected (Koyeb)" "$KOYEB_API_URL" "$elapsed"
        monitor_health "$KOYEB_API_URL" "Koyeb" && continue 2
        # monitor_health returned 1 = disconnected, restart outer loop
        continue 2
      fi
      sleep "$HEALTH_CHECK_INTERVAL"
      elapsed=$((elapsed + HEALTH_CHECK_INTERVAL))
      write_status "warming" "Connecting to Koyeb..." "" "$elapsed"
    done

    # Phase 2: Koyeb timed out — try local
    write_status "fallback" "Trying local backend..." "" "0"

    if probe_health "$LOCAL_API_URL"; then
      write_status "ready" "Connected (local)" "$LOCAL_API_URL" "0"
      monitor_health "$LOCAL_API_URL" "local" || continue
      continue
    fi

    # Phase 3: Auto-start local backend (only once)
    if [ ! -f "$LOCAL_BACKEND_PID_FILE" ]; then
      write_status "starting" "Starting local backend..." "" "0"
      dotnet run --project "$REPO_ROOT/backend/src/TacBlog.Api" \
        > /tmp/tac-local-backend.log 2>&1 &
      echo $! > "$LOCAL_BACKEND_PID_FILE"
    fi

    local local_elapsed=0
    while [ "$local_elapsed" -lt "$LOCAL_STARTUP_TIMEOUT" ]; do
      sleep "$HEALTH_CHECK_INTERVAL"
      local_elapsed=$((local_elapsed + HEALTH_CHECK_INTERVAL))
      write_status "starting" "Starting local backend..." "" "$local_elapsed"
      if probe_health "$LOCAL_API_URL"; then
        write_status "ready" "Connected (local)" "$LOCAL_API_URL" "$local_elapsed"
        monitor_health "$LOCAL_API_URL" "local" || continue 2
        continue 2
      fi
    done

    # Phase 4: Both backends unavailable
    write_status "unavailable" "Backend unavailable" "" "0"
    # Wait before retrying the whole cycle
    sleep "$KOYEB_TIMEOUT"
  done
}

monitor_health() {
  local url="$1"
  local label="$2"
  while true; do
    sleep "$HEALTH_CHECK_INTERVAL"
    if ! probe_health "$url"; then
      write_status "disconnected" "Backend disconnected" "" "0"
      # Signal disconnection — the main check loop will restart
      return 1
    fi
  done
}

start_health_check() {
  # Clean previous status
  rm -f "$STATUS_FILE"
  write_status "checking" "Connecting to Koyeb..." "" "0"

  background_health_check &
  echo $! > "$HEALTH_PID_FILE"
}

# ── Cleanup ───────────────────────────────────────────────────────────────

cleanup() {
  # Kill background health check
  if [ -f "$HEALTH_PID_FILE" ]; then
    local pid
    pid=$(cat "$HEALTH_PID_FILE" 2>/dev/null)
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null
      # Kill any child processes (the sleep/curl subprocesses)
      kill -- -"$pid" 2>/dev/null || true
    fi
    rm -f "$HEALTH_PID_FILE"
  fi

  # Kill local backend only if we started it
  if [ -f "$LOCAL_BACKEND_PID_FILE" ]; then
    local pid
    pid=$(cat "$LOCAL_BACKEND_PID_FILE" 2>/dev/null)
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null
    fi
    rm -f "$LOCAL_BACKEND_PID_FILE"
  fi

  # Kill editor server if we started it
  if [ -f /tmp/tac-editor.pid ]; then
    local pid
    pid=$(cat /tmp/tac-editor.pid 2>/dev/null)
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null
    fi
    rm -f /tmp/tac-editor.pid
  fi

  rm -f "$STATUS_FILE"
}

trap cleanup EXIT INT TERM

# ── Status Bar Rendering ──────────────────────────────────────────────────

render_status_bar() {
  local data
  data=$(read_status)
  local status
  status=$(get_status_field "$data" 1)
  local label
  label=$(get_status_field "$data" 2)
  local elapsed
  elapsed=$(get_status_field "$data" 4)

  case "$status" in
    ready)
      gum style --foreground 2 --bold "● $label"
      ;;
    warming)
      local progress=""
      local filled=$((elapsed * 20 / KOYEB_TIMEOUT))
      local empty=$((20 - filled))
      local i=0
      while [ $i -lt $filled ]; do progress="${progress}█"; i=$((i + 1)); done
      i=0
      while [ $i -lt $empty ]; do progress="${progress}░"; i=$((i + 1)); done
      gum style --foreground 208 "◐ $label  [$progress] ${elapsed}s/${KOYEB_TIMEOUT}s"
      ;;
    fallback|starting)
      gum style --foreground 208 "◐ $label"
      ;;
    unavailable)
      gum style --foreground 1 --bold "✗ $label"
      ;;
    disconnected)
      gum style --foreground 1 "⚠ $label — reconnecting..."
      ;;
    *)
      gum style --foreground 245 "◌ Initializing..."
      ;;
  esac
}

# ── Backend Status Helpers ────────────────────────────────────────────────

is_backend_ready() {
  local data
  data=$(read_status)
  local status
  status=$(get_status_field "$data" 1)
  [ "$status" = "ready" ]
}

get_resolved_api_url() {
  local data
  data=$(read_status)
  get_status_field "$data" 3
}

require_backend() {
  if ! is_backend_ready; then
    echo ""
    gum style --foreground 208 "Backend not yet available. Please wait..."
    echo ""
    wait_for_enter
    return 1
  fi
  TAC_API_URL=$(get_resolved_api_url)
  return 0
}

# ── CLI Helpers ────────────────────────────────────────────────────────────

build_cli() {
  if ! command -v dotnet &>/dev/null; then
    return 1
  fi
  dotnet build --configuration Release --nologo -v quiet "$CLI_PROJECT" \
    > "$CLI_BUILD_LOG" 2>&1 || return 1
  CLI_DLL="$CLI_PROJECT/bin/Release/net10.0/TacBlog.Cli.dll"
  [ -f "$CLI_DLL" ] || CLI_DLL=""
}

resolve_api_key() {
  local data
  data=$(read_status)
  local label
  label=$(get_status_field "$data" 2)

  case "$label" in
    *Koyeb*)
      if [ -z "${TAC_API_KEY:-}" ]; then
        echo "" >&2
        gum style --foreground 1 --bold "TAC_API_KEY is required for the Koyeb backend." >&2
        echo "Set it with: export TAC_API_KEY=your-key" >&2
        echo "" >&2
        wait_for_enter
        return 1
      fi
      echo "$TAC_API_KEY"
      ;;
    *)
      echo "${TAC_API_KEY:-dev-admin-key}"
      ;;
  esac
}

run_cli() {
  local api_key
  api_key=$(resolve_api_key) || return 1

  local tmpout
  tmpout=$(mktemp)

  if [ -n "$CLI_DLL" ]; then
    gum spin --spinner dot --title "Contacting backend..." -- \
      bash -c "dotnet exec \"$CLI_DLL\" --url \"$TAC_API_URL\" --key \"$api_key\" $* > \"$tmpout\" 2>&1"
  else
    gum spin --spinner dot --title "Contacting backend..." -- \
      bash -c "dotnet run --project \"$CLI_PROJECT\" -- --url \"$TAC_API_URL\" --key \"$api_key\" $* > \"$tmpout\" 2>&1"
  fi
  local rc=$?
  cat "$tmpout"
  rm -f "$tmpout"
  return $rc
}

# ── Menu Actions ───────────────────────────────────────────────────────────

action_list() {
  require_backend || return
  echo ""
  run_cli post list 2>&1 | sed 's/ *([^)]*)$//' || true
  echo ""
  wait_for_enter
}

action_new_draft() {
  local title
  title=$(gum input --placeholder "Post title..." --prompt "Title: ") || return
  [ -z "$title" ] && return

  local tags_input
  tags_input=$(gum input --placeholder "tdd, clean-architecture, ddd  (leave empty to skip)" --prompt "Tags: ") || true

  local tags_yaml="[]"
  if [ -n "$tags_input" ]; then
    tags_yaml="[$(echo "$tags_input" | sed 's/[[:space:]]*,[[:space:]]*/", "/g; s/^/"/; s/$/"/' | tr -d '\n')]"
  fi

  local scheduled_at=""
  scheduled_at=$(pick_datetime "Publish at:")

  local date_prefix
  date_prefix=$(date +%Y-%m-%d)
  local slug
  slug=$(slugify "$title")
  local file="$POSTS_DIR/drafts/${date_prefix}-${slug}.md"

  cat > "$file" <<TEMPLATE
---
title: "$title"
tags: $tags_yaml
scheduledAt: $scheduled_at
postId:
---

<!-- Una frase che cattura il punto centrale del post. Il lettore deve capire subito perché leggerlo. -->

## Il problema

<!-- Descrivi il contesto. Cosa stavi facendo quando hai incontrato questo problema?
     Sii specifico: codice reale, situazione reale. -->

## La soluzione

<!-- La tua risposta. Mostra codice, ragionamento, esempi concreti.
     Non nascondere i passaggi intermedi — il percorso è parte del valore. -->

## Conclusione

<!-- Cosa porta a casa il lettore? Un'idea, una tecnica, un cambio di prospettiva.
     Una frase sola basta se è quella giusta. -->
TEMPLATE

  echo "Created: $file"
  start_editor_server || return
  open_url "${EDITOR_URL}/edit?file=${file}&edit=1"
}

action_edit_draft() {
  while true; do
    pick_file_by_title "$POSTS_DIR/drafts" "Select a draft:" || return

    local draft_action
    draft_action=$(printf "Edit\nDelete" | gum choose --no-show-help --header "$(extract_title "$PICKED_FILE")") || continue

    case "$draft_action" in
      Edit)
        start_editor_server || return
        open_url "${EDITOR_URL}/edit?file=${PICKED_FILE}&edit=1"
        return
        ;;
      Delete)
        local title
        title=$(extract_title "$PICKED_FILE")
        gum confirm "Delete \"$title\"?" || continue
        rm "$PICKED_FILE"
        gum style --foreground 2 "Deleted: $title"
        sleep 1
        ;;
    esac
  done
}

extract_frontmatter_field() {
  local file="$1"
  local field="$2"
  grep -m1 "^${field}:" "$file" 2>/dev/null | sed "s/^${field}:[[:space:]]*//" | sed "s/^[\"']*//;s/[\"']*$//" | tr -d ' '
}

action_mark_ready() {
  pick_file_by_title "$POSTS_DIR/drafts" "Select a draft to mark as ready:" || return
  local filename
  filename=$(basename "$PICKED_FILE")
  mv "$PICKED_FILE" "$POSTS_DIR/ready/$filename"
  local ready_file="$POSTS_DIR/ready/$filename"
  echo "Moved to: $ready_file"

  # Auto-schedule if scheduledAt is set in frontmatter
  local scheduled_at
  scheduled_at=$(extract_frontmatter_field "$ready_file" "scheduledAt")

  if [ -n "$scheduled_at" ]; then
    echo ""
    gum style --foreground 208 "Scheduled publish at: $scheduled_at"
    echo "Setting up auto-publish..."

    if ! is_backend_ready; then
      gum style --foreground 1 "Backend not available. Mark as ready succeeded, but scheduling requires backend."
      gum style --foreground 245 "Run 'Schedule post' later when backend is connected."
      wait_for_enter
      return
    fi
    TAC_API_URL=$(get_resolved_api_url)

    local output
    output=$(run_cli post create --file "$ready_file" --draft true 2>&1)
    echo "$output"

    local id
    id=$(echo "$output" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)

    if [ -z "$id" ]; then
      gum style --foreground 1 "Could not extract post ID. Schedule manually with 'Schedule post'."
      wait_for_enter
      return
    fi

    run_cli post schedule "$id" --at "$scheduled_at"
    mv "$ready_file" "$POSTS_DIR/scheduled/$filename"
    echo "Moved to: $POSTS_DIR/scheduled/$filename"
  fi

  wait_for_enter
}

action_publish() {
  require_backend || return
  pick_file_by_title "$POSTS_DIR/ready" "Select a post to publish:" || return
  echo "Publishing: $PICKED_FILE"
  run_cli post create --file "$PICKED_FILE" --draft false
}

action_schedule() {
  require_backend || return
  pick_file_by_title "$POSTS_DIR/ready" "Select a post to schedule:" || return

  local when
  when=$(pick_datetime "Schedule for:")
  [ -z "$when" ] && return

  local output
  output=$(run_cli post create --file "$PICKED_FILE" --draft true 2>&1) || return
  echo "$output"

  local id
  id=$(echo "$output" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)

  if [ -z "$id" ]; then
    echo "Error: could not extract post ID from output." >&2
    return
  fi

  run_cli post schedule "$id" --at "$when"

  local filename
  filename=$(basename "$PICKED_FILE")
  mv "$PICKED_FILE" "$POSTS_DIR/scheduled/$filename"
  echo "Moved to: $POSTS_DIR/scheduled/$filename"
}

# ── Menu Building ─────────────────────────────────────────────────────────

build_menu_choices() {
  local backend_up="$1"

  if [ "$backend_up" = "true" ]; then
    echo "1. List posts"
    echo "2. New draft"
    echo "3. Edit draft"
    echo "4. Mark as ready"
    echo "5. Publish post"
    echo "6. Schedule post"
    echo "7. Exit"
  else
    echo "1. List posts  (waiting...)"
    echo "2. New draft"
    echo "3. Edit draft"
    echo "4. Mark as ready"
    echo "5. Publish post  (waiting...)"
    echo "6. Schedule post  (waiting...)"
    echo "7. Exit"
  fi
}

# ── Main Loop ──────────────────────────────────────────────────────────────

require_gum
start_health_check
build_cli

while true; do
  clear
  echo ""
  gum style \
    --border double \
    --border-foreground 208 \
    --padding "1 4" \
    --bold \
    "The Augmented Craftsman — Post Manager"
  echo ""

  # Render status bar
  render_status_bar
  gum style --foreground 245 --faint "Esc = back  •  ↑↓ = navigate  •  Enter = select"
  echo ""

  BACKEND_UP="false"
  is_backend_ready && BACKEND_UP="true"

  if [ "$BACKEND_UP" = "true" ]; then
    CHOICE=$(build_menu_choices "$BACKEND_UP" | gum choose --no-show-help --header "Select an action:") || CHOICE=""
  else
    # Auto-refresh every 3s so the status bar stays dynamic
    CHOICE=$(build_menu_choices "$BACKEND_UP" | gum choose --no-show-help --timeout 3s --header "Select an action:") || CHOICE=""
  fi

  # Strip "(waiting...)" suffix for matching
  ACTION=$(echo "$CHOICE" | sed 's/  *(waiting\.\.\.)$//')

  case "$ACTION" in
    "1. List posts")     action_list ;;
    "2. New draft")      action_new_draft ;;
    "3. Edit draft")     action_edit_draft ;;
    "4. Mark as ready")  action_mark_ready ;;
    "5. Publish post")   action_publish ;;
    "6. Schedule post")  action_schedule ;;
    "7. Exit")           break ;;
  esac
done

echo "Bye."
