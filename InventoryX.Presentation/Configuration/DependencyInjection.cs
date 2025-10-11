using System.Text;
using InventoryX.Application.Repository;
using InventoryX.Application.Services;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;
using InventoryX.Infrastructure;
using InventoryX.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace InventoryX.Presentation.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(ISalePurchaseRepository<>), typeof(SalePurchaseRepository<>));
            return services;
        }
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => {} , typeof(Program).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
            services.AddScoped<IInventoryItemService, InventoryItemService>();
            services.AddScoped<IInventoryItemTypeService, InventoryItemTypeService>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISaleService, SaleService>();
            services.AddScoped<IRetailStockService, RetailStockService>();
            services.AddHttpContextAccessor();
            return services;
        }
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
            builder =>
            {
                var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                builder.WithOrigins(allowedOrigins) // Ensure the correct port number
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials(); // Include credentials if needed
            });
            });
            services.AddControllers();
            //Add if only there is a cyclical reference
            //    .AddJsonOptions(options =>
            //{
            //    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            //});
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorisation",
                    Type = SecuritySchemeType.ApiKey
                });
                opt.OperationFilter<SecurityRequirementsOperationFilter>();
            }
            );

            return services;
        }
        public static IServiceCollection AddAuth(this IServiceCollection services)
        {
            services.AddAuthentication();
            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddApiEndpoints().AddDefaultTokenProviders();

            //services.AddIdentityApiEndpoints<User>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            //Configuring cookie auth handler for SPAs
            services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            return services;
        }

    }
}
