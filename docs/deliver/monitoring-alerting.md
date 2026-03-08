# Monitoring and Alerting -- The Augmented Craftsman v1

**Approach**: Lightweight monitoring proportional to a personal blog
**Tools**: Fly.io metrics, health check endpoints, free uptime monitoring
**No SaaS**: No Datadog, no New Relic, no PagerDuty

---

## 1. Monitoring Stack

| Layer | Tool | Cost |
|-------|------|------|
| Container metrics | Fly.io dashboard + `flyctl status` | Included |
| Health checks | ASP.NET Health Checks | Included |
| Uptime monitoring | UptimeRobot (free tier) or Cronitor (free) | Free |
| Log-based alerting | Manual log review via `flyctl logs` | Included |
| Frontend monitoring | Vercel Analytics (free tier) | Free |

---

## 2. Fly.io Built-in Monitoring

Fly.io provides machine-level metrics via dashboard and CLI:

| Metric | What It Shows | How to Check |
|--------|--------------|--------------|
| Machine status | Running, stopped, or failed | `flyctl status` |
| Memory usage | Machine RAM consumption | Fly.io dashboard |
| Logs | Application stdout/stderr | `flyctl logs` (live tail) |
| Deployment status | Current release health | `flyctl releases` |
| Machine restarts | Auto-restart events | `flyctl logs` (search for restart events) |

### Fly.io CLI Access

```bash
flyctl status          # Machine status and health
flyctl logs            # Live tail application logs
flyctl releases        # List recent deployments
flyctl ssh console     # SSH into running machine (emergency debugging)
```

Check `flyctl status` periodically (weekly is sufficient for a personal blog).

---

## 3. Health Check Endpoints

Defined in `observability-design.md`. Summary:

| Endpoint | Purpose | Probe Interval |
|----------|---------|---------------|
| `/health` | Liveness: app is running | Docker HEALTHCHECK every 30s |
| `/health/ready` | Readiness: app + database are healthy | UptimeRobot every 5 minutes |

### Health Check Behavior

```
Machine starts
  |
  +-- HEALTHCHECK polls /health every 30s
  |     |
  |     +-- 200? Machine is alive
  |     +-- Non-200 x3? Fly.io restarts machine
  |
  +-- UptimeRobot polls /health/ready every 5m
        |
        +-- 200? All systems healthy
        +-- Non-200? Email notification
```

---

## 4. Uptime Monitoring

### UptimeRobot Configuration (Free Tier)

| Monitor | URL | Interval | Alert |
|---------|-----|----------|-------|
| API Health | `https://api.theaugmentedcraftsman.christianborrello.dev/health/ready` | 5 minutes | Email |
| Frontend | `https://theaugmentedcraftsman.christianborrello.dev` | 5 minutes | Email |

**Setup**:
1. Create free account at uptimerobot.com
2. Add HTTP(s) monitor for each URL
3. Set alert contact to personal email
4. Alert on: down for 2 consecutive checks (10 minutes)

### What UptimeRobot Detects

| Scenario | Detection Time | Alert |
|----------|---------------|-------|
| API container crash | ~10 minutes (2 checks) | Email: "API Health is DOWN" |
| Database unreachable | ~10 minutes (2 checks) | Email: "API Health is DOWN" (readiness fails) |
| Fly.io outage | ~10 minutes (2 checks) | Email: "API Health is DOWN" |
| Vercel outage | ~10 minutes (2 checks) | Email: "Frontend is DOWN" |
| SSL certificate expiry | Detected by UptimeRobot | Email notification |

### What UptimeRobot Does NOT Detect

| Scenario | Why Not | Mitigation |
|----------|---------|------------|
| Slow API responses | UptimeRobot checks availability, not latency | Manual log review if users report slowness |
| Data corruption | Health check does not validate data integrity | Acceptance tests in CI catch logic errors |
| Broken publish workflow | Health check does not test business flows | Manual verification after publishing |
| High error rate | UptimeRobot checks one endpoint | Review error logs via `flyctl logs` periodically |

---

## 5. Alerting Strategy

### Alert Channels

| Priority | Channel | Example |
|----------|---------|---------|
| Service down | Email (UptimeRobot) | "API Health is DOWN" |
| Deployment failure | GitHub Actions notification (email) | "CI workflow failed on main" |
| Everything else | Manual log review | Check `flyctl logs` weekly |

### Alert Response Procedures

**API is DOWN (UptimeRobot alert):**

```
1. Check Fly.io machine status: `flyctl status`
2. Check Fly.io logs: `flyctl logs`
   - Migration failure? Fix migration, push new commit
   - Out of memory? Check for memory leaks in logs
   - Database down? Check Neon dashboard for PostgreSQL status
3. If machine is crash-looping: check the last deployment commit
4. Rollback: deploy previous release via flyctl
   flyctl deploy --image ghcr.io/<owner>/tacblog-api:<previous-sha>
5. Investigate root cause on a feature branch
```

**CI pipeline failure on main (GitHub notification):**

```
1. Check GitHub Actions run log
2. Identify failing stage (unit test? integration test? Docker build?)
3. Fix on a feature branch
4. Push fix to main
5. Verify deployment succeeds
```

**Frontend is DOWN (UptimeRobot alert):**

```
1. Check Vercel dashboard for deployment status
2. Check if last build succeeded
3. If build failed: check build logs for API connectivity issue
4. If Vercel outage: wait for Vercel status page resolution
5. Manual redeploy via Vercel dashboard if needed
```

---

## 6. Key Metrics to Watch

These are not automated dashboards. They are metrics to check manually when investigating issues.

### API Health Indicators

| Metric | Where to Check | Healthy Range |
|--------|---------------|---------------|
| Response time (p95) | `flyctl logs` (Elapsed field) | < 500ms |
| Error rate | `flyctl logs` (`"@l":"Error"` count) | < 1% of requests |
| Machine restarts | `flyctl logs` (restart events) | 0 per day |
| Memory usage | Fly.io dashboard | < 256MB |
| Database connection count | Neon dashboard | < 20 |

### Frontend Health Indicators

| Metric | Where to Check | Healthy Range |
|--------|---------------|---------------|
| Build time | Vercel dashboard | < 2 minutes |
| Build success rate | Vercel dashboard | 100% (static site) |
| Edge response time | Vercel Analytics | < 100ms (static) |

---

## 7. Incident Response

### Severity Levels

| Level | Definition | Response Time | Example |
|-------|-----------|---------------|---------|
| S1 - Critical | API completely unreachable | Within 1 hour | Container crash-loop, database down |
| S2 - Major | Specific feature broken | Within 4 hours | Image upload fails, login broken |
| S3 - Minor | Non-blocking issue | Next working session | Slow response times, log noise |

### Response Time Justification

This is a personal blog, not a commercial service. Readers see static HTML (unaffected by API downtime). The admin is the only user impacted by API issues. Response times reflect this reality:
- S1 within 1 hour because it means the admin cannot manage content
- S2 within 4 hours because a single feature is broken but others work
- S3 at convenience because it does not block any functionality

---

## 8. Deployment Monitoring

### Post-Deployment Checklist

After every deployment to production (automated via CI, verified manually if smoke test passes):

```
[ ] Health endpoint returns 200
[ ] `flyctl logs` shows "Application started" with correct version
[ ] No error entries in the first 5 minutes of logs
[ ] (If migration was applied) Logs show "Database migration applied"
```

### Deployment Failure Detection

| Signal | Source | Automated? |
|--------|--------|-----------|
| CI pipeline fails | GitHub Actions | Yes (email notification) |
| Smoke test fails | CI pipeline | Yes (pipeline reports failure) |
| Health check fails | Docker HEALTHCHECK | Yes (Fly.io restarts) |
| API unreachable | UptimeRobot | Yes (email notification) |

---

## 9. Database Recovery

### Failed EF Core Migration

If a migration fails during deployment, the application may crash-loop because the database schema does not match the expected model.

**Detection**: `flyctl logs` shows `Microsoft.EntityFrameworkCore` errors at startup, health check fails.

**Recovery procedure**:

```
1. Check which migration failed: `flyctl logs | grep "Migration"`
2. Connect to Neon database via psql or Neon console
3. Check `__EFMigrationsHistory` table for applied migrations
4. Options:
   a. Fix migration code, push new commit → CI deploys corrected version
   b. Manually apply/revert SQL via Neon console (emergency only)
   c. Rollback app to previous version: `flyctl deploy --image ghcr.io/<owner>/tacblog-api:<previous-sha>`
5. Verify: `curl https://api.theaugmentedcraftsman.christianborrello.dev/health/ready` returns 200
```

### Data Recovery

Neon provides point-in-time restore on the free tier (up to 7 days of history). If data corruption occurs:

```
1. Go to Neon dashboard → project → Branches
2. Create a new branch from a point-in-time before the corruption
3. Update the connection string in Fly.io secrets to point to the restored branch
4. Verify data integrity
5. When confirmed, optionally merge the restored branch back to main
```

### Prevention

- EF Core migrations run automatically at startup (`Database.Migrate()`)
- The CI pipeline catches migration issues via integration tests (Testcontainers runs the same migrations against a real PostgreSQL)
- Always test migrations locally before pushing: `dotnet ef database update`

---

## 10. Cost Summary

| Service | Tier | Monthly Cost |
|---------|------|-------------|
| Fly.io (API) | Free (3 shared VMs, 256MB) | $0 |
| Neon (PostgreSQL) | Free (0.5GB serverless) | $0 |
| Vercel (Frontend) | Free | $0 |
| UptimeRobot | Free (50 monitors) | $0 |
| ImageKit | Free (20GB storage + 20GB bandwidth/mo) | $0 |
| GitHub Actions | Free (2000 min/mo) | $0 |
| **Total** | | **$0/month** |

---

## 11. Evolution Path

When the blog grows beyond free tier limits, add these in order:

| Trigger | Action | Tool |
|---------|--------|------|
| Error volume > 10/day | Add error tracking | Sentry free tier |
| Traffic > 1000 req/day | Add request metrics | Prometheus + Grafana Cloud free tier |
| Multiple services | Add distributed tracing | OpenTelemetry + Jaeger |
| Latency SLO needed | Add latency monitoring | Grafana Cloud |
| Team grows > 1 | Add on-call rotation | PagerDuty or OpsGenie |

Each step has a clear trigger. Do not add complexity before the trigger condition is met.
