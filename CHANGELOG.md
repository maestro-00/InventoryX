# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project setup with Clean Architecture
- Inventory item management functionality
- Item type categorization system
- Purchase tracking features
- Sales tracking features
- Retail stock management
- ASP.NET Identity authentication and authorization
- RESTful API with Swagger documentation
- CQRS pattern implementation with MediatR
- AutoMapper for object mapping
- Comprehensive unit tests
- GitHub issue templates and PR template
- Contributing guidelines and Code of Conduct

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.0.0] - 2024-09-14

### Added
- Initial release of InventoryX
- Core domain entities (InventoryItem, ItemType, Purchase, Sale, RetailStock)
- Application layer with CQRS commands and queries
- Infrastructure layer with Entity Framework Core
- Presentation layer with ASP.NET Core Web API
- Database migrations for SQL Server
- API documentation with Swagger/OpenAPI
- Authentication and authorization system
- Basic CRUD operations for all entities
- Project documentation (README, LICENSE, CONTRIBUTING)

---

## How to Update This Changelog

When making changes to the project:

1. Add your changes under the `[Unreleased]` section
2. Use the following categories:
   - **Added** for new features
   - **Changed** for changes in existing functionality
   - **Deprecated** for soon-to-be removed features
   - **Removed** for now removed features
   - **Fixed** for any bug fixes
   - **Security** for vulnerability fixes

3. When releasing a new version:
   - Change `[Unreleased]` to the version number and date
   - Create a new `[Unreleased]` section at the top
   - Update the version comparison links at the bottom

### Example Entry Format

```markdown
### Added
- New bulk import feature for inventory items (#123)
- Export to CSV functionality (#145)

### Fixed
- Fixed authentication token expiration issue (#167)
- Resolved database connection timeout in high-load scenarios (#178)
```
