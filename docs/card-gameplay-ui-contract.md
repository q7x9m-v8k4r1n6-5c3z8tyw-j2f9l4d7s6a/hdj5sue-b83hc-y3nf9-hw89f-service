# Card Gameplay UI Contract v1

> Cập nhật: 06/09/2026  
> Backend branch: `feature/card-gameplay-backend`  
> Mục tiêu: tài liệu bàn giao đủ để một team frontend có thể mock, triển khai UI và nối API song song trong khi backend hoàn thiện gameplay trong 4 ngày.

## 1. Phạm vi và nguyên tắc sử dụng tài liệu

Tài liệu này là contract tích hợp cho các card sau:

| Card ID | Tên | Loại | Trạng thái backend |
|---|---|---|---|
| `OVERCLOCK` | Overclock | Core Chip | Đã dùng card và tạo effect; chưa có cửa sổ dự đoán và bước resolve |
| `CUPID` | Cupid | Core Chip | Đã resolve theo kết quả booth finalized và cooldown 15 phút |
| `ENGINEER` | Engineer | Data Patch | Đã resolve booth trí óc trong transaction submit score |
| `ATHLETE` | Athlete | Data Patch | Đã resolve booth thể chất theo maximum score trong transaction submit score |
| `REVIVE` | Revive | Data Patch | Đã tạo request và xác nhận bởi quản trạm; còn thiếu API liệt kê request đang chờ |
| `SWAP` | Swap | Data Patch | Đã dùng, consume ngay và gửi tin nhắn để xử lý thủ công |
| `TRAP` | Trap | Data Patch | Đã đặt effect và kích hoạt lúc request vào booth |

Các card `TAXMAN`, `FIREWALL`, `SHIELD`, `SCOUT`, `INSIGHT`, `BLACKOUT`, `REPLICATOR` **không thuộc contract v1 này**. Frontend không tạo form, không hard-code rule và không hiển thị chúng như card có thể dùng cho đến khi backend catalog trả về.

Ký hiệu trong tài liệu:

- **CURRENT**: API/code hiện đã tồn tại trên branch.
- **REQUIRED v1**: contract đã khóa cho frontend nhưng backend còn phải hoàn thiện trước bàn giao.
- **DEFERRED**: chưa làm trong đợt 4 ngày; frontend không phụ thuộc.

Nếu code hiện tại khác phần **REQUIRED v1**, backend phải sửa theo tài liệu; frontend không tự tạo contract khác.

## 2. Kiến trúc đã chốt

### 2.1 Không dùng React Flow làm business logic

Gameplay được chọn bằng typed handler theo `cardId`:

```text
OVERCLOCK -> OverclockCardUseHandler
CUPID     -> CupidCardUseHandler
ENGINEER  -> EngineerCardUseHandler
ATHLETE   -> AthleteCardUseHandler
REVIVE    -> ReviveCardUseHandler
SWAP      -> SwapCardUseHandler
TRAP      -> TrapCardUseHandler
```

`cardConfig` trong MongoDB chỉ lưu con số hoặc policy nhỏ được phép chỉnh theo race. Nó không quyết định card chạy handler nào. Frontend không gửi `isEffect`, handler name, trigger code hoặc arbitrary workflow JSON.

### 2.2 Core không phụ thuộc trực tiếp plugin 2026

Core phát gameplay event qua `IPluginHub`. Plugin đăng ký handler tương ứng. Nếu gỡ plugin, core vẫn build và chạy bằng `NoopPluginHub`.

Frontend không gọi `IPluginHub`. Đây là luồng nội bộ backend:

```text
Core command thành công
    -> tạo PluginEventContext ổn định
    -> IPluginHub.DispatchAsync(...)
    -> plugin tìm effect phù hợp
    -> resolve effect và áp dụng gameplay
```

### 2.3 Nguồn dữ liệu

Backend dùng hai Mongo collection:

- `race_cards`: catalog theo race, stock, card instance của từng team và lịch sử `cardUse`.
- `effect`: effect đang chờ hoặc đã được xử lý.

Core SQL hiện vẫn giữ race, team, booth, booth session và scoring log. UI **không đọc MongoDB trực tiếp** và không join dữ liệu ở client; mọi dữ liệu đi qua API.

### 2.4 Một object card là một card instance

- Mỗi lần admin cấp một card sẽ sinh một `cardInstanceId` mới.
- Một team có thể giữ nhiều Data Patch cùng loại dưới dạng nhiều instance riêng.
- Mỗi team chỉ được có một Core Chip chưa bị xóa.
- Riêng Trap: một team chỉ được nhận một Trap trong toàn race.
- `cardId` là loại card; `cardInstanceId` là bản card cụ thể thuộc inventory của team.

## 3. Quy ước HTTP và ID

### 3.1 Base route và xác thực

```text
/api/v1/plugin/cards
```

Mọi endpoint cần JWT:

```http
Authorization: Bearer <access-token>
Content-Type: application/json
```

Endpoint quản lý yêu cầu role `admin` hoặc `organizer`. Endpoint `/team/...` lấy team hiện tại từ claim `nameidentifier` hoặc `sub`; frontend không được truyền `teamId` để dùng card thay đội khác.

### 3.2 Các loại ID

| Field | Kiểu qua API | Ai sinh | Ý nghĩa |
|---|---|---|---|
| `raceId` | UUID string | Core backend | Race hiện tại |
| `teamId` | UUID string | Core backend | User/team |
| `boothId` | UUID string | Core backend | Booth |
| `cardInstanceId` | UUID string | Card backend | Một bản card đã cấp cho team |
| `cardUseId` | UUID string | Frontend | Idempotency key của đúng một ý định dùng card |
| `effectId` | Mongo ObjectId, 24 hex chars | Card backend | Effect được tạo từ một lần dùng card |
| `eventId` | String duy nhất | Core backend | Chống xử lý lặp cùng một gameplay event |

Frontend chỉ tự sinh `cardUseId` bằng `crypto.randomUUID()`.

Quy tắc retry:

1. Người dùng bấm xác nhận dùng card: sinh một `cardUseId`.
2. Cùng request bị timeout/mất mạng: retry với **đúng ID đó**.
3. Không sinh ID mới cho mỗi lần HTTP retry.
4. Sau khi nhận response xác định hoặc người dùng thay đổi input để tạo thao tác mới, lần thao tác mới dùng ID mới.
5. `cardInstanceId`, `effectId` và `eventId` không do UI tự tạo.

### 3.3 Date/time

Tất cả thời gian là UTC ISO-8601:

```json
"2026-09-06T10:30:00.000Z"
```

UI có thể hiển thị theo múi giờ thiết bị nhưng không được gửi chuỗi thời gian local không có offset.

## 4. Envelope dùng chung

### 4.1 Thành công

```ts
export interface ApiResponse<T> {
  statusCode: number;
  message: string;
  detailError: string;
  data: T;
}
```

Ví dụ:

```json
{
  "statusCode": 200,
  "message": "Success",
  "detailError": "",
  "data": {}
}
```

### 4.2 Thất bại

```json
{
  "statusCode": 409,
  "message": "Conflict",
  "detailError": "Card không còn sẵn sàng để sử dụng.",
  "data": null
}
```

UI hiển thị `detailError` nếu khác rỗng; nếu rỗng thì dùng `message`.

| HTTP | Ý nghĩa UI | Xử lý đề nghị |
|---:|---|---|
| `400` | Payload/input sai | Giữ modal, hiển thị lỗi cạnh form hoặc toast |
| `401` | Token không hợp lệ/hết hạn | Chạy flow refresh/login hiện có |
| `403` | Không có quyền | Ẩn action và hiển thị thông báo quyền |
| `404` | Race/card/effect không tồn tại | Refetch; nếu vẫn lỗi thì quay về danh sách |
| `409` | State đã đổi, card/effect đã dùng hoặc conflict đồng thời | Refetch card và effect ngay |
| `429` | Quá nhiều request | Khóa nút theo header `Retry-After` |
| `503` | Mongo/transaction tạm thời lỗi hoặc kết quả commit chưa rõ | Không tự tạo request mới; refetch bằng cùng `cardUseId` trước |
| `500` | Lỗi chưa xử lý | Thông báo chung, không tự suy đoán card đã bị trừ |

## 5. TypeScript contract v1

```ts
export type CardId =
  | "OVERCLOCK"
  | "CUPID"
  | "ENGINEER"
  | "ATHLETE"
  | "REVIVE"
  | "SWAP"
  | "TRAP";

export type CardType = "core_chip" | "data_patch";
export type CardInstanceStatus = "received" | "used" | "deleted";
export type CardUseStatus = "pending" | "active" | "resolved" | "failed";
export type EffectStatus = "active" | "resolved" | "expired" | "blocked";

export type JsonPrimitive = string | number | boolean | null;
export type JsonValue =
  | JsonPrimitive
  | JsonValue[]
  | { [key: string]: JsonValue };

export type CardInputType =
  | "opponent_team"
  | "booth"
  | "overclock_predictions";

export interface CardInputDefinition {
  key: string;
  label: string;
  type: CardInputType;
  required: boolean;
  description: string;
}

export interface CardUseHistory {
  cardUseId: string;
  effectId: string | null;
  status: CardUseStatus;
  inputs: Record<string, JsonValue>;
  usedAt: string;
  endAt: string | null;
  failureReason: string | null;
  result: Record<string, JsonValue> | null;
}

export interface CardAvailability {
  canUse: boolean;
  reasonCode:
    | "available"
    | "used"
    | "disabled"
    | "pending_confirmation"
    | "effect_active"
    | "cooldown"
    | "wrong_game_phase"
    | "not_between_booths"
    | "not_in_booth"
    | "backend_not_ready";
  reason: string;
  nextTimeAvailable: string | null;
}

export interface TeamCard {
  cardInstanceId: string;
  cardId: CardId;
  cardName: string;
  cardType: CardType;
  description: string;
  usage: string;
  inputs: CardInputDefinition[];
  config: Record<string, JsonValue>;
  cardUseCountRemain: number;
  receivedAt: string;
  receiveReason: string;
  status: CardInstanceStatus;
  availability: CardAvailability;
  cardUses: CardUseHistory[];
}

export interface CardUseResponse {
  cardUseId: string;
  effectId: string | null;
  cardInstanceId: string;
  cardId: CardId;
  cardName: string;
  status: CardUseStatus;
  usedAt: string;
  endAt: string | null;
  message: string;
}
```

**CURRENT:** backend đã trả `availability` và DTO lịch sử ổn định; BSON nội bộ không còn rò trực tiếp sang response team card.

## 6. State machine UI

### 6.1 Card instance

```text
received -> used
received -> deleted (chỉ admin, card chưa từng được dùng)
```

- `received`: còn ít nhất một lượt hoặc đang có request/effect chờ.
- `used`: số lượt còn lại bằng `0`; hiển thị xám, không cho dùng.
- `deleted`: bị admin xóa khi chưa từng dùng; team API không trả card này.

UI không được tự đổi status. Sau mutation luôn refetch.

### 6.2 Card use

```text
pending  -> resolved | failed
active   -> resolved | failed
```

- `pending`: chờ con người xác nhận, hiện dùng cho Revive.
- `active`: effect đã được arm và chờ event gameplay.
- `resolved`: đã xử lý xong, có thể có hoặc không có bonus/phạt.
- `failed`: không thể hoàn thành sau khi request đã được ghi nhận.

Không dùng string `succeeded`; backend alias cũ phải serialize thành `resolved`.

### 6.3 Effect

- `active`: đang chờ đúng event.
- `resolved`: đã kích hoạt/xử lý.
- `blocked`: bị một defense card chặn; hiện chưa dùng trong scope này ngoài contract tương lai.
- `expired`: quá hạn mà không kích hoạt.

UI team không cần gọi collection `effect`; dùng `cardUses` và API pending-interaction.

### 6.4 `result` của từng CardUse

`result` chỉ có dữ liệu khi backend đã resolve. Frontend dùng các shape sau và không đọc key động ngoài contract:

```ts
export interface OverclockUseResult {
  predictions: Array<{
    targetTeamId: string;
    boothId: string;
    outcome: "correct" | "incorrect" | "not_finalized";
    ownerDelta: number;
    targetDelta: number;
  }>;
  totalOwnerDelta: number;
  resolvedAt: string;
}

export interface CupidUseResult {
  targetTeamId: string;
  boothId: string;
  boothSessionId: string;
  boothResult: "succeeded" | "failed";
  finalAwardedPoints: number;
  ownerDelta: number;
  resolvedByEventId: string;
}

export interface BoothBonusUseResult {
  boothId: string;
  boothSessionId: string;
  boothType: "intellectual" | "physical" | "other";
  boothResult: "succeeded" | "failed";
  submittedPoints: number;
  boothMaximumScore: number | null;
  qualified: boolean;
  bonusPoints: number;
  finalAwardedPoints: number;
  resolvedByEventId: string;
}

export interface ReviveUseResult {
  confirmedBy: string;
  confirmedAt: string;
}

export interface SwapUseResult {
  targetTeamId: string;
}

export interface TrapUseResult {
  targetTeamId: string;
  boothId: string;
  penaltyPoints: number;
  triggeredAt: string;
  resolvedByEventId: string;
}
```

Mapping:

| Card | `result` |
|---|---|
| Overclock | `OverclockUseResult` |
| Cupid | `CupidUseResult` |
| Engineer | `BoothBonusUseResult` |
| Athlete | `BoothBonusUseResult` |
| Revive | `ReviveUseResult` |
| Swap | `SwapUseResult` |
| Trap | `TrapUseResult` |

**REQUIRED v1:** khi effect resolve, backend phải cập nhật cả document `effect` và `race_cards.teams[].card[].cardUse[]`. Hiện Trap mới resolve effect nhưng history card use vẫn có thể còn `active`; đó chưa phải contract hoàn chỉnh.

## 7. API team player — CURRENT

### 7.1 Lấy inventory card của team hiện tại

```http
GET /api/v1/plugin/cards/team/races/{raceId}/cards
```

Response v1:

```json
{
  "statusCode": 200,
  "message": "Success",
  "detailError": "",
  "data": [
    {
      "cardInstanceId": "972d4d72-5cef-47f3-9405-a1cfe5de0cad",
      "cardId": "CUPID",
      "cardName": "Cupid",
      "cardType": "core_chip",
      "description": "Theo dõi kết quả finalized tiếp theo của một đội đối thủ.",
      "usage": "Chọn một đội khi chưa có lượt Cupid nào đang chờ.",
      "inputs": [
        {
          "key": "targetTeamId",
          "label": "Đội được chọn",
          "type": "opponent_team",
          "required": true,
          "description": "Đội đối thủ được theo dõi."
        }
      ],
      "config": {
        "card_use_count_max": 3,
        "timeBetweenUseMinutes": 15,
        "rewardMultiplier": 1.0,
        "failurePenalty": 5
      },
      "cardUseCountRemain": 2,
      "receivedAt": "2026-09-06T08:00:00.000Z",
      "receiveReason": "assigned_at_race_start",
      "status": "received",
      "availability": {
        "canUse": false,
        "reasonCode": "effect_active",
        "reason": "Lượt Cupid trước đang chờ kết quả booth.",
        "nextTimeAvailable": null
      },
      "cardUses": []
    }
  ]
}
```

Empty state là `data: []`, không phải `404`.

### 7.2 Lấy chi tiết một card instance

```http
GET /api/v1/plugin/cards/team/races/{raceId}/cards/{cardInstanceId}
```

Response là một `TeamCard`. `404` nếu card không thuộc team hiện tại hoặc đã bị xóa.

### 7.3 Dùng card

```http
POST /api/v1/plugin/cards/team/races/{raceId}/cards/{cardInstanceId}/use
```

```json
{
  "cardUseId": "4d69b15b-a2c7-4c2c-b2ad-a80fc8df6177",
  "inputs": {}
}
```

`inputs` luôn phải là JSON object, kể cả card không cần input.

Response:

```json
{
  "statusCode": 200,
  "message": "Success",
  "detailError": "",
  "data": {
    "cardUseId": "4d69b15b-a2c7-4c2c-b2ad-a80fc8df6177",
    "effectId": "68bc06434610ce8768c5398b",
    "cardInstanceId": "972d4d72-5cef-47f3-9405-a1cfe5de0cad",
    "cardId": "ENGINEER",
    "cardName": "Engineer",
    "status": "active",
    "usedAt": "2026-09-06T08:05:00.000Z",
    "endAt": null,
    "message": "Engineer đang chờ booth phù hợp tiếp theo."
  }
}
```

Sau response thành công:

1. Hiển thị `data.message`.
2. Đóng modal khi phù hợp.
3. Refetch list và detail card.
4. Refetch scoreboard/message nếu card resolve ngay như Swap.
5. Không tự trừ lượt ở local trước response.

## 8. API admin/organizer — CURRENT

### 8.1 Lấy catalog và stock theo race

```http
GET /api/v1/plugin/cards/races/{raceId}
```

```ts
export interface AdminCardInventory {
  cardId: CardId;
  cardName: string;
  cardType: CardType;
  description: string;
  price: number;
  remainingStock: number;
  usage: string;
  inputs: CardInputDefinition[];
  config: Record<string, JsonValue>;
}

export interface CardStoreOverview {
  cards: AdminCardInventory[];
}
```

`price` hiện là metadata hard-code trong catalog. Shop/purchase không thuộc scope branch này, vì vậy UI chỉ hiển thị read-only; không làm nút mua hoặc mở/đóng shop.

### 8.2 Restock thủ công

```http
POST /api/v1/plugin/cards/races/{raceId}/inventory/restock
```

```json
{
  "quantities": {
    "CUPID": 1,
    "ENGINEER": 3,
    "ATHLETE": 3
  }
}
```

Giá trị là số lượng **cộng thêm**, không phải stock tuyệt đối. Số âm bị từ chối. Card ID ngoài catalog bị `404`.

### 8.3 Cập nhật config card

```http
PUT /api/v1/plugin/cards/races/{raceId}/cards/{cardId}/config
```

```json
{
  "config": {
    "scoreMultiplier": 2.0
  }
}
```

Endpoint có merge semantics: chỉ field gửi lên được cập nhật. Phải giữ đúng JSON type; không gửi `"2"` thay cho `2`.

UI chỉ cho sửa các field được đánh dấu editable tại mục 10. Các field invariant vẫn hiển thị read-only. Backend là nơi validate cuối cùng.

### 8.4 Cấp một card cho team

```http
POST /api/v1/plugin/cards/races/{raceId}/cards/{cardId}/teams
```

```json
{
  "teamId": "3fabf6a7-8c52-45d4-bc0e-741f53f36277",
  "teamName": "Team Alpha",
  "reason": "assigned_at_race_start"
}
```

Kết quả trả `CardTeamResponse` có `cardInstanceId` mới. Stock giảm một. Backend chặn hết stock, Core Chip thứ hai và Trap thứ hai của cùng team.

### 8.5 Danh sách team đã nhận một loại card

```http
GET /api/v1/plugin/cards/races/{raceId}/cards/{cardId}/teams
```

```ts
export interface AdminTeamCard {
  teamId: string;
  teamName: string;
  cardInstanceId: string;
  cardId: CardId;
  cardName: string;
  cardType: CardType;
  cardUseCountRemain: number;
  receivedAt: string;
  receiveReason: string;
  status: CardInstanceStatus;
  canDelete: boolean;
  disabledAt: string | null;
  disabledReason: string | null;
  cardUses: CardUseHistory[];
}
```

**REQUIRED v1:** `canDelete` chỉ được là `true` khi `status === "received"` và `cardUses.length === 0`. Code response hiện mới kiểm tra status; endpoint delete vẫn kiểm tra đúng. UI có thể kiểm tra thêm `cardUses.length === 0` để tránh nút sai trong thời gian backend sửa DTO.

### 8.6 Xóa card được cấp nhầm

```http
DELETE /api/v1/plugin/cards/races/{raceId}/teams/{teamId}/cards/{cardInstanceId}
```

```json
{
  "reason": "Cấp nhầm đội"
}
```

Chỉ card chưa có bất kỳ `cardUse` nào được xóa và hoàn một stock. Card đã dùng một phần không được delete/hoàn kho qua endpoint này.

## 9. Quy tắc và UI flow theo từng card

## 9.1 Overclock

### Luật

- Là Core Chip.
- Dùng sau khi kết thúc giai đoạn chơi booth và trước game cuối.
- Admin mở màn dự đoán.
- Team chọn đúng một booth mà mỗi đội đối thủ đã thất bại và gửi toàn bộ dự đoán một lần.
- Chỉ so với booth result đã finalized sau Revive.
- Đoán đúng: target team `-cdSteal`, owner `+cdSteal`.
- Đoán sai: owner `-cdSelfPenalty`.
- Kết quả chưa finalized không tính đúng hoặc sai và không đổi điểm.
- Không thể chỉnh dự đoán sau khi xác nhận gửi.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only trong race đang chạy |
| `cdSteal` | integer | `15` | Editable trước khi mở dự đoán |
| `cdSelfPenalty` | integer | `5` | Editable trước khi mở dự đoán |

### Use request

```json
{
  "cardUseId": "d29faf4a-b83f-40cf-9584-c75902326f20",
  "inputs": {
    "predictions": [
      {
        "targetTeamId": "83ca8919-0064-43a9-8502-2caa49b993d2",
        "boothId": "1de5af6a-b4a8-458c-82f2-5715af637fdd"
      },
      {
        "targetTeamId": "3c9c7efd-638d-4b19-a39b-f21826761cf4",
        "boothId": "5fd07eb7-72a4-4d1e-86ac-dc0635304a01"
      }
    ]
  }
}
```

### Team UI

1. Chỉ mở action khi `availability.canUse` là true.
2. Render một dòng cho mỗi đội đối thủ.
3. Mỗi dòng chọn đúng một booth; không cho trùng `targetTeamId`.
4. Nút `Reset` chỉ reset form local trước khi gửi.
5. Nút `Gửi dự đoán` mở confirm: “Bạn có chắc chắn? Danh sách không thể thay đổi sau khi gửi.”
6. Sau thành công hiển thị trạng thái `active`/“Đã khóa dự đoán”.
7. Không hiển thị kết quả booth bí mật trước khi team gửi.

### Backend còn thiếu — REQUIRED v1

- Trạng thái cửa sổ `closed | open | resolving | resolved` theo race.
- API admin mở và resolve cửa sổ theo contract dưới đây.
- `availability` cho team.
- Resolver đối chiếu finalized booth results và ghi điểm atomically/idempotently.
- API trả kết quả từng dự đoán sau khi resolve.

Frontend xây component bằng mock trước; chưa gọi nút mở/resolve cho đến khi Swagger có endpoint.

### Overclock window API — REQUIRED v1

Lấy trạng thái:

```http
GET /api/v1/plugin/cards/races/{raceId}/overclock-window
```

```ts
export interface OverclockWindow {
  status: "closed" | "open" | "resolving" | "resolved";
  openedAt: string | null;
  openedBy: string | null;
  resolvedAt: string | null;
  resolvedBy: string | null;
  eligibleTeamCount: number;
  submittedTeamCount: number;
}
```

Admin mở:

```http
POST /api/v1/plugin/cards/races/{raceId}/overclock-window/open
```

Không có body. Response `ApiResponse<OverclockWindow>`.

Admin đóng và resolve:

```http
POST /api/v1/plugin/cards/races/{raceId}/overclock-window/resolve
```

Không có body. Backend đổi `open -> resolving -> resolved`, resolve mỗi `cardUseId` idempotently và trả trạng thái cuối. Double click/retry không chạy điểm lần hai.

## 9.2 Cupid

### Luật

- Là Core Chip, có ba lượt dùng.
- Mỗi lần chọn một đội đối thủ.
- Effect theo dõi đúng kết quả booth finalized tiếp theo của target.
- Target thành công: owner nhận `finalAwardedPoints * rewardMultiplier`.
- `finalAwardedPoints` là điểm cuối GSV thực tế trao cho booth, đã bao gồm Engineer/Athlete.
- Target thất bại: owner bị trừ `failurePenalty`.
- Revive xảy ra trước finalization, vì vậy Cupid dùng kết quả sau Revive.
- Một card Cupid không được tạo lượt mới khi lượt trước vẫn `active`.
- Sau khi lượt trước resolve, phải chờ `timeBetweenUseMinutes` rồi mới dùng tiếp.
- Không có luật cấm target lại cùng một đội ở lượt sau.
- Nhiều owner khác nhau có thể cùng target một team.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `3` | Read-only sau khi cấp card |
| `timeBetweenUseMinutes` | integer | `15` | Editable trước race |
| `rewardMultiplier` | number | `1.0` | Editable trước race; UI hiển thị `100%` |
| `failurePenalty` | integer | `5` | Editable trước race |

### Use request

```json
{
  "cardUseId": "c066e89c-0884-4d47-9f40-30848620fd03",
  "inputs": {
    "targetTeamId": "83ca8919-0064-43a9-8502-2caa49b993d2"
  }
}
```

### Team UI

- Modal chọn một opponent, không có team hiện tại.
- Confirm phải ghi rõ: “Cupid sẽ theo dõi kết quả hoàn tất tiếp theo của đội này.”
- Sau use: badge `Đang theo dõi`, hiển thị target từ `cardUse.inputs.targetTeamId`.
- Khi resolve: refetch để hiển thị `result` gồm kết quả, điểm gốc dùng tính Cupid và delta của owner.
- Trong cooldown: disable và hiển thị countdown từ `availability.nextTimeAvailable`.

### Backend hiện tại

- Core đã phát `booth.result.finalized` qua `IPluginHub` trước SQL commit.
- Plugin resolve effect, cập nhật scoring log và `cardUse.result`.
- `nextTimeAvailable = resolvedAt + timeBetweenUseMinutes` được lưu sau khi resolve.
- `eventId` được ghi trên effect; booth completion ID đồng thời khóa việc submit lại cùng booth session.

## 9.3 Engineer

### Luật

- Data Patch, một lượt mỗi card instance.
- Team kích hoạt trước khi bắt đầu booth.
- Chờ booth `intellectual` phù hợp tiếp theo; đi qua booth loại khác không làm mất effect.
- Khi GSV finalize booth trí óc, lấy số điểm GSV nhập và nhân `scoreMultiplier`.
- Ví dụ GSV nhập `15`, multiplier `2`: điểm booth cuối là `30`.
- Nếu kết quả failed/điểm `0`, card vẫn mất nhưng không có bonus.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only sau khi cấp |
| `requiredBoothType` | string | `intellectual` | Invariant, không cho sửa |
| `scoreMultiplier` | number | `2.0` | Editable trước race |

### Use request

```json
{
  "cardUseId": "d07934fd-1853-48b1-938f-dbe56608d831",
  "inputs": {}
}
```

### UI

- Confirm: “Engineer sẽ áp dụng cho booth trí óc phù hợp tiếp theo. Thẻ vẫn mất nếu thất bại.”
- Sau use hiển thị `Đang chờ booth trí óc`.
- Không yêu cầu team chọn booth.
- UI quản trạm vẫn nhập điểm bình thường; không tự nhân đôi ở frontend.
- Sau submit, scoreboard và card history lấy kết quả từ backend.

### Backend hiện tại

- `Booth.Type = intellectual | physical | other` đã được expose ở create/edit/read API.
- Finalization event và resolver đã nối vào submit gameplay; lỗi handler làm SQL transaction rollback.
- Backend chặn kích hoạt khi team đang chiếm booth.

## 9.4 Athlete

### Luật

- Data Patch, một lượt mỗi card instance.
- Team kích hoạt trước khi bắt đầu booth.
- Chờ booth `physical` phù hợp tiếp theo; booth trí óc không consume effect.
- Đạt điều kiện khi điểm GSV nhập bằng `Booth.MaximumScore`.
- Nếu đạt điều kiện, điểm được nhân `scoreMultiplier`.
- Nếu score thấp hơn maximum hoặc failed, card vẫn mất và không nhận bonus.
- Không có checkbox “lần đầu” ở UI quản trạm.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only sau khi cấp |
| `requiredBoothType` | string | `physical` | Invariant |
| `scoreMultiplier` | number | `2.0` | Editable trước race |
| `qualificationMode` | string | `score_equals_booth_max` | Invariant |

### Use request

```json
{
  "cardUseId": "e87b98d7-ec6f-4594-a886-cf60e3b99194",
  "inputs": {}
}
```

### UI

- Confirm nêu rõ điều kiện phải đạt điểm tối đa.
- Hiển thị `Đang chờ booth thể chất` sau khi dùng.
- Form tạo/sửa booth của admin cần field `type` và `maximumScore` khi backend core cung cấp.
- Form chấm điểm không có Athlete checkbox; backend tự so sánh score với maximum.

### Backend hiện tại

- Đã expose `Booth.Type`, `Booth.MaximumScore` và có migration `007_CardBoothMetadata.sql`.
- Đã validate `maximumScore > 0` với booth physical.
- Resolver tự so sánh điểm GSV nhập với maximum score và tính bonus.

## 9.5 Revive

### Luật

- Data Patch, một lượt.
- Dùng khi team đang ở booth có status `occupied` và trước khi GSV kết thúc booth.
- Team và GSV giao tiếp việc thất bại ngoài app; team bấm Revive trước khi GSV submit điểm `0`.
- Lần bấm của team chỉ tạo request `pending`; chưa trừ card.
- Quản trạm được phân công booth hoặc admin xác nhận.
- Khi confirm: card mới mất, request `resolved`, team tiếp tục chơi trong cùng booth.
- Nếu booth đã submit/cancel trước confirm: confirm bị từ chối; card không bị trừ.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only sau khi cấp |
| `consumeWhen` | string | `operator_confirmed` | Invariant |

### Team use request

```json
{
  "cardUseId": "a86ea796-df11-4d82-a838-d4396611b8ca",
  "inputs": {
    "boothId": "1de5af6a-b4a8-458c-82f2-5715af637fdd"
  }
}
```

Response có `status: "pending"` và `effectId`.

### Team UI

1. Chỉ hiện/use khi `availability.canUse` true và team đang trong booth.
2. Confirm hai bước: “Bạn chắc chắn gửi yêu cầu Revive? Thẻ chỉ mất khi quản trạm xác nhận.”
3. Sau success hiển thị banner “Đang chờ quản trạm xác nhận”, disable bấm lại.
4. Khi confirmed, đổi thành “Revive đã được xác nhận. Hãy tiếp tục chơi booth.”
5. Khi booth đóng trước confirm, hiển thị failed và card vẫn còn lượt.

### Organizer confirm — CURRENT

```http
POST /api/v1/plugin/cards/races/{raceId}/revive-effects/{effectId}/confirm
```

Không có body.

### Pending list — REQUIRED v1

```http
GET /api/v1/plugin/cards/races/{raceId}/revive-effects/pending?boothId={boothId}
```

```ts
export interface PendingRevive {
  effectId: string;
  cardUseId: string;
  teamId: string;
  teamName: string;
  boothId: string;
  boothName: string;
  requestedAt: string;
}
```

Trong MVP, organizer screen poll endpoint mỗi 3–5 giây khi booth đang occupied. Không cần tạo SignalR event riêng trong 4 ngày.

## 9.6 Swap

### Luật

- Data Patch, một lượt.
- Dùng giữa hai booth, chọn một team đối thủ.
- Sau khi dùng, card resolve và mất ngay.
- Backend gửi tin nhắn cho target team và toàn bộ organizer.
- Việc xem tối đa `mapPieceViewLimit` mảnh bản đồ được hai đội/BTC/GSV xử lý thủ công.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only sau khi cấp |
| `mapPieceViewLimit` | integer | `4` | Editable trước race |

### Use request

```json
{
  "cardUseId": "f79c76fc-090a-446e-91fe-a8de08bd013c",
  "inputs": {
    "targetTeamId": "83ca8919-0064-43a9-8502-2caa49b993d2"
  }
}
```

Response `status: "resolved"`, `effectId: null`.

### UI

- Modal chọn opponent và confirm.
- Success screen hiển thị: “Vui lòng liên hệ BTC/GSV để xử lý mảnh bản đồ.”
- Không làm UI tự chuyển/sửa map pieces trong scope v1.
- Nếu message delivery lỗi nhưng card đã commit, backend trả message có câu “Thông báo chưa đồng bộ”; UI phải bảo người dùng tải lại/liên hệ BTC, không cho bấm lại.

### Backend còn thiếu — REQUIRED v1

- Validate owner đang giữa hai booth.
- Validate target thuộc race hiện tại.

## 9.7 Trap

### Luật đang được code hiện tại

- Data Patch, một lượt.
- Mỗi team chỉ được cấp một Trap trong race.
- Team chọn một booth để đặt.
- Một booth chỉ có tối đa một Trap active.
- Nếu owner tự request booth đó, Trap của owner không kích hoạt và tiếp tục chờ đối thủ.
- Đội đối thủ đầu tiên request entry kích hoạt Trap.
- Effect resolve một lần, target bị trừ `penaltyPoints`.
- Request vào booth vẫn tiếp tục theo core flow hiện tại.

### Config

| Key | Type | Default | UI admin |
|---|---|---:|---|
| `card_use_count_max` | integer | `1` | Read-only sau khi cấp |
| `penaltyPoints` | integer | `15` | Editable; phải lớn hơn 0 |

### Use request

```json
{
  "cardUseId": "e33982b7-dfd6-49de-a1f2-70acfb19f42c",
  "inputs": {
    "boothId": "1de5af6a-b4a8-458c-82f2-5715af637fdd"
  }
}
```

### UI

- Hiển thị danh sách booth hợp lệ nhưng không tiết lộ booth đang có Trap bằng label kiểu “Có Trap”.
- Nếu backend trả `409 Booth này đã có Trap đang hoạt động`, hiển thị thông báo trung tính: “Không thể đặt card tại booth này. Vui lòng chọn booth khác.”
- Sau use, chỉ owner thấy card đang active và booth đã chọn.
- Đội bị dính Trap nhận thay đổi điểm qua scoreboard/notification hiện có; không tự trừ local.

### Rủi ro backend phải xử lý trước production

Core hiện chuyển booth sang `pending` rồi mới dispatch plugin. Plugin exception bị hub log và bỏ qua. Vì vậy lỗi Mongo/score có thể làm request booth thành công nhưng Trap không áp dụng. Backend phải chốt transaction/compensation hoặc trả kết quả plugin bắt buộc đối với gameplay; UI không thể sửa tính nhất quán này.

## 10. Config schema tổng hợp

Frontend không dựng editor JSON tự do. Dùng form typed dưới đây:

| Card | Field | Type | Min | Editable |
|---|---|---|---:|---|
| Overclock | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Overclock | `cdSteal` | integer | 1 | Có, trước khi mở |
| Overclock | `cdSelfPenalty` | integer | 0 | Có, trước khi mở |
| Cupid | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Cupid | `timeBetweenUseMinutes` | integer | 0 | Có, trước race |
| Cupid | `rewardMultiplier` | number | 0 | Có, trước race |
| Cupid | `failurePenalty` | integer | 0 | Có, trước race |
| Engineer | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Engineer | `requiredBoothType` | string | — | Không |
| Engineer | `scoreMultiplier` | number | 1 | Có, trước race |
| Athlete | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Athlete | `requiredBoothType` | string | — | Không |
| Athlete | `scoreMultiplier` | number | 1 | Có, trước race |
| Athlete | `qualificationMode` | string | — | Không |
| Revive | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Revive | `consumeWhen` | string | — | Không |
| Swap | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Swap | `mapPieceViewLimit` | integer | 1 | Có, trước race |
| Trap | `card_use_count_max` | integer | 1 | Không sau cấp card |
| Trap | `penaltyPoints` | integer | 1 | Có, trước race |

**REQUIRED v1:** backend hiện chỉ validate chặt `penaltyPoints`; phải thêm typed config validation cho toàn bộ field trước khi UI admin được bật mutation config.

## 11. Contract booth finalization mà card backend cần

Frontend quản trạm hiện gọi:

```http
POST /api/v1/Booth/submit-score
```

Current body:

```json
{
  "teamId": "3fabf6a7-8c52-45d4-bc0e-741f53f36277",
  "boothId": "1de5af6a-b4a8-458c-82f2-5715af637fdd",
  "score": 20
}
```

V1 giữ request này để tránh frontend phải đổi ngay. Backend tự tạo event sau khi score và booth result commit:

```ts
export interface BoothResultFinalizedEvent {
  eventId: string;
  eventCode: "booth.result.finalized";
  raceId: string;
  boothId: string;
  boothSessionId: string;
  teamId: string;
  result: "succeeded" | "failed";
  boothType: "intellectual" | "physical" | "other";
  submittedPoints: number;
  boothMaximumScore: number | null;
  finalAwardedPoints: number;
  finalizedAt: string;
}
```

Ý nghĩa:

- `submittedPoints`: điểm GSV nhập trước bonus card.
- `finalAwardedPoints`: tổng điểm của booth sau Engineer/Athlete; Cupid dùng field này.
- `result`: `failed` khi điểm GSV nhập bằng `0`, còn lại là `succeeded`.
- `eventId`: do backend sinh ổn định từ booth session/finalization, không dùng `DateTime.Ticks` cho retry.

Thứ tự xử lý bắt buộc:

```text
GSV submit
  -> kiểm tra Revive pending
  -> chốt result của BoothSession
  -> Engineer/Athlete tính finalAwardedPoints
  -> cập nhật điểm + scoring log
  -> Cupid dùng finalAwardedPoints/result
  -> complete cardUse/effect
  -> commit transaction gameplay
  -> phát notification sau commit
```

Nếu bất kỳ bước gameplay bắt buộc nào lỗi, submit booth thất bại và transaction rollback. UI hiển thị thông báo tinh tế, ví dụ “Hệ thống chưa thể hoàn tất kết quả trạm. Vui lòng thử lại.”

### 11.1 Giới hạn transaction SQL + MongoDB

SQL transaction không thể tự bao trùm MongoDB transaction. Vì vậy câu “rollback toàn bộ” không được hiện thực bằng cách commit Mongo và SQL nối tiếp rồi hy vọng cả hai cùng thành công.

Contract backend v1 phải dùng idempotent operation:

```text
operationId = card-effect:{effectId}:{eventId}
```

Luồng tối thiểu:

1. Đọc/claim effect bằng compare-and-set để hai request không cùng xử lý một effect.
2. Tính toàn bộ score delta từ snapshot event.
3. Ghi scoring log SQL với `operationId` unique; retry cùng operation không cộng điểm lần hai.
4. Commit SQL.
5. Mark Mongo effect/cardUse resolved bằng `eventId` và `operationId`.
6. Nếu bước 5 lỗi tạm thời, retry ngay tối đa ba lần; request sau dùng cùng operation để reconcile, không chạy score lần hai.
7. Nếu vẫn không thể đồng bộ, trả finalization failed để admin xử lý; không báo thành công mập mờ cho GSV.

`processing` có thể là trạng thái nội bộ của effect khi claim. Frontend vẫn xem nó như `active` và không tạo action thứ hai. Không expose một nút “retry với ID mới”.

Điểm bắt buộc là unique `operationId` ở scoring log hoặc bảng operation tương đương. Chỉ có Mongo `version` không ngăn SQL cộng điểm lặp.

## 12. UI pages/components cần làm

### 12.1 Team

1. `TeamCardInventoryPage`
   - Group visual theo Core Chip/Data Patch nhưng key theo `cardInstanceId`.
   - Card `used` tô xám.
   - Một team có thể có nhiều instance cùng `cardId`.
2. `TeamCardDetailModal`
   - Description, usage, số lượt còn lại, availability, history.
3. `UseCardModal`
   - Render form theo `inputs[].type`.
   - Confirm và idempotency.
4. `OverclockPredictionModal`
   - Form nhiều opponent, reset local, confirm bất khả hoàn tác.
5. `ActiveCardBanner`
   - pending/active/cooldown và countdown.
6. `CardUseResult`
   - Dùng message/result từ backend, không tự tính score.

### 12.2 Admin/organizer

1. `RaceCardCatalogPage`
   - Stock, config read/edit, restock.
2. `CardAssignmentPanel`
   - Chọn team, cấp card, xem từng instance, delete card chưa dùng.
3. `PendingRevivePanel`
   - Poll request theo booth, confirm một lần, chống double click.
4. `OverclockControlPanel` — chờ REQUIRED endpoint
   - Open, theo dõi số team đã submit, resolve.
5. `BoothForm` — chờ core fields
   - `type`, `maximumScore`.

### 12.3 Form renderer

| `inputs[].type` | Component |
|---|---|
| `opponent_team` | Single-select, loại current team |
| `booth` | Single-select booth hợp lệ trong race |
| `overclock_predictions` | Danh sách opponent, mỗi opponent chọn một booth |

Label/description lấy từ API. Không dịch `cardId`, input key hoặc enum trong business payload.

## 13. Cache/refetch contract

Query key đề nghị:

```ts
["race-cards", "admin", raceId]
["race-cards", "admin-card-teams", raceId, cardId]
["race-cards", "team", raceId]
["race-cards", "team-card", raceId, cardInstanceId]
["race-cards", "pending-revive", raceId, boothId]
["race-cards", "overclock-window", raceId]
```

Sau mutation:

| Mutation | Invalidate |
|---|---|
| Restock/config | admin overview |
| Assign/delete | admin overview, card teams, team cards nếu đang mở |
| Use card | team list, team detail, scoreboard/messages nếu resolve ngay |
| Confirm Revive | pending Revive, team list/detail, booth state |
| Resolve gameplay event | team list/detail, scoreboard, scoring logs/messages |

Không optimistic-update số lượt card hoặc score. Mutation gameplay chỉ cập nhật UI sau response/refetch.

## 14. Loading, double-click và network failure

- Disable nút submit trong khi request đang chạy.
- Một modal giữ một `cardUseId` cho tới khi thao tác kết thúc.
- Timeout không đồng nghĩa thất bại; refetch card history để tìm `cardUseId`.
- Nếu tìm thấy ID, dùng trạng thái backend và không gửi thao tác mới.
- Nếu `409`, refetch ngay vì card/stock/effect đã đổi ở request khác.
- Không cho đóng tab làm mất draft Overclock; lưu draft local theo `raceId + cardInstanceId`, nhưng xóa draft sau submit thành công.

## 15. Realtime và notification

Hiện card dùng hệ thống Race Message có sẵn; chưa có SignalR event riêng cho mọi CardUse.

- Swap gửi message cho target team và toàn bộ organizer sau khi card commit.
- Revive confirm gửi message cho owner team sau commit.
- Trap cập nhật score qua notification score hiện có.
- Với pending Revive, MVP dùng polling 3–5 giây.
- Mọi realtime/message chỉ là tín hiệu làm UI refetch; không phải source of truth.
- Nếu gửi notification lỗi sau commit, card vẫn đã dùng. UI hiển thị message backend và refetch, tuyệt đối không bấm use lại bằng ID mới.

## 16. Những contract UI không được tự suy diễn

- Không tính score Engineer/Athlete/Cupid ở frontend.
- Không tự kết luận booth result từ text notification.
- Không đọc `effect` collection.
- Không tạo `effectId`, `eventId`, `cardInstanceId`.
- Không dùng `cardId` làm React key của inventory vì có thể có nhiều instance cùng loại.
- Không cho admin nhập config key ngoài schema.
- Không coi `remainingStock` là số card team đang giữ.
- Không coi HTTP timeout là card chưa bị dùng.
- Không làm shop/open-store trong v1.
- Không hiển thị card ngoài catalog API như card hoạt động.

## 17. Backend gap list cần đóng trong 4 ngày

### P0 — khóa contract và an toàn dữ liệu

- Map `CardUseHistory` DTO thay raw `BsonDocument`.
- Thêm `availability` và `nextTimeAvailable` vào team response.
- Sửa `canDelete` xét cả `cardUses.length === 0`.
- Typed validation cho toàn bộ `cardConfig`.
- Bảo đảm Mongo deploy chạy replica set/Atlas vì use card + effect dùng transaction nhiều collection.

### P1 — gameplay booth

- Thêm `Booth.Type`, `Booth.MaximumScore`, `BoothSessionId`/finalized result phù hợp core.
- Phát `booth.result.finalized` qua core-owned `IPluginHub`.
- Resolver Engineer, Athlete, Cupid trong transaction với score/scoring log.
- Event idempotency; cùng finalization không được cộng điểm hai lần.

### P2 — interactions

- API pending Revive và tự fail request khi booth đóng trước confirmation.
- Validate Swap “giữa hai booth” và target thuộc race.
- Sửa Trap để gameplay effect không bị nuốt lỗi sau khi booth đã chuyển pending.

### P3 — Overclock

- Window state + API admin open/resolve.
- Team availability và progress submit.
- Resolver dựa trên finalized booth history.
- Result DTO cho từng prediction.

## 18. Kế hoạch phối hợp 4 ngày

| Ngày | Backend | Frontend có thể làm song song |
|---|---|---|
| 1 | Chốt DTO, availability, validation, Swagger examples | API client, inventory, detail, generic use modal, admin catalog |
| 2 | Booth fields + finalized event + Engineer/Athlete | Engineer/Athlete states, booth form fields, mock result |
| 3 | Cupid resolver, Revive pending, Swap/Trap hardening | Cupid modal/countdown, Revive operator panel, Swap/Trap modal |
| 4 | Overclock MVP, integration tests, error cases | Overclock modal/control panel, full E2E, polish/error handling |

Nếu P3 không kịp, 6 card còn lại vẫn có thể release; Overclock phải feature-flag và không cấp cho team. Không được để nút use hoạt động khi backend mới chỉ lưu effect nhưng chưa bao giờ resolve.

## 19. Acceptance checklist chung

Frontend chỉ coi một card hoàn tất khi các case sau pass:

- Load list có 0, 1 và nhiều instance cùng loại.
- Card used hiển thị xám và không dùng lại.
- Double click chỉ tạo một `cardUseId` và một lịch sử.
- Retry network bằng cùng `cardUseId` không trừ thêm lượt.
- `400/409/503` hiển thị đúng và refetch đúng.
- Config number/boolean giữ đúng JSON type.
- Admin không delete được card đã có history.
- Revive pending không consume; confirm mới consume.
- Engineer/Athlete/Cupid cập nhật score đúng một lần sau finalization.
- Trap owner tự vào booth không kích hoạt Trap của mình.
- Swap commit dù notification lỗi, UI không cho dùng lại.
- Không card nào ngoài catalog được render như có thể thao tác.

## 20. Câu trả lời ngắn cho team UI

1. **API nào dùng để lấy card?** `GET /api/v1/plugin/cards/team/races/{raceId}/cards`.
2. **Dùng card bằng gì?** `cardInstanceId` trên URL và `cardUseId` UUID trong body.
3. **Có tự tính hiệu ứng không?** Không. Backend trả status/result và cập nhật score.
4. **Có nhiều card cùng loại không?** Có với Data Patch; render theo `cardInstanceId`.
5. **Card đang active có dùng tiếp không?** Theo `availability`; Cupid không dùng lượt tiếp khi effect trước active.
6. **Timeout có bấm lại không?** Retry cùng `cardUseId`, sau đó refetch.
7. **React Flow ở đâu?** Không nằm trong gameplay production v1.
8. **Shop ở đâu?** Ngoài scope v1; hiện admin restock và assign trực tiếp.
9. **Nguồn thật của trạng thái?** API backend; notification chỉ yêu cầu refetch.
10. **Card nào chưa nối xong gameplay?** Overclock còn thiếu cửa sổ dự đoán/resolve nên backend trả `backend_not_ready`; Cupid, Engineer và Athlete đã nối với booth finalized.
