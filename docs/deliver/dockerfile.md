# Dockerfile Design -- The Augmented Craftsman v1

**Target**: Multi-stage Dockerfile for .NET 10 API
**Optimized for**: Build cache efficiency, minimal image size, security

---

## 1. Multi-Stage Build Strategy

```
Stage 1: restore    -- Restore NuGet packages (cached unless .csproj files change)
Stage 2: build      -- Build solution
Stage 3: test       -- Run unit tests (fail build if tests fail)
Stage 4: publish    -- Publish release build
Stage 5: runtime    -- Final minimal image with published output only
```

### Why This Order

- **Cache efficiency**: NuGet restore is the slowest step. By copying only `.csproj` and `.sln` files first, Docker caches the restore layer. Source code changes do not invalidate the restore cache.
- **Test in build**: Unit tests run inside the Docker build. If they fail, the image is never published. Integration/acceptance tests run separately in CI (they need Testcontainers).
- **Minimal runtime**: The final image contains only the published output and the ASP.NET runtime. No SDK, no source code, no test projects.

---

## 2. Dockerfile

```dockerfile
# ==============================================================================
# Stage 1: Restore NuGet packages
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src

# Copy solution and project files only (for cache-efficient restore)
COPY TacBlog.sln ./
COPY src/TacBlog.Domain/TacBlog.Domain.csproj src/TacBlog.Domain/
COPY src/TacBlog.Application/TacBlog.Application.csproj src/TacBlog.Application/
COPY src/TacBlog.Infrastructure/TacBlog.Infrastructure.csproj src/TacBlog.Infrastructure/
COPY src/TacBlog.Api/TacBlog.Api.csproj src/TacBlog.Api/
COPY tests/TacBlog.Domain.Tests/TacBlog.Domain.Tests.csproj tests/TacBlog.Domain.Tests/
COPY tests/TacBlog.Application.Tests/TacBlog.Application.Tests.csproj tests/TacBlog.Application.Tests/

RUN dotnet restore

# ==============================================================================
# Stage 2: Build
# ==============================================================================
FROM restore AS build

# Copy all source code
COPY src/ src/
COPY tests/TacBlog.Domain.Tests/ tests/TacBlog.Domain.Tests/
COPY tests/TacBlog.Application.Tests/ tests/TacBlog.Application.Tests/

RUN dotnet build -c Release --no-restore

# ==============================================================================
# Stage 3: Run unit tests (fail build if tests fail)
# ==============================================================================
FROM build AS test

RUN dotnet test tests/TacBlog.Domain.Tests/ -c Release --no-build --logger "console;verbosity=minimal" && \
    dotnet test tests/TacBlog.Application.Tests/ -c Release --no-build --logger "console;verbosity=minimal"

# ==============================================================================
# Stage 4: Publish
# ==============================================================================
FROM test AS publish

RUN dotnet publish src/TacBlog.Api/TacBlog.Api.csproj -c Release --no-build -o /app/publish

# ==============================================================================
# Stage 5: Runtime (minimal image)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Security: run as non-root user
RUN addgroup -S tacblog && adduser -S tacblog -G tacblog

WORKDIR /app
COPY --from=publish /app/publish .

# Security: non-root execution
USER tacblog

# Fly.io exposes port 8080 by default (configured in fly.toml)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check for Fly.io
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TacBlog.Api.dll"]
```

---

## 3. Environment Variables at Runtime

The Docker image contains no secrets. All configuration is injected via environment variables at runtime using `fly secrets set`.

| Variable | Source | Required | Purpose |
|----------|--------|----------|---------|
| `ConnectionStrings__DefaultConnection` | Neon dashboard | Yes | PostgreSQL connection string |
| `Jwt__Secret` | Generated (64+ chars) | Yes | JWT signing key |
| `Jwt__Issuer` | `https://api.theaugmentedcraftsman.christianborrello.dev` | Yes | JWT issuer claim |
| `Jwt__Audience` | `https://api.theaugmentedcraftsman.christianborrello.dev` | Yes | JWT audience claim |
| `ImageKit__UrlEndpoint` | ImageKit dashboard | Yes | ImageKit URL endpoint |
| `ImageKit__PublicKey` | ImageKit dashboard | Yes | ImageKit public key |
| `ImageKit__PrivateKey` | ImageKit dashboard | Yes | ImageKit private key |
| `Admin__Email` | Chosen by admin | Yes | Admin login email |
| `Admin__PasswordHash` | ASP.NET Identity hasher | Yes | Admin password hash (seeded at startup) |
| `VercelDeployHookUrl` | Vercel dashboard | Yes | Triggers frontend rebuild on publish |
| `ASPNETCORE_URLS` | Dockerfile (baked in) | -- | Set to `http://+:8080` in Dockerfile |
| `ASPNETCORE_ENVIRONMENT` | `fly secrets set` | No | Defaults to `Production` |

### Setting Secrets on Fly.io

```bash
fly secrets set \
  ConnectionStrings__DefaultConnection="Host=...;Database=tacblog;Username=...;Password=...;Ssl Mode=Require" \
  Jwt__Secret="<generated-64-char-secret>" \
  Jwt__Issuer="https://api.theaugmentedcraftsman.christianborrello.dev" \
  Jwt__Audience="https://api.theaugmentedcraftsman.christianborrello.dev" \
  ImageKit__UrlEndpoint="https://ik.imagekit.io/augmented" \
  ImageKit__PublicKey="<public-key>" \
  ImageKit__PrivateKey="<private-key>" \
  Admin__Email="christian.borrello@live.it" \
  Admin__PasswordHash="<generated-hash>" \
  VercelDeployHookUrl="https://api.vercel.com/v1/integrations/deploy/..."
```

Fly.io encrypts secrets at rest and injects them as environment variables. They are never stored in the Docker image or in source control.

---

## 4. .dockerignore

Place at `backend/.dockerignore`:

```
# Build artifacts
**/bin/
**/obj/

# Test projects that need Docker (not included in image build)
tests/TacBlog.Api.Tests/
tests/TacBlog.Acceptance.Tests/

# IDE and OS files
**/.vs/
**/.vscode/
**/.idea/
**/*.user
**/*.suo
**/launchSettings.json
.git/
.gitignore

# Documentation
docs/
*.md

# Frontend
frontend/
```

---

## 5. Build Context

The Dockerfile expects to be run from the `backend/` directory:

```bash
# Local build
cd backend
docker build -t tacblog-api .

# CI build
docker build -t tacblog-api -f backend/Dockerfile backend/
```

---

## 6. Image Size Analysis

| Stage | Base Image | Approximate Size |
|-------|-----------|-----------------|
| SDK (build stages) | `mcr.microsoft.com/dotnet/sdk:10.0-alpine` | ~500MB (not in final image) |
| Runtime (final) | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` | ~90MB |
| Published app | N/A | ~15-25MB |
| **Final image** | | **~110-115MB** |

### Why Alpine

- Smallest official .NET base image
- Reduces attack surface (fewer OS packages)
- Sufficient for a simple API (no native dependencies that require glibc)
- If globalization issues arise (ICU), switch to `aspnet:10.0-alpine-extra` or add `ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`

---

## 7. Security Hardening

| Measure | Implementation |
|---------|---------------|
| Non-root execution | `USER tacblog` -- process runs as unprivileged user |
| Minimal base image | Alpine -- fewer packages, smaller attack surface |
| No SDK in runtime | Multi-stage build excludes SDK, compilers, and source code |
| No test code in runtime | Only `TacBlog.Api.dll` and its dependencies are published |
| Read-only filesystem | Fly.io supports read-only container filesystem |
| No secrets in image | All secrets via environment variables at runtime |

---

## 8. Port Configuration

Fly.io routes traffic to the internal port defined in `fly.toml` (default: 8080). The Dockerfile sets `ASPNETCORE_URLS=http://+:8080` to match.

The `Program.cs` should NOT hardcode a port. ASP.NET Minimal API respects `ASPNETCORE_URLS` automatically.

### fly.toml

Place at `backend/fly.toml`:

```toml
app = "tacblog-api"
primary_region = "cdg"  # Paris (closest to author)

[build]

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = "stop"
  auto_start_machines = true
  min_machines_running = 0

[checks]
  [checks.health]
    port = 8080
    type = "http"
    interval = "30s"
    timeout = "5s"
    path = "/health"
```

**Key settings**:
- `auto_stop_machines = "stop"`: Machine stops after inactivity (free tier friendly)
- `auto_start_machines = true`: Machine wakes on incoming request (~5-10s cold start)
- `min_machines_running = 0`: Allows full stop when idle (saves free tier resources)

---

## 9. Health Check

The `HEALTHCHECK` instruction uses `wget` (available in Alpine) to probe `/health`. This endpoint is implemented in the .NET API:

```csharp
// In Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

Fly.io uses the `[checks]` section in `fly.toml` for health monitoring. The Docker `HEALTHCHECK` serves as an additional safety net. If the health check fails repeatedly, Fly.io restarts the machine.

---

## 10. Build Cache Optimization

```
Layer 1: Base image (cached until .NET version changes)
Layer 2: .csproj + .sln files (cached until project structure changes)
Layer 3: NuGet restore (cached unless dependencies change)
Layer 4: Source code copy + build (invalidated on any code change)
Layer 5: Test execution (invalidated on any code change)
Layer 6: Publish (invalidated on any code change)
Layer 7: Runtime copy (invalidated on any code change)
```

**Key insight**: Layers 1-3 change rarely. Layers 4-7 change on every commit but are fast because the restore cache is preserved. A typical rebuild after a source code change takes 30-60 seconds.

---

## 11. Local Development

The Dockerfile is for CI/CD and production. Local development uses `dotnet run` directly:

```bash
cd backend
dotnet run --project src/TacBlog.Api
```

No Docker Compose is needed for local development. The database for integration tests is provided by Testcontainers (which manages its own Docker containers).
