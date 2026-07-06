namespace OVCMOVE.Api.Extensions
{
    public static class MappingExtension
    {
        /// <summary>
        /// Adds mapping services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the mapping services to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddMapping(this IServiceCollection services)
        {
            // Scan all Profile in the API assembly
            services.AddAutoMapper(typeof(Program).Assembly);
            return services;
        }
    }
}
