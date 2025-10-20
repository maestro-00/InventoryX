# Google OAuth Setup Guide

This guide explains how to set up Google OAuth authentication for your InventoryX SPA application.

## Overview

The application now supports two authentication methods:
1. **Traditional Password Authentication** - Using ASP.NET Core Identity with email/password
2. **Google OAuth** - Using Google as an external authentication provider

## Prerequisites

1. A Google Cloud Console account
2. Your application running on a known URL (e.g., `http://localhost:5173` for development)

## Step 1: Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Configure the OAuth consent screen if prompted:
   - Choose **External** for user type (unless you have a Google Workspace)
   - Fill in the required fields (App name, User support email, Developer contact)
   - Add scopes: `email`, `profile`, `openid`
   - Add test users if in testing mode
6. Create OAuth 2.0 Client ID:
   - Application type: **Web application**
   - Name: `InventoryX` (or your preferred name)
   - Authorized JavaScript origins:
     - `http://localhost:5173` (your SPA URL)
     - `https://yourdomain.com` (production URL)
   - Authorized redirect URIs:
     - `http://localhost:5000/api/auth/google-callback` (your API URL)
     - `https://api.yourdomain.com/api/auth/google-callback` (production API URL)
7. Click **Create** and copy the **Client ID** and **Client Secret**

## Step 2: Configure Application Settings

### Option A: Using appsettings.json (Development Only)

Update `/InventoryX.Presentation/appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET_HERE"
    }
  }
}
```

### Option B: Using User Secrets (Recommended for Development)

```bash
cd InventoryX.Presentation
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID_HERE"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET_HERE"
```

### Option C: Using Environment Variables (Production)

Set the following environment variables:
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`

## Step 3: API Endpoints

The following endpoints are available for authentication:

### 1. Traditional Login (Password)
```http
POST /api/auth/login?useCookies=true
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

### 2. Google OAuth Login (For SPA)

**Step 1: Initiate Google Login**

Simply redirect the user to the external login endpoint with query parameters:
```http
GET /api/auth/external-login?provider=Google&returnUrl=http://localhost:5173/auth/callback
```

Or use POST with JSON body:
```http
POST /api/auth/external-login
Content-Type: application/json

{
  "provider": "Google",
  "returnUrl": "http://localhost:5173/auth/callback"
}
```

This will redirect the user to Google's login page.

**Step 2: Google Authenticates and Redirects to Backend**

After the user authenticates with Google, Google will redirect to:
```
GET /api/auth/google-callback
```

This backend endpoint will:
- Retrieve the external login information from Google
- Check if the user exists in the database
- If not, create a new user account with the Google email
- Automatically confirm the email for Google users
- Sign in the user with a cookie
- **Redirect to your SPA's return URL** (the `returnUrl` you provided in Step 1)

**Step 3: Handle Callback in Your SPA**

Your SPA will receive the redirect at the `returnUrl` you specified (e.g., `http://localhost:5173/auth/callback`). At this point, the authentication cookie is already set, and you can verify the user is authenticated.

### 3. Register (Traditional)
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "name": "John Doe"
}
```

### 4. Logout
```http
POST /api/auth/logout
```

### 5. Check Authentication Status
```http
GET /api/auth/pingauth
```

## Step 4: SPA Implementation

### React/Vue/Angular Example

```typescript
// 1. Initiate Google Login
function loginWithGoogle() {
  // Redirect to the external login endpoint
  // The backend will handle the OAuth flow and redirect back to your callback URL
  const returnUrl = encodeURIComponent(window.location.origin + '/auth/callback');
  window.location.href = `/api/auth/external-login?provider=Google&returnUrl=${returnUrl}`;
}

// 2. Handle callback in your SPA (at /auth/callback route)
// This route is called AFTER the backend completes authentication
async function handleAuthCallback() {
  // The cookie is already set by the backend after successful authentication
  // Verify the user is authenticated
  const response = await fetch('/api/auth/pingauth', {
    credentials: 'include'
  });
  
  if (response.ok) {
    const data = await response.json();
    console.log('Logged in as:', data.email);
    // Redirect to dashboard or home page
    window.location.href = '/dashboard';
  } else {
    // Handle authentication error
    console.error('Authentication failed');
    window.location.href = '/login?error=auth_failed';
  }
}

// 3. Traditional login
async function loginWithPassword(email: string, password: string) {
  const response = await fetch('/api/auth/login?useCookies=true', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: JSON.stringify({ email, password })
  });
  
  if (response.ok) {
    // Login successful
    window.location.href = '/dashboard';
  } else {
    // Handle error
    const error = await response.json();
    console.error('Login failed:', error);
  }
}

// 4. Logout
async function logout() {
  await fetch('/api/auth/logout', {
    method: 'POST',
    credentials: 'include'
  });
  window.location.href = '/login';
}
```

## Step 5: CORS Configuration

Ensure your CORS policy allows credentials:

```csharp
// Already configured in DependencyInjection.cs
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); // Important for cookies
    });
});
```

## Step 6: Cookie Configuration

The application is configured to use secure cookies for SPA:

```csharp
// Already configured in DependencyInjection.cs
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});
```

## Testing the Flow

1. Start your API server (e.g., `http://localhost:5000`)
2. Start your SPA (e.g., `http://localhost:5173`)
3. Navigate to the login page in your SPA
4. Click "Sign in with Google" button (which calls `loginWithGoogle()` function)
5. You'll be redirected to Google's login page
6. After successful authentication with Google:
   - Google redirects to: `http://localhost:5000/api/auth/google-callback`
   - Backend processes the authentication and sets the cookie
   - Backend redirects to: `http://localhost:5173/auth/callback`
7. Your SPA's `/auth/callback` route handles the final redirect
8. The authentication cookie is already set and ready to use

## Troubleshooting

### Issue: "Redirect URI mismatch"
- Ensure the redirect URI in Google Cloud Console exactly matches your backend callback URL
- For development: `http://localhost:5000/api/auth/google-callback` (adjust port as needed)
- For production: `https://api.yourdomain.com/api/auth/google-callback`
- The domain and port must match your API server, NOT your frontend
- Do NOT add your frontend URL to the Google Cloud Console redirect URIs

### Issue: CORS errors
- Verify that your SPA origin is listed in `appsettings.json` under `Frontend:AllowedOrigins`
- Ensure `AllowCredentials()` is set in CORS configuration

### Issue: Cookie not being set
- Check that your API is running on HTTPS (or use `CookieSecurePolicy.SameAsRequest` for development)
- Verify `SameSite = None` is set for cross-origin requests
- Ensure your frontend and backend are on allowed origins in CORS configuration

### Issue: Google redirects to frontend instead of backend
- This issue has been fixed in the latest version
- The `/external-login` endpoint now properly configures the OAuth redirect to go to the backend `/google-callback` endpoint first
- After backend authentication, it redirects to your frontend `returnUrl`

### Issue: "Google ClientId not configured"
- Verify that the Google credentials are properly set in appsettings.json or user secrets
- Check that the configuration section name is exactly `Authentication:Google:ClientId`

## Security Considerations

1. **Never commit secrets to source control** - Use user secrets or environment variables
2. **Use HTTPS in production** - Required for secure cookies
3. **Validate redirect URLs** - Ensure returnUrl is from your domain
4. **Keep credentials secure** - Rotate secrets regularly
5. **Monitor OAuth usage** - Check Google Cloud Console for suspicious activity

## Database Migrations

The application uses OpenIddict for OAuth support. If you haven't run migrations yet:

```bash
cd InventoryX.Presentation
dotnet ef migrations add AddGoogleOAuth --project ../InventoryX.Infrastructure
dotnet ef database update
```

## Additional Resources

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
