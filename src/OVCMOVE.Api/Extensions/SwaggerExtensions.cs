using Microsoft.OpenApi;

namespace OVCMOVE.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OVCMOVE API",
                Version = "v1"
            });

            options.SwaggerDoc("plugin-2026", new OpenApiInfo
            {
                Title = "OVCMOVE API Plugin",
                Version = "MOVE 2026"
            });

            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                var groupName = apiDesc.GroupName;

                if (docName == "v1")
                    return groupName == "v1" || string.IsNullOrEmpty(groupName);

                if (docName == "plugin-2026")
                    return groupName == "plugin-2026";

                return false;
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "OVCMOVE API v1");
            options.SwaggerEndpoint("/swagger/plugin-2026/swagger.json", "OVCMOVE API Plugin MOVE 2026");

            options.RoutePrefix = "swagger";
        });

        return app;
    }
}