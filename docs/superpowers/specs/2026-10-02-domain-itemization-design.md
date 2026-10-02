# Sub-project 4: Trang bị, Gear Score, độ bền, Cường hóa/Nâng Sao/Tinh Luyện

Ngày: 2026-10-02. Nguồn: [05](../../designs/05_Itemization_Gear_System.md), [12](../../designs/12_Item_Catalog.md), [04](../../designs/04_Monster_System.md), [02](../../designs/02_HUB_Economy_Infrastructure.md).
Quy ước: tên code tiếng Anh, comment tiếng Việt có dấu, Domain thuần C#, ngẫu nhiên qua `SimRandom`, tie-break theo ID ổn định.

## 1. Khóa bởi GDD (Locked)
- 30 slot: Trainer Tiện ích 6 (Nón, Áo, Balo, Giày, Bình nước, Găng; Xưởng Dệt), Trainer Hào quang 6 (Còi, Huy hiệu, Áo choàng, Kính, Trang Sức, Vệ tinh; Tiệm Kim Hoàn), mỗi Monster 6 (Vũ khí, Giáp, Vòng cổ, Lục lạc, Guốc, Lõi Nguyên tố; Lò Rèn).
- Mỗi slot chứa tối đa 1 món; món ở đúng loại slot.
- Bán: Giám đốc chào hàng từng Trainer; Trainer chấp nhận theo Gear Score, giá, tiền còn lại. Đồ cũ biến mất trừ khi HUB yêu cầu mua lại.
- Cường hóa +1..+20, flat stat, đốt Gold + Đá Cường hóa; từ +11 thất bại có xác suất vỡ; Bùa Bảo Hộ chống vỡ.
- Nâng Sao 1..5, % chỉ số ẩn, hiến tế đồ "rác" cùng loại.
- Tinh Luyện Normal→Mythic, cần Tinh Thể Boss Thế Giới + Nước Cất (giá x10).
- Độ bền: Monster hao theo tung chiêu/bị đánh (sửa Lò Rèn); Tiện ích Trainer hao theo thời gian farm + thời tiết (sửa Xưởng Dệt); Hào quang không hao. Độ bền 0 = vô hiệu hóa (không tính chỉ số).
- Set Bonus 2/4/6 món.
- Kính từ một Tier trở lên có Nhìn đêm (thay cờ `Trainer.HasNightVision`).

## 2. Mô hình
- `Game.Domain.Gear`: `GearSlot`, `GearGroup`, `GearItem` (Id, Slot, Tier 1..5, Level, Stars, Refine, Durability, SetId), `GearLoadout` (map slot→item, chủ sở hữu), `GearCatalog` (định nghĩa slot, chỉ số gốc theo Tier, set), `GearConfig` (mọi hằng số kèm Id/đơn vị/trạng thái/nguồn), `GearScore`, `GearForge` (Enhance/StarUp/Refine/Repair), `GearWear` (hao mòn), `GearMarket` (offer/accept/buyback).
- Chỉ số: `Effective = (Base[tier] + Level * PerLevel) * (1 + Stars*StarPct) * RefineMult[grade]`; vô hiệu khi Durability = 0. Set bonus cộng theo số món Active cùng SetId (2/4/6).
- Monster gear cộng vào `MonsterStats` khi chụp snapshot (không sửa gen/Stats gốc). Trainer Tiện ích: Balo → `BackpackCapacity`, Kính → nhìn đêm, Bình nước → hệ số tụt Nước; Hào quang buff cho Monster.
- Hao mòn: Monster mỗi action/hit trong `BattleResult`; Trainer theo phút farm (+ hệ số thời tiết = 1 đến khi SP6).
- Sửa chữa: phí Gold tỉ lệ độ bền mất, trả cho HUB (Treasury) qua event; ngưỡng Trainer tự sửa khi về HUB.

## 3. Tham số (Prototype/TBD)
GDD không cho số: chỉ số gốc, hệ số mỗi cấp, %/sao, hệ số Refine, tỉ lệ Nâng Sao, hao mòn, phí sửa, ngưỡng Gear Score chấp nhận → `Prototype` hoặc `TBD`. `EnhancementModel` (+1..+20 success/cost/break) được kế thừa nhưng tách khỏi `System.Random`: dùng `SimRandom`. Tất cả vào workbook, sheet `Gear & Durability`, Id tiền tố `gear.`.

## 4. Ngoài phạm vi
Thời tiết thực (SP6), Gem/Bùa mua bằng Gem (SP monetization), Set được "ép AI vứt bỏ" đã gồm trong chấp nhận theo Gear Score.

## 5. Kiểm thử
Slot hợp lệ, vô hiệu khi độ bền 0, Cường hóa tất định theo seed, vỡ chỉ từ +11 và Bùa chặn, Nâng Sao tiêu hao đồ cùng loại, Tinh Luyện cần vật liệu, set 2/4/6, offer chấp nhận/từ chối, đồ cũ biến mất/mua lại, hao mòn + sửa, bảo toàn Gold/vật phẩm, tất định cùng seed, Game.Sim `gear`.
