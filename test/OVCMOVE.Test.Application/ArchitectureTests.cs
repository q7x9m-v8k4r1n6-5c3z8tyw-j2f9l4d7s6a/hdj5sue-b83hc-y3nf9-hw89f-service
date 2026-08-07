using OVCMOVE.Application;
using OVCMOVE.Domain.Common;
using OVCMOVE.Infrastructure;

namespace OVCMOVE.Test.Application;

public class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var references = typeof(BaseEntity).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("OVCMOVE.") == true);

        Assert.Empty(references);
    }

    [Fact]
    public void Application_ReferencesOnlyDomainProject()
    {
        var references = typeof(AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("OVCMOVE.") == true);

        Assert.Equal(["OVCMOVE.Domain"], references.Order());
    }

    [Fact]
    public void Infrastructure_DoesNotReferenceApi()
    {
        var references = typeof(
            OVCMOVE.Infrastructure.DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain("OVCMOVE.Api", references);
    }

    [Fact]
    public void Plugin_DoesNotReferenceInfrastructureOrApi()
    {
        var references = typeof(
            OVCMOVE2026.Plugin.DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain("OVCMOVE.Infrastructure", references);
        Assert.DoesNotContain("OVCMOVE.Api", references);
    }

    [Fact]
    public void DomainEntities_OnlyExposeDataMembers()
    {
        var entityTypes = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "OVCMOVE.Domain.Entities");

        foreach (var entityType in entityTypes)
        {
            var behaviorMethods = entityType
                .GetMethods(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.Empty(behaviorMethods);
        }
    }
}
