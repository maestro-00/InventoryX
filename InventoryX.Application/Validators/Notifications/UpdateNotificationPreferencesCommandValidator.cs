using FluentValidation;
using InventoryX.Application.Commands.Requests.Notifications;

namespace InventoryX.Application.Validators.Notifications;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(command => command.Preferences)
            .Must(preferences => preferences.Select(item => (item.Type, item.Channel)).Distinct().Count() == preferences.Count)
            .WithMessage("Each notification type and channel pair may appear only once.");
        RuleForEach(command => command.Preferences).ChildRules(preference =>
        {
            preference.RuleFor(item => item.Type).IsInEnum();
            preference.RuleFor(item => item.Channel).IsInEnum();
            preference.RuleFor(item => item.Threshold).GreaterThanOrEqualTo(0m).When(item => item.Threshold.HasValue);
        });
    }
}
