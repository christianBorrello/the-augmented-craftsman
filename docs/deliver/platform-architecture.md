# Platform Architecture -- The Augmented Craftsman v1

**Scope**: Environment topology, service dependencies, deployment targets, and platform constraints.

---

## 1. Environment Topology

### Production

```
                    [Author Browser]                    [Reader Browser]
                          |                                   |
                     (HTTPS/JSON)                        (HTTPS/HTML)
                          |                                   |
                          v                                   v
                 +------------------+                +------------------+
                 | .NET 10 API      |                | Astro Frontend   |
                 | Fly.io           |                | Vercel CDN       |
                 | (Docker)         |                | (Static HTML)    |
                 | Single machine   |                | Edge-distributed |
                 +------------------+                +------------------+
                    |         |                             |
                    v         v                        (build-time
               +--------+ +--------+                    fetch only)
               | PgSQL  | |Image- |                        |
               | Neon    | |Kit    |                        |
               |Serverless| | SaaS  |                        |
               +--------+ +--------+                        |
                    ^                                        |
                    +---- GET /api/posts (at build) ---------+
```

### Development (Local)

```
Developer Machine
  |
  +-- .NET 10 API (dotnet run, port 5000)
  |     |
  |     +-- PostgreSQL (Docker via Testcontainers)
  |
  +-- Astro dev server (npm run dev, port 4321)
        |
        +-- Fetches from localhost:5000
```

### CI (GitHub Actions)

```
GitHub Actions Runner (ubuntu-latest)
  |
  +-- .NET 10 SDK
  +-- Docker (for Testcontainers)
  +-- Node.js 20+ (for Astro)
```

---

## 2. Service Inventory

| Service | Host | Tier | Purpose | SLA Target |
|---------|------|------|---------|------------|
| .NET 10 API | Fly.io | Free (3 shared VMs, 256MB) | Blog management API, admin CRUD, public read endpoints | Best-effort (personal blog) |
| PostgreSQL 16 | Neon | Free (0.5GB storage, serverless) | Primary data store for posts, tags, post-tag associations | Managed serverless PostgreSQL |
| Astro Frontend | Vercel | Free | Static HTML served via edge CDN, zero JS content pages | 99.9% (Vercel SLA) |
| ImageKit | ImageKit | Free tier | Image upload, storage, and CDN delivery | Managed SaaS |
| GitHub Actions | GitHub | Free (2000 min/mo) | CI/CD pipeline, automated testing, deployment triggers | Managed SaaS |

---

## 3. Service Dependencies

```
.NET API
  --> PostgreSQL (required, fail-fast if unavailable)
  --> ImageKit (required for image upload only, other features work without it)
  --> Vercel Deploy Hook (fire-and-forget on publish, non-blocking)

Astro Frontend
  --> .NET API (build-time only, build fails if API unreachable)
  --> Vercel CDN (runtime, serves static files)

GitHub Actions
  --> Docker Hub (pull postgres:16-alpine for Testcontainers)
  --> GitHub Container Registry (cache Docker layers)
  --> Fly.io API (deployment trigger via flyctl)
  --> Vercel API (deployment trigger)
```

### Dependency Criticality

| Dependency | Impact if Down | Mitigation |
|------------|---------------|------------|
| PostgreSQL (Neon) | API returns 500 for all data operations | Health check endpoint detects, Fly.io auto-restarts |
| ImageKit | Image upload fails (503), all other features work | Error message: "Upload failed. Try again." |
| Vercel Deploy Hook | Published content not visible to readers until next deploy | Manual redeploy via Vercel dashboard |
| GitHub Actions | No CI/CD, manual deploy still possible | Deploy via `flyctl deploy` as fallback |

---

## 4. Environment Configuration

### Environment Variable Contract

All variables are **required**. The API fails fast at startup with a clear error message if any are missing. No silent fallbacks.

**Backend (.NET API) -- Production:**

| Variable | Example | Source |
|----------|---------|--------|
| `ConnectionStrings__DefaultConnection` | `Host=...;Database=tacblog;...;Ssl Mode=Require` | Neon connection string (from Neon dashboard) |
| `Jwt__Secret` | `<64+ character random string>` | Fly.io secret (`fly secrets set`) |
| `Jwt__Issuer` | `https://api.theaugmentedcraftsman.christianborrello.dev` | Fly.io secret |
| `Jwt__Audience` | `https://api.theaugmentedcraftsman.christianborrello.dev` | Fly.io secret |
| `ImageKit__UrlEndpoint` | `https://ik.imagekit.io/augmented` | Fly.io secret |
| `ImageKit__PublicKey` | `public_xxx` | Fly.io secret |
| `ImageKit__PrivateKey` | `<secret>` | Fly.io secret |
| `Admin__Email` | `christian.borrello@live.it` | Fly.io secret |
| `Admin__PasswordHash` | `<ASP.NET Identity hash>` | Fly.io secret |
| `VercelDeployHookUrl` | `https://api.vercel.com/v1/integrations/deploy/...` | Fly.io secret |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Fly.io secret |

**Frontend (Astro) -- Build-time:**

| Variable | Example | Source |
|----------|---------|--------|
| `PUBLIC_API_URL` | `https://api.theaugmentedcraftsman.christianborrello.dev` | Vercel environment variable |

### Startup Validation

```
Application Start
  |
  +-- Check ConnectionStrings__DefaultConnection --> missing? HALT with error
  +-- Check Jwt__Secret --> missing? HALT with error
  +-- Check ImageKit__* --> missing? HALT with error
  +-- Check Admin__Email, Admin__PasswordHash --> missing? HALT with error
  |
  +-- Apply EF Core migrations
  |     |
  |     +-- Migration fails? HALT with error (do not serve traffic)
  |
  +-- Start accepting HTTP requests
```

---

## 5. Network Topology

### DNS and Routing

| Domain | Target | Provider |
|--------|--------|----------|
| `theaugmentedcraftsman.christianborrello.dev` | Vercel (Astro frontend) | DNS provider |
| `api.theaugmentedcraftsman.christianborrello.dev` | Fly.io (.NET API) | DNS provider |

### CORS Configuration

The .NET API allows CORS from:
- `https://theaugmentedcraftsman.christianborrello.dev` (production frontend)
- `http://localhost:4321` (Astro dev server, development only)

### TLS

- Fly.io provides automatic HTTPS for custom domains via Let's Encrypt
- Vercel provides automatic HTTPS for custom domains
- All inter-service communication is HTTPS in production

---

## 6. Data Flow

### Write Path (Author publishes a post)

```
1. Author --> POST /api/auth/login --> JWT token
2. Author --> POST /api/posts (JWT) --> Draft created in PostgreSQL
3. Author --> POST /api/posts/{id}/publish (JWT) --> Status: Published
4. API --> POST Vercel Deploy Hook (fire-and-forget)
5. Vercel --> GET /api/posts (build-time) --> Fetch all published posts
6. Vercel --> Build static HTML --> Deploy to CDN (30-90 seconds)
7. Reader --> HTTPS --> Vercel CDN --> Static HTML
```

### Read Path (Reader views a post)

```
1. Reader --> HTTPS --> Vercel CDN edge node
2. Edge node --> Static HTML (pre-built, no API call)
3. Response: HTML + CSS (zero JavaScript)
```

---

## 7. Scaling Characteristics

This is a personal blog. Infrastructure is deliberately minimal.

| Dimension | Current | When to Scale |
|-----------|---------|---------------|
| API instances | 1 (Fly.io single machine) | Not needed (admin-only write traffic) |
| Database | Single managed instance | Not needed (minimal data volume) |
| Frontend | Vercel edge CDN (global) | Already scaled (static files on CDN) |
| Build frequency | On publish (webhook) | Not needed (infrequent publishes) |

### Rejected Alternatives (Simplest Infrastructure First)

1. **Kubernetes**: Rejected. Single container, minimal traffic. K8s adds operational overhead with zero benefit.
2. **Multi-region database**: Rejected. Single author, single region sufficient. Readers served by Vercel CDN globally.
3. **API Gateway / Load Balancer**: Rejected. Single machine, Fly.io provides built-in routing via Fly Proxy.
4. **Redis cache**: Rejected. Static site means zero runtime API calls from readers. No cache needed.

---

## 8. Platform ADRs

### ADR-P001: Fly.io + Neon over Railway for Backend Hosting

**Status**: Accepted

**Context**: Need a Docker container host with managed PostgreSQL for a solo developer's blog API. Budget target: $0/month.

**Decision**: Fly.io free tier for API hosting + Neon free tier for serverless PostgreSQL.

**Rationale**:
- $0/month total (vs Railway Hobby at $5/month)
- Fly.io free tier: 3 shared-cpu-1x VMs with 256MB RAM each
- Neon free tier: serverless PostgreSQL with 0.5GB storage, always available, no expiration
- Cold starts acceptable: only the admin uses the API directly; readers see static HTML on Vercel
- `flyctl` CLI for deployment and emergency manual deploys
- Fly.io logs via `fly logs` CLI command

**Trade-offs**:
- Cold starts after inactivity (~5-10s wake-up): acceptable for admin-only API usage
- Neon serverless connection has cold start on first query after idle: acceptable for low-traffic blog
- Fly.io requires `fly.toml` config file and `flyctl` CLI (slightly more setup than Railway push-to-deploy)

**Rejected**: Railway -- excellent developer experience but $5/month minimum, unjustified for near-zero traffic personal blog.

### ADR-P002: Recreate Deployment Strategy

**Status**: Accepted

**Context**: Need a deployment strategy for the .NET API. Blog has a single admin user and readers see static HTML.

**Decision**: Recreate (stop old, start new). Brief downtime acceptable.

**Rationale**:
- Single machine -- no zero-downtime requirement
- Admin is the only user affected by API downtime
- Readers see static HTML from Vercel (unaffected by API deploys)
- EF Core migrations at startup assume single instance
- Blue-green or canary adds complexity with no audience to justify it

**Rejected alternatives**:
1. Blue-green: Requires two Fly.io machines, uses free tier quota, no benefit for single-user admin
2. Canary: Requires traffic splitting, meaningless with 1 user
3. Rolling: Requires multiple instances, not applicable to single container

### ADR-P003: EF Core Migrations at Startup

**Status**: Accepted

**Context**: Need to apply database schema changes during deployment.

**Decision**: Apply EF Core migrations automatically at application startup.

**Rationale**:
- Single instance means no race condition between multiple migration runners
- Simplifies deployment pipeline (no separate migration step)
- Startup fails fast if migration fails (container restarts, Fly.io detects unhealthy)
- Acceptable for a personal blog with brief downtime tolerance

**Risk**: If a migration is destructive and fails midway, manual intervention is needed. Mitigated by testing migrations against Testcontainers in CI before deploying.
