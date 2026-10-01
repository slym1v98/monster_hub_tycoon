# SUB-GDD 13: BALANCE PARAMETERS (BẢNG THAM SỐ CÂN BẰNG)

Đây là nơi duy nhất ghi các con số kinh tế. Các tài liệu khác (Quest, Thành tựu, Monetization) tham chiếu về đây. Cột "Trạng thái": **Chốt** = đã quyết trong thiết kế; **Khởi điểm** = giá trị tạm để dựng prototype (kèm "mô phỏng" nếu đã chạy qua `tools/Game.Sim`, "chưa mô phỏng" nếu mới là giá trị đề xuất); **TBD** = chưa có số, sẽ xác định bằng mô phỏng (Roadmap Bước 5). Khi dữ liệu thật có, chuyển sang CSV/Google Sheets và parse vào ScriptableObjects (xem [00_Tech_Stack](../00_Tech_Stack.md)).

## 1. Thời gian
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| 1 ngày in-game | 15 phút thực (Payday mỗi 7.5 giờ thực) | Chốt |
| Ban ngày / Ban đêm | 06:00-18:00 / 18:00-06:00 | Chốt |
| 1 tháng (Payday) | 30 ngày | Chốt |
| Chu kỳ cổ tức | 15 ngày | Chốt |
| Tick | 0.2 giây | Chốt |
| Giới hạn offline | Mô phỏng tới Payday gần nhất (tối đa 30 ngày in-game, khoảng 7.5 giờ thực) | Chốt |

## 2. Dân số và quy mô
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Trainer tối đa | 30 | Chốt |
| Monster / Trainer | 3 (1 Active + 2 Reserve) | Chốt |
| Slot trang bị / bộ | 30 (12 + 3 x 6) | Chốt |
| Ngưỡng Swap | HP < 15% | Chốt |

## 3. Tài chính
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Thuế giao dịch mặc định (FTUE) | 20% | Chốt |
| Ngưỡng phạt Thanh Tra | Thuế giao dịch > 30% hoặc Stress đỏ | Khởi điểm |
| Giám đốc giữ cổ phần khi IPO | 51% | Chốt |
| Markup trang bị | x5 đến x10 giá vốn | Chốt |
| Markup Nước Cất | x10 | Chốt |
| Thuế Tự Do Tài Chính | 30% lợi nhuận chứng khoán đã chốt; lỗ không bị thuế | Khởi điểm (mô hình + test) |
| Cổ tức (mỗi 15 ngày) | 10% doanh thu 15 ngày của công trình đã IPO, chia theo tỉ lệ cổ phần; số cổ phiếu IPO chọn để ra tỉ suất mục tiêu (mặc định 8%/năm) | Khởi điểm (mô hình + test) |
| Biến động giá cổ phiếu | ±3%/ngày ngẫu nhiên + 0.5 x thay đổi Traffic; khoảng 6 cơn hoảng loạn (giảm ≥10% trong 3 ngày), 3.6 cú sập (giảm ≥20% trong 15 ngày) và 5 đợt tăng mạnh mỗi năm | Khởi điểm (mô phỏng) |
| Lương theo Rarity (5 bậc) | Hợp đồng cố định; đề nghị ban đầu neo quanh 30% thu nhập kỳ vọng theo chỉ số, Rarity, tính cách (mô phỏng cũ dùng 30% thu nhập ròng tháng trước); thu nhập và chi tiêu nhân 1.7 mỗi bậc Rarity | Khởi điểm (mô phỏng) |
| Payday trả thiếu | Chia đều theo tỉ lệ; thiếu là toàn bộ đình công; thang vỡ nợ Payday 1 / 2 / 3+ ([02](02_HUB_Economy_Infrastructure.md) §2) | Chốt |
| Cho Trainer Vay: lãi suất, hạn mức, quá hạn | Lãi mặc định 10%/Payday (Giám đốc chỉnh 5-40%); hạn mức mặc định **2 lần lương tháng**; trừ 50% thu nhập và lương để trả nợ; quá hạn = dư nợ vượt hạn mức 2 Payday liên tiếp thì Trainer đình công | Khởi điểm (mô phỏng) |
| Vay Từ Trainer Rank V | Tối đa khoảng 50% tiền mặt mỗi Trainer; lãi 5%/Payday; hạn 1-2 Payday; quá hạn thì cấn nợ bằng dịch vụ miễn phí | Khởi điểm (chưa mô phỏng) |
| Cách thu phí Ngân Hàng Gene | **Mỗi Payday, sau khi trả lương** (không thu hằng ngày); thiếu phí thì bị tịch thu 1 Monster | Khởi điểm (mô phỏng) |
| Phí lưu trữ Ngân Hàng Gene / Monster / ngày | 20 Gold (tính cho 30 ngày, thu ở Payday) | Khởi điểm (mô phỏng) |

## 4. Trang bị và Monster
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Khoảng Cường hóa | +1 đến +20 | Chốt |
| Bắt đầu có nguy cơ vỡ đồ | +11 | Chốt |
| Tỉ lệ thành công Cường hóa | +1: 100%, giảm 5%/cấp tới +10 (55%); +11: 50%, giảm 3.5%/cấp tới +20 (18.5%) | Khởi điểm (mô phỏng) |
| Vỡ đồ khi thất bại từ +11 (không có Bùa) | 30% | Khởi điểm |
| Chi phí mỗi lần Cường hóa | CostBase x 1.30^(cấp-1) | Khởi điểm |
| Nâng Sao (5 bước) | Thành công 90 / 75 / 55 / 35 / 20%; thất bại từ bước 3 rớt 1 sao; chi phí 300 x 1.8^bước + 100 phôi Hiến Tế | Khởi điểm (mô phỏng) |
| Tinh Luyện (Normal -> Mythic, 4 bước) | Thành công 70 / 50 / 30 / 15%; thất bại chỉ mất chi phí; chi phí 1,000 x 2.5^bước | Khởi điểm (mô phỏng) |
| Tăng tư chất (Rarity Common -> Ultimate, 4 bước; Monster Lv ≥ 40) | Thành công 60 / 40 / 25 / 10%; thất bại mất chi phí (Bùa Bảo Hộ bảo vệ vật phẩm); chi phí 2,000 x 3^bước | Khởi điểm (mô phỏng, trước đây gọi là Tiến hóa) |
| Tiến hóa (đổi hình thái) | Tối đa 2 lần; tỉ lệ và chi phí TBD | TBD |
| Cấp Monster | 20 x (Rank Trainer - 1) + ⌈Lv Trainer / 5⌉ | Chốt |
| Hệ số IV | D 0.8, C 0.9, B 1.0, A 1.1, S 1.2, SS 1.3, SSS 1.5 | Khởi điểm |
| Hệ số K của Điểm Quản Lý (Rebellion) | K = 20: một bậc Rarity tương đương 20 cấp độ. Điểm Quản Lý = Cấp Monster + 20 x bậc; Điểm Lãnh đạo = 20 + 20 x bậc + Cấp quy đổi Trainer (20 x (Rank - 1) + Lv/5) + thưởng | Khởi điểm (mô hình + test; `RebellionModel` cần cập nhật cấp quy đổi) |
| Bảng khắc chế 9 hệ (hệ số 2 / 0.5 / 1, không có miễn nhiễm) | Xem [04](04_Monster_System.md) §2 | Khởi điểm |
| Tốc độ hao mòn độ bền | Đồ Monster và đồ Tiện ích; Hào quang không hao. 2% độ bền tối đa mỗi chuyến (hỏng sau khoảng 50 chuyến); sửa chữa 25 Gold/chuyến cho Common, đã nằm trong giá sửa đồ | Khởi điểm (mô phỏng) |

## 5. Công cụ trả phí
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Cooldown Roi Kỷ Luật | 3 ngày in-game | Khởi điểm |
| Giới hạn Đồng Hồ Cát | 1 lần/ngày thực, dừng trước Payday, phần chưa tua được giữ lại | Khởi điểm |

## 6. Gacha
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Tỉ lệ Thư Mời Hoàng Gia | Epic 80%, Legendary 17%, Ultimate 3% (phải công bố trong game) | Khởi điểm (mô phỏng) |
| Pity | Ultimate chắc chắn sau 60 lượt (kỳ vọng ~28 lượt); Legendary trở lên chắc chắn sau 10 lượt (kỳ vọng ~4.5 lượt) | Khởi điểm (mô phỏng) |

## 7. Mốc Quest và Thành tựu
Các mốc ghi trong [08](08_Quests_Achievements_Collections.md) (ví dụ 50,000 Gold/ngày, 1,000,000 Gold) là **placeholder**, cần chỉnh sau khi có số liệu mô phỏng ở mục 3 và 4.

## 8. Chi tiêu của Trainer Common (khởi điểm, mô phỏng)
Đơn vị: Gold. Thu nhập thô của Trainer Common = 100 Gold/chuyến x 4 chuyến/ngày = 400 Gold/ngày (đây là đơn vị tham chiếu, không phải giá cuối cùng). Các Rarity cao nhân 1.7 mỗi bậc.

| Khoản | Giá trị Common | Ghi chú |
|---|---|---|
| Viện phí | 1.4 Gold/HP (mất khoảng 20 HP/chuyến) | |
| Ăn (Nhà Hàng) | 80 Gold/ngày | |
| Ngủ (Nhà Trọ) | 45 Gold/ngày | |
| Sửa trang bị (Lò Rèn) | 25 Gold/chuyến | |
| Mua trang bị | 40% số vàng dư mỗi ngày | AI luôn "nghèo đi" |
| Bar | 800 Gold/lần, khi Stress đạt 100 (tăng 25/ngày); Bar chiếm khoảng 10% doanh thu dịch vụ | Trainer vào Bar trước khi mua trang bị |
| Gia vốn dịch vụ | 25% doanh thu | |
| Hiệu suất gia công (HUB thu về trên 1 Gold nguyên liệu) | 0.92 | |
| Chi phí vận hành công trình | 40 Gold/công trình/ngày | |

## 9. Kết quả và giới hạn của mô phỏng sơ bộ
Chạy bằng `tools/Game.Sim` (xem [README](../../README.md)); kết quả đầy đủ tại [economy_sim_output.md](../reports/economy_sim_output.md). Đây là **mô hình đồ chơi** (một dòng tiền, không có chứng khoán, vay nợ, sự kiện, thời tiết, Boss), nên **chỉ dùng tỉ lệ và xu hướng, không dùng số tuyệt đối**.

*   **Cường hóa:** Chi phí kỳ vọng để lên +15 khoảng 1,030 x CostBase nếu không có Bùa, 440 x CostBase nếu có Bùa giá 6 x CostBase. Lên +20: khoảng 34,600 x CostBase (không Bùa) so với 2,600 x CostBase (có Bùa). Bùa hòa vốn ở giá khoảng 56 x CostBase cho +15 và 1,000 x CostBase cho +20. Nghĩa là Giám đốc có biên rất rộng để định giá Bùa mà AI vẫn có lợi khi mua, nhất là ở +20.
*   **Gacha:** Pity 60 cho Ultimate (xác suất 3%) cho kỳ vọng 28 lượt; Pity 40 giảm còn 23.5; Pity 80 tăng lên 30.4. Phần lớn người chơi sẽ chạm Pity nếu để xuống 40-60.
*   **Thuế:** Mỗi +10 điểm thuế giao dịch làm lợi nhuận HUB tăng khoảng 5% nhưng vàng trung bình của Trainer giảm khoảng 12%. Thuế là đòn bẩy yếu với HUB, mạnh với Trainer. Tức là rủi ro Thanh Tra (thuế > 30%) là cái giá chính của việc tăng thuế.
*   **Phát hiện chính:** Nếu Giám đốc không tiêu tiền (chỉ tích trữ), HUB gần như **không thể phá sản**: tiền lương Trainer quay lại HUB qua dịch vụ nên Đình công chỉ xảy ra khi lương vượt khoảng 100% thu nhập ròng của Trainer. Khủng hoảng Payday vì vậy phải đến từ **chính quyết định tái đầu tư của người chơi** và các **cú sốc** (xem §10), không phải từ mô hình thu chi tự nhiên.

## 10. Rủi ro Payday: chi phí và cú sốc phía Giám đốc (khởi điểm, mô phỏng)
Mô hình thêm hai thứ: (1) chính sách tái đầu tư (Giám đốc giữ lại `Dự trữ x quỹ lương dự kiến`, tiêu một nửa phần vượt mỗi ngày vào nâng cấp, mở rộng); (2) cú sốc ngẫu nhiên (Siege, Thanh Tra phạt, Boss làm hỏng công trình) có chi phí bằng **10 ngày lợi nhuận trung bình**.

| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Tỉ lệ tái đầu tư phần vượt dự trữ | 50%/ngày | Khởi điểm |
| Chi phí cú sốc | 10 ngày lợi nhuận | Khởi điểm |
| Xác suất cú sốc | 20%/tháng (nhẹ), 50%/tháng (nặng) | Khởi điểm |

Tỉ lệ Payday dẫn đến Đình công (30 Trainer, 24 tháng, 100 lần chạy):

| Dự trữ (x quỹ lương) | Không sốc | Sốc 20%/tháng | Sốc 50%/tháng |
|---|---|---|---|
| 0.50x | 100% | 100% | 100% |
| 0.75x | 50% | 66% | 82% |
| 0.90x | 8% | 34% | 46% |
| 1.00x | 0% | 6% | 17% |
| 1.25x | 0% | 4% | 12% |
| 2.00x | 0% | 0% | 2% |

Diễn giải: có một **ngưỡng rõ rệt quanh 0.75-1.0x quỹ lương**. Dưới ngưỡng, Đình công gần như chắc chắn. Trên ngưỡng, rủi ro chủ yếu đến từ cú sốc và giảm dần khi dự trữ tăng. Đây là hình dạng rủi ro mong muốn cho game: người chơi tham lam tái đầu tư quá tay sẽ bị phạt, người cẩn thận ít bị. Ngưỡng sắc là hệ quả của mô hình lợi nhuận ổn định; trong game thật, lợi nhuận biến động sẽ làm đường cong dốc ít hơn.

## 11. Nâng cấp công trình và chi phí vận hành (khởi điểm, mô phỏng)
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Hoàn vốn mục tiêu (mô hình cũ 3 cấp) | 2 tháng cho bước đầu, 4 tháng cho bước sau; cần làm lại cho 5 / 25 cấp | Khởi điểm (cần mô phỏng lại) |
| Chi phí vận hành mỗi công trình | 50 Gold/ngày (khoảng 10% lợi nhuận tháng của HUB 30 Trainer, 16 công trình) | Khởi điểm |
| Lợi nhuận ròng mỗi Trainer Common (sau lương) | khoảng 7,900 Gold/tháng; lương khoảng 2,900 Gold/tháng | Kết quả mô phỏng |
| Dân số theo Ký túc xá và mốc Zone | 10 / 15 / 20 / 25 / 30 Trainer (Ký túc xá Lv1-5, Zone 1-5) | Chốt |
| Cấp tối đa công trình | Tòa Thị Chính và công trình dịch vụ 25; Ký túc xá, Trạm, Nhà máy, Lò Phản Ứng, Lò Rèn, Xưởng Dệt, Kim Hoàn 5 | Chốt |
| Hiệu suất gia công theo cấp | 0.85 / 0.92 / 0.99 (mô hình cũ 3 cấp) | Khởi điểm (cần làm lại cho 5 cấp) |
| Chênh giá Thương nhân | Mua của Trainer rẻ hơn giá Trạm 5-10%; bán cho HUB đắt hơn giá Trạm 5-10% | Khởi điểm |
| Sức chứa công trình dịch vụ | Số chỗ cùng lúc = 5 + 0.8 x cấp (Lv5: 9, Lv25: 25), luôn thiếu nhẹ so với dân số | Khởi điểm |
| Bệnh Viện: khu hồi phục | 100 giường cho Monster mới bắt | Khởi điểm |
| Chi phí mở khóa (Lò Rèn, Tiệm Kim Hoàn, PTN, Học Viện, Xưởng, Cổng) | 1 tháng (Lvl 2) và 2 tháng (Lvl 3) doanh thu dịch vụ toàn HUB (mô hình cũ 3 cấp) | Khởi điểm (cần mô phỏng lại) |
| Doanh thu dịch vụ ròng/Trainer Common/tháng | Bệnh Viện 2,524; Nhà Hàng 1,800; Nhà Trọ 1,012; Lò Rèn 1,852; Trang bị 1,259; Bar 928 (tổng khoảng 9,375) | Kết quả mô phỏng |

**Chi phí kỳ vọng của các thang nâng cấp** (Trainer Common, đơn vị tham chiếu 400 Gold/ngày): Nâng Sao 0 -> 5 khoảng 71,000 Gold (178 ngày thu nhập; 25,000 nếu không rớt sao), Tinh Luyện khoảng 131,000 (329 ngày), Tăng tư chất Common -> Ultimate khoảng 630,000 (1,576 ngày). Bước cuối luôn chiếm phần lớn: bước 5 của Nâng Sao 16,000 trong 25,000 (khi không rớt sao; rớt sao đẩy tổng lên 71,000); bước 4 của Tinh Luyện 104,000 trong 131,000; bước 4 của Tăng tư chất 540,000 trong 630,000. Chi phí tính trên thu nhập Trainer Common; Trainer Rarity cao kiếm nhiều hơn 1.7 lần mỗi bậc nên gánh nhẹ hơn.

## 12. Vay nợ và cổ phiếu (kết quả mô phỏng)
**Cho Trainer Vay** (30 Trainer, 12 tháng, `Game.Sim loans`): Trainer Common có nhu cầu chi tiêu gần bằng thu nhập nên **gần như mọi Trainer đều vay**, dư nợ ổn định khoảng 0.5-0.7 lần lương tháng. Lãi suất một mình là đòn bẩy yếu khi hạn mức thấp. Tỉ lệ lãi tích lũy trên lợi nhuận tháng của HUB (lãi suất mỗi Payday) và số lần đình công do quá hạn (mỗi Trainer mỗi năm):

| Hạn mức | Lãi 10% | Lãi 20% | Lãi 40% |
|---|---|---|---|
| 1 lương | 1.9% lợi nhuận, 0 đình công | 3.9%, 0 | 8.1%, 0 |
| 2 lương | 5.9%, 0 | 12.4%, 0 | 30.0%, 5.4 đình công |
| 3 lương | 9.8%, 0 | 21.5%, 1.0 đình công | 53.1%, 4.9 đình công |

Diễn giải: hạn mức 2 lương với lãi 10-20% cho thu nhập lãi đáng kể (6-12% lợi nhuận) mà không gây đình công; hạn mức 3 lương hoặc lãi 40% chuyển sang bóc lột quá mức, Trainer đình công hàng loạt. Đây là vùng "Chủ Nợ Máu Lạnh" có chủ đích. Thành tựu ẩn "Chủ Nợ Máu Lạnh" (số dư ròng thấp hơn -2 lần lương tháng): ở mặc định (2 lương, lãi 10-20%) không Trainer nào đạt; ở 2 lương với lãi 40% có khoảng 12% Trainer đạt; ở 3 lương thì gần như mọi Trainer đạt (nhưng kèm đình công hàng loạt). Vì vậy điều kiện này đòi hỏi Giám đốc chủ động nới hạn mức.

**Cổ phiếu** (`Game.Sim stock`, 360 ngày): biến động ±3%/ngày cho khoảng 6 cơn hoảng loạn, 3.6 cú sập và 5 đợt tăng mạnh mỗi năm (khoảng 1 sự kiện mỗi 1-2 tháng): đủ để Trainer *Nhát gan* panic sell và Pump & Dump có cơ hội, mà không loạn. Biến động 1-2% gần như không có sự kiện; 5% có khoảng 23 cơn hoảng loạn mỗi năm, quá dày. Mô hình chưa mô phỏng cổ tức, Thuế Tự Do Tài Chính và hành vi mua bán theo tính cách.

## 13. Tính cách, Ngân Hàng Gene, Rebellion, tài chính (kết quả mô phỏng)
**Tính cách** (`Game.Sim personality`; hệ số khởi điểm: Háo chiến thu nhập x1.25 và mất HP x1.5; Nhát gan thu nhập x0.85 và mất HP x0.5; Tham ăn tiền ăn x1.6; Tư bản được giảm 10% giá dịch vụ và đòi lương x1.3). Trainer Common, 12 tháng:

| Tính cách | Vàng TB | Thu nhập/tháng | Lợi nhuận HUB/tháng (20 Trainer cùng tính cách) | Dấu hiệu |
|---|---|---|---|---|
| Háo chiến | 3,601 | 12,004 | 198,700 | Viện phí cao nhất (3,785/Trainer), HUB lãi nhiều nhất |
| Nhát gan | 2,446 | 8,161 | 134,900 | Nghèo nhất, HUB lãi ít nhất |
| Tham ăn | 2,885 | 9,600 | 158,400 | Tiền Nhà Hàng 2,880 so với 1,800 |
| Tư bản | 3,746 | 9,598 | 154,800 | Giàu thứ hai; ép lương cao và giảm giá khiến HUB lãi ít hơn Háo chiến 22% |

Thứ tự lợi nhuận HUB khớp mô tả thiết kế (Háo chiến "tốn viện phí" nhiều nhất, Tham ăn "tốn tiền nhà hàng"). Tư bản vẫn giữ nhiều vàng nhất sau Háo chiến; hướng cân bằng tiếp theo là cho Tư bản lướt sóng chứng khoán (chưa mô phỏng hành vi mua bán).

**Ngân Hàng Gene** (`Game.Sim genebank`, 0-3 Monster gửi mỗi Trainer): thu phí **hằng ngày** thất bại ngay cả ở mức 5 Gold/ngày vì Trainer Common hết tiền mặt mỗi ngày (mất 1.0-1.5 Monster mỗi Trainer mỗi năm, doanh thu gần 0). Thu phí **mỗi Payday** (sau khi trả lương) ổn: ở 20 Gold/ngày doanh thu 678 Gold/Trainer/tháng (khoảng 24% lương), không ai mất Monster; ở 40 Gold/ngày doanh thu 1,120 và 0.26 Monster bị tịch thu/Trainer/năm; ở 80 Gold/ngày 0.78 Monster. Vì vậy mặc định thu mỗi Payday, 20 Gold/Monster/ngày, và tịch thu là hậu quả của việc không đủ phí (đây là vùng của Achievement "Tài Liệt").

**Rebellion** (`RebellionModel`, có test): một bậc Rarity bằng 20 cấp độ. Monster cùng bậc quản lý được tới Cấp độ Trainer + 20; Monster hơn Trainer một bậc chỉ quản lý được nếu cấp độ không cao hơn Trainer; Monster hơn hai bậc cần thưởng Lãnh đạo (Khóa Giao Tiếp, Học Viện). Quy tắc dễ hiểu cho người chơi và cho UI ("cần Lãnh đạo X").

**Tài chính** (`FinanceModel`, có test): cổ tức 10% doanh thu 15 ngày; Thuế Tự Do Tài Chính 30% trên lãi đã chốt; số cổ phiếu IPO tính theo tỉ suất mục tiêu (8%/năm) để cổ tức hấp dẫn nhưng không lấn át biến động giá.
