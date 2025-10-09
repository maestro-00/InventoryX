# Security Policy

## Supported Versions

We release patches for security vulnerabilities. The following versions are currently being supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

The InventoryX team takes security bugs seriously. We appreciate your efforts to responsibly disclose your findings, and will make every effort to acknowledge your contributions.

### How to Report a Security Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via one of the following methods:

1. **Email**: Send details to the project maintainers (contact information in the repository)
2. **GitHub Security Advisory**: Use GitHub's private vulnerability reporting feature
   - Go to the Security tab of this repository
   - Click "Report a vulnerability"
   - Fill out the form with details

### What to Include in Your Report

To help us better understand and resolve the issue, please include as much of the following information as possible:

- **Type of vulnerability** (e.g., SQL injection, XSS, authentication bypass)
- **Full paths of source file(s)** related to the vulnerability
- **Location of the affected source code** (tag/branch/commit or direct URL)
- **Step-by-step instructions** to reproduce the issue
- **Proof-of-concept or exploit code** (if possible)
- **Impact of the issue**, including how an attacker might exploit it
- **Any special configuration** required to reproduce the issue

### What to Expect

After you submit a report, you can expect:

1. **Acknowledgment**: We will acknowledge receipt of your vulnerability report within 48 hours
2. **Assessment**: We will investigate and assess the vulnerability within 5 business days
3. **Updates**: We will keep you informed about our progress
4. **Resolution**: We will work on a fix and coordinate the disclosure timeline with you
5. **Credit**: We will publicly acknowledge your responsible disclosure (unless you prefer to remain anonymous)

### Security Update Process

1. The security issue is received and assigned to a primary handler
2. The problem is confirmed and affected versions are determined
3. Code is audited to find any similar problems
4. Fixes are prepared for all supported releases
5. Fixes are released and security advisory is published

## Security Best Practices for Users

### Authentication & Authorization

- **Use strong passwords**: Ensure all user accounts have strong, unique passwords
- **Implement HTTPS**: Always use HTTPS in production environments
- **Secure cookie settings**: Configure secure cookie settings in production
- **Token management**: Properly manage and secure authentication tokens

### Database Security

- **Connection strings**: Never commit connection strings with credentials to version control
- **Use environment variables**: Store sensitive configuration in environment variables or secure vaults
- **Least privilege**: Database users should have minimum required permissions
- **Regular backups**: Maintain regular encrypted backups

### API Security

- **Rate limiting**: Implement rate limiting to prevent abuse
- **Input validation**: Always validate and sanitize user input
- **CORS configuration**: Configure CORS appropriately for your environment
- **API versioning**: Keep APIs versioned and maintain backward compatibility

### Deployment Security

- **Keep dependencies updated**: Regularly update NuGet packages and dependencies
- **Security scanning**: Use security scanning tools in your CI/CD pipeline
- **Environment separation**: Keep development, staging, and production environments separate
- **Logging and monitoring**: Implement comprehensive logging and monitoring

### Configuration Security

```json
// Example: Secure appsettings.json structure
{
  "ConnectionStrings": {
    "DefaultConnection": "Use environment variables or Azure Key Vault"
  },
  "Jwt": {
    "SecretKey": "Use environment variables or secure key management"
  }
}
```

## Known Security Considerations

### Current Security Features

- ✅ ASP.NET Identity for authentication and authorization
- ✅ HTTPS enforcement
- ✅ Input validation with data annotations
- ✅ SQL injection protection via Entity Framework Core parameterized queries
- ✅ CORS configuration support

### Recommended Additional Security Measures

For production deployments, consider implementing:

- **Rate limiting** for API endpoints
- **API key authentication** for service-to-service communication
- **JWT token authentication** as an alternative to cookie-based auth
- **Audit logging** for sensitive operations
- **Data encryption at rest** for sensitive information
- **Web Application Firewall (WAF)** for additional protection

## Security-Related Configuration

### Example Production Configuration

```csharp
// Program.cs - Security configurations
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// HTTPS enforcement
app.UseHttpsRedirection();
app.UseHsts();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```

## Vulnerability Disclosure Policy

- We request that you give us reasonable time to fix the vulnerability before public disclosure
- We will acknowledge your contribution in our security advisories (if desired)
- We do not currently offer a bug bounty program, but we deeply appreciate responsible disclosure

## Security Updates and Notifications

- Security updates will be released as patch versions (e.g., 1.0.1)
- Critical security issues will be announced via:
  - GitHub Security Advisories
  - Release notes
  - Project README

## Contact

For any security-related questions or concerns, please contact the project maintainers through the repository.

---

**Thank you for helping keep InventoryX and its users safe!**
