using InventoryX.Domain.Models;
using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args); 
builder.Services.AddInfrastructure(builder.Configuration).AddApplication().AddAuth().AddPresentation(builder.Configuration); 

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGroup("/api/auth")
    .MapIdentityApi<User>();
app.MapPost("/api/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});
app.MapGet("/api/auth/pingauth", (ClaimsPrincipal user) =>
{
    var email = user.FindFirstValue(ClaimTypes.Email); 
    return Results.Json(new {Email = email});
}).RequireAuthorization();
app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");
app.UseAuthorization();
app.MapControllers();

app.Run();
