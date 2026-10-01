# SUB-GDD 13: BALANCE PARAMETERS (BẢNG THAM SỐ CÂN BẰNG)

Đây là nơi duy nhất ghi các con số kinh tế. Các tài liệu khác (Quest, Thành tựu, Monetization) tham chiếu về đây. Cột "Trạng thái": **Chốt** = đã quyết trong thiết kế; **Khởi điểm** = giá trị tạm để dựng prototype; **TBD** = chưa có số, sẽ xác định bằng mô phỏng (Roadmap Bước 5). Khi dữ liệu thật có, chuyển sang CSV/Google Sheets và parse vào ScriptableObjects (xem [00_Tech_Stack](../00_Tech_Stack.md)).

## 1. Thời gian
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| 1 ngày in-game | 10 phút thực | Khởi điểm |
| Ban ngày / Ban đêm | 06:00-18:00 / 18:00-06:00 | Chốt |
| 1 tháng (Payday) | 30 ngày | Chốt |
| Chu kỳ cổ tức | 15 ngày | Chốt |
| Tick | 0.2 giây | Chốt |
| Giới hạn offline | 8 giờ thực | Chốt |

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
| Thuế Tự Do Tài Chính | "Nặng" | TBD |
| Lương theo Rarity (5 bậc) | 30% thu nhập ròng tháng trước của chính Trainer đó; thu nhập và chi tiêu nhân 1.7 mỗi bậc Rarity | Khởi điểm (mô phỏng) |
| Lãi suất Cho Trainer Vay, hạn mức, hậu quả quá hạn | Chưa có | TBD |
| Lãi suất Vay Từ Trainer Rank V | Chưa có | TBD |
| Số ngày không trả phí Ngân Hàng Gene trước khi bị tịch thu | Chưa có | TBD |
| Phí lưu trữ Ngân Hàng Gene / Monster / ngày | Chưa có | TBD |

## 4. Trang bị và Monster
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Khoảng Cường hóa | +1 đến +20 | Chốt |
| Bắt đầu có nguy cơ vỡ đồ | +11 | Chốt |
| Tỉ lệ thành công Cường hóa | +1: 100%, giảm 5%/cấp tới +10 (55%); +11: 50%, giảm 3.5%/cấp tới +20 (18.5%) | Khởi điểm (mô phỏng) |
| Vỡ đồ khi thất bại từ +11 (không có Bùa) | 30% | Khởi điểm |
| Chi phí mỗi lần Cường hóa | CostBase x 1.30^(cấp-1) | Khởi điểm |
| Tỉ lệ thành công Nâng Sao, Tinh Luyện, Tiến hóa | Chưa có | TBD |
| Hệ số K của Điểm Quản Lý (Rebellion) | Chưa có | TBD |
| Bảng khắc chế 9 hệ (hệ số 2 / 0.5 / 1, không có miễn nhiễm) | Xem [04](04_Monster_System.md) §2 | Khởi điểm |
| Tốc độ hao mòn độ bền | Chưa có | TBD |

## 5. Công cụ trả phí
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Cooldown Roi Kỷ Luật | 3 ngày in-game | Khởi điểm |
| Giới hạn Đồng Hồ Cát | 1 lần/ngày thực, dừng trước Payday | Khởi điểm |

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
| Bar | 70 Gold/lần, khi Stress đạt 100 (tăng 12/ngày) | |
| Gia vốn dịch vụ | 25% doanh thu | |
| Hiệu suất gia công (HUB thu về trên 1 Gold nguyên liệu) | 0.92 | |
| Chi phí vận hành công trình | 40 Gold/công trình/ngày | |

## 9. Kết quả và giới hạn của mô phỏng sơ bộ
Chạy bằng `tools/Game.Sim` (xem [README](../../README.md)); kết quả đầy đủ tại [economy_sim_output.md](../reports/economy_sim_output.md). Đây là **mô hình đồ chơi** (một dòng tiền, không có chứng khoán, vay nợ, sự kiện, thời tiết, Boss), nên **chỉ dùng tỉ lệ và xu hướng, không dùng số tuyệt đối**.

*   **Cường hóa:** Chi phí kỳ vọng để lên +15 khoảng 1,030 x CostBase nếu không có Bùa, 440 x CostBase nếu có Bùa giá 6 x CostBase. Lên +20: khoảng 34,600 x CostBase (không Bùa) so với 2,600 x CostBase (có Bùa). Bùa hòa vốn ở giá khoảng 56 x CostBase cho +15 và 1,000 x CostBase cho +20. Nghĩa là Giám đốc có biên rất rộng để định giá Bùa mà AI vẫn có lợi khi mua, nhất là ở +20.
*   **Gacha:** Pity 60 cho Ultimate (xác suất 3%) cho kỳ vọng 28 lượt; Pity 40 giảm còn 23.5; Pity 80 tăng lên 30.4. Phần lớn người chơi sẽ chạm Pity nếu để xuống 40-60.
*   **Thuế:** Mỗi +10 điểm thuế giao dịch làm lợi nhuận HUB tăng khoảng 5% nhưng vàng trung bình của Trainer giảm khoảng 15%. Thuế là đòn bẩy yếu với HUB, mạnh với Trainer. Tức là rủi ro Thanh Tra (thuế > 30%) là cái giá chính của việc tăng thuế.
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
| 0.75x | 51% | 67% | 84% |
| 0.90x | 2% | 31% | 44% |
| 1.00x | 0% | 6% | 17% |
| 1.25x | 0% | 4% | 12% |
| 2.00x | 0% | 0% | 2% |

Diễn giải: có một **ngưỡng rõ rệt quanh 0.75-1.0x quỹ lương**. Dưới ngưỡng, Đình công gần như chắc chắn. Trên ngưỡng, rủi ro chủ yếu đến từ cú sốc và giảm dần khi dự trữ tăng. Đây là hình dạng rủi ro mong muốn cho game: người chơi tham lam tái đầu tư quá tay sẽ bị phạt, người cẩn thận ít bị. Ngưỡng sắc là hệ quả của mô hình lợi nhuận ổn định; trong game thật, lợi nhuận biến động sẽ làm đường cong dốc ít hơn.
