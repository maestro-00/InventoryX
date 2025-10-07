# InventoryX

A comprehensive Inventory Management API service built with ASP.NET Core 8.0 following Clean Architecture principles. This service enables organizations to efficiently manage their inventory, track stock levels, handle purchases and sales, and maintain retail stock information.

## 🏗️ Architecture

The project follows Clean Architecture with the following layers:

- **InventoryX.Domain** - Core business entities and domain logic
- **InventoryX.Application** - Application business rules, CQRS commands/queries, and service interfaces
- **InventoryX.Infrastructure** - Data access, external services, and infrastructure concerns
- **InventoryX.Presentation** - API controllers, middleware, and presentation logic

## ✨ Features

- 📦 Inventory item management
- 🏷️ Item type categorization
- 💰 Purchase and sales tracking
- 🏪 Retail stock management
- 🔐 Authentication and authorization with ASP.NET Identity
- 📊 RESTful API with Swagger documentation
- 🎯 CQRS pattern with MediatR
- 🗺️ AutoMapper for object mapping

## 🛠️ Tech Stack

- **.NET 8.0** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **MediatR** - CQRS implementation
- **AutoMapper** - Object mapping
- **Swagger/OpenAPI** - API documentation
- **ASP.NET Identity** - Authentication & Authorization

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended)
- [Git](https://git-scm.com/)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/InventoryX.git
cd InventoryX
```

### 2. Configure Database Connection

Update the connection string in `InventoryX.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InventoryXDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Configure CORS (Optional)

If you're running a frontend application, add allowed origins in `appsettings.json`:

```json
{
  "Frontend": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  }
}
```

### 4. Restore Dependencies

Using .NET CLI:
```bash
dotnet restore
```

Or using Visual Studio:
- Right-click on the solution → **Restore NuGet Packages**

### 5. Apply Database Migrations

```bash
cd InventoryX.Presentation
dotnet ef database update
```

If you don't have EF Core tools installed:
```bash
dotnet tool install --global dotnet-ef
```

### 6. Run the Application

Using .NET CLI:
```bash
dotnet run --project InventoryX.Presentation
```

Using Visual Studio/Rider:
- Press **F5** or click the **Run** button

The API will be available at:
- **HTTPS**: `https://localhost:7xxx`
- **HTTP**: `http://localhost:5xxx`
- **Swagger UI**: `https://localhost:7xxx/swagger`

## 🧪 Running Tests

```bash
dotnet test
```

## 📚 API Documentation

Once the application is running, navigate to `/swagger` to view the interactive API documentation.

### Key Endpoints

- **Authentication**: `/api/auth/*`
- **Inventory Items**: `/api/inventoryitems/*`
- **Item Types**: `/api/inventoryitemtypes/*`
- **Purchases**: `/api/purchases/*`
- **Sales**: `/api/sales/*`
- **Retail Stocks**: `/api/retailstocks/*`

## 🔑 Authentication

The API uses ASP.NET Identity with cookie-based authentication. To access protected endpoints:

1. Register a new user via `/api/auth/register`
2. Login via `/api/auth/login`
3. Use the returned authentication cookie for subsequent requests

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on how to get started.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 👤 Author

**Adam Tungtaiya Lukman**

## 🙏 Acknowledgments

- Built with Clean Architecture principles
- Inspired by modern .NET best practices
- Community contributions and feedback

## 📞 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Contact the maintainers

---

**Happy Coding! 🚀**
