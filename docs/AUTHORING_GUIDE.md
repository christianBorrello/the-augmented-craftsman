# Authoring Guide

How to write, schedule, and publish posts on The Augmented Craftsman.

## The Pipeline at a Glance

A post moves through four directories before going live:

```
docs/posts/drafts/     You're writing it
       |
docs/posts/ready/      Written, reviewed, ready to go
       |
docs/posts/scheduled/  Uploaded to the backend, waiting for its publish date
       |
docs/posts/published/  Live on the blog
```

Each transition happens through the **GUM UI** (`./scripts/tac-ui.sh`), a terminal interface built on [charmbracelet/gum](https://github.com/charmbracelet/gum).

---

## Step 1: Write a Draft

There are two ways to start a post.

### Option A: The `/blog-post-writer` Skill (recommended)

In a Claude Code session inside this repo, run:

```
/blog-post-writer
```

The skill will ask 4-6 questions about what you experienced, learned, or built. It uses your answers to write a draft that sounds like you — no invented anecdotes, no teaching tone, no motivational language. Just honest sharing.

The output is a markdown file saved to `docs/posts/drafts/` with frontmatter:

```yaml
---
title: "Your Post Title"
tags: ["Tag1", "Tag2"]
scheduledAt:
postId:
---
```

`scheduledAt` and `postId` are filled in later when you schedule the post.

### Option B: The GUM UI

Run `./scripts/tac-ui.sh` and select **"2. New draft"**:

1. Enter a title
2. Add tags (comma-separated, optional)
3. Pick a publish date and time (optional — you can set this later)
4. The editor opens in your browser with a post template

The template has Italian section headers as writing prompts (Il problema, La soluzione, Conclusione). Replace them with your content.

---

## Step 2: Edit in the Browser Editor

When you create or edit a draft, the GUM UI launches a local editor server at `http://127.0.0.1:3456`. It opens automatically in your browser.

The editor gives you:

- **Split view**: markdown source on the left, rendered preview on the right
- **Live preview**: updates as you type, with syntax highlighting (Shiki)
- **Word count and reading time**: displayed in the toolbar
- **Table of contents**: auto-generated from `##` and `###` headings
- **Dark/light theme toggle**

Files are saved directly to disk. The editor watches for external changes too — if you edit the file in your text editor, the browser preview updates automatically.

To edit an existing draft, select **"3. Edit draft"** in the GUM UI.

---

## Step 3: Mark as Ready

When you're happy with the draft, select **"4. Mark as ready"** in the GUM UI.

This moves the file from `drafts/` to `ready/`.

If the draft has a `scheduledAt` date in its frontmatter, the GUM UI will automatically:

1. Upload the post to the backend as a draft
2. Generate a GitHub Actions workflow to publish it at the scheduled time
3. Move the file to `scheduled/`

If there's no `scheduledAt`, the file stays in `ready/` until you publish or schedule it manually.

---

## Step 4: Schedule or Publish

From the GUM UI main menu:

### Publish immediately (option 5)

Select **"5. Publish post"** and choose a post from `ready/`. The post goes live on the next frontend rebuild.

### Schedule for later (option 6)

Select **"6. Schedule post"**, choose a post from `ready/`, and pick a date and time. The GUM UI will:

1. Upload the post to the backend as a draft
2. Call `tac post schedule <id> --at <datetime>` to generate a GitHub Actions workflow
3. Move the file to `scheduled/`

---

## How Scheduled Publishing Works

Scheduling is a two-phase process that separates "publish" from "visibility."

### Phase 1: Midnight — the workflow publishes the post

The CLI generates a GitHub Actions workflow file in `.github/workflows/` with a cron trigger set to **midnight UTC** on the publish date. The cron delay (which can be 0-60 minutes) doesn't matter because the post isn't visible yet.

The workflow:
1. Validates that the `TAC_API_KEY` secret exists
2. Wakes up the backend (Koyeb cold start can take up to 60s)
3. Calls `POST /api/posts/{id}/publish?publishAt=<scheduled-time>`

The `publishAt` parameter sets the post's publication timestamp to your chosen time (e.g., 09:00 UTC), not the actual time the workflow runs.

### Phase 2: 09:00 UTC — the frontend rebuilds

An external cron service (cron-job.org) calls a Vercel Deploy Hook every day at 09:00 UTC. This triggers a frontend rebuild. During the build, the Astro SSG fetches all posts from the API — but the API only returns posts where `publishAt <= now`. Posts scheduled for the future are filtered out.

So a post published at midnight with `publishAt = 09:00` only appears on the blog when the 09:00 rebuild happens.

### What if I push code that triggers a CI rebuild before 09:00?

The post won't appear early. The API filters by `publishAt <= now` at query time — if the rebuild happens at 23:00 and the post is scheduled for 09:00 tomorrow, it's excluded from the build output.

---

## Post Frontmatter Reference

```yaml
---
title: "Post Title"              # Required. Max 200 characters.
tags: ["TDD", "Clean Code"]      # Optional. Created automatically if new.
scheduledAt: 2026-04-01T09:00:00Z  # Optional. ISO 8601, UTC.
postId: <guid>                   # Filled by the CLI when the post is uploaded.
---
```

---

## File Naming

Posts follow the convention `YYYY-MM-DD-slug-title.md` or just `slug-title.md`. The slug is auto-generated from the title (lowercased, hyphens, no special characters).

---

## Useful Commands

### GUM UI

```bash
./scripts/tac-ui.sh
```

Requires `gum` installed (`brew install gum`). The UI auto-connects to the Koyeb backend, falling back to a local backend if Koyeb is unavailable.

### CLI (direct)

```bash
# List all posts
dotnet run --project backend/src/TacBlog.Cli -- \
  --url <api-url> --key <api-key> post list

# Publish a draft immediately
dotnet run --project backend/src/TacBlog.Cli -- \
  --url <api-url> --key <api-key> post publish <post-id>

# Schedule a post
dotnet run --project backend/src/TacBlog.Cli -- \
  --url <api-url> --key <api-key> post schedule <post-id> --at "2026-04-01T09:00:00Z"
```

### Re-run a failed publish workflow

```bash
gh workflow run "publish-<slug>-<date>.yml"
```

### Trigger a frontend rebuild manually

```bash
gh workflow run ci.yml
```

---

## When Things Go Wrong

### The publish workflow failed

Check the GitHub Actions tab. Common causes:

- **`TAC_API_KEY` is empty**: the secret doesn't exist or is environment-scoped. Fix with `gh secret set TAC_API_KEY --body "<key>"`. The workflow creates a GitHub Issue automatically on failure.
- **Backend didn't wake up**: Koyeb cold start exceeded the 60s retry window. Re-run the workflow — it has `workflow_dispatch` as a manual trigger.

### Post is published but not visible on the blog

The frontend is a static site (SSG). It only reflects what was live at build time. Either:

- Wait for the daily 09:00 UTC rebuild (Vercel Deploy Hook via cron-job.org)
- Trigger a rebuild manually: `gh workflow run ci.yml`

### Post shows the wrong publication date

The `publishAt` parameter controls the displayed date. If a post was published without `publishAt` (e.g., via the old workflow), it uses `clock.UtcNow` at the time of the API call.
