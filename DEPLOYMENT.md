# Deployment Guide

Production stack: Koyeb (backend) + Vercel (frontend) + Neon (PostgreSQL) + ImageKit (images).
Target: $0/month on free tiers.

## One-Time Setup

### 1. Neon Database

1. Create account at neon.tech
2. Create project "tacblog" in EU region (AWS Frankfurt)
3. Copy the connection string (format: `Host=...;Database=tacblog;Username=...;Password=...;Ssl Mode=Require`)

### 2. ImageKit Account

1. Create account at imagekit.io
2. Note: URL Endpoint, Public Key, Private Key from dashboard

### 3. Koyeb Backend

1. Create account at koyeb.com
2. Create a new **Web Service**:
   - Source: **Docker** → image `ghcr.io/<owner>/tacblog-api:latest`
   - Instance type: **Free** (nano, 256MB RAM)
   - Region: **Frankfurt** (fra)
   - Port: **8080**
3. Set environment variables:
   ```
   # Database
   ConnectionStrings__DefaultConnection=Host=<host>;Database=neondb;Username=<user>;Password=<password>;Ssl Mode=Require;Channel Binding=Require

   # Auth
   Jwt__Secret=<generated-jwt-secret>
   Jwt__Issuer=https://api.theaugmentedcraftsman.christianborrello.dev
   AdminCredentials__Email=christian.borrello@live.it
   AdminCredentials__HashedPassword=<generated-hash>

   # OAuth — GitHub app credentials (required in production, startup throws without them)
   OAuth__GitHub__ClientId=<github-oauth-app-client-id>
   OAuth__GitHub__ClientSecret=<github-oauth-app-client-secret>

   # ImageKit
   ImageKit__UrlEndpoint=https://ik.imagekit.io/cbdev
   ImageKit__PublicKey=<public-key>
   ImageKit__PrivateKey=<private-key>
   ```

   > **Note**: `ASPNETCORE_ENVIRONMENT` defaults to `Production` in .NET when unset — no need to set it explicitly, but confirm `IsDevelopment()` never returns true in production containers.
4. Configure health check: HTTP GET `/health` on port 8080
5. Note the Koyeb service URL (e.g., `tacblog-api-<hash>.koyeb.app`)
6. Add custom domain: `api.theaugmentedcraftsman.christianborrello.dev`
7. Note the CNAME target Koyeb provides for DNS configuration

**Auto-deploy**: Koyeb watches `ghcr.io/<owner>/tacblog-api:latest` — when CI pushes a new `:latest` tag, Koyeb automatically redeploys.

### 4. Vercel Frontend

1. Import repository at vercel.com
2. Set root directory to `frontend`
3. Set environment variable: `PUBLIC_API_URL` = `https://api.theaugmentedcraftsman.christianborrello.dev`
4. Set up custom domain: `theaugmentedcraftsman.christianborrello.dev`

### 5. DNS Configuration

| Record | Name | Value |
|--------|------|-------|
| CNAME | `api.theaugmentedcraftsman` | `<cname-target-from-koyeb>` |
| CNAME | `theaugmentedcraftsman` | `cname.vercel-dns.com` |

### 6. GitHub Repository Settings

**Variables** (Settings > Secrets and variables > Actions > Variables):
- `API_URL`: `https://api.theaugmentedcraftsman.christianborrello.dev`

No deploy token needed — Koyeb auto-deploys when CI pushes a new image to GHCR.

## Manual Deploy (CI Fallback)

```bash
# Backend — push a new image to GHCR, Koyeb picks it up automatically
cd backend
docker build -t tacblog-api:manual .
docker tag tacblog-api:manual ghcr.io/<owner>/tacblog-api:latest
docker push ghcr.io/<owner>/tacblog-api:latest

# Frontend (triggers from Vercel dashboard or git push)
cd frontend
npm run build
```

## Pre-Deploy Env Var Checklist

Before triggering a deploy (or after rotating secrets), verify all required env vars are set on Koyeb.
A missing variable causes a **startup crash** → crash loop → 503 on `/health`.

```bash
koyeb services describe tacblog-api --app competent-lara
```

Required variables (startup throws if absent in production):

| Variable | Source | Throws if missing |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Neon dashboard → Connection string (URI format) | Yes (migration fails) |
| `Jwt__Secret` | Generated | Yes |
| `AdminCredentials__Email` | Fixed value | Yes |
| `AdminCredentials__HashedPassword` | `dotnet run -- --generate-password-hash` | Yes |
| `OAuth__GitHub__ClientId` | GitHub → Settings → OAuth Apps | Yes |
| `OAuth__GitHub__ClientSecret` | GitHub → Settings → OAuth Apps | Yes |

Optional (have fallbacks, but should be set):

| Variable | Source |
|---|---|
| `Jwt__Issuer` | Fixed URL |
| `ImageKit__UrlEndpoint` | ImageKit dashboard |
| `ImageKit__PublicKey` | ImageKit dashboard |
| `ImageKit__PrivateKey` | ImageKit dashboard |

## Rollback

Koyeb redeploys from the `:latest` GHCR tag. To rollback:

```bash
# Tag the previous working image as :latest and push
docker pull ghcr.io/<owner>/tacblog-api:<previous-commit-sha>
docker tag ghcr.io/<owner>/tacblog-api:<previous-commit-sha> ghcr.io/<owner>/tacblog-api:latest
docker push ghcr.io/<owner>/tacblog-api:latest
```

Koyeb will detect the new `:latest` and redeploy the previous version.

## Smoke Test Checklist

```bash
# Liveness
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/health

# Readiness (DB connectivity)
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/health/ready

# Public posts endpoint
curl -s https://api.theaugmentedcraftsman.christianborrello.dev/api/posts | head -c 200

# Frontend loads
curl -s -o /dev/null -w "%{http_code}" https://theaugmentedcraftsman.christianborrello.dev
```

## Generating Admin Password Hash

The admin password must be hashed using ASP.NET Identity's password hasher before setting as a Koyeb environment variable.
Run this from the backend directory:

```bash
dotnet run --project src/TacBlog.Api -- --generate-password-hash "your-secure-password"
```

Or use the .NET REPL:

```csharp
using Microsoft.AspNetCore.Identity;
var hasher = new PasswordHasher<object>();
Console.WriteLine(hasher.HashPassword(null!, "your-secure-password"));
```
