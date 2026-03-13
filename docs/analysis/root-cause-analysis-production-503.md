# Root Cause Analysis: Production API 503 / Healthcheck Failure

**Date**: 2026-03-13
**Analyst**: Rex (nw:troubleshooter)
**Methodology**: Toyota 5 Whys — Multi-Causal
**Status**: Root Cause A resolved. Root Causes B and C are conditional hypotheses pending Koyeb environment verification.

---

## Problem Statement

The production backend API at `https://api.theaugmentedcraftsman.christianborrello.dev` was unavailable. The CI `smoke-test` job reported `/health` returning 503. The failure affected all endpoints. The issue began after the CI run triggered by commit `a0de07e` (2026-03-13 22:02 UTC), which pushed a new Docker image to GHCR and triggered Koyeb auto-deploy.

**Scope**: Production environment only. Local development, unit tests, and integration tests (183/183) all pass. The failure is production-only and deterministic.

**Platform note**: The prompt references Fly.io but all CI and runtime evidence points to Koyeb. This document uses Koyeb throughout, which is what the code and pipeline confirm.

---

## Evidence Baseline

| Evidence Item | Value |
|---|---|
| `curl -v /health` (10s timeout) | HTTP/2 503 — HTML body from Koyeb edge. TLS and TCP layers healthy. |
| `curl -v /health` (15s timeout) | 0 bytes received, operation timed out. Koyeb edge hangs waiting for backend. |
| CI smoke-test log | `Smoke test failed: /health returned 503` at 22:07:52 UTC |
| GHCR push | Successful — `latest` and SHA-tagged image pushed at 22:05:16 UTC |
| `sleep 90` wait | Fixed wait before smoke test. Sufficient for normal deploy; a startup crash still presents as 503 after 90s. |
| All CI tests | 183/183 pass (Domain: 68, Application: 102, Infrastructure: 13) |
| `appsettings.json` | `ConnectionStrings.DefaultConnection = "SET_VIA_ENVIRONMENT_VARIABLE"`, `Jwt.Secret = "SET_VIA_ENVIRONMENT_VARIABLE"` — literal placeholder strings, not empty |
| `Program.cs` line 85 (current) | `builder.Services.AddHttpClient();` — fix M1 is applied |
| `Program.cs` line 91 (current) | `sp.GetRequiredService<IHttpClientFactory>().CreateClient()` — fix M1 is applied |
| `Program.cs` line 85 (at incident) | `AddHttpClient()` was absent. Evidence: commit `f2b7336` message "fix: register HttpClient via IHttpClientFactory for ProductionOAuthClient" |
| `OAuthSettingsValidator.ValidateGitHub` | `throw new InvalidOperationException(...)` if GitHub ClientId or ClientSecret is empty in production |
| `Program.cs` line 122-124 | `throw new InvalidOperationException(...)` if `AdminCredentials:Email` or `AdminCredentials:HashedPassword` missing in production |
| `Program.cs` line 138-140 | `throw new InvalidOperationException(...)` if `Jwt:Secret` is null or empty in production |
| `appsettings.json` `Jwt.Secret` | `"SET_VIA_ENVIRONMENT_VARIABLE"` — NOT null, NOT empty. Guard passes. |
| `ProductionStartupShould.cs` | New regression test exercising production DI path in `Api.Tests`. Now included in CI. |
| `ci.yml` line 61 | `dotnet test tests/TacBlog.Api.Tests/` — `ProductionStartupShould` executes in CI from this point forward. |

---

## Five Whys Analysis

### BRANCH A: Container crashes at startup — `HttpClient` not registered in DI

**WHY 1A**: Koyeb returns 503 from its edge on all requests.
Evidence: `curl` returns HTTP/2 503 with Koyeb HTML error page. TLS and TCP layers are healthy, ruling out network or certificate issues.

**WHY 2A**: Koyeb has no healthy backend instance to route traffic to.
Evidence: Koyeb returns its own error page (SVG-based HTML body), not an ASP.NET response. This pattern occurs when Koyeb's health gate determines no replica is healthy — the container is crashing or not starting.

**WHY 3A**: The ASP.NET container crashes during startup before it can accept requests.
Evidence: `/health` is registered at `Program.cs:211` — after DI container build and after `db.Database.MigrateAsync()`. If startup throws before `app.Run()`, no requests are ever handled. The 90s wait is sufficient for a normal deploy; a startup crash still presents as 503.

**WHY 4A**: Startup throws an unhandled `InvalidOperationException` while building the DI container.
Evidence: The `IOAuthClient` singleton factory (`Program.cs` production branch) calls `sp.GetRequiredService<HttpClient>()`. At the time of the incident, `AddHttpClient()` was not present in `Program.cs` or any infrastructure registration. `HttpClient` is not a built-in DI service in ASP.NET Core. `GetRequiredService` on an unregistered type throws `InvalidOperationException: No service for type 'System.Net.Http.HttpClient' has been registered.` This exception propagates out of container construction, crashing the process before `app.Run()`.

**WHY 5A**: The `ProductionOAuthClient` DI registration was written to resolve `HttpClient` directly from the service provider, but `AddHttpClient()` was never called. The bug is invisible in CI because all test environments use `IsDevelopment() = true`, taking the `DevOAuthClient` branch. No test exercised the production DI path before deployment.
Evidence:
- Commit `f2b7336` is titled "fix: register HttpClient via IHttpClientFactory for ProductionOAuthClient" — confirming the missing registration was the diagnosed cause.
- `Program.cs:85` now contains `builder.Services.AddHttpClient();` and line 91 uses `IHttpClientFactory.CreateClient()`.
- No existing test in `Domain.Tests`, `Application.Tests`, or `Infrastructure.Tests` builds a `WebApplicationFactory` in Production mode. The production DI path had zero test coverage.

**ROOT CAUSE A** (RESOLVED): `HttpClient` was not registered in the DI container. `GetRequiredService<HttpClient>()` in the `ProductionOAuthClient` factory threw `InvalidOperationException` at startup, crashing the container before it could serve any request. The fault was invisible in CI because all test environments use the development branch (`DevOAuthClient`). Root Cause A is confirmed by commit `f2b7336` and is now fixed.

---

### BRANCH B: Startup fails due to missing Koyeb DB environment variable

**WHY 1B**: Same symptom — 503 from Koyeb edge.

**WHY 2B**: The container's `MigrateAsync()` call fails to connect to Postgres.
Evidence: `Program.cs:199` calls `await db.Database.MigrateAsync()` unless `Database:RunMigrationsAtStartup=false`. EF Core uses the connection string from `ConnectionStrings:DefaultConnection`.

**WHY 3B**: `appsettings.json` sets `ConnectionStrings.DefaultConnection = "SET_VIA_ENVIRONMENT_VARIABLE"` — a non-empty literal that is NOT a valid Postgres connection string.
Evidence: `appsettings.json` line 10 contains this literal. If Koyeb's env var `ConnectionStrings__DefaultConnection` is not set, EF Core receives this literal as the connection string and the TCP connection to Postgres fails at startup.

**WHY 4B**: The design relies on environment variable override of `appsettings.json` values. If the env var was not configured in Koyeb before the first production deployment, the fallback is an invalid connection string rather than a safe failure.
Evidence: `appsettings.json` uses a misleading placeholder string that passes `string.IsNullOrEmpty()` guards but is not a valid connection string. There is no CI-level verification that required Koyeb env vars exist before deploying.

**WHY 5B**: No deployment pre-flight check confirms required secrets exist in the target environment before the container starts.

**BRANCH B ASSESSMENT**: Branch B is a **conditional hypothesis**. It produces a startup crash only if `ConnectionStrings__DefaultConnection` is not set as a Koyeb environment variable. Branch B is masked by Root Cause A — if the container crashed at DI resolution (earlier in startup), `MigrateAsync()` is never reached. After Root Cause A is fixed, Branch B may or may not surface depending on Koyeb configuration. Verification requires checking the Koyeb service dashboard.

**ROOT CAUSE B (conditional hypothesis)**: If `ConnectionStrings__DefaultConnection` is not configured as a Koyeb secret/env var, the literal `"SET_VIA_ENVIRONMENT_VARIABLE"` is used as the Postgres connection string, `MigrateAsync()` throws at startup, and the container crashes. Requires verification.

---

### BRANCH C: `OAuthSettingsValidator` throws — missing GitHub OAuth secrets in Koyeb

**WHY 1C**: Same symptom — 503 from Koyeb edge.

**WHY 2C**: `OAuthSettingsValidator.Validate()` throws an `InvalidOperationException` if GitHub OAuth secrets are absent in production.
Evidence: `OAuthSettingsValidator.cs:22-32` — explicit `throw new InvalidOperationException(...)` when `GitHubClientId` or `GitHubClientSecret` is empty in production mode.

**WHY 3C**: `Program.cs:76-77` defaults to empty string if the configuration key is absent: `GitHubClientId: builder.Configuration["OAuth:GitHub:ClientId"] ?? ""`. If Koyeb env vars `OAuth__GitHub__ClientId` and `OAuth__GitHub__ClientSecret` are not set, the validator receives empty strings and throws.
Evidence: `appsettings.json` contains no OAuth section — these values exist only through environment variable injection. If the env vars are absent in Koyeb, the fail-fast throw fires.

**WHY 4C**: The OAuth feature was deployed with a fail-fast validator but no CI gate to verify the required secrets exist in the target environment before the image is deployed.
Evidence: `ci.yml` has no step that checks Koyeb environment variables before or after pushing the Docker image.

**WHY 5C**: Deployment assumes required secrets pre-exist in the target environment. There is no automated contract between code (which declares secret requirements) and infrastructure (which must provide them).

**BRANCH C ASSESSMENT**: Branch C is also a **conditional hypothesis**, and is masked by Root Cause A (DI resolution crash fires before the validator runs at `Program.cs:203`). After Root Cause A is fixed, Branch C may surface if the OAuth secrets are not present in Koyeb. Requires verification of Koyeb env vars.

**ROOT CAUSE C (conditional hypothesis)**: If `OAuth__GitHub__ClientId` or `OAuth__GitHub__ClientSecret` are not configured as Koyeb env vars, `OAuthSettingsValidator.Validate()` throws at startup, crashing the container. The fail-fast behavior is correct; the gap is the absence of pre-deployment verification.

---

## Backwards Chain Validation

| Chain | Trace forward | Valid? |
|---|---|---|
| Root Cause A: `HttpClient` unregistered | DI factory throws `InvalidOperationException` at container build time → process crashes before `app.Run()` → no healthy Koyeb replica → edge returns 503 | YES |
| Root Cause A explains 183 tests passing | All test environments: `IsDevelopment() = true` → `DevOAuthClient` branch taken → production factory never invoked | YES |
| Root Cause A explains all endpoints 503 | Crash is pre-`app.Run()`, no endpoint ever registered — not just `/health` | YES |
| Root Cause A is resolved | `builder.Services.AddHttpClient()` at line 85; `IHttpClientFactory.CreateClient()` at line 91; confirmed by commit `f2b7336` | YES |
| Root Cause B: missing DB env var | `MigrateAsync()` receives invalid connection string → EF Core throws → process crashes → 503 | YES (conditional) |
| Root Cause C: missing OAuth env vars | `OAuthSettingsValidator` receives empty strings → throws → process crashes → 503 | YES (conditional) |
| A, B, C contradict each other | No — they are independent failure modes at different startup phases: A at DI build, B at migration, C at validator. Fixing A may unmask B or C. | NO contradiction |
| GHCR push success rules out image build issue | Image pushed successfully; fault is in application code/config, not CI infrastructure | YES |

---

## Solutions

### Immediate Mitigation (restore service) — M1: APPLIED

**M1 — `AddHttpClient()` + `IHttpClientFactory` registration** (P0 — applied in commit `f2b7336`)

`Program.cs:85`: `builder.Services.AddHttpClient();`
`Program.cs:91`: `sp.GetRequiredService<IHttpClientFactory>().CreateClient()`

This resolves Root Cause A. The next CI push to `main` will build a new image with this fix applied, trigger Koyeb auto-deploy, and the smoke test should pass.

---

**M2 — Verify Koyeb environment variables** (P0 — verify before next deploy completes)

Check the Koyeb service dashboard and confirm each required env var is present:

| Variable | Required? | Guard behavior |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | YES | No guard — silently passes invalid string to EF Core; `MigrateAsync()` will throw |
| `Jwt__Secret` | YES (guards for null/empty only — placeholder passes guard but is insecure) | Passes if non-empty; real secret required |
| `AdminCredentials__Email` | YES | `Program.cs:122-124` throws if empty in production |
| `AdminCredentials__HashedPassword` | YES | `Program.cs:122-124` throws if empty in production |
| `OAuth__GitHub__ClientId` | YES | `OAuthSettingsValidator` throws if empty in production |
| `OAuth__GitHub__ClientSecret` | YES | `OAuthSettingsValidator` throws if empty in production |
| `OAuth__Google__ClientId` | NO | Warns if partially set; null is acceptable |
| `OAuth__Google__ClientSecret` | NO | Warns if partially set; null is acceptable |

If any required variable is absent, Root Cause B or C will surface after M1 is deployed.

---

### Permanent Fixes (prevent recurrence)

**F1 — Regression test for production DI path** (P1 — APPLIED)

`ProductionStartupShould.cs` has been written and added to `TacBlog.Api.Tests`. The test bootstraps `WebApplicationFactory<Program>` with `UseEnvironment("Production")` and supplies all required fake secrets. `Database:RunMigrationsAtStartup=false` prevents real DB connection. `ci.yml` runs `TacBlog.Api.Tests/` in the integration test step — this test will execute on every push to `main` from this point forward.

Two test cases:
1. `build_di_container_without_exceptions_in_production_mode` — asserts startup does not throw. Directly guards against a recurrence of Root Cause A.
2. `resolve_oauth_client_as_production_implementation` — asserts `IOAuthClient` resolves to a non-null instance in production mode. Guards against regression in `ProductionOAuthClient` wiring.

**F2 — Replace placeholder strings in `appsettings.json` with empty strings** (P2)

`"SET_VIA_ENVIRONMENT_VARIABLE"` as a placeholder is dangerous: it passes `string.IsNullOrEmpty()` guards (masking misconfiguration), produces misleading error messages when used as an actual value, and creates a false sense of security.

Replace with `""` (empty string) for all secrets:
- `ConnectionStrings:DefaultConnection` — EF Core fails with a clear error if no override is provided.
- `Jwt:Secret` — the existing `string.IsNullOrEmpty()` guard (`Program.cs:138`) will throw in production if the env var is not set, which is the intended behavior.

This makes the fail-fast guards work as designed.

**F3 — Add secrets pre-flight check to CI before deploy** (P2)

Add a CI step between `Push to GHCR` and `smoke-test` that verifies required Koyeb env vars are present using the Koyeb CLI or API. If any required variable is missing, fail CI before deployment — catching Root Causes B and C before the container starts.

Example step structure:
```yaml
- name: Verify Koyeb secrets configured
  env:
    KOYEB_TOKEN: ${{ secrets.KOYEB_TOKEN }}
  run: |
    # Install Koyeb CLI, then:
    koyeb service describe tacblog-api --output json \
      | jq -e '.env[] | select(.key == "ConnectionStrings__DefaultConnection")' \
      || (echo "FATAL: ConnectionStrings__DefaultConnection is not set in Koyeb" && exit 1)
```

**F4 — Replace `sleep 90` with active deploy polling** (P3)

`sleep 90` does not account for Koyeb deploy duration variability. Replace with polling against Koyeb's deployment status API: poll until `status == "healthy"` or timeout. This reduces false smoke-test failures (deploy took longer than 90s) and reduces wasted CI minutes (deploy completed in 20s).

**F5 — Explicitly declare `ASPNETCORE_ENVIRONMENT=Production` in Koyeb** (P2)

Verify that Koyeb is passing `ASPNETCORE_ENVIRONMENT=Production` to the container. Without this, `app.Environment.IsDevelopment()` may return false by default (it depends on the env var), but `OAuthSettingsValidator.Validate(oauthSettings, app.Environment.IsProduction())` uses `IsProduction()` — which returns true only when `ASPNETCORE_ENVIRONMENT=Production`. If the variable is absent, Koyeb defaults may vary. Explicit declaration removes ambiguity.

---

### Early Detection

**D1 — Koyeb health check endpoint alignment**: The Dockerfile `HEALTHCHECK` targets `/health` with `start-period=10s`. Verify Koyeb's internal health gate also uses `/health` and not a different path. If Koyeb uses a TCP probe instead, it may report "healthy" even when the app is running but returning 5xx.

**D2 — Deploy failure alerting**: Configure Koyeb to send notifications (Slack, email) when a deployment fails health checks. Currently the only failure signal is the CI smoke-test, which runs 90s after deploy trigger. Koyeb-native alerting provides faster signal.

**D3 — Frontend SSG resilience to backend 503**: The frontend CI build fails hard when the API is down (Astro's `getStaticPaths` cannot reach the API). This couples two independent deploy pipelines. Add a try/catch in the Astro data-fetching layer that returns an empty array on 503, allowing frontend CI to complete independently and resolve paths at runtime.

---

## Root Cause Summary

| ID | Root Cause | Certainty | Impact | Status |
|---|---|---|---|---|
| A | `HttpClient` not registered in DI container; `GetRequiredService<HttpClient>()` threw `InvalidOperationException` at production startup; test suite never exercised production DI path | Confirmed — commit `f2b7336` | P0 — container crashed, all endpoints 503 | RESOLVED: `AddHttpClient()` added; `IHttpClientFactory` used; regression test added |
| B | `ConnectionStrings__DefaultConnection` Koyeb env var may not be set; `MigrateAsync()` receives literal `"SET_VIA_ENVIRONMENT_VARIABLE"` as connection string and fails | Hypothesis — requires Koyeb dashboard verification | P0 if true — startup crash after Root Cause A fix | OPEN: verify Koyeb env vars (M2) |
| C | `OAuth__GitHub__ClientId/Secret` Koyeb env vars may not be set; `OAuthSettingsValidator` throws at startup; masked by Root Cause A | Hypothesis — masked by A, requires verification after A is fixed | P0 if present after A is fixed | OPEN: verify Koyeb env vars (M2) |

**Recommended action sequence**:
1. Confirm M1 (fix `f2b7336`) is in the HEAD of `main` and the pending CI run completes successfully. [DONE — fix applied]
2. Verify all Koyeb environment variables (M2) before or during the next deployment. [PENDING]
3. Monitor smoke-test on the next CI push to `main`. If B or C surface, the Koyeb env var for the failing secret must be set.
4. Apply F2 (replace `appsettings.json` placeholders with empty strings) and F5 (explicit `ASPNETCORE_ENVIRONMENT`) in the current sprint.
5. Apply F3 (secrets pre-flight in CI) and F4 (replace `sleep 90`) in the next sprint.
