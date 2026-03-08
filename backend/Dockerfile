# ==============================================================================
# Stage 1: Restore NuGet packages
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src

COPY src/TacBlog.Domain/TacBlog.Domain.csproj src/TacBlog.Domain/
COPY src/TacBlog.Application/TacBlog.Application.csproj src/TacBlog.Application/
COPY src/TacBlog.Infrastructure/TacBlog.Infrastructure.csproj src/TacBlog.Infrastructure/
COPY src/TacBlog.Api/TacBlog.Api.csproj src/TacBlog.Api/

RUN dotnet restore src/TacBlog.Api/TacBlog.Api.csproj

# ==============================================================================
# Stage 2: Build and publish
# ==============================================================================
FROM restore AS publish

COPY src/ src/

RUN dotnet publish src/TacBlog.Api/TacBlog.Api.csproj -c Release --no-restore -o /app/publish

# ==============================================================================
# Stage 3: Runtime (minimal image)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

RUN addgroup -S tacblog && adduser -S tacblog -G tacblog

WORKDIR /app
COPY --from=publish /app/publish .

USER tacblog

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TacBlog.Api.dll"]
