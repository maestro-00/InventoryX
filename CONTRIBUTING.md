# Contributing to InventoryX

First off, thank you for considering contributing to InventoryX! It's people like you that make InventoryX such a great tool.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Enhancements](#suggesting-enhancements)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (code snippets, screenshots, etc.)
- **Describe the behavior you observed and what you expected**
- **Include your environment details** (.NET version, OS, SQL Server version)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the suggested enhancement**
- **Explain why this enhancement would be useful**
- **List any alternative solutions you've considered**

### Your First Code Contribution

Unsure where to begin? Look for issues labeled:

- `good first issue` - Simple issues perfect for newcomers
- `help wanted` - Issues that need attention
- `bug` - Bug fixes are always welcome

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB, Express, or full version)
- Visual Studio 2022 / JetBrains Rider / VS Code
- Git

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork locally**
   ```bash
   git clone https://github.com/your-username/InventoryX.git
   cd InventoryX
   ```

3. **Add the upstream repository**
   ```bash
   git remote add upstream https://github.com/original-owner/InventoryX.git
   ```

4. **Create a branch** for your changes
   ```bash
   git checkout -b feature/your-feature-name
   ```

5. **Configure the database**
   - Update `appsettings.json` with your connection string
   - Run migrations: `dotnet ef database update`

6. **Restore dependencies**
   ```bash
   dotnet restore
   ```

7. **Build the solution**
   ```bash
   dotnet build
   ```

8. **Run tests** to ensure everything works
   ```bash
   dotnet test
   ```

## Coding Standards

### General Guidelines

- Follow **Clean Architecture** principles
- Write **clean, readable, and maintainable** code
- Keep methods **small and focused** (Single Responsibility Principle)
- Use **meaningful names** for variables, methods, and classes
- Add **XML documentation comments** for public APIs
- Write **unit tests** for new features and bug fixes

### C# Coding Conventions

Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

- Use **PascalCase** for class names, method names, and properties
- Use **camelCase** for local variables and parameters
- Use **_camelCase** for private fields (with underscore prefix)
- Use **meaningful names** - avoid abbreviations
- Place opening braces on a **new line**
- Use **explicit access modifiers** (public, private, etc.)

### Project Structure

Maintain the Clean Architecture layers:

```
InventoryX.Domain/          # Entities, value objects, domain logic
InventoryX.Application/     # Use cases, DTOs, interfaces, CQRS
InventoryX.Infrastructure/  # Data access, external services
InventoryX.Presentation/    # Controllers, API configuration
```

### Dependency Rules

- **Domain** has no dependencies
- **Application** depends only on Domain
- **Infrastructure** depends on Application and Domain
- **Presentation** depends on all layers

### CQRS Pattern

- Place **Commands** in `Application/Commands/`
- Place **Queries** in `Application/Queries/`
- Place **Handlers** in `Application/Commands/RequestHandlers/` or `Application/Queries/RequestHandlers/`
- Use **MediatR** for command/query dispatching

### Testing

- Write **unit tests** for business logic
- Write **integration tests** for database operations
- Aim for **high code coverage** (minimum 70%)
- Use **meaningful test names** that describe the scenario
- Follow **AAA pattern** (Arrange, Act, Assert)

Example test naming:
```csharp
[Fact]
public async Task Handle_ValidCommand_ShouldCreateInventoryItem()
{
    // Arrange
    var command = new CreateInventoryItemCommand { ... };
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);
}
```

## Commit Guidelines

### Commit Message Format

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, missing semicolons, etc.)
- **refactor**: Code refactoring without changing functionality
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Maintenance tasks, dependency updates

### Examples

```
feat(inventory): add bulk import functionality

Implemented CSV import for inventory items with validation
and error handling.

Closes #123
```

```
fix(auth): resolve token expiration issue

Fixed bug where tokens were expiring prematurely due to
incorrect timezone handling.

Fixes #456
```

## Pull Request Process

### Before Submitting

1. **Update your branch** with the latest upstream changes
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run all tests** and ensure they pass
   ```bash
   dotnet test
   ```

3. **Build the solution** without warnings
   ```bash
   dotnet build
   ```

4. **Update documentation** if needed (README, XML comments, etc.)

5. **Add tests** for new features or bug fixes

### Submitting the Pull Request

1. **Push your branch** to your fork
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub

3. **Fill out the PR template** completely:
   - Clear description of changes
   - Link to related issues
   - Screenshots (if UI changes)
   - Testing performed

4. **Ensure CI checks pass** (builds, tests, linting)

### PR Title Format

Use the same format as commit messages:
```
feat(scope): add new feature
fix(scope): resolve bug
```

### Review Process

- At least **one approval** is required from maintainers
- Address all **review comments** promptly
- Keep the PR **focused** - one feature/fix per PR
- Be **responsive** to feedback and questions
- **Squash commits** if requested before merging

### After Your PR is Merged

1. **Delete your branch**
   ```bash
   git branch -d feature/your-feature-name
   git push origin --delete feature/your-feature-name
   ```

2. **Update your local main branch**
   ```bash
   git checkout main
   git pull upstream main
   ```

## Style Guide

### Code Formatting

- Use **4 spaces** for indentation (no tabs)
- Maximum line length: **120 characters**
- Use **file-scoped namespaces** (C# 10+)
- Use **nullable reference types**

### Naming Conventions

- **Interfaces**: Prefix with `I` (e.g., `IInventoryService`)
- **DTOs**: Suffix with `Dto` (e.g., `InventoryItemDto`)
- **Commands**: Suffix with `Command` (e.g., `CreateInventoryItemCommand`)
- **Queries**: Suffix with `Query` (e.g., `GetInventoryItemQuery`)
- **Handlers**: Suffix with `Handler` (e.g., `CreateInventoryItemCommandHandler`)

### Async/Await

- Always use `async`/`await` for asynchronous operations
- Suffix async methods with `Async` (e.g., `GetItemAsync`)
- Use `CancellationToken` parameters for long-running operations

## Questions?

If you have questions about contributing, feel free to:

- Open an issue with the `question` label
- Reach out to the maintainers
- Check existing documentation and issues

## Recognition

Contributors will be recognized in:
- The project README
- Release notes
- GitHub contributors page

Thank you for contributing to InventoryX! 🎉
