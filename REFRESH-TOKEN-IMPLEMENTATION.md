# Refresh Token Implementation

## Overview
Implemented a refresh token mechanism to address JWT token expiration issues. The system now uses short-lived access tokens (60 minutes) with long-lived refresh tokens (30 days).

## Changes Made

### Backend (.NET)

#### New Files
- `Models/RefreshToken.cs` - Model for refresh token data
- `Services/RefreshTokenStore.cs` - In-memory store for refresh tokens (thread-safe using ConcurrentDictionary)

#### Modified Files
- `Services/JwtTokenService.cs`
  - Changed token lifetime from 7 days to configurable minutes (default 60)
  - Added `steamId` claim for easier token validation

- `Controllers/AuthController.cs`
  - Added `IRefreshTokenStore` dependency
  - Updated `Callback()` to return refresh token in URL
  - Updated `GetToken()` to return `AuthResultDto` with refresh token
  - Added `Refresh()` endpoint (POST `/auth/steam/refresh`) to exchange refresh token for new tokens
  - Updated `Logout()` to revoke refresh tokens
  - Added request models: `RefreshTokenRequest`, `LogoutRequest`

- `Dto/Output/AuthResultDto.cs`
  - Added `refreshToken` property

- `Extensions/ServiceRegistrationExtensions.cs`
  - Registered `IRefreshTokenStore` as singleton service

- `appsettings.json`
  - Added `AccessTokenLifetimeMinutes: 60`
  - Added `RefreshTokenLifetimeDays: 30`

### Frontend (Angular)

#### New Files
- `services/auth-api.service.ts` - Service for authentication API calls (refresh, logout)

#### Modified Files
- `services/auth.service.ts`
  - Added `refreshToken` signal
  - Added `setRefreshToken()` method
  - Added `setTokens()` method
  - Updated `clear()` to remove refresh token from localStorage

- `models/auth-result.dto.ts`
  - Added `refreshToken` property

- `interceptors/auth-token.interceptor.ts`
  - Added automatic token refresh on 401 errors
  - Retry failed requests with new access token
  - Clear tokens and redirect to home on refresh failure
  - Prevent concurrent refresh requests with `isRefreshing` flag

- `app.component.ts`
  - Updated to handle `refreshToken` query parameter from callback
  - Updated to use `setTokens()` method

- `components/profile-widget/profile-widget.ts`
  - Updated `logout()` to call backend API with refresh token

- `components/login-button/login-button.component.ts`
  - Updated `logout()` to call backend API with refresh token

## How It Works

### Login Flow
1. User authenticates via Steam OpenID
2. Backend creates both access token (60 min) and refresh token (30 days)
3. Both tokens are returned to frontend
4. Frontend stores both in localStorage

### Token Refresh Flow
1. API request fails with 401 (token expired)
2. Interceptor catches error and calls `/auth/steam/refresh` with refresh token
3. Backend validates refresh token and issues new access + refresh tokens
4. Old refresh token is revoked
5. Interceptor retries original request with new access token
6. If refresh fails, user is logged out and redirected to home

### Logout Flow
1. User clicks logout
2. Frontend sends refresh token to backend
3. Backend revokes the refresh token
4. Frontend clears all tokens from localStorage

## Security Features
- Short-lived access tokens (60 minutes) minimize exposure
- Refresh tokens are single-use (rotated on each refresh)
- Refresh tokens can be revoked (logout, password change)
- Expired refresh tokens are automatically cleaned up
- In-memory store is thread-safe (uses ConcurrentDictionary)

## Configuration
Adjust token lifetimes in `appsettings.json`:
```json
"Jwt": {
  "AccessTokenLifetimeMinutes": 60,
  "RefreshTokenLifetimeDays": 30
}
```

## Future Enhancements
- Replace in-memory store with database for production (tokens lost on restart)
- Add refresh token fingerprinting for additional security
- Add rate limiting on refresh endpoint
- Add token revocation list for compromised tokens
- Add refresh token usage tracking and anomaly detection
