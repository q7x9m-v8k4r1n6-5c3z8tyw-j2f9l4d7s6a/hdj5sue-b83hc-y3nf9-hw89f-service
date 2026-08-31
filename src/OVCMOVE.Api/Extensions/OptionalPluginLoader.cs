using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OVCMOVE.Api.Extensions;

/// <summary>
/// Loads optional feature assemblies without making the core API depend on them.
/// A missing, incomplete, or unloadable plugin is intentionally ignored.
/// </summary>
public static class OptionalPluginLoader
{
    private const string Move2026AssemblyName = "OVCMOVE2026.Plugin";
    private const string Move2026RegistrationType =
        "OVCMOVE2026.Plugin.DependencyInjection";

    public static Assembly? RegisterMove2026(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // The MOVE 2026 plugin owns Mongo-backed card state. Treat an absent
        // connection string as an uninstalled plugin so a core-only local
        // environment still starts normally.
        if (string.IsNullOrWhiteSpace(configuration["MongoDb:ConnectionString"]))
        {
            Console.WriteLine(
                "CẢNH BÁO: Bỏ qua plugin MOVE 2026 vì thiếu MongoDb:ConnectionString.");
            return null;
        }

        var assembly = FindAssembly(Move2026AssemblyName);
        if (assembly is null)
        {
            Console.WriteLine(
                $"CẢNH BÁO: Không tìm thấy assembly tùy chọn {Move2026AssemblyName}; plugin sẽ được bỏ qua.");
            return null;
        }

        try
        {
            var registrationType = assembly.GetType(Move2026RegistrationType);
            var registrationMethod = registrationType?.GetMethod(
                "AddMove2026Plugin",
                BindingFlags.Public | BindingFlags.Static);

            if (registrationType is null || registrationMethod is null)
            {
                Console.WriteLine(
                    $"CẢNH BÁO: Không tìm thấy điểm đăng ký plugin MOVE 2026 trong {assembly.FullName}.");
                return null;
            }

            registrationMethod.Invoke(null, [services, configuration]);
            services.AddControllers().AddApplicationPart(assembly);
            Console.WriteLine(
                $"THÀNH CÔNG: Đã nạp plugin {Move2026AssemblyName}.");
            return assembly;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or
            FileLoadException or
            BadImageFormatException or
            ReflectionTypeLoadException or
            TypeLoadException or
            MissingMethodException or
            InvalidOperationException or
            TargetInvocationException)
        {
            var rootException = exception is TargetInvocationException
                && exception.InnerException is not null
                ? exception.InnerException
                : exception;
            Console.WriteLine(
                $"CẢNH BÁO: Bỏ qua plugin {Move2026AssemblyName}: {rootException.Message}");
            return null;
        }
    }

    private static Assembly? FindAssembly(string assemblyName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException)
        {
            // Continue with the explicit optional output locations below.
        }
        catch (FileLoadException)
        {
            return null;
        }

        var pluginPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll"),
            Path.Combine(AppContext.BaseDirectory, "Plugins", $"{assemblyName}.dll")
        };

        foreach (var pluginPath in pluginPaths)
        {
            if (!File.Exists(pluginPath))
                continue;

            try
            {
                return Assembly.LoadFrom(pluginPath);
            }
            catch (BadImageFormatException)
            {
                // Continue looking for another optional copy.
            }
            catch (FileLoadException)
            {
                // Continue looking for another optional copy.
            }
        }

        return null;
    }
}
