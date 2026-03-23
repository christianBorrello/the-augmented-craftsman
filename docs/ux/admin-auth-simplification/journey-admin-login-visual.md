# Journey: Admin Login — Visual Map

**Feature**: admin-auth-simplification
**Persona**: Christian (sole author and administrator of The Augmented Craftsman)
**Goal**: Authenticate to the admin panel to manage blog posts
**Date**: 2026-03-15

---

## Emotional Arc

```
BEFORE          STEP 1          STEP 2          STEP 3          AFTER
Navigate to  →  See login   →  Enter creds  →  Dashboard   →  Writing posts
admin panel     form            & submit        loads           with confidence

[Purposeful]  [Familiar &   [Focused &    [Immediate    [Settled &
              Expected]      Calm]          Relief]        Productive]
```

Arc pattern: **Problem Relief** (Frustrated with old OAuth complexity → Hopeful with familiar form → Relieved and productive)

---

## Journey Flow

```
+------------------------------------------------------------------+
|  TRIGGER: Christian navigates to /admin                          |
|  Context: About to write or edit a blog post                     |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|  STEP 1: View Admin Login Page                                   |
|  URL: /admin/login (Astro page)                                  |
|  Emotion entry: Purposeful                                       |
|                                                                  |
|  +----------------------------------------------------------+    |
|  |  The Augmented Craftsman                                 |    |
|  |  ── Admin Login ──────────────────────────────────────  |    |
|  |                                                          |    |
|  |  Email                                                   |    |
|  |  ┌────────────────────────────────────────────────┐     |    |
|  |  │ christian@theaugmentedcraftsman.dev             │     |    |
|  |  └────────────────────────────────────────────────┘     |    |
|  |                                                          |    |
|  |  Password                                                |    |
|  |  ┌────────────────────────────────────────────────┐     |    |
|  |  │ ••••••••••••                                    │     |    |
|  |  └────────────────────────────────────────────────┘     |    |
|  |                                                          |    |
|  |  [        Sign In        ]                               |    |
|  |                                                          |    |
|  +----------------------------------------------------------+    |
|                                                                  |
|  Emotion exit: Familiar & Expected                               |
|  Integration: Login form renders without external OAuth deps     |
+------------------------------------------------------------------+
                              |
              +---------------+---------------+
              |                               |
              v                               v
+---------------------------+   +---------------------------+
|  STEP 2a: Valid submit    |   |  STEP 2b: Invalid submit  |
|  POST /api/auth/login     |   |  POST /api/auth/login     |
|  {email, password}        |   |  {email, bad password}    |
|  Emotion: Focused & Calm  |   |  Emotion: Momentary alert |
+---------------------------+   +---------------------------+
              |                               |
              v                               v
+---------------------------+   +---------------------------+
|  STEP 3a: JWT returned    |   |  STEP 3b: Error shown     |
|  role: "admin"            |   |  "Invalid credentials"    |
|  lifetime: 480 min        |   |  (no details — security)  |
|  stored in httpOnly cookie|   |  Attempt N of 5 counted   |
|  Emotion: Immediate Relief|   |  Emotion: Alert but guided|
+---------------------------+   +---------------------------+
              |                               |
              v                               v
+---------------------------+   +---------------------------+
|  STEP 4a: Redirect to     |   |  STEP 4b: After 5 fails   |
|  /admin/dashboard         |   |  Account locked 15 min    |
|  Full admin access        |   |  "Too many attempts.      |
|  Emotion: Settled &       |   |   Try again in 15 min."   |
|  Productive               |   |  Emotion: Mildly annoyed  |
|                           |   |  but understands why      |
+---------------------------+   +---------------------------+
```

---

## Step-by-Step Emotional Annotations

### Step 1 — View Login Page
- **Entry emotion**: Purposeful — Christian has a clear intent (write a post)
- **Exit emotion**: Familiar & Expected — the form is standard and unambiguous
- **Design principle**: No friction, no OAuth redirect, no "wait for GitHub". The form IS the login.
- **UX note**: Email field can pre-fill if browser has saved credential. The experience feels like any trusted internal tool.

### Step 2a — Submit Valid Credentials
- **Entry emotion**: Focused & Calm — Christian types credentials he knows
- **Exit emotion**: Anticipating — a brief moment while the API responds
- **Design principle**: Button shows loading state immediately (< 100ms feedback). No spinning OAuth wheels.
- **UX note**: Form submits on Enter key. No CAPTCHA required for an admin-only page behind HTTPS.

### Step 2b — Submit Invalid Credentials
- **Entry emotion**: Momentary uncertainty — typo or wrong password
- **Exit emotion**: Alert but guided — error tells what happened, not how to exploit it
- **Design principle**: Generic error message protects against enumeration. Attempt counter visible only when near lockout (4th attempt: "1 attempt remaining before lockout").

### Step 3a — Authentication Succeeds
- **Entry emotion**: Anticipating
- **Exit emotion**: Immediate Relief — no extra verification step, no nonce, no token round-trip
- **Design principle**: One step replaces the former 4-step OAuth dance. The simplicity itself communicates trust.

### Step 3b — Lockout Reached
- **Entry emotion**: Alert
- **Exit emotion**: Resigned but reassured — Christian understands brute-force protection is working
- **Design principle**: Lockout duration (15 min) is displayed explicitly. No ambiguity.

### Step 4a — Dashboard Loads
- **Entry emotion**: Immediate Relief
- **Exit emotion**: Settled & Productive — the goal (writing posts) is now reachable
- **Design principle**: Redirect is instant. No intermediate confirmation page.

---

## Error Path: Lockout Recovery

```
+------------------------------------------------------------------+
|  STEP 4b: Lockout Active                                         |
|                                                                  |
|  +----------------------------------------------------------+    |
|  |  The Augmented Craftsman — Admin Login                   |    |
|  |                                                          |    |
|  |  ⚠ Too many failed attempts.                            |    |
|  |    Account locked until 14:35 (15 minutes).             |    |
|  |                                                          |    |
|  |  [Sign In] (disabled)                                    |    |
|  |                                                          |    |
|  +----------------------------------------------------------+    |
|                                                                  |
|  Integration: FailureTracker persists state (in-memory or Redis) |
|  Emotion exit: Resigned but reassured — explicit timer shown     |
+------------------------------------------------------------------+
```

---

## Shared Artifact Crossings

| Artifact | Step Produced | Steps Consumed | Risk |
|---|---|---|---|
| `admin_jwt_token` | Step 3a (LoginHandler) | Step 4a (admin endpoints) | HIGH — must carry `role: "admin"` claim |
| `admin_email` (config) | App config | Step 2 (LoginHandler validation) | MEDIUM — must match `AdminCredentials.Email` |
| `jwt_secret` (IAdminSettings) | App config | Step 3a (token signing) + Admin endpoints (token verification) | HIGH — same secret for sign and verify |
| `lockout_state` | Step 2b/3b (FailureTracker) | Step 2 (pre-check) | LOW — in-memory, single instance |
