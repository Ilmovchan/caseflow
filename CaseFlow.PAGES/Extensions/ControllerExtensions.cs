using CaseFlow.API.Converters;

namespace CaseFlow.API.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DetectiveStatusJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new ApprovalStatusJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new CaseStatusJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new EvidenceTypeJsonConverter());
            });
        
        return services;
    }
}