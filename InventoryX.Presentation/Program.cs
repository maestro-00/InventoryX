using InventoryX.Application.DTOs.Users;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure;
using InventoryX.Presentation.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration).AddApplication().AddAuth().AddPresentation(builder.Configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var group = app.MapGroup("/api/auth");
group.MapIdentityApi<User>();
group.MapPost("/register", async (
    UserManager<User> userManager,
    RegisterUserDto request) =>
{
    var user = new User
    {
        UserName = request.Email,
        Email = request.Email,
        Name = request.Name
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.BadRequest(result.Errors);
    }

    return Results.Ok("User registered successfully");
});

app.MapPost("/api/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});
app.MapGet("/api/auth/pingauth", (ClaimsPrincipal user) =>
{
    var email = user.FindFirstValue(ClaimTypes.Email);
    return Results.Json(new { Email = email });
}).RequireAuthorization();
app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");
app.UseAuthorization();
app.MapControllers();

app.Run();
