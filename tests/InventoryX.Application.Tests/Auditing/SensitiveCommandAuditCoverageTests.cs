using FluentAssertions;
using InventoryX.Application.Behaviors;
using InventoryX.Application.Commands.Requests.Auth;
using InventoryX.Application.Commands.Requests.Catalog;
using InventoryX.Application.Commands.Requests.Inventory;
using InventoryX.Application.Commands.Requests.Selling;
using InventoryX.Application.DTOs.Users;
using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models;

namespace InventoryX.Application.Tests.Auditing;

public sealed class SensitiveCommandAuditCoverageTests
{
    private sealed class RecordingAuditLogWriter : IAuditLogWriter
    {
        public string? Action { get; private set; }
        public string? EntityType { get; private set; }
        public string? EntityId { get; private set; }

        public Task WriteAsync(
            string action,
            string entityType,
            string entityId,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            return Task.CompletedTask;
        }
    }

    public static TheoryData<Type> SensitiveCommands => new()
    {
        typeof(UpdateProductCommand),
        typeof(CreateReturnCommand),
        typeof(CreateExchangeCommand),
        typeof(VoidSaleCommand),
        typeof(RecordStockAdjustmentCommand),
        typeof(ApproveStockAdjustmentCommand),
        typeof(RejectStockAdjustmentCommand),
        typeof(UpdateUserAccessCommand),
    };

    [Theory]
    [MemberData(nameof(SensitiveCommands))]
    public void Fr008_sensitive_command_is_audited(Type commandType)
    {
        typeof(IAuditedCommand).IsAssignableFrom(commandType).Should().BeTrue(
            $"{commandType.Name} changes prices, refunds, voids, stock, or permissions");
    }

    [Fact]
    public async Task Successful_sensitive_command_is_written_to_the_audit_log()
    {
        var writer = new RecordingAuditLogWriter();
        var behavior = new AuditBehavior<UpdateUserAccessCommand, UserListItemDto>(writer);
        var command = new UpdateUserAccessCommand { UserId = "user-1" };

        var result = await behavior.Handle(
            command,
            _ => Task.FromResult(new UserListItemDto("user-1", "a@b.c", null, null, null, UserStatus.Active, false, "stamp")),
            CancellationToken.None);

        result.Id.Should().Be("user-1");
        writer.Action.Should().Be("user.permissions.update");
        writer.EntityType.Should().Be("User");
        writer.EntityId.Should().Be("user-1");
    }
}
