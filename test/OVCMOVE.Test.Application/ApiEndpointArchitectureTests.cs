using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Api.Controllers.v1.Admin;

namespace OVCMOVE.Test.Application;

public class ApiEndpointArchitectureTests
{
    public static TheoryData<Type, string, int> FeatureControllers => new()
    {
        {
            typeof(AuthController),
            "api/v1/[controller]",
            6
        },
        {
            typeof(ImageController),
            "api/v1/[controller]",
            1
        },
        {
            typeof(OrganizerController),
            "api/v1/[controller]",
            5
        },
        {
            typeof(RaceController),
            "api/v1/[controller]",
            12
        },
        {
            typeof(TeamController),
            "api/v1/[controller]",
            10
        },
        {
            typeof(BoothController),
            "api/v1/[controller]",
            6
        },
        {
            typeof(WorkflowController),
            "api/v1/workflows",
            9
        },
        {
            typeof(FunctionCardController),
            "api/v1/function-cards",
            6
        }
    };

    public static TheoryData<Type, string, int> AdminControllers => new()
    {
        {
            typeof(OrganizersController),
            "api/v1/admin/organizers",
            4
        },
        {
            typeof(RbacAssignmentsController),
            "api/v1/admin/rbac/assignments",
            4
        },
        {
            typeof(RbacPermissionsController),
            "api/v1/admin/rbac/permissions",
            4
        },
        {
            typeof(RbacRolesController),
            "api/v1/admin/rbac/roles",
            4
        }
    };

    [Fact]
    public void ImageUploadEndpoint_IsPresent()
    {
        var action = typeof(ImageController).GetMethod(
            nameof(ImageController.Upload));

        Assert.NotNull(action);
        Assert.NotEmpty(
            action.GetCustomAttributes<HttpPostAttribute>());
    }

    [Fact]
    public void BoothRejectEntryEndpoint_IsPresentAndProtected()
    {
        var action = typeof(BoothController).GetMethod(
            nameof(BoothController.RejectEntry));

        Assert.NotNull(action);
        var post = Assert.Single(
            action.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("reject-entry", post.Template);
        Assert.NotEmpty(
            action.GetCustomAttributes().OfType<IAuthorizeData>());
    }

    [Fact]
    public void TeamMySessionEndpoint_IsPresentAndProtected()
    {
        var action = typeof(TeamController).GetMethod(
            nameof(TeamController.GetMySession));

        Assert.NotNull(action);
        var get = Assert.Single(
            action.GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal("my-session", get.Template);
        Assert.NotEmpty(
            action.GetCustomAttributes().OfType<IAuthorizeData>());
    }

    [Theory]
    [MemberData(nameof(FeatureControllers))]
    [MemberData(nameof(AdminControllers))]
    public void FeatureController_KeepsExpectedRouteAndActionCount(
        Type controllerType,
        string expectedRoute,
        int expectedActionCount)
    {
        var route = controllerType.GetCustomAttribute<RouteAttribute>();
        var actions = GetActions(controllerType);

        Assert.Equal(expectedRoute, route?.Template);
        Assert.Equal(expectedActionCount, actions.Count());
    }

    [Theory]
    [MemberData(nameof(FeatureControllers))]
    [MemberData(nameof(AdminControllers))]
    public void FeatureController_DependsOnlyOnMediator(
        Type controllerType,
        string expectedRoute,
        int expectedActionCount)
    {
        Assert.NotEmpty(expectedRoute);
        Assert.True(expectedActionCount > 0);
        var constructor = Assert.Single(controllerType.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IMediator), parameter.ParameterType);
    }

    [Fact]
    public void EveryFeatureAction_DeclaresItsAccessPolicy()
    {
        Type[] controllerTypes =
        [
            typeof(AuthController),
            typeof(ImageController),
            typeof(OrganizerController),
            typeof(RaceController),
            typeof(TeamController),
            typeof(BoothController),
            typeof(WorkflowController),
            typeof(FunctionCardController),
            typeof(OrganizersController),
            typeof(RbacAssignmentsController),
            typeof(RbacPermissionsController),
            typeof(RbacRolesController)
        ];

        foreach (var action in controllerTypes.SelectMany(GetActions))
        {
            var isAnonymous = action
                .GetCustomAttributes<AllowAnonymousAttribute>()
                .Any();
            var requiresAuthorization = action
                .GetCustomAttributes()
                .OfType<IAuthorizeData>()
                .Any();

            Assert.True(
                isAnonymous || requiresAuthorization,
                $"{action.DeclaringType?.Name}.{action.Name} must explicitly declare its access policy.");
        }
    }

    private static IEnumerable<MethodInfo> GetActions(Type controllerType) =>
        controllerType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Any());
}
