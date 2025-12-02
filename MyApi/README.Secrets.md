# Secrets and local configuration (short guide)

This document explains how to manage sensitive configuration (client IDs, client secrets, API keys) for the `MyApi` project during local development and deployment. Do NOT commit real secrets to the repository.

Purpose
- Keep secrets out of source control while allowing the application to load configuration in Development and Production environments.

Prerequisites
- .NET 9 SDK installed
- PowerShell/Bash/Terminal for setting environment variables or using `dotnet user-secrets`

Recommended approaches (ordered by typical usage)

1) dotnet user-secrets (recommended for local development)

- Initialize user-secrets for the project (run once in `MyApi` project folder):
  `dotnet user-secrets init`

- Set secrets:
  `dotnet user-secrets set "Withings:ClientId" "your-client-id"`
  `dotnet user-secrets set "Withings:ClientSecret" "your-client-secret"`
  `dotnet user-secrets set "Withings:State" "optional_state_value"`

- ASP.NET Core will load these when `ASPNETCORE_ENVIRONMENT` is `Development`.

2) Environment variables (recommended for CI / production)

- Use double-underscore `__` to represent nested configuration keys.

  Linux / macOS (bash/zsh):
  `export Withings__ClientId="your-client-id"`
  `export Withings__ClientSecret="your-client-secret"`

  Windows PowerShell:
  `$env:Withings__ClientId = 'your-client-id'`
  `$env:Withings__ClientSecret = 'your-client-secret'`

- Environment variables are applied before appsettings files and are suitable for containerized or cloud deployments.

3) File-based local overrides (only when ignored by git)

- You may create `MyApi/appsettings.Secrets.json` or update `appsettings.Development.json` with secret values for quick local testing, but ensure such files are added to `.gitignore` and never committed.

Configuration priority (highest ? lowest)
- Command-line arguments
- Environment variables
- User secrets (when in Development)
- appsettings.{Environment}.json
- appsettings.json

Example Withings settings (appsettings.example.json)
- The repository contains `appsettings.example.json` with the following keys used by the app:
  - `Withings:ClientId`
  - `Withings:ClientSecret`
  - `Withings:State` (optional)
  - `Withings:AccountUrl`
  - `Withings:WbsApiUrl`
  - `Withings:CallbackUri`

Troubleshooting
- Secrets not picked up in Development: confirm `ASPNETCORE_ENVIRONMENT` is `Development` and that `dotnet user-secrets init` was run in the project folder.
- Environment variables ignored in containers: confirm variables are passed into the container runtime (Docker `-e` or compose `environment` section).
- Wrong key name: nested keys require `:` in JSON or `__` in environment variables.

Security best practices
- Do not commit secrets to git.
- Use a managed secret store for production (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
- Restrict access to secret stores and rotate credentials regularly.
- Grant least privilege to API credentials.

Further reading
- Microsoft docs: Configuration in ASP.NET Core
- Microsoft docs: Secret Manager tool

If you need help wiring a specific secret provider (Key Vault, AWS Secrets Manager, etc.), add that request to the project issues or open a new ticket.
