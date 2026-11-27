using InventoryX.Application.Commands.RequestHandlers.Billing;
using InventoryX.Application.Commands.Requests.Billing;
using InventoryX.Application.Repository;
using InventoryX.Application.Services.IServices;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace InventoryX.Application.Tests.Billing;

public sealed class BillingDependencyInjectionTests
{
    [Fact]
    public void Recovery_handler_resolves_with_application_registrations()
    {
        var services = new ServiceCollection();
        services.AddApplication(new ConfigurationBuilder().Build());
        services.AddScoped(_ => Mock.Of<IAppDbContext>());
        services.AddScoped(_ => Mock.Of<ITenantContext>());
        services.AddScoped(_ => Mock.Of<IPaymentGateway>());
        services.AddScoped(_ => Mock.Of<IBillingSecretProtector>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<StartRecoveryCommand, CreatePaymentIntentDto>>();

        Assert.IsType<StartRecoveryCommandHandler>(handler);
    }
}
