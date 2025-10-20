# Quick Start: Google OAuth Setup

## TL;DR - Get Started in 5 Minutes

### 1. Get Google Credentials (2 minutes)
1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create OAuth 2.0 Client ID
3. Add redirect URI: `http://localhost:5000/api/auth/google-callback`
4. Copy Client ID and Client Secret

### 2. Configure Your App (1 minute)
```bash
cd InventoryX.Presentation
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

### 3. Run Your App (1 minute)
```bash
dotnet run
```

### 4. Test It (1 minute)

**From your SPA:**
```javascript
// Redirect to Google login
window.location.href = 'http://localhost:5000/api/auth/external-login?provider=Google&returnUrl=' + 
  encodeURIComponent(window.location.origin + '/auth/callback');
```

**Or test with Swagger:**
1. Navigate to `http://localhost:5000/swagger`
2. Find `/api/auth/external-login` endpoint
3. Execute with: `{"provider": "Google", "returnUrl": "http://localhost:5173"}`

## Available Endpoints

### 🔐 Password Login
```http
POST /api/auth/login?useCookies=true
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

### 🔐 Google OAuth Login
```http
POST /api/auth/external-login
Content-Type: application/json

{
  "provider": "Google",
  "returnUrl": "http://localhost:5173/auth/callback"
}
```

### 📝 Register New User
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "name": "John Doe"
}
```

### 🚪 Logout
```http
POST /api/auth/logout
```

### ✅ Check Auth Status
```http
GET /api/auth/pingauth
```

## How It Works

### Traditional Login Flow
```
SPA → POST /api/auth/login → Cookie Set → Authenticated
```

### Google OAuth Flow
```
1. SPA → GET/POST /api/auth/external-login (with returnUrl parameter)
2. Backend → Redirect to Google OAuth
3. User → Authenticates with Google
4. Google → Redirects to Backend /api/auth/google-callback
5. Backend → Creates/Signs In User + Sets Cookie
6. Backend → Redirects to SPA returnUrl
7. SPA → Receives redirect (already authenticated with cookie)
```

## Key Features

✅ **Dual Authentication**: Password or Google OAuth  
✅ **Automatic User Creation**: New users created on first Google login  
✅ **Email Auto-Confirmation**: Google users don't need email verification  
✅ **Cookie-Based Auth**: Secure, HttpOnly cookies for SPA  
✅ **CORS Configured**: Ready for cross-origin requests  
✅ **OpenIddict Integration**: Full OAuth 2.0 support  

## Configuration Files Modified

- ✅ `IdentityApiExtensions.cs` - Added Google OAuth endpoints
- ✅ `DependencyInjection.cs` - Configured Google authentication
- ✅ `appsettings.json` - Added Google credentials section
- ✅ `AppDbContext.cs` - OpenIddict tables (already present)

## Next Steps

1. **Set up Google credentials** (see GOOGLE_OAUTH_SETUP.md)
2. **Implement SPA integration** (see SPA_AUTH_EXAMPLE.tsx)
3. **Test the flow** with your frontend
4. **Deploy to production** with proper HTTPS and credentials

## Troubleshooting

**Problem**: "Google ClientId not configured"  
**Solution**: Set user secrets or update appsettings.json

**Problem**: CORS errors  
**Solution**: Add your SPA origin to `Frontend:AllowedOrigins` in appsettings.json

**Problem**: Cookie not set  
**Solution**: Ensure `credentials: 'include'` in fetch requests

**Problem**: Redirect URI mismatch  
**Solution**: Verify callback URL in Google Console matches exactly: `http://localhost:5000/api/auth/google-callback`

**Problem**: Google redirects to frontend instead of backend  
**Solution**: This has been fixed. The `/external-login` endpoint now properly configures Google to redirect to the backend `/google-callback` first, then the backend redirects to your frontend `returnUrl`

## Security Notes

⚠️ **Never commit secrets to Git**  
⚠️ **Use HTTPS in production**  
⚠️ **Rotate credentials regularly**  
⚠️ **Validate redirect URLs**  

## Support

For detailed documentation, see:
- `GOOGLE_OAUTH_SETUP.md` - Complete setup guide
- `SPA_AUTH_EXAMPLE.tsx` - React integration examples
