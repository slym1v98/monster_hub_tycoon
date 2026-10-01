# Sub-project 1: Lõi thời gian & Trainer (Game.Domain)

Ngày: 2026-10-01. Nguồn quyết định: các tài liệu GDD sau commit `441fbc4` (đặc biệt [00](../../designs/00_Master_GDD.md) §4-5, [02](../../designs/02_HUB_Economy_Infrastructure.md) §1.5 và §2, [03](../../designs/03_Trainer_AI_System.md) §1.1).

Quy ước code: tên hàm, tham số, giả mã bằng tiếng Anh; chú thích và comment bằng tiếng Việt có dấu.

## 1. Mục tiêu và phạm vi

Dựng động cơ mô phỏng **sự kiện rời rạc theo phút in-game** trong `src/Game.Domain`, thay thế mô hình gộp theo ngày `HubEconomy`. Sau sub-project này, Trainer sống một vòng đời thật: đi farm, về HUB, xếp hàng, dùng dịch vụ, nhận lương. Đây là nền để 5 sub-project sau cắm thêm hệ thống.

**Trong phạm vi:** đồng hồ và hàng đợi sự kiện; Trainer (4 thanh nhu cầu, Gold, tính cách, Rarity, Rank/Lv tĩnh, lương hợp đồng); FSM; 4 công trình dịch vụ (Nhà Trọ, Nhà Hàng, Quán Bar, khu cấp cứu Bệnh Viện); giá hợp lý + Stress; xếp hàng; hết tiền (Tổng tài, donate, ứng lương); chi phí vận hành và Thiếu bảo trì; Payday cơ bản (trả thiếu, đình công, nợ lương, thang vỡ nợ); `RunUntilPayday`; sự kiện Domain; kịch bản Game.Sim `core`; xóa `HubEconomy`.

**Ngoài phạm vi (sub-project sau):** chiến đấu và Monster thật (3), trang bị/độ bền/kính thật (4), Trạm Giao Thương/Thương nhân/kho/xưởng (2), vay nợ/cổ phiếu (5), Tòa Thị Chính/Ký túc xá/mở Zone/Danh tiếng/sự kiện (6), lưu/tải save.

## 2. Kiến trúc

```
src/Game.Domain/
  Simulation/  SimClock, EventQueue, SimRandom
  Hub/         HubWorld, Treasury, Payroll
  Trainers/    Trainer, Needs, PersonalityProfile, TrainerBrain
  Buildings/   ServiceBuilding
  World/       Zone, IFarmResolver + SimpleFarmResolver, IMaterialMarket + FixedPriceMarket
  Events/      IDomainEvent và các sự kiện
  Config/      SimConfig (+ SimConfig.Default)
```

*   **Domain thuần C#**, không phụ thuộc Unity; dữ liệu phẳng (Trainer trong `List`, tra theo chỉ số).
*   **`HubWorld`** là điểm vào duy nhất cho Unity, Game.Sim và test.
*   **`IFarmResolver`** (sub-project 3 thay bằng chiến đấu) và **`IMaterialMarket`** (sub-project 2 thay bằng Trạm + Thương nhân) là điểm cắm; lõi không đổi khi thay.
*   **Gold là `long`** (không dùng `double`) để tránh sai số và lệch kết quả giữa máy.
*   **Tất định:** mọi ngẫu nhiên qua `SimRandom` có seed; không `DateTime`, không `Random` static; sự kiện cùng thời điểm xếp theo số thứ tự vào hàng; duyệt Trainer theo thứ tự id.

## 3. Thời gian

*   1 ngày = 1440 phút in-game (= 15 phút thực), 1 tháng = 30 ngày. Ban ngày 06:00-18:00.
*   Sự kiện mốc: `Dawn` (06:00), `Dusk` (18:00), `DayStart` (00:00, trừ chi phí vận hành), `PaydayDue` (23:59 ngày 30).

```csharp
// Các loại sự kiện trong hàng đợi.
enum SimEventKind {
    TrainerDecide,     // Trainer chọn việc tiếp theo
    TrainerArriveZone, // tới Zone, bắt đầu farm
    FarmChunk,         // hết một khúc farm 30 phút
    TrainerArriveHub,  // về tới HUB
    ServiceDone,       // dùng xong một dịch vụ, nhả chỗ
    WaitTick,          // mỗi giờ chờ (hết tiền / hết chỗ): cộng Stress, thử Tổng tài
    Dawn, Dusk,        // 06:00 và 18:00
    DayStart,          // 00:00: trừ chi phí vận hành
    PaydayDue          // 23:59 ngày 30: RunUntilPayday dừng tại đây
}
```

Bất biến: mỗi Trainer có **đúng 1** sự kiện cá nhân đang chờ (trừ khi đang ở trong hàng đợi của công trình, khi đó công trình giữ Trainer).

## 4. Trainer và FSM

**Trạng thái:** `AtHub`, `Traveling`, `Farming`, `Returning`, `Queued`, `InService`, `WaitingForMoney`, `OnStrike`.

Ghi chú cài đặt: không có trạng thái `OnStrike` riêng; Trainer đang đình công là `AtHub` với lý do `Strike` (cờ đình công là `StrikeDaysLeft > 0`). Khi đình công, Trainer không dùng Bệnh Viện, chỉ dùng Nhà Hàng/Nhà Trọ/Bar.

**Dữ liệu Trainer:** `Id`, `Rarity`, `Rank`, `Level`, `Personality`, `Needs` (Stamina, Satiety, Hydration, Stress; 0-100), `Gold`, `TeamHp` / `TeamHpMax` (HP gộp của 3 Monster, tạm cho tới sub-project 3), `Backpack` (tổng số đơn vị), `HasNightVision` (cờ tạm cho tới sub-project 4), `ContractWage`, `WageDebtOwed` (HUB nợ Trainer), `WageAdvance` (Trainer ứng trước), `StrikeDaysLeft`, `State`, `StateReason`.

```csharp
// Một khúc farm: gọi bộ giải kết quả tạm, trừ nhu cầu, kiểm tra điều kiện về HUB.
void OnFarmChunk(Trainer t) {
    FarmResult r = farmResolver.Resolve(t, zone, minutes: 30);   // nguyên liệu, Gold, HP mất
    t.Backpack.Add(r.MaterialUnits); t.Gold += r.Gold; t.TeamHp -= r.HpLost;
    needs.Decay(t, TrainerActivity.Farming, 30);                 // tụt thanh theo hệ số tính cách
    if (ShouldReturn(t, out var reason)) StartReturn(t, reason);
    else Schedule(SimEventKind.FarmChunk, t, 30);
}

// Về HUB khi gặp điều kiện đầu tiên.
bool ShouldReturn(Trainer t, out ReturnReason reason);
//   một thanh (Stamina/Satiety/Hydration) < ngưỡng tính cách -> Hungry/Thirsty/Tired
//   Balo đầy                                                 -> BackpackFull
//   TeamHp <= 0                                              -> TeamDown
//   trời tối và !HasNightVision                              -> Night

// Ở HUB: bán hàng, rồi hồi nhu cầu cần nhất.
void OnArriveHub(Trainer t) {
    market.Sell(t);                                     // tạm: Thương nhân mua giá cố định, HUB thu thuế
    ServiceBuilding b = PickService(t);                 // null nếu mọi nhu cầu đều đủ
    if (b == null) { Schedule(SimEventKind.TrainerDecide, t, 0); return; }
    if (!t.CanAfford(b.PriceFor(t))) { BeginWaitForMoney(t); return; }
    b.Enqueue(t);                                       // hết chỗ thì xếp hàng FIFO
}
```

**`PickService` (thứ tự):** (1) Stress = 100 -> Bar (bắt buộc); (2) TeamHp < 100% -> Bệnh Viện; (3) trời tối và không có kính -> Nhà Trọ; (4) thanh thấp nhất dưới mức "đủ" (60) -> công trình của thanh đó (Stamina -> Nhà Trọ; Satiety/Hydration -> Nhà Hàng); (5) Stress ≥ 70 -> Bar; (6) không có gì -> `null`.

**`TrainerDecide`:** nếu đang đình công thì chỉ dùng Nhà Hàng/Nhà Trọ/Bar khi cần, còn lại nghỉ ở HUB 1 giờ; nếu trời tối và không có kính thì ở HUB tới `Dawn`; ngược lại đi Zone 1 (`Traveling`, mất `Zone.TravelMinutes`).

**Ban đêm:** khi `Dusk`, mọi Trainer đang farm mà không có kính bắt đầu về HUB (lý do `Night`).

## 5. Công trình dịch vụ

```csharp
// Công trình dịch vụ: số chỗ cố định, hàng đợi FIFO, giá do Giám đốc đặt.
sealed class ServiceBuilding {
    BuildingKind Kind; int Level;                // cấp 1-25
    int Slots => Maintained ? 5 + (int)(0.8 * Level) : (5 + (int)(0.8 * Level)) / 2;
    long Price; long FairPrice;                  // giá Giám đốc đặt, giá hợp lý theo cấp
    int ServiceMinutes;                          // thời gian phục vụ một lượt
    long UpkeepPerDay; bool Maintained;          // thiếu tiền vận hành -> Maintained = false
}
```

| Công trình | Phục vụ | Thời gian | Giá hợp lý khởi điểm | Ghi chú |
|---|---|---|---|---|
| Nhà Trọ | Stamina -> 100 | 360 phút | 45 | Ban đêm ngủ tới `Dawn` (tối đa 360 phút) |
| Nhà Hàng | Satiety và Hydration -> 100 | 30 phút | 40 | Chế độ cấn nợ: miễn phí, trừ vào nợ lương |
| Quán Bar | Stress -> 20 | 60 phút | 800 | Đình công vẫn tự trả tiền |
| Bệnh Viện (cấp cứu) | TeamHp -> TeamHpMax | 30 phút | 1.4 Gold/HP (làm tròn lên) | |

*   **Số chỗ:** `5 + floor(0.8 x Level)`; Thiếu bảo trì thì chia đôi.
*   **Stress do giá:** mỗi lượt cộng `10 x max(0, Price / FairPrice - 1) x PriceSensitivity` (Tư bản x1.5).
*   **Tư bản** được giảm 10% giá dịch vụ (`PriceFor(t)`).
*   **Tiền vào:** doanh thu dịch vụ vào Kho bạc; giá vốn dịch vụ tạm tính 25% doanh thu (cho tới sub-project 2).
*   **Chi phí vận hành:** 50 Gold/công trình/ngày, trừ lúc `DayStart`. Không đủ tiền thì công trình đó `Maintained = false` tới `DayStart` kế tiếp trả đủ; nợ không cộng dồn.

## 6. Hết tiền và xếp hàng

*   **Xếp hàng:** mỗi giờ chờ trong hàng cộng Stress +2.
*   **Hết tiền (`WaitingForMoney`):** mỗi giờ (`WaitTick`) Stress +2 và có 10% xác suất Tổng tài xuất hiện. Sau 24 giờ chờ, Tổng tài chắc chắn xuất hiện. Tổng tài donate đủ cho dịch vụ đang cần x2.
*   **Lệnh Giám đốc:** `Donate` (không hoàn lại), `AdvanceWage` (trừ vào Payday kế tiếp). Nhận tiền xong thì Trainer xử lý lại từ `OnArriveHub`.

## 7. Payday

*   `RunFor` / `RunUntilPayday` dừng tại `PaydayDue`; sau đó `RunFor` chạy 0 phút cho tới khi gọi `ResolvePayday()`.
*   **Quỹ lương** = Σ (`ContractWage` + `WageDebtOwed` - `WageAdvance`) của mọi Trainer.
*   **Đủ tiền:** trả đủ, xóa nợ lương và ứng lương, `UnpaidStreak = 0`.
*   **Thiếu tiền:** trả mỗi Trainer theo tỉ lệ `Treasury / quỹ lương` (làm tròn xuống), Kho bạc về 0; phần thiếu cộng vào `WageDebtOwed`; **toàn bộ** đình công 5 ngày; `UnpaidStreak++`.
    *   `UnpaidStreak = 2`: mọi Trainer Stress +30.
    *   `UnpaidStreak ≥ 3`: chế độ cấn nợ: kết quả farm x0.5; Nhà Hàng miễn phí cho Trainer đó, giá trị trừ vào `WageDebtOwed`; Bar vẫn tự trả tiền.
*   **Lương hợp đồng khởi điểm:** `2,900 x 1.7^bậcRarity`, Tư bản x1.3 (đàm phán làm ở sub-project 5).

## 8. Giao diện

```csharp
// Điểm vào duy nhất của Domain.
public sealed class HubWorld {
    public HubWorld(SimConfig config, int seed);

    // Chạy tối đa `minutes` phút in-game, dừng sớm nếu tới Payday.
    public RunResult RunFor(int minutes);          // RunResult: MinutesRun, StopReason (Completed | PaydayDue)
    // Dùng cho offline và Đồng Hồ Cát: chạy tới ngay trước Payday gần nhất.
    public RunResult RunUntilPayday();
    // Người chơi xử lý Payday.
    public PaydayOutcome ResolvePayday();          // PaidRatio, StrikeStarted, UnpaidStreak

    // Lệnh Giám đốc. Lỗi người chơi trả về Rejected kèm lý do, không ném exception.
    public CommandResult Donate(int trainerId, long gold);
    public CommandResult AdvanceWage(int trainerId, long gold);
    public CommandResult SetPrice(BuildingKind building, long price);

    // Truy vấn chỉ đọc cho UI.
    public IReadOnlyList<TrainerView> Trainers { get; }
    public IReadOnlyList<BuildingView> Buildings { get; }
    public long Treasury { get; }
    public SimTime Now { get; }
    public PaydayForecast Forecast { get; }        // ngày còn lại, quỹ lương cần, Kho bạc hiện có

    // Sự kiện Domain (C# thuần); Presenter dùng R3 ở lớp Presentation.
    public event Action<IDomainEvent> EventRaised;
}
```

**Sự kiện Domain:** `TrainerStateChanged` (from, to, reason), `ServiceUsed` (building, price, fairPrice, stressAdded), `QueueChanged`, `TreasuryChanged` (delta, reason), `PaydayDue`, `PaydayResolved`, `BuildingMaintenanceChanged`, `DonationReceived` (source: Patron = Tổng tài / Director).

**Lỗi:** tham số/cấu hình sai -> exception. Lệnh người chơi không hợp lệ (donate vượt Kho bạc, trainerId không tồn tại, giá ≤ 0) -> `CommandResult.Rejected(reason)`.

**Bất biến:** Kho bạc ≥ 0; thanh nhu cầu trong 0-100; mỗi chỗ ≤ 1 Trainer; mỗi Trainer ≤ 1 sự kiện cá nhân đang chờ.

## 9. Tham số khởi điểm (`SimConfig.Default`)

Quy đổi từ [13](../../designs/13_Balance_Parameters.md) §8 (theo ngày) sang phút; là giá trị khởi điểm, chỉnh khi cân bằng.

| Tham số | Giá trị |
|---|---|
| Tụt thanh khi farm/đi lại (mỗi giờ) | Stamina -6, Satiety -5, Hydration -6, Stress +0.5 |
| Tụt thanh khi ở HUB / chờ (mỗi giờ) | Stamina -2, Satiety -2, Hydration -2 |
| Mức "đủ" của thanh | 60 |
| `NightSleepBelow` | 90 (ban đêm không kính: chỉ vào Nhà Trọ khi Thể lực < 90) |
| Ngưỡng về HUB theo tính cách | Háo chiến 15, Nhát gan 50, Tham ăn 30 (Satiety 50), Tư bản 25 |
| Hệ số tính cách | Háo chiến: loot x1.25, HP mất x1.5. Nhát gan: loot x0.85, HP mất x0.5. Tham ăn: Satiety tụt x1.6. Tư bản: giảm giá dịch vụ 10%, độ nhạy giá x1.5, tỉ lệ nhặt nguyên liệu 1.0 (các tính cách khác 0.85) |
| Zone 1 | Đi bộ 30 phút |
| Farm tạm (mỗi khúc 30 phút, Trainer Common) | 3 đơn vị nguyên liệu x tỉ lệ nhặt; 2 Gold; mất 3 HP; nhân `1.7^bậcRarity` |
| TeamHpMax | 300 |
| Balo | 30 đơn vị |
| Bán nguyên liệu (tạm) | `FixedPriceMarket` đóng vai người mua bên ngoài (Thương nhân): trả Trainer 10 Gold/đơn vị (Gold từ ngoài vào); HUB thu thuế giao dịch 20% trên giá đó. HUB không mua, không bán nguyên liệu cho tới sub-project 2 |
| Ngưỡng Bar | ≥ 70; bắt buộc ở 100 |
| Stress khi chờ | +2/giờ |
| Tổng tài | 10%/giờ chờ; chắc chắn sau 24 giờ; donate = giá dịch vụ cần x2 |
| Đình công | 5 ngày |
| Vận hành | 50 Gold/công trình/ngày |
| Khởi tạo kịch bản mặc định | 10 Trainer Common, tính cách ngẫu nhiên; 4 công trình dịch vụ Lv5 (9 chỗ); Kho bạc 20,000 |

## 10. Kiểm thử (xUnit)

| File | Kiểm tra |
|---|---|
| `SimClockTests`, `EventQueueTests` | Mốc 06:00/18:00/00:00; Payday ở 23:59 ngày 30; sự kiện cùng thời điểm giữ thứ tự vào hàng |
| `TrainerBrainTests` | Về HUB đúng ngưỡng từng tính cách, khi Balo đầy, TeamHp = 0, 18:00 không kính; `PickService` đúng thứ tự §4 |
| `ServiceBuildingTests` | Số chỗ = 5 + floor(0.8 x cấp); FIFO; Thiếu bảo trì chia đôi chỗ; Stress theo mức chênh giá; Tư bản giảm giá 10% |
| `PayrollTests` | Đủ tiền trả đủ; thiếu chia theo tỉ lệ và toàn bộ đình công; nợ lương cộng dồn; thang Payday 1/2/3+; ứng lương trừ kỳ sau |
| `HubWorldTests` | Cùng seed -> kết quả giống hệt; `RunUntilPayday` dừng đúng và báo thời gian còn lại; `RunFor` chạy 0 phút khi Payday chưa xử lý; lệnh bị từ chối đúng lúc; Kho bạc không âm; không Trainer nào chờ tiền quá 24 giờ |

## 11. Game.Sim và dọn mô hình cũ

*   **Xóa:** `src/Game.Domain/HubEconomy.cs`; các test phụ thuộc trong `DomainTests.cs`, `LoansAndStockTests.cs`, `RemainingSystemsTests.cs` (giữ test của `EnhancementModel`, `GachaModel`, `StockMarket`, `FinanceModel`, `RebellionModel`, `UpgradeLadder`, tách file nếu cần); các kịch bản Game.Sim dựa trên `HubEconomy` (mặc định, `stress`, `calibrate`, `crisis`, `upgrades`, `bar`, `loans`, `personality`, `genebank`).
*   **Giữ:** kịch bản `ladders`, `stock`.
*   **Thêm kịch bản `core`** (mặc định khi chạy không tham số): 10 Trainer Common, 3 tháng; in lợi nhuận HUB theo tháng, Gold trung bình Trainer, % thời gian Farm/Đi lại/Xếp hàng/Dịch vụ/Chờ tiền, hàng đợi dài nhất từng công trình, kết quả từng Payday. Tiêu chí hiệu năng: 3 tháng x 10 Trainer chạy dưới 1 giây.
*   **Tài liệu:** README cập nhật lệnh Game.Sim; doc 13 ghi chú số liệu §8-§13 đến từ mô hình cũ đã xóa; 99_Open_Issues mục O7 cập nhật (các lệch về lương, vận hành, Stress thuế đã giải quyết trong lõi mới; phần vay, Gene Bank, Rebellion chuyển sang sub-project tương ứng).

## 12. Tiêu chí hoàn thành

*   `dotnet test` xanh, gồm toàn bộ test §10 và test các mô hình độc lập được giữ.
*   `dotnet run --project tools/Game.Sim` chạy kịch bản `core` dưới 1 giây và in đủ số liệu §11.
*   Không còn tham chiếu tới `HubEconomy` / `EconomyParams` trong repo.
