# Development

## Prerequisites and configuration

Use a .NET 10 SDK compatible with `src/Directory.build.props`, PostgreSQL, and access to the configured NuGet feeds. Versions are managed centrally in `src/Directory.Packages.props`. Do not copy credentials from `nuget.config`, appsettings, seed code or logs into instructions. Use local environment variables or a secure configuration source for secrets.

Required configuration includes `ConnectionStrings:DefaultConnection` and `JwtSettings` (`SecretKey`, `Issuer`, `Audience`, token expiration values). Environment-variable equivalents include `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `JwtSettings__Issuer`, `JwtSettings__Audience`. Check `Options/AuthTokenOption.cs` and option setup for other configured integrations; do not assume the building-block `Authentication` section replaces local `JwtSettings`.

## Build and run from solution root

```powershell
dotnet --info
dotnet restore feature-flag.sln
dotnet build feature-flag.sln -c Release --no-restore
dotnet run --project src/WebApi/WebApi.csproj --launch-profile http
```

The current http launch profile uses `http://localhost:5012` and a `scalar` launch URL. Startup performs database seeding before mapping middleware; use a disposable development database and review `DatabaseSeeder` before running. Startup does not itself prove migrations were applied. HTTPS redirection and OpenAPI settings can affect actual access; inspect current environment settings.

For an authorized model change with a local compatible EF tool, the source projects for a migration are:

```powershell
dotnet ef migrations add <MeaningfulName> --project src/Infrastructure --startup-project src/WebApi
```

Review generated migrations and database impact. Apply migrations only to the explicitly intended database with task authorization. No automatic migration or shared-database mutation is implied by documenting this command.

## Validation appropriate to a change

- Code/package changes: restore, Release build and relevant behavior checks. Confirm generated handler discovery, validation, DI lifetimes, EF translation and HTTP mapping when affected. No test project was present at this baseline; add focused tests for meaningful behavior when needed, not an empty testing ceremony.
- Endpoint changes: synchronize `docs/ts-contracts`, inspect OpenAPI from the running app when available, verify request binding, authorization, defaults, null handling and success/error responses. Test unauthenticated and forbidden cases when access changes.
- Contracts/docs only: validate relative links, source-aligned record fields, routes and complete endpoint coverage. Type-check contracts with an available TypeScript compiler, for example `tsc --noEmit --strict --target ES2020 --module ESNext docs/ts-contracts/*.ts` in a shell that expands the glob. No npm project or runtime dependency is required by these declaration-only contracts.

The root Dockerfile is currently stale (.NET 7 and a missing Framework project). Do not treat it as a working .NET 10 deployment recipe. Existing README SDK material is an [unimplemented proposal](specs/01-sdk-evaluation-proposal.specs.md).

Use the existing graph indexes plus source for exploration. Never invoke indexing as setup, validation or cleanup. At task completion identify repositories requiring the user's manual re-index.
