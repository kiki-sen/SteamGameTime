# Development Environment Setup

## Steam API Configuration

The Steam API key is **not** stored in the repository for security reasons. To set up your development environment:

### Option 1: appsettings.Development.json (Recommended)

1. Copy the template file:
   ```bash
   cp "Steam API/appsettings.Development.json.template" "Steam API/appsettings.Development.json"
   ```

2. Edit `appsettings.Development.json` and replace `YOUR_STEAM_API_KEY_HERE` with your actual Steam API key.

3. The file is automatically ignored by git, so your API key won't be committed.

### Option 2: User Secrets (Alternative)

You can also use .NET User Secrets for storing the API key:

1. Navigate to the Steam API project directory:
   ```bash
   cd "Steam API"
   ```

2. Initialize user secrets:
   ```bash
   dotnet user-secrets init
   ```

3. Set your Steam API key:
   ```bash
   dotnet user-secrets set "Steam:ApiKey" "YOUR_STEAM_API_KEY_HERE"
   ```

### Option 3: Environment Variables

Set the Steam API key as an environment variable:

**Windows (PowerShell):**
```powershell
$env:Steam__ApiKey = "YOUR_STEAM_API_KEY_HERE"
```

**Windows (Command Prompt):**
```cmd
set Steam__ApiKey=YOUR_STEAM_API_KEY_HERE
```

**Linux/macOS:**
```bash
export Steam__ApiKey="YOUR_STEAM_API_KEY_HERE"
```

## Getting a Steam API Key

1. Go to [Steam Web API Key page](https://steamcommunity.com/dev/apikey)
2. Sign in with your Steam account
3. Fill out the form with your domain (you can use `localhost` for development)
4. Copy the generated API key

## Testing the Configuration

Run the application in development mode:
```bash
dotnet run --environment Development
```

The application should start without errors if the API key is properly configured.

## Security Notes

- **Never** commit API keys to version control
- The `appsettings.Development.json` file is automatically ignored by git
- Use different API keys for development, staging, and production environments
- Consider using Azure Key Vault or similar services for production secrets