# Definition of Ready Checklist -- Epic 5: User Engagement

All 7 user stories validated against the 8-item DoR hard gate.

---

## US-050: Like a Blog Post

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Ana Ferreira finds it impossible to express appreciation for a post she enjoyed. There is no feedback mechanism." Domain language used. |
| User/persona identified | PASS | Ana Ferreira, backend developer, reads on phone during commute and desktop at work. |
| 3+ domain examples | PASS | 3 examples: happy path (Ana likes "TDD Is Not About Testing"), return visit (like persists), edge case (localStorage cleared). All with real post titles and data. |
| UAT scenarios (3-7) | PASS | 5 scenarios in Given/When/Then. Covers happy path, persistence, deduplication, optimistic UI, JS disabled. |
| AC derived from UAT | PASS | 9 functional + 8 non-functional criteria, each traceable to scenarios. |
| Right-sized | PASS | Size M (1-2 days), 5 scenarios, single demo-able feature (tap heart, see count). |
| Technical notes | PASS | Astro Island hydration, API endpoints, localStorage strategy, rate limiting documented. |
| Dependencies tracked | PASS | US-043 (post view). Post view already exists in Epic 4. |

### DoR Status: PASSED

---

## US-051: Sign In with Social Login to Comment

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Tomasz wants to ask a clarifying question. He will not create a custom account for a blog." |
| User/persona identified | PASS | Tomasz Kowalski (senior dev, GitHub user) and Maria Santos (junior dev, Google user). Two distinct personas. |
| 3+ domain examples | PASS | 4 examples: GitHub sign-in, Google sign-in, OAuth denied, provider error. Real names and providers. |
| UAT scenarios (3-7) | PASS | 5 scenarios covering both providers, denial, error, and session persistence. |
| AC derived from UAT | PASS | 9 functional + 5 non-functional criteria traceable to scenarios. |
| Right-sized | PASS | Size L (2-3 days), 5 scenarios. OAuth integration is inherently complex but scoped to sign-in only. |
| Technical notes | PASS | Provider setup (Google Cloud, GitHub Developer), credential storage, server-side exchange, session strategy. |
| Dependencies tracked | PASS | OAuth provider accounts (external, free). No blocking code dependencies. |

### DoR Status: PASSED

---

## US-052: Post a Comment on a Blog Post

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Currently there is no way for readers to engage in discussion or provide feedback. The blog is a one-way channel." |
| User/persona identified | PASS | Tomasz Kowalski (counterpoint) and Maria Santos (thanks). Different motivations for same feature. |
| 3+ domain examples | PASS | 4 examples: happy path (post comment), character limit, network failure, session expired. Real data. |
| UAT scenarios (3-7) | PASS | 5 scenarios covering post, empty validation, character limit, network failure, session expiry. |
| AC derived from UAT | PASS | 9 functional + 6 non-functional criteria. |
| Right-sized | PASS | Size M (1-2 days), 5 scenarios. CRUD-level complexity (form + API + display). |
| Technical notes | PASS | API endpoints, comment entity shape, rate limiting, CSRF protection. |
| Dependencies tracked | PASS | US-051 (OAuth), US-043 (post view). Both tracked. |

### DoR Status: PASSED

---

## US-053: Author Moderates Comments

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Christian expects that occasionally spam or inappropriate comments will appear. He needs a way to remove such comments." |
| User/persona identified | PASS | Christian, blog author and single admin. Moderates at own pace. |
| 3+ domain examples | PASS | 3 examples: delete spam, keep valid comment, already-deleted comment. |
| UAT scenarios (3-7) | PASS | 3 scenarios. Lean but sufficient for a delete-only moderation flow. |
| AC derived from UAT | PASS | 7 functional + 2 non-functional criteria. |
| Right-sized | PASS | Size S (< 1 day), 3 scenarios. Simple CRUD delete with confirmation. |
| Technical notes | PASS | DELETE endpoint, admin JWT requirement, extends existing admin panel. |
| Dependencies tracked | PASS | US-052 (comments exist), US-010 (admin auth from Epic 1). |

### DoR Status: PASSED

---

## US-054: Share a Blog Post

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Ana wants to send a post to her team on Slack. Currently she has to manually copy the URL from the address bar." |
| User/persona identified | PASS | Ana Ferreira (desktop, Slack) and Tomasz Kowalski (mobile, Twitter/X). |
| 3+ domain examples | PASS | 3 examples: mobile share sheet, desktop clipboard, old browser fallback. |
| UAT scenarios (3-7) | PASS | 4 scenarios covering Web Share API, clipboard, fallback, JS disabled. |
| AC derived from UAT | PASS | 6 functional + 7 non-functional criteria. |
| Right-sized | PASS | Size S (< 1 day), 4 scenarios. Frontend-only feature, no API. |
| Technical notes | PASS | Feature detection chain, Astro Island, no backend component. |
| Dependencies tracked | PASS | US-043 (post view), US-055 (OG tags). |

### DoR Status: PASSED

---

## US-055: Open Graph Meta Tags for Social Sharing

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Without proper Open Graph tags, the preview shows a generic title and no image -- making the shared link look unprofessional." |
| User/persona identified | PASS | Blog reader who shared a post. Indirectly serves all sharing readers. |
| 3+ domain examples | PASS | 3 examples: post with featured image, post without featured image, canonical URL match. |
| UAT scenarios (3-7) | PASS | 3 scenarios. Focused on OG tag correctness. |
| AC derived from UAT | PASS | 7 functional + 3 non-functional criteria. |
| Right-sized | PASS | Size S (< 1 day), 3 scenarios. Astro template change only. |
| Technical notes | PASS | Astro layout template, static asset for default image, extends NFR-S02. |
| Dependencies tracked | PASS | US-043 (post view), US-031 (featured image for og:image source). |

### DoR Status: PASSED

---

## US-056: View Comments on a Post

| DoR Item | Status | Evidence |
|----------|--------|---------|
| Problem statement clear | PASS | "Maria wants to know if other readers found it useful or had questions. Currently there is no way to see community feedback." |
| User/persona identified | PASS | Maria Santos, junior developer viewing a post. |
| 3+ domain examples | PASS | 3 examples: post with comments, empty state, many comments. |
| UAT scenarios (3-7) | PASS | 2 scenarios. At the lower bound but the feature is a read-only display with a clear empty state. |
| AC derived from UAT | PASS | 5 functional + 4 non-functional criteria. |
| Right-sized | PASS | Size S (< 1 day), 2 scenarios. Read-only API fetch + display. |
| Technical notes | PASS | API endpoint, Astro Island, semantic HTML, no pagination in v1. |
| Dependencies tracked | PASS | US-043 (post view), US-052 (comments exist to display). |

### DoR Status: PASSED

---

## Summary

| Story | DoR Status | Blocking Issues |
|-------|------------|-----------------|
| US-050: Like a Blog Post | PASSED | None |
| US-051: OAuth Sign-In | PASSED | None |
| US-052: Post a Comment | PASSED | None |
| US-053: Moderate Comments | PASSED | None |
| US-054: Share a Post | PASSED | None |
| US-055: OG Meta Tags | PASSED | None |
| US-056: View Comments | PASSED | None |

All 7 stories pass the 8-item Definition of Ready hard gate. Ready for handoff to DESIGN wave.

---

## Risk Assessment

| Risk | Probability | Impact | Category | Mitigation |
|------|-------------|--------|----------|------------|
| OAuth provider changes consent screen flow | Low | Medium | Technical | Standard OAuth 2.0 flow; well-documented by both providers |
| Like count inflation via script | Medium | Low | Security | Rate limiting + visitor_id deduplication. Accepted trade-off for anonymous likes. |
| Comment spam | Medium | Low | Business | Post-moderation by author. Social login raises barrier vs anonymous. |
| Web Share API browser support gaps | Low | Low | Technical | Three-tier fallback chain (Web Share -> Clipboard -> Selectable URL) |
| Astro Island hydration performance | Low | Medium | Performance | `client:visible` lazy loading. Islands are small and focused. |
| Session cookie security | Medium | High | Security | httpOnly + Secure + SameSite=Lax. Server-side session store. |
| GDPR / privacy concerns with OAuth data | Low | Medium | Compliance | Minimal data stored (no email). Display name and avatar only. |
