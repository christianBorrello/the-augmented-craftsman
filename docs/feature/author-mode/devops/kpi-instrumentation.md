# KPI Instrumentation: Author Mode

**Feature**: author-mode
**Wave**: PLATFORM (DESIGN — infrastructure readiness)
**Date**: 2026-03-14
**Architect**: Apex (platform-architect)

---

## Observability Decision

**PD-04**: No formal observability tooling. KPIs are measured manually via Vercel logs + DB queries as documented in outcome-kpis.md. No dashboards, no automated alerting (except KPI-4 which uses Vercel's built-in Web Analytics).

This document specifies the exact manual measurement procedures for each KPI so measurement is consistent and repeatable.

---

## KPI-1: Admin Login Success Rate

**Who**: Christian (author)
**Metric**: 100% of login attempts with authorized email succeed
**Type**: Leading indicator
**Baseline**: 0% (feature does not exist)
**Target**: 100%

### Data Source

Upstash Redis — session keys created by Astro Sessions driver.

### Measurement Procedure

```
1. Go to https://console.upstash.com → tacblog-sessions database
2. Click "Data Browser"
3. Filter keys by prefix: astro-session:
4. Count keys present
   - Each key = one active admin session
   - Keys with TTL > 0 are active sessions
   - Keys without TTL (expired) are no longer visible

Alternative (CLI):
  curl "$UPSTASH_REDIS_REST_URL/keys/astro-session:*" \
    -H "Authorization: Bearer $UPSTASH_REDIS_REST_TOKEN"
  # Returns list of active session keys
```

### Interpretation

- At least 1 key present after a login attempt = success.
- Key absent after login attempt = login failure (check Vercel function logs for error detail).
- Key with `isAdmin: false` or missing = whitelist check failed (wrong email).

### Frequency

Check after every login attempt during initial validation. Weekly thereafter.

---

## KPI-2: Posts Published via Admin UI (North Star)

**Who**: Christian (author)
**Metric**: Posts published via admin UI, not via direct database/API
**Type**: North Star / Leading indicator
**Baseline**: 0 posts
**Target**: 1 post published within first week; 1 post/week for 4 consecutive weeks

### Data Source

Neon PostgreSQL — `BlogPosts` table.

### Measurement Query

```sql
-- Count posts published through the admin UI
-- (any post with status='Published' and created/updated via API after author-mode deployment)
SELECT
    COUNT(*) AS total_published,
    MIN("PublishedAt") AS first_published_at,
    MAX("PublishedAt") AS last_published_at
FROM "BlogPosts"
WHERE "Status" = 'Published'
AND "PublishedAt" >= '2026-03-14'  -- author-mode deployment date
ORDER BY "PublishedAt" DESC;

-- Weekly publishing frequency
SELECT
    DATE_TRUNC('week', "PublishedAt") AS week,
    COUNT(*) AS posts_published
FROM "BlogPosts"
WHERE "Status" = 'Published'
AND "PublishedAt" >= '2026-03-14'
GROUP BY week
ORDER BY week;
```

Note: All posts created after author-mode deployment are by definition created via admin UI (no other write path exists for the author). The filter on `PublishedAt >= deployment_date` scopes to author-mode usage only.

### How to Run

```
1. Go to https://console.neon.tech → your project → SQL Editor
2. Paste query above
3. Run
```

### Frequency

Weekly check. Log results in a note (Obsidian, Notes app, or comment in the blog itself).

---

## KPI-3: In-Place Edit Flow Usage (EditControls)

**Who**: Christian (author)
**Metric**: `admin.updatePost` Action calls with `Referer: /blog/*` header
**Type**: Leading indicator
**Baseline**: 0 (feature does not exist)
**Target**: At least 1 use per month

### Data Source

Vercel Function logs — logged when the `admin.updatePost` Astro Action is called.

### What to Log (Implementation Guidance for Software-Crafter)

In `src/actions/admin/posts.ts`, the `admin.updatePost` action must log the referer header to enable this measurement. Add structured logging:

```typescript
// In admin.updatePost action handler
const referer = context.request.headers.get('Referer') ?? 'unknown';
const isInPlace = referer.includes('/blog/');
console.log(JSON.stringify({
  event: 'admin.updatePost',
  postId: input.id,
  referer,
  isInPlace,
  timestamp: new Date().toISOString()
}));
```

This log is emitted as a Vercel Function log and is visible in the Vercel dashboard.

### Measurement Procedure

```
1. Go to https://vercel.com → your project → Logs (left sidebar)
2. Filter by: Function Logs
3. Search: admin.updatePost
4. Look for entries where isInPlace: true
5. Count occurrences per month
```

### Alternative: Vercel Log Drain (if manual becomes tedious)

If monthly log review becomes tedious after 3+ months, configure a Vercel Log Drain to a free Axiom or Logtail account. Not needed for initial validation.

### Frequency

Monthly review. Counts toward the "in-place editing is actually used" hypothesis.

---

## KPI-4: Reader Experience Guardrail (LCP on /blog/*)

**Who**: Readers
**Metric**: No measurable increase in LCP or FID on `/blog/*` pages
**Type**: Guardrail
**Baseline**: LCP < 1.5s (current, from Vercel Analytics)
**Target**: LCP remains < 1.5s after author-mode deployment

### Data Source

Vercel Web Analytics — built-in, no configuration needed.

### Measurement Procedure

```
1. Go to https://vercel.com → your project → Analytics
2. Select "Web Vitals" tab
3. Filter: URL starts with /blog/
4. Check metrics: LCP, FID (First Input Delay), CLS (Cumulative Layout Shift)
5. Compare "before author-mode" period vs "after author-mode" period
```

### Automated Alert Setup

Vercel Analytics does not offer programmatic alerting on the free tier. Manual procedure:

```
After each deployment that touches blog page rendering:
1. Wait 24 hours for real traffic to populate Web Vitals
2. Check Vercel Analytics → Web Vitals for /blog/* routes
3. If LCP > 1.5s: investigate (likely cause: Server Island cold start affecting fallback delay)
4. If LCP > 2.0s: rollback the deployment and open a performance investigation
```

### Why This Guardrail Matters

Author Mode introduces two changes that could affect `/blog/*` performance:
1. `output: 'hybrid'` — no impact on static pages (they remain SSG)
2. `EditControls` Server Island with `server:defer` — adds one async request per blog page load for the author's session check

The `server:defer` attribute means the Server Island loads asynchronously, with an empty fragment fallback. For readers, this means:
- Initial HTML loads immediately (SSG, unaffected)
- Server Island request fires after paint
- Readers see empty fragment (no toolbar) — the request completes but renders nothing

If the Server Island request is slow (Upstash cold start > 500ms), it should not affect LCP because the Island uses `server:defer` with an empty fallback. The fallback is shown immediately; the Island result is injected when ready. LCP is measured on the initial paint.

**Expected result**: KPI-4 passes with no action required.

---

## KPI Measurement Log Template

Use this format to record KPI checks over time (in a private note, not in the repo):

```
Date: YYYY-MM-DD
--
KPI-1 (Login success): [pass/fail] — [notes]
KPI-2 (Posts published): [count this week] — cumulative: [total]
KPI-3 (In-place edits): [count this month] — [notes]
KPI-4 (LCP): [p75 value] — [pass if < 1.5s]
--
```

---

## Hypothesis Validation Criteria

From `outcome-kpis.md`:

**KPI-2 hypothesis validated when**: Christian publishes at least 1 post per week for 4 consecutive weeks using the admin UI.

Measurement: run the weekly query above; check for 4 consecutive rows with `posts_published >= 1`.

**KPI-4 hypothesis validated when**: LCP on `/blog/*` remains below 1.5s after author-mode is deployed and used in production.

Measurement: check Vercel Web Vitals 7 days after the first publish via admin UI.
