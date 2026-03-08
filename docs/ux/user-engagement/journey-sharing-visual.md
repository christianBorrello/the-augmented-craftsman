# Journey: Reader Shares a Post

## Persona

**Ana Ferreira** -- Backend developer. She just read a post on "Value Objects in Practice" that perfectly explains something her team has been debating. She wants to share it with her team on Slack and also post it on LinkedIn.

**Tomasz Kowalski** -- Senior developer. He wants to share a TDD article on Twitter/X from his phone during his lunch break.

---

## Goal

Reader shares a blog post URL with their network using the device's native share capabilities. No third-party SDKs, no tracking scripts, no social media buttons.

## Trigger

Reader finishes (or is partway through) an article and wants to send it to someone or post it on a social platform. They see a share button near the like button.

## Success

The post URL (and title) is shared through the reader's preferred channel -- whether that is a native share sheet on mobile, a copy-to-clipboard on desktop, or any app the device supports.

---

## Happy Path Flow -- Mobile (Web Share API)

```
  READING POST       TAP SHARE BUTTON     NATIVE SHARE SHEET     CHOOSE APP          SHARED
+---------------+  +-----------------+   +------------------+   +--------------+   +---------------+
| Tomasz reads  |  | Taps share icon |   | OS share sheet   |   | Selects      |   | Article URL   |
| "TDD Myths"   |->| next to like    |->| appears with     |->| Twitter/X    |->| + title       |
| on his phone  |  | button          |   | app list         |   | from list    |   | pre-filled    |
+---------------+  +-----------------+   +------------------+   +--------------+   +---------------+
       |                  |                      |                     |                  |
       v                  v                      v                     v                  v
  Engaged            Quick impulse          Familiar OS          Natural choice      Done, effortless
  "Want to share"    "One tap"              interface            "My apps, my way"   "Sent!"
```

## Happy Path Flow -- Desktop (Copy to Clipboard Fallback)

```
  READING POST       CLICK SHARE BUTTON    URL COPIED             PASTE ELSEWHERE
+---------------+  +-----------------+   +------------------+   +-----------------+
| Ana reads     |  | Clicks share    |   | Toast: "Link     |   | Ana pastes in   |
| "Value Objects"|->| icon next to   |->| copied!"         |->| Slack channel    |
| on desktop    |  | like button     |   | Clipboard has    |   |                 |
+---------------+  +-----------------+   | post URL         |   +-----------------+
       |                  |                      |                      |
       v                  v                      v                      v
  Engaged            Quick impulse          Confirmed              Done
  "Team needs this"  "One click"            "Got it"               "Shared!"
```

## Emotional Arc

```
Satisfaction
   5 |                                   *-----------*
   4 |                   *
   3 |  *
   2 |
   1 |
     +------------------------------------------------>
       Reading      Tap Share     Share Sheet/    Done
                                  Copy Toast
```

Sharing is the lightest journey -- three steps at most. The emotional arc is a quick uptick from "want to share" to "done." No friction, no decisions beyond choosing the target app.

---

## Step Detail

| Step | Reader Does | System Responds | Artifacts | Emotional State |
|------|-------------|-----------------|-----------|-----------------|
| Reading post | Engages with content | Share button visible alongside like button | `slug`, `post_title`, `post_url` | Engaged |
| Tap/click share | Taps share icon | **Mobile**: invoke `navigator.share()` with title + URL. **Desktop**: copy URL to clipboard. | -- | Quick impulse |
| Share sheet (mobile) | Chooses app from OS share sheet | OS handles the rest -- pre-fills title and URL | -- | Familiar, natural |
| Copy toast (desktop) | Sees confirmation | Toast: "Link copied!" disappears after 3 seconds | -- | Confirmed |
| Done | Pastes/sends in chosen app | N/A (outside our system) | -- | Satisfied |

---

## Web UI Mockup -- Share Button (Desktop)

```
+-----------------------------------------------------------------------+
|                                                                       |
|  ---                                                                 |
|                                                                       |
|  [heart-outline] 12        [share-icon] Share                        |
|                                                                       |
+-----------------------------------------------------------------------+

After clicking Share (desktop):

+-----------------------------------------------------------------------+
|                                                                       |
|  ---                                                                 |
|                                                                       |
|  [heart-outline] 12        [share-icon] Share                        |
|                                                                       |
|  +-------------------+                                               |
|  | Link copied!      |  <-- Toast notification, auto-dismiss 3s     |
|  +-------------------+                                               |
|                                                                       |
+-----------------------------------------------------------------------+
```

## Web UI Mockup -- Share Button (Mobile)

```
+-----------------------------------+
|                                   |
|  ---                             |
|                                   |
|   [heart] 12        [share]      |
|                                   |
+-----------------------------------+

Tapping [share] invokes the native OS share sheet:

+-----------------------------------+
|                                   |
|  Share "Value Objects in Practice"|
|  theaugmentedcraftsman.com/...   |
|                                   |
|  [Slack] [WhatsApp] [Twitter/X]  |
|  [LinkedIn] [Email] [Copy Link]  |
|                                   |
|  [Cancel]                        |
|                                   |
+-----------------------------------+
```

---

## Error Paths

```
Web Share API not supported (older desktop browser):
  Automatic fallback to copy-to-clipboard
  No error shown -- reader sees "Share" button, click copies URL
  Behavior is transparent

Clipboard API not supported (very old browser):
  Fallback: show a small popover with the URL as selectable text
  Reader can manually select and copy
  Message: "Copy this link:" with the URL

navigator.share() rejected by user (mobile):
  User dismissed the share sheet
  No action taken, no error shown
  Share button returns to default state

navigator.share() fails (browser error):
  Silent fallback to copy-to-clipboard
  Toast: "Link copied!" (same as desktop behavior)

JavaScript disabled:
  Share button hidden (Astro Island does not hydrate)
  Post remains fully readable
  URL is in the browser address bar -- reader can copy manually
```

---

## Cross-Device Behavior

| Scenario | Behavior | Rationale |
|----------|----------|-----------|
| Mobile (iOS Safari) | Native share sheet via Web Share API | Full OS integration |
| Mobile (Android Chrome) | Native share sheet via Web Share API | Full OS integration |
| Desktop Chrome/Firefox | Copy to clipboard + toast | Web Share API limited on desktop |
| Desktop Safari (macOS) | Web Share API available (native share sheet) | macOS supports it |
| Desktop with Web Share API | Use Web Share API (preferred over clipboard) | Better UX when available |

## Feature Detection Logic

```
if (navigator.share) {
  // Use Web Share API (mobile, macOS Safari)
} else if (navigator.clipboard) {
  // Copy to clipboard + toast
} else {
  // Show URL in selectable text popover
}
```

---

## Shared Artifacts

| Artifact | Source | Consumers |
|----------|--------|-----------|
| `post_title` | Page metadata / API response | `navigator.share({ title })` |
| `post_url` | `window.location.href` or constructed from slug | `navigator.share({ url })`, clipboard |
| `slug` | Post URL path | URL construction |

---

## Astro Island Notes

The share button is a small Astro Island that hydrates on visible. It detects browser capabilities and renders the appropriate behavior.

```
[Static HTML page]
  |
  +-- [Astro Island: ShareButton]
        - client:visible
        - props: { title, url }
        - detects: navigator.share support
        - fallback: navigator.clipboard.writeText
        - last resort: selectable URL popover
```

---

## SEO / Open Graph Considerations

When a post URL is shared on social media, the platform will scrape Open Graph meta tags. These must be present on every post page:

```html
<meta property="og:title" content="Value Objects in Practice" />
<meta property="og:description" content="How wrapping primitives in..." />
<meta property="og:url" content="https://theaugmentedcraftsman.com/posts/value-objects-in-practice" />
<meta property="og:image" content="https://ik.imagekit.io/.../featured.jpg" />
<meta property="og:type" content="article" />
<meta name="twitter:card" content="summary_large_image" />
```

This is already partially covered by NFR-S02 (Meta tags) from v1 requirements. The sharing feature makes these tags actively important rather than just SEO hygiene.
