using System.Text.Json;
using AutoMapper;
using InventoryX.Application.Commands.Requests.Reports;
using InventoryX.Domain.Models.Auditing;

namespace InventoryX.Application.Extensions;

public sealed class ReportScheduleMappingProfile : Profile
{
    public ReportScheduleMappingProfile()
    {
        CreateMap<ReportSchedule, ReportScheduleDto>()
            .ForCtorParam(nameof(ReportScheduleDto.Recipients), options =>
                options.MapFrom(source => DeserializeRecipients(source.RecipientsJson)));
    }

    public static IReadOnlyList<string> DeserializeRecipients(string recipientsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(recipientsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
