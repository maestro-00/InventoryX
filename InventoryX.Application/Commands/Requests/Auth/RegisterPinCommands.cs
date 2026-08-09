using InventoryX.Application.Behaviors;
using MediatR;

namespace InventoryX.Application.Commands.Requests.Auth;

public sealed class SetRegisterPinCommand : IRequest<bool>, ITenantWriteCommand, IAuditedCommand
{
    public required string UserId { get; init; }
    public required string Pin { get; init; }
    public string AuditAction => "user.pin.set";
    public string AuditEntityType => "User";
    public string AuditEntityId => UserId;
}

public sealed class ExchangeRegisterPinCommand : IRequest<RegisterPinExchangeResult>
{
    public required string UserId { get; init; }
    public required string Pin { get; init; }
    public Guid RegisterId { get; init; }
}

public record RegisterPinExchangeResult(string AccessToken, string TokenType = "Bearer");
