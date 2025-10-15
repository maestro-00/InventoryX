using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using InventoryX.Application;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration).AddApplication(builder.Configuration).AddPresentation(builder.Configuration);

var app = builder.Build();

app.UsePresentation();

app.Run();
