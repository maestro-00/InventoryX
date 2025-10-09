# Support

Thank you for using InventoryX! This document provides information on how to get help with the project.

## 📚 Documentation

Before seeking support, please check our documentation:

- **[README.md](README.md)** - Project overview, setup instructions, and basic usage
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Guidelines for contributing to the project
- **[API Documentation](https://localhost:7xxx/swagger)** - Interactive API documentation (when running locally)

## 🐛 Reporting Issues

If you've found a bug or issue:

1. **Search existing issues** to see if it's already been reported
2. If not found, **[create a new issue](../../issues/new/choose)**
3. Use the appropriate issue template:
   - **Bug Report** - For reporting bugs
   - **Feature Request** - For suggesting new features
   - **Question** - For asking questions

### What to Include in Bug Reports

- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, SQL Server version)
- Error messages or logs
- Screenshots (if applicable)

## 💬 Getting Help

### GitHub Discussions

For questions, ideas, and general discussions:
- Visit our [GitHub Discussions](../../discussions)
- Search existing discussions before creating a new one
- Be respectful and follow our [Code of Conduct](CODE_OF_CONDUCT.md)

### Common Questions

#### Setup and Installation

**Q: I'm getting database connection errors**
- Verify your connection string in `appsettings.json`
- Ensure SQL Server is running
- Check that the database exists (run `dotnet ef database update`)

**Q: How do I run the project?**
- See the [Getting Started](README.md#-getting-started) section in the README
- Ensure you have .NET 8.0 SDK installed
- Run `dotnet restore` and `dotnet run --project InventoryX.Presentation`

**Q: Tests are failing**
- Run `dotnet restore` to ensure all dependencies are installed
- Check that you're using .NET 8.0 or later
- Verify no other instance is using the test database

#### Development

**Q: How do I add a new feature?**
- Read our [Contributing Guidelines](CONTRIBUTING.md)
- Follow the Clean Architecture structure
- Add appropriate tests
- Submit a pull request

**Q: What coding standards should I follow?**
- See the [Coding Standards](CONTRIBUTING.md#coding-standards) section
- Follow C# conventions and Clean Architecture principles
- Ensure tests pass before submitting

**Q: How do I add a database migration?**
```bash
cd InventoryX.Presentation
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

#### API Usage

**Q: How do I authenticate with the API?**
- Register via `/api/auth/register`
- Login via `/api/auth/login`
- Use the returned authentication cookie for subsequent requests

**Q: Where can I find API documentation?**
- Run the application and navigate to `/swagger`
- Interactive documentation with all endpoints and models

## 🔍 Troubleshooting

### Common Issues and Solutions

#### Build Errors

```bash
# Clear build artifacts and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Database Issues

```bash
# Reset database
dotnet ef database drop
dotnet ef database update
```

#### Port Already in Use

```bash
# Check what's using the port
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux/Mac

# Kill the process or change the port in launchSettings.json
```

## 📞 Contact

### For Security Issues

**Do not report security vulnerabilities through public issues.**

Please see our [Security Policy](SECURITY.md) for instructions on reporting security vulnerabilities.

### For Other Inquiries

- **General questions**: Use [GitHub Discussions](../../discussions)
- **Bug reports**: Create an [issue](../../issues/new/choose)
- **Feature requests**: Create an [issue](../../issues/new/choose)
- **Contributing**: See [CONTRIBUTING.md](CONTRIBUTING.md)

## 🤝 Community

### Ways to Contribute

Even if you're not ready to contribute code, you can help by:

- ⭐ Starring the repository
- 🐛 Reporting bugs
- 💡 Suggesting features
- 📖 Improving documentation
- 💬 Helping others in discussions
- 🔄 Sharing the project

### Code of Conduct

This project follows a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold this code.

## 📋 Resources

### Learning Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR)

### Related Projects

- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [MediatR](https://github.com/jbogard/MediatR)
- [AutoMapper](https://github.com/AutoMapper/AutoMapper)

## ⏱️ Response Times

Please note that this is an open-source project maintained by volunteers. Response times may vary:

- **Bug reports**: We aim to acknowledge within 48 hours
- **Feature requests**: We aim to review within 1 week
- **Pull requests**: We aim to review within 1 week
- **Questions**: Community members typically respond within 24-48 hours

## 🙏 Thank You

Thank you for using InventoryX! Your feedback and contributions help make this project better for everyone.

---

**Need immediate help?** Check our [FAQ](../../discussions/categories/q-a) or start a [discussion](../../discussions/new).
