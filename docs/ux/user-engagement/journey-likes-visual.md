# Journey: Reader Likes a Post

## Persona

**Ana Ferreira** -- Backend developer, 3 years experience, reads the blog on her phone during commute and on desktop at work. Interested in TDD and Clean Architecture. She wants to signal appreciation without any friction -- no accounts, no forms, no popups.

---

## Goal

Reader expresses appreciation for a post with a single tap/click, no login required.

## Trigger

Ana finishes reading a post and feels it was valuable. She notices a like button (heart icon with count) near the title or at the article footer.

## Success

The like is registered, the count increments, and the button visually confirms her action. On return visits, the button shows she already liked this post.

---

## Happy Path Flow

```
  READING POST          NOTICE LIKE BUTTON      TAP/CLICK LIKE        VISUAL CONFIRMATION     RETURN VISIT
+----------------+    +------------------+    +------------------+    +------------------+    +------------------+
| Ana reads      |    | Sees heart icon  |    | Taps heart       |    | Heart fills      |    | Ana returns to   |
| "TDD Is Not    |--->| with count: 12   |--->| (single tap)     |--->| Count: 12 -> 13  |--->| same post later  |
| About Testing" |    | at article end   |    |                  |    | Brief animation  |    | Heart is filled  |
|                |    |                  |    |                  |    |                  |    | Count: 13        |
+----------------+    +------------------+    +------------------+    +------------------+    +------------------+
       |                      |                      |                      |                      |
       v                      v                      v                      v                      v
  Engaged &              Curious,               Instant,               Satisfied,            Recognized,
  appreciative           low barrier            effortless             warm feedback         "my like stuck"
  "Good post"            "I can react"          "One tap"              "It counted"          "It remembers me"
```

## Emotional Arc

```
Satisfaction
   5 |                                           *-----------*
   4 |                        *                                          *
   3 |  *
   2 |
   1 |
     +------------------------------------------------------------------>
       Reading     Notice       Tap Like     Confirmation    Return Visit
```

The arc is brief and lightweight -- matching the weight of the interaction. No anxiety, no decision complexity. A small moment of delight when the heart fills and the count increments.

---

## Step Detail

| Step | Ana Does | System Responds | Artifacts | Emotional State |
|------|----------|-----------------|-----------|-----------------|
| Reading post | Scrolls to end of article | Like button visible below content or near title | `current_post`, `slug` | Engaged |
| Notice like button | Sees heart icon + count | Heart is outlined (not filled) if Ana has not liked | `like_count`, `visitor_id` (from cookie) | Curious, low barrier |
| Tap/click like | Single tap on heart icon | POST to API, increment count, fill heart, brief scale animation | `like_count` (incremented), `visitor_liked` cookie | Instant gratification |
| Visual confirmation | Sees filled heart + new count | Filled heart persists, count updated | `visitor_liked` cookie set | Satisfied |
| Return visit | Opens same post again | Heart is pre-filled, count reflects total | `visitor_liked` cookie read | Recognized |

---

## Web UI Mockup -- Post Footer (Desktop)

```
+-----------------------------------------------------------------------+
|                                                                       |
|  "TDD Is Not About Testing"                                          |
|  Published March 5, 2026 · Tags: TDD, Testing                       |
|                                                                       |
|  [... article content ...]                                           |
|                                                                       |
|  ---                                                                 |
|                                                                       |
|  [heart-outline] 12        [share-icon] Share                        |
|                                                                       |
+-----------------------------------------------------------------------+

After liking:

+-----------------------------------------------------------------------+
|                                                                       |
|  ---                                                                 |
|                                                                       |
|  [heart-filled] 13         [share-icon] Share                        |
|                                                                       |
+-----------------------------------------------------------------------+
```

## Web UI Mockup -- Post Footer (Mobile)

```
+-----------------------------------+
|                                   |
|  [... article content ...]       |
|                                   |
|  ---                             |
|                                   |
|   [heart-outline] 12   [share]   |
|                                   |
+-----------------------------------+
```

Touch target: minimum 44x44px for the heart icon.

---

## Error Paths

```
Network error on like:
  Heart fills optimistically (instant feedback)
  API call fails silently
  On next page load, heart state reconciles with server
  If API never received the like, heart reverts to outline
  No error message shown (like is low-stakes)

Cookie/localStorage cleared:
  Heart appears as outline (not filled) even if previously liked
  Ana can like again (acceptable trade-off per brainstorm decision)
  Count may increment by 1 (one extra like, not a real problem)

API rate limiting:
  If same visitor_id sends >5 likes in 10 seconds, ignore silently
  No error shown to user
  Prevents script-based like inflation

JavaScript disabled:
  Like button hidden via Astro Island hydration
  Post remains fully readable without it
  No broken UI elements
```

---

## Cross-Device Behavior

| Scenario | Behavior | Rationale |
|----------|----------|-----------|
| Desktop Chrome, then mobile Safari | Like not carried over (different cookies) | Anonymous identity is per-device |
| Same browser after clearing cookies | Can like again | Acceptable trade-off for zero-auth simplicity |
| Same browser, same device, weeks later | Like persists (cookie/localStorage) | Persistent local storage |
| Private/incognito mode | Like works for session, not persisted | Expected browser behavior |

---

## Shared Artifacts

| Artifact | Source | Consumers |
|----------|--------|-----------|
| `visitor_id` | Generated client-side, stored in localStorage | Like API (deduplication) |
| `visitor_liked_{slug}` | Set after successful like, stored in localStorage | Like button state on page load |
| `like_count` | API response `GET /api/posts/{slug}/likes` | Like button display |
| `slug` | Post URL path | Like API endpoint |

---

## Astro Island Notes

The like button is an interactive Astro Island (requires client-side JS). The rest of the post page remains static HTML. The island hydrates on visible (lazy) to avoid blocking page load.

```
[Static HTML page]
  |
  +-- [Astro Island: LikeButton]
  |     - client:visible
  |     - props: { slug, initialCount }
  |     - reads: localStorage for visitor_liked_{slug}
  |     - calls: POST /api/posts/{slug}/likes
  |
  +-- [Astro Island: ShareButton]
        - client:visible
        - props: { title, url }
```
