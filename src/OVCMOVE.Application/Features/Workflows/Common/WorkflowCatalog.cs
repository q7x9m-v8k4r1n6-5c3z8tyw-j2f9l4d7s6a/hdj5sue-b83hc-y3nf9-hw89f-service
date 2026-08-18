using System.Text.Json;

namespace OVCMOVE.Application.Features.Workflows.Common;

public static class WorkflowCatalog
{
    public static IReadOnlyCollection<WorkflowCatalogItemModel> Items { get; } =
    [
        Item(WorkflowConstants.NodeType.TriggerActivated, "trigger", "Kích hoạt",
            "Bắt đầu khi người chơi sử dụng thẻ.", true, new { }),
        Item(WorkflowConstants.NodeType.TriggerAttacked, "trigger", "Khi bị tấn công",
            "Bắt đầu khi chủ thẻ trở thành mục tiêu tấn công.", true, new { }),
        Item(WorkflowConstants.NodeType.Condition, "logic", "Điều kiện",
            "Rẽ nhánh Đúng/Sai theo dữ liệu sự kiện hoặc biến.", false,
            new { left = new { kind = "path", path = "event.actorTeamId" }, @operator = "equals", right = new { kind = "path", path = "event.targetTeamId" } }),
        Item(WorkflowConstants.NodeType.CreateVariable, "data", "Tạo biến",
            "Khởi tạo một biến để các action phía sau sử dụng.", false,
            new { name = "bienMoi", value = new { kind = "literal", value = "" } }),
        Item(WorkflowConstants.NodeType.SetVariable, "data", "Gán biến",
            "Gán giá trị mới cho một biến đã được tạo trước đó.", false,
            new { name = "", value = new { kind = "literal", value = "" } }),
        Item(WorkflowConstants.NodeType.RandomNumber, "data", "Số ngẫu nhiên",
            "Sinh số nguyên ngẫu nhiên và lưu vào biến.", false,
            new { name = "ketQua", min = 1, max = 6 }),
        Item(WorkflowConstants.NodeType.AdjustScore, "team", "Cộng điểm",
            "Cộng một số điểm dương cho đội được chọn.", false,
            new { target = "actor", delta = 10, reason = "Hiệu ứng thẻ chức năng" }),
        Item(WorkflowConstants.NodeType.Attack, "attack", "Tấn công",
            "Chọn một sub-action tấn công trước khi nối sang bước tiếp theo.", false,
            new { subAction = "", amount = 10, durationSeconds = 60, defenseTags = Array.Empty<string>() }),
        Item(WorkflowConstants.NodeType.SendMessage, "notify", "Gửi thông báo",
            "Gửi thông báo tới đội dùng thẻ, đội mục tiêu hoặc tất cả đội.", false,
            new { target = "actor", message = "Thẻ đã được kích hoạt." }),
        Item(WorkflowConstants.NodeType.Scope, "flow", "Scope Try/Catch",
            "Chạy nhánh Try và chuyển sang nhánh Catch nếu một action phát sinh lỗi.", false,
            new { }),
        Item(WorkflowConstants.NodeType.Stop, "flow", "Dừng workflow",
            "Kết thúc nhánh hiện tại ngay lập tức.", false, new { })
    ];

    private static WorkflowCatalogItemModel Item(
        string type,
        string category,
        string label,
        string description,
        bool isTrigger,
        object defaultConfig) =>
        new(type, category, label, description, isTrigger,
            JsonSerializer.SerializeToElement(defaultConfig, WorkflowJson.Options));
}
