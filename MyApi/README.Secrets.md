Secrets and local configuration (short guide)

This project keeps non-sensitive configuration in `appsettings.json`. Do NOT commit real secrets (client IDs, client secrets, API keys) to the repository.

Recommended local development approaches

1) dotnet user-secrets (recommended for local dev)

- Initialize in the project directory (runs once):
  dotnet user-secrets init

- Set secrets:
  dotnet user-secrets set "Withings:ClientId" "your-client-id"
  dotnet user-secrets set "Withings:ClientSecret" "your-client-secret"
  dotnet user-secrets set "Withings:State" "optional_state"

ASP.NET Core will load these when the environment is Development.

2) Environment variables (good for CI/prod)

- Use double-underscore to represent nested keys. Example:
  (Linux/macOS)
  export Withings__ClientId="your-client-id"
  export Withings__ClientSecret="your-client-secret"

  (Windows PowerShell)
  $env:Withings__ClientId = 'your-client-id'
  $env:Withings__ClientSecret = 'your-client-secret'

3) File-based local overrides (not recommended unless ignored)

- You may create `MyApi/appsettings.Secrets.json` or `appsettings.Development.json` with secret values, but ensure such files are added to `.gitignore`.

Priority of configuration providers (highest to lowest):
- Command-line args
- Environment variables
- User secrets (when in Development)
- appsettings.{Environment}.json
- appsettings.json

Security tips
- Rotate credentials regularly.
- Use a managed secret store for production (Azure Key Vault, AWS Secrets Manager, etc.).
- Limit who can view secrets and use least privilege for API credentials.
