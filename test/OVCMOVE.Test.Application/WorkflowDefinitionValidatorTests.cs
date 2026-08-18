using System.Text.Json;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Test.Application;

public sealed class WorkflowDefinitionValidatorTests
{
    private readonly WorkflowDefinitionValidator _validator = new();

    [Fact]
    public void PublishableGraph_AcceptsConnectedConditionBranches()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("condition", WorkflowConstants.NodeType.Condition, new
            {
                left = new { kind = "path", path = "event.actorTeamId" },
                @operator = "equals",
                right = new { kind = "path", path = "event.targetTeamId" }
            }),
            Node("stop", WorkflowConstants.NodeType.Stop)
        ],
        [
            Edge("a", "trigger", "condition"),
            Edge("b", "condition", "stop", "true"),
            Edge("c", "condition", "stop", "false")
        ]);

        _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true);
    }

    [Fact]
    public void CyclicGraph_IsRejectedBeforePublishing()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("variable", WorkflowConstants.NodeType.SetVariable, new
            {
                name = "counter",
                value = new { kind = "literal", value = 1 }
            })
        ],
        [
            Edge("a", "trigger", "variable"),
            Edge("b", "variable", "trigger")
        ]);

        var exception = Assert.Throws<ApplicationValidationException>(() =>
            _validator.Validate(definition, WorkflowConstants.Trigger.Activated, false));

        Assert.Contains("vòng lặp", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TriggerMustMatchWorkflowTriggerType()
    {
        var definition = Definition(
            [Node("trigger", WorkflowConstants.NodeType.TriggerAttacked)],
            []);

        Assert.Throws<ApplicationValidationException>(() =>
            _validator.Validate(definition, WorkflowConstants.Trigger.Activated, false));
    }

    [Theory]
    [InlineData("attack", WorkflowConstants.Trigger.Activated)]
    [InlineData("effect", WorkflowConstants.Trigger.Activated)]
    [InlineData("defense", WorkflowConstants.Trigger.Attacked)]
    public void Trigger_IsDerivedFromCardCategory(string category, string expected)
    {
        Assert.Equal(expected, CreateWorkflowCommandHandler.TriggerForCard(category));
    }

    [Fact]
    public void Catalog_ContainsCodeReplacementBuildingBlocks()
    {
        var types = WorkflowCatalog.Items.Select(item => item.Type).ToHashSet();

        Assert.Contains(WorkflowConstants.NodeType.Condition, types);
        Assert.Contains(WorkflowConstants.NodeType.CreateVariable, types);
        Assert.Contains(WorkflowConstants.NodeType.SetVariable, types);
        Assert.Contains(WorkflowConstants.NodeType.RandomNumber, types);
        Assert.Contains(WorkflowConstants.NodeType.AdjustScore, types);
        Assert.Contains(WorkflowConstants.NodeType.Attack, types);
        Assert.Contains(WorkflowConstants.NodeType.SendMessage, types);
        Assert.Contains(WorkflowConstants.NodeType.Scope, types);
        Assert.DoesNotContain(WorkflowConstants.NodeType.ApplyCardEffect, types);
    }

    [Fact]
    public void Scope_RequiresTryAndCatchBranchesWhenPublishing()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("scope", WorkflowConstants.NodeType.Scope),
            Node("try", WorkflowConstants.NodeType.Stop),
            Node("catch", WorkflowConstants.NodeType.Stop)
        ],
        [
            Edge("a", "trigger", "scope"),
            Edge("b", "scope", "try", "try"),
            Edge("c", "scope", "catch", "catch")
        ]);

        _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true);
    }

    [Fact]
    public void CustomTeamTarget_AcceptsAConcreteTeamId()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("score", WorkflowConstants.NodeType.AdjustScore, new
            {
                target = "custom",
                teamIds = new[]
                {
                    "11111111-1111-1111-1111-111111111111",
                    "22222222-2222-2222-2222-222222222222"
                },
                delta = 10,
                reason = "Thưởng"
            })
        ],
        [Edge("a", "trigger", "score")]);

        _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true);
    }

    [Fact]
    public void AttackNode_AcceptsASubActionAndDefenseTags()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("attack", WorkflowConstants.NodeType.Attack, new
            {
                subAction = "steal",
                amount = 10,
                durationSeconds = 60,
                defenseTags = new[] { "Khiên", "Miễn nhiễm" }
            })
        ],
        [Edge("a", "trigger", "attack")]);

        _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true);
    }

    [Fact]
    public void AddScoreNode_RejectsANegativeValue()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("score", WorkflowConstants.NodeType.AdjustScore, new
            {
                target = "actor",
                delta = -10,
                reason = "Không hợp lệ"
            })
        ],
        [Edge("a", "trigger", "score")]);

        Assert.Throws<ApplicationValidationException>(() =>
            _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true));
    }

    [Fact]
    public void InputValueNode_AcceptsInputKeyAndTargetVariable()
    {
        var definition = Definition(
        [
            Node("trigger", WorkflowConstants.NodeType.TriggerActivated),
            Node("input", WorkflowConstants.NodeType.ReadInputValue, new
            {
                inputKey = "target_team",
                variableName = "selectedTeam"
            })
        ],
        [Edge("a", "trigger", "input")]);

        _validator.Validate(definition, WorkflowConstants.Trigger.Activated, true);
    }

    private static WorkflowDefinitionModel Definition(
        IReadOnlyCollection<WorkflowNodeModel> nodes,
        IReadOnlyCollection<WorkflowEdgeModel> edges) => new()
        {
            Nodes = nodes,
            Edges = edges
        };

    private static WorkflowNodeModel Node(string id, string type, object? config = null) => new()
    {
        Id = id,
        Type = type,
        Config = JsonSerializer.SerializeToElement(config ?? new { })
    };

    private static WorkflowEdgeModel Edge(
        string id,
        string source,
        string target,
        string? sourceHandle = null) => new()
        {
            Id = id,
            Source = source,
            Target = target,
            SourceHandle = sourceHandle
        };
}
