# Observability Design -- The Augmented Craftsman v1

**Approach**: Structured JSON logs via Serilog, viewed through Fly.io's `flyctl logs`
**No external SaaS**: No Datadog, no Grafana Cloud, no ELK. Proportional to a personal blog.

---

## 1. Observability Stack

| Layer | Tool | Purpose |
|-------|------|---------|
| Structured Logging | Serilog + Serilog.Formatting.Compact | JSON logs to stdout |
| Log Viewing | Fly.io `flyctl logs` | Live tail and search logs |
| Health Checks | ASP.NET Health Checks middleware | Liveness and readiness probes |
| Request Tracing | Correlation ID middleware | Trace requests across log entries |
| Metrics | None (v1) | Add when traffic justifies it |
| Distributed Tracing | None (v1) | Single service, no need |

---

## 2. Structured Logging with Serilog

### Configuration

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TacBlog.Api")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

### Log Level Strategy

| Source | Level | Rationale |
|--------|-------|-----------|
| Application code | `Information` | Business events: post created, published, login |
| ASP.NET framework | `Warning` | Suppress routine request logs, keep errors |
| EF Core general | `Warning` | Suppress routine ORM logs |
| EF Core SQL commands | `Information` | Log actual SQL queries for debugging |
| Startup/shutdown | `Information` | Configuration validation, migration status |
| Errors | `Error` | Exceptions, failed operations |
| Security events | `Warning` | Failed logins, locked accounts |

### Log Output Format (CompactJsonFormatter)

Each log entry is a single JSON line written to stdout. Fly.io captures stdout automatically via `flyctl logs`.

```json
{"@t":"2026-03-05T10:00:00.123Z","@mt":"Post created","PostId":"a1b2c3d4","Slug":"tdd-is-not-about-testing","CorrelationId":"req-abc123","@l":"Information"}
{"@t":"2026-03-05T10:00:01.456Z","@mt":"Post published","PostId":"a1b2c3d4","CorrelationId":"req-def456","@l":"Information"}
{"@t":"2026-03-05T10:00:02.789Z","@mt":"Login failed for {Email}","Email":"attacker@evil.com","CorrelationId":"req-ghi789","@l":"Warning"}
```

---

## 3. Log Events Catalog

### Business Events (Information)

| Event | Properties | When |
|-------|-----------|------|
| `Post created` | PostId, Slug, Title | CreatePost handler succeeds |
| `Post published` | PostId, Slug | PublishPost handler succeeds |
| `Post updated` | PostId, Slug | UpdatePost handler succeeds |
| `Post deleted` | PostId | DeletePost handler succeeds |
| `Tag created` | TagId, Name | CreateTag handler succeeds |
| `Tag renamed` | TagId, OldName, NewName | RenameTag handler succeeds |
| `Tag deleted` | TagId | DeleteTag handler succeeds |
| `Image uploaded` | Url, FileName, SizeBytes | UploadImage handler succeeds |
| `Login succeeded` | Email | Login handler succeeds |
| `Vercel deploy hook triggered` | StatusCode | After post publish |

### Security Events (Warning)

| Event | Properties | When |
|-------|-----------|------|
| `Login failed` | Email, Reason | Invalid credentials |
| `Account locked` | Email, LockedUntil | 5 failed attempts |
| `Unauthorized access attempt` | Path, Method | Request without valid JWT to protected endpoint |

### Error Events (Error)

| Event | Properties | When |
|-------|-----------|------|
| `ImageKit upload failed` | FileName, Error | ImageKit API error |
| `Database operation failed` | Operation, Error | EF Core exception |
| `Vercel deploy hook failed` | StatusCode, Error | Hook returns non-2xx |
| `Unhandled exception` | ExceptionType, Message, StackTrace | Global exception handler |

### Startup Events (Information)

| Event | Properties | When |
|-------|-----------|------|
| `Application starting` | Environment, Version | Application boot |
| `Configuration validated` | -- | All required env vars present |
| `Database migration applied` | MigrationName | EF Core migration runs |
| `Application started` | Url, Port | Kestrel listening |
| `Application stopping` | -- | Graceful shutdown |

---

## 4. Correlation ID

Every HTTP request gets a unique correlation ID that flows through all log entries for that request. This enables tracing a single request across multiple log lines.

### Middleware

```csharp
// CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

### Usage

- Incoming requests with `X-Correlation-Id` header: use the provided value
- Incoming requests without the header: generate a short ID (12 chars)
- Response always includes `X-Correlation-Id` header
- All log entries within the request include `CorrelationId` property

---

## 5. Health Checks

### Endpoints

| Path | Purpose | Auth | Checks |
|------|---------|------|--------|
| `/health` | Liveness probe | Public | App is running and can respond |
| `/health/ready` | Readiness probe | Public | App is running AND database is reachable |

### Implementation

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: ["ready"]);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false // Liveness: no dependency checks
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Health Check Response

```json
// GET /health
{ "status": "healthy" }

// GET /health/ready
{
  "status": "healthy",
  "checks": {
    "postgresql": "healthy"
  }
}
```

### Docker HEALTHCHECK

The Dockerfile uses `/health` (liveness) for the Docker health check. Fly.io also checks health via the `[checks]` section in `fly.toml` and restarts unhealthy machines.

---

## 6. Request Logging

Serilog's request logging middleware replaces ASP.NET's verbose request logging with a single structured log entry per request.

```csharp
// Program.cs
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});
```

### Request Log Entry

```json
{
  "@t": "2026-03-05T10:00:00.123Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
  "RequestMethod": "POST",
  "RequestPath": "/api/posts",
  "StatusCode": 201,
  "Elapsed": 45.2,
  "RequestHost": "api.theaugmentedcraftsman.christianborrello.dev",
  "UserAgent": "Mozilla/5.0...",
  "CorrelationId": "req-abc123",
  "@l": "Information"
}
```

---

## 7. Exception Handling

### Global Exception Handler

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        Log.Error(exception, "Unhandled exception on {Method} {Path}",
            context.Request.Method, context.Request.Path);

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
    });
});
```

### Rules

- Never expose stack traces or internal details in API responses
- Always log the full exception with stack trace to Serilog
- Return a generic error message to the client
- Include the correlation ID in the response header so the admin can search logs

---

## 8. NuGet Packages

| Package | Purpose |
|---------|---------|
| `Serilog.AspNetCore` | Serilog integration with ASP.NET |
| `Serilog.Formatting.Compact` | CompactJsonFormatter for structured JSON output |
| `Serilog.Enrichers.Environment` | Enrich with machine name, environment (optional) |
| `AspNetCore.HealthChecks.NpgSql` | PostgreSQL health check |

---

## 9. Log Viewing with Fly.io

Fly.io captures all stdout/stderr output from the machine. Access logs via `flyctl logs` CLI command.

- **Live tail**: `flyctl logs` streams logs in real-time
- **Search**: Pipe to `grep` or `jq` for structured JSON filtering
- **Historical**: `flyctl logs` shows recent log history

### Common Search Patterns

```bash
# All errors
flyctl logs | grep '"@l":"Error"'

# Specific request by correlation ID
flyctl logs | grep 'req-abc123'

# Login failures
flyctl logs | grep '"Login failed"'

# Slow requests (parse Elapsed with jq)
flyctl logs | jq 'select(.Elapsed > 500)'

# Post operations
flyctl logs | grep '"PostId"'

# Deployment health
flyctl logs | grep '"Application starting"'
```

---

## 10. What Is NOT Included (and Why)

| Capability | Status | When to Add |
|-----------|--------|-------------|
| APM (Application Performance Monitoring) | Skipped | When performance issues are suspected |
| Distributed tracing (OpenTelemetry) | Skipped | When adding a second service |
| Custom metrics (Prometheus) | Skipped | When traffic exceeds hobby tier |
| External log aggregation (ELK, Loki) | Skipped | When `flyctl logs` is insufficient |
| Alerting (PagerDuty, OpsGenie) | Skipped | See monitoring-alerting.md for lightweight approach |
| Error tracking (Sentry) | Skipped | When error volume justifies a dedicated tool |

This is a personal blog with a single admin user. The observability stack is proportional to the problem. Every item in the "Skipped" column has a clear trigger condition for when it becomes necessary.
