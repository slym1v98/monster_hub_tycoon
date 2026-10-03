# Sub-project 4: Itemization & Gear — Implementation Plan

> Task-by-task, RED → GREEN → REFACTOR. Domain thuần C#, comment tiếng Việt. Mọi tham số mới ghi workbook `docs/balance/MonsterHUB_Balance.xlsx` sheet `Gear & Durability` (Id `gear.…`, Locked/Prototype/TBD + nguồn).

Spec: `docs/superpowers/specs/2026-10-02-domain-itemization-design.md`

## Global Constraints
- `HubWorld` là điểm vào duy nhất; ngẫu nhiên qua `SimRandom`; tie-break theo ID ổn định.
- Không đưa hào quang/túi đồ vào HP Trainer (Trainer không có HP).
- Độ bền 0 = món vô hiệu, không cộng chỉ số.
- Mọi thay đổi Gold/vật phẩm/trang bị là transition + event tường minh, view bất biến.

## Task 1: Gear domain types + catalog
Files: `src/Game.Domain/Gear/GearTypes.cs`, `GearItem.cs`, `GearCatalog.cs`, `GearConfig.cs`; test `tests/Game.Domain.Tests/GearCatalogTests.cs`.
- 18 slot kind: 6 TrainerUtility, 6 Aura, 6 MonsterCombat. `GearItem` gồm Id, Slot, Tier(1..5), Enhance(0..20), Stars(1..5), Refine(0..4), Durability/MaxDurability, SetId.
- `GearCatalog`: base stat theo slot×tier, refine multiplier, star pct, set definitions 2/4/6.
- Test: 18 slot, tier bounds, slot ownership, set counts.

## Task 2: GearScore + hiệu ứng chỉ số
Files: `GearScore.cs`, `GearStats.cs`; test `GearScoreTests.cs`.
- `GearScore(item)` = tổng chỉ số hiệu dụng; `GearLoadout` cộng dồn theo slot; set bonus 2/4/6.
- Test: độ bền 0 vô hiệu; enhance/stars/refine tăng score; set bonus chỉ khi đủ món Active.

## Task 3: Cường hóa / Nâng Sao / Tinh Luyện / Sửa chữa
Files: `GearForge.cs`; test `GearForgeTests.cs`.
- Enhance +1..+20 dùng `EnhancementModel` + `SimRandom`, curves từ GDD 13, thất bại từ +11 có tỉ lệ vỡ; Bùa Bảo Hộ chặn vỡ; tiêu Gold + Đá.
- Nâng Sao dùng GDD 13 success 90/75/55/35% cho các bước tới trần 5 sao; fail từ target sao 3 rớt 1 sao; đồ hiến tế cùng slot bị tiêu thụ khi thử. GDD 13 ghi thêm bước 5/20%, được giữ trong workbook như xung đột tài liệu và không mở khóa sao 6.
- Tinh Luyện qua bốn grade dùng success 70/50/30/15%, mỗi lần tiêu Gold theo 1000×2.5^step, Tinh Thể Boss và Nước Cất; thất bại giữ nguyên grade.
- Gold/vật phẩm chỉ trừ sau khi xác thực affordability; lệnh/event ghi rõ thành công/thất bại/chi phí.
- Repair: phí Gold theo độ bền thiếu, trả Treasury.
- Test: tất định seed, vỡ +11, Bùa, sao/refine tiêu hao đúng, repair.

## Task 4: Độ bền + hao mòn
Files: `GearWear.cs`; test `GearWearTests.cs`.
- MonsterCombat hao mỗi action/hit; TrainerUtility hao theo phút farm; Aura không hao. Về 0 vô hiệu.
- Hook vào `BattleResult`/farm.

## Task 5: Trainer/Monster loadout + snapshot
Files: `GearLoadout.cs`, sửa `Trainer.cs`, `Monster.cs`, `TrainerSnapshot.cs`, `MonsterSnapshot.cs`, `IExpeditionResolver.cs`.
- Monster gear cộng vào snapshot combat (không đổi Stats gốc). Balo→BackpackCapacity, Kính→Nhìn đêm, Bình nước→hệ số tụi Nước, Găng→sản lượng.

## Task 6: Gear market (offer/accept/buyback)
Files: `GearMarket.cs`; test `GearMarketTests.cs`.
- Giám đốc chào hàng; Trainer chấp nhận theo GearScore, giá, tiền. Đồ cũ biến mất trừ khi HUB mua lại.

## Task 7: HubWorld commands/views/events/invariants + Game.Sim `gear`
Files: sửa `HubWorld.*`, `HubWorldTypes.cs`, `events`; test `HubWorldGearTests.cs`; sửa `tools/Game.Sim`.
- Lệnh: offer gear, buyback, enhance, star, refine, repair. View gear cho Trainer/Monster.
- Invariant: 1 món/slot, Durability ∈ [0,Max], Gold bảo toàn, số món bảo toàn khi mua/bán.
- Scenario `gear`: mua/bán, cường hóa, hao mòn, sửa, đối soát Gold/vật phẩm = 0.

## Task 8: Workbook + docs
- Sheet `Gear & Durability`; thêm đủ tunables runtime, Locked/Prototype/TBD, unit, nguồn và kiểm tra mọi `gear.*` từ config/scenario đều được ghi.
- Cập nhật `05`, `12`, `13`, `99`, `README`, task report/checklist. So khớp hành vi với GDD, build/test, chạy `Game.Sim gear`, review conservation/determinism; commit sub-project.
