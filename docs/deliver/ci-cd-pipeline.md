# CI/CD Pipeline -- The Augmented Craftsman v1

**Platform**: GitHub Actions
**Strategy**: Trunk-Based Development with automated deployment on green main
**Docker**: Required in CI for Testcontainers (integration + acceptance tests)

---

## 1. Pipeline Overview

```
Push to any branch
  |
  v
[Lint & Format Check] -----> FAIL? Stop.
  |
  v
[Unit Tests] ------------------> FAIL? Stop.
  (Domain + Application)
  |
  v
[Build Docker Image] ----------> FAIL? Stop.
  (multi-stage, includes unit tests)
  |
  v
[Integration Tests] -----------> FAIL? Stop.
  (Testcontainers PostgreSQL)
  |
  v
[Acceptance Tests] ------------> FAIL? Stop.
  (Testcontainers PostgreSQL)
  |
  v
[Push Docker Image] ----+
  (main branch only)    |
  |                     |
  v                     |
[Deploy Backend] -------+------> Fly.io
  (main branch only)
  |
  v
[Smoke Test] ------------------> FAIL? Alert + manual rollback.
  (main branch only)
```

---

## 2. Pipeline Stages

### Stage 1: Lint and Format Check

**Purpose**: Catch formatting issues before tests run. Fast feedback.

```yaml
- name: Check formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic
  working-directory: backend
```

**Gate**: Build fails if code is not formatted. Developer must run `dotnet format` locally before pushing.

### Stage 2: Unit Tests

**Purpose**: Fast feedback on domain logic and use case handlers. No Docker required.

```yaml
- name: Run domain tests
  run: dotnet test tests/TacBlog.Domain.Tests/ -c Release --no-build --logger "trx;LogFileName=domain.trx"
  working-directory: backend

- name: Run application tests
  run: dotnet test tests/TacBlog.Application.Tests/ -c Release --no-build --logger "trx;LogFileName=application.trx"
  working-directory: backend
```

**Gate**: Any test failure stops the pipeline.

### Stage 3: Docker Build

**Purpose**: Build the production Docker image. The multi-stage Dockerfile runs unit tests internally as an additional verification. See `dockerfile.md` for the full Dockerfile.

```yaml
- name: Build Docker image
  run: docker build -t tacblog-api:${{ github.sha }} .
  working-directory: backend
```

**Gate**: Build failure or test failure inside Docker stops the pipeline.

### Stage 4: Integration Tests

**Purpose**: Verify API endpoints with a real PostgreSQL database via Testcontainers.

```yaml
- name: Run integration tests
  run: dotnet test tests/TacBlog.Api.Tests/ -c Release --logger "trx;LogFileName=integration.trx"
  working-directory: backend
```

**Requires**: Docker running on the CI runner (GitHub Actions `ubuntu-latest` includes Docker).

**Gate**: Any test failure stops the pipeline.

### Stage 5: Acceptance Tests

**Purpose**: End-to-end feature validation through the public HTTP API with a real database.

```yaml
- name: Run acceptance tests
  run: dotnet test tests/TacBlog.Acceptance.Tests/ -c Release --logger "trx;LogFileName=acceptance.trx"
  working-directory: backend
```

**Requires**: Docker running on the CI runner.

**Gate**: Any test failure stops the pipeline.

### Stage 6: Push Docker Image (main only)

**Purpose**: Push the verified image to a container registry for deployment.

```yaml
- name: Push to GitHub Container Registry
  if: github.ref == 'refs/heads/main'
  run: |
    echo "${{ secrets.GITHUB_TOKEN }}" | docker login ghcr.io -u ${{ github.actor }} --password-stdin
    docker tag tacblog-api:${{ github.sha }} ghcr.io/${{ github.repository_owner }}/tacblog-api:${{ github.sha }}
    docker tag tacblog-api:${{ github.sha }} ghcr.io/${{ github.repository_owner }}/tacblog-api:latest
    docker push ghcr.io/${{ github.repository_owner }}/tacblog-api:${{ github.sha }}
    docker push ghcr.io/${{ github.repository_owner }}/tacblog-api:latest
```

### Stage 7: Deploy Backend (main only)

**Purpose**: Deploy to Fly.io using the flyctl CLI.

```yaml
- name: Deploy to Fly.io
  if: github.ref == 'refs/heads/main'
  run: flyctl deploy --remote-only
  env:
    FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

**Note**: `flyctl deploy --remote-only` builds the Docker image on Fly.io's remote builders and deploys it. Stage 6 (Push to GHCR) serves a distinct purpose: it provides rollback images tagged by commit SHA. If Fly.io's remote build cache is lost or a fast rollback is needed, the GHCR image enables `flyctl deploy --image ghcr.io/<owner>/tacblog-api:<previous-sha>` without rebuilding. Both stages are intentionally kept.

### Stage 8: Smoke Test (main only)

**Purpose**: Verify the deployment is healthy after Fly.io finishes deploying.

```yaml
- name: Wait for deployment
  if: github.ref == 'refs/heads/main'
  run: sleep 30

- name: Smoke test (liveness)
  if: github.ref == 'refs/heads/main'
  run: |
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ vars.API_URL }}/health)
    if [ "$STATUS" != "200" ]; then
      echo "Smoke test failed: /health returned $STATUS"
      exit 1
    fi

- name: Smoke test (readiness)
  if: github.ref == 'refs/heads/main'
  run: |
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ vars.API_URL }}/health/ready)
    if [ "$STATUS" != "200" ]; then
      echo "Smoke test failed: /health/ready returned $STATUS (database may be unreachable)"
      exit 1
    fi

- name: Check for startup errors
  if: github.ref == 'refs/heads/main'
  run: |
    ERRORS=$(flyctl logs --no-tail 2>/dev/null | grep '"@l":"Error"' | head -5)
    if [ -n "$ERRORS" ]; then
      echo "Warning: errors detected in recent logs:"
      echo "$ERRORS"
    fi
  env:
    FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

**Gate**: If smoke test fails, the pipeline reports failure. Manual rollback is required (redeploy previous release via `flyctl releases` and `flyctl deploy --image`). The startup error check is informational (non-blocking) to avoid false positives from pre-deployment log entries.

---

## 3. GitHub Actions Workflow File

Path: `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main]
    paths:
      - 'backend/**'
      - '.github/workflows/ci.yml'
  pull_request:
    branches: [main]
    paths:
      - 'backend/**'
      - '.github/workflows/ci.yml'

env:
  DOTNET_VERSION: '10.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  ci:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore
        working-directory: backend

      - name: Check formatting
        run: dotnet format --verify-no-changes --verbosity diagnostic
        working-directory: backend

      - name: Build
        run: dotnet build -c Release --no-restore
        working-directory: backend

      - name: Unit tests (Domain)
        run: dotnet test tests/TacBlog.Domain.Tests/ -c Release --no-build --logger "trx;LogFileName=domain.trx"
        working-directory: backend

      - name: Unit tests (Application)
        run: dotnet test tests/TacBlog.Application.Tests/ -c Release --no-build --logger "trx;LogFileName=application.trx"
        working-directory: backend

      - name: Integration tests
        run: dotnet test tests/TacBlog.Api.Tests/ -c Release --no-build --logger "trx;LogFileName=integration.trx"
        working-directory: backend

      - name: Acceptance tests
        run: dotnet test tests/TacBlog.Acceptance.Tests/ -c Release --no-build --logger "trx;LogFileName=acceptance.trx"
        working-directory: backend

      - name: Build Docker image
        run: docker build -t tacblog-api:${{ github.sha }} .
        working-directory: backend

      - name: Push to GHCR
        if: github.ref == 'refs/heads/main' && github.event_name == 'push'
        run: |
          echo "${{ secrets.GITHUB_TOKEN }}" | docker login ghcr.io -u ${{ github.actor }} --password-stdin
          docker tag tacblog-api:${{ github.sha }} ghcr.io/${{ github.repository_owner }}/tacblog-api:${{ github.sha }}
          docker tag tacblog-api:${{ github.sha }} ghcr.io/${{ github.repository_owner }}/tacblog-api:latest
          docker push ghcr.io/${{ github.repository_owner }}/tacblog-api:${{ github.sha }}
          docker push ghcr.io/${{ github.repository_owner }}/tacblog-api:latest

  deploy:
    needs: ci
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Install flyctl
        uses: superfly/flyctl-actions/setup-flyctl@master

      - name: Deploy to Fly.io
        run: flyctl deploy --remote-only
        working-directory: backend
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}

      - name: Wait for deployment
        run: sleep 30

      - name: Smoke test (liveness)
        run: |
          STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ vars.API_URL }}/health)
          if [ "$STATUS" != "200" ]; then
            echo "Smoke test failed: /health returned $STATUS"
            exit 1
          fi

      - name: Smoke test (readiness)
        run: |
          STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ vars.API_URL }}/health/ready)
          if [ "$STATUS" != "200" ]; then
            echo "Smoke test failed: /health/ready returned $STATUS (database may be unreachable)"
            exit 1
          fi

      - name: Check for startup errors
        run: |
          ERRORS=$(flyctl logs --no-tail 2>/dev/null | grep '"@l":"Error"' | head -5)
          if [ -n "$ERRORS" ]; then
            echo "Warning: errors detected in recent logs:"
            echo "$ERRORS"
          fi
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
```

---

## 4. Frontend Pipeline

The Astro frontend is deployed by Vercel automatically on push to `main`. No GitHub Actions workflow is needed for frontend deployment.

However, a separate workflow validates frontend changes:

Path: `.github/workflows/frontend.yml`

```yaml
name: Frontend CI

on:
  push:
    branches: [main]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend.yml'
  pull_request:
    branches: [main]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend.yml'

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: frontend

      - name: Type check
        run: npx astro check
        working-directory: frontend

      - name: Build
        run: npm run build
        working-directory: frontend
        env:
          PUBLIC_API_URL: https://api.theaugmentedcraftsman.christianborrello.dev
```

**Note**: The frontend build fetches from the production API at build time. If the API is down during a frontend CI build on a feature branch, the build fails. This is acceptable -- the API should be available during CI runs. For feature branch builds where the API might not have the latest data, `PUBLIC_API_URL` could point to a mock or be skipped.

---

## 5. Vercel Deploy Hook (Content Publish Trigger)

When the admin publishes a post via `POST /api/posts/{id}/publish`, the .NET API fires a Vercel deploy hook to rebuild the static site:

```csharp
// Inside PublishPostHandler (simplified)
// After publishing the post in the database:
await httpClient.PostAsync(vercelDeployHookUrl, null);
```

This is fire-and-forget. If the hook fails, the published post is still in the database and the next Vercel build (manual or automated) will pick it up.

---

## 6. Required GitHub Secrets and Variables

### Secrets

| Name | Purpose |
|------|---------|
| `FLY_API_TOKEN` | Fly.io CLI authentication for deployment |

**Note**: `GITHUB_TOKEN` is automatically provided by GitHub Actions for GHCR authentication.

### Variables

| Name | Purpose |
|------|---------|
| `API_URL` | Production API URL for smoke tests (e.g., `https://api.theaugmentedcraftsman.christianborrello.dev`) |

---

## 7. Pipeline Timing Estimates

| Stage | Duration | Cumulative |
|-------|----------|------------|
| Checkout + Setup | ~15s | 15s |
| Restore | ~20s | 35s |
| Format check | ~5s | 40s |
| Build | ~15s | 55s |
| Unit tests | ~5s | 1m |
| Integration tests | ~30s (Testcontainers startup + tests) | 1m 30s |
| Acceptance tests | ~30s (shares Testcontainers from integration) | 2m |
| Docker build | ~60s (with layer caching) | 3m |
| Docker push (main only) | ~15s | 3m 15s |
| Deploy to Fly.io (main only) | ~60s + 30s wait | 4m 30s |
| Smoke test | ~5s | 4m 35s |

**Total**: ~2 minutes for feature branches, ~5 minutes for main with deployment.

---

## 8. Rollback Procedure

The deployment strategy is Recreate (stop old, start new). Rollback is manual.

### Rollback Steps

1. **Identify the last known good release**: `flyctl releases` to list recent deployments.
2. **Rollback via flyctl**:
   ```bash
   flyctl deploy --image <previous-image-ref>
   ```
   Or redeploy from GHCR: `flyctl deploy --image ghcr.io/<owner>/tacblog-api:<previous-sha>`
3. **Verify health endpoint**: `curl https://api.theaugmentedcraftsman.christianborrello.dev/health`
4. **Investigate the failure**: Check Fly.io logs (`flyctl logs`), fix the issue, push a new commit.

### When to Rollback

- Smoke test fails after deployment
- Health check fails repeatedly (Fly.io auto-restarts, but if it keeps failing)
- Functional issues discovered after deployment (broken endpoint, data corruption)

### Rollback Time Target

Under 5 minutes from detection to restored service. This is acceptable for a personal blog with a single admin user.

---

## 9. Pipeline Design Decisions

### Why Not Parallel Jobs?

The CI stages run sequentially in a single job. Parallel jobs would split unit tests and integration tests into separate runners, but:
- The total pipeline time is ~2 minutes, not worth optimizing
- Sequential ensures Docker is available for Testcontainers without coordination
- A single job is simpler to maintain for a solo developer

### Why Not Fly.io GitHub Integration Instead?

Fly.io supports GitHub Actions integration via `superfly/flyctl-actions`. The deploy step in CI uses `flyctl deploy --remote-only` which builds on Fly.io's remote builders from the Dockerfile. This is simpler than pushing to GHCR first.

**Trade-off**: Remote builds on Fly.io are slightly slower (~60s) than local Docker builds but avoid the GHCR push step. For a personal blog with infrequent deploys, this is acceptable. If build speed becomes an issue, switch to pre-built images pushed to GHCR.

### Why Test Results as .trx Files?

`.trx` files (Visual Studio Test Results) can be uploaded as GitHub Actions artifacts for post-mortem analysis. This is optional but useful when debugging flaky tests.
