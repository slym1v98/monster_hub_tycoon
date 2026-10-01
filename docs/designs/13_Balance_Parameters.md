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
| Lương theo Rarity (5 bậc) | Chưa có | TBD |
| Lãi suất Cho Trainer Vay, hạn mức, hậu quả quá hạn | Chưa có | TBD |
| Lãi suất Vay Từ Trainer Rank V | Chưa có | TBD |
| Số ngày không trả phí Ngân Hàng Gene trước khi bị tịch thu | Chưa có | TBD |
| Phí lưu trữ Ngân Hàng Gene / Monster / ngày | Chưa có | TBD |

## 4. Trang bị và Monster
| Tham số | Giá trị | Trạng thái |
|---|---|---|
| Khoảng Cường hóa | +1 đến +20 | Chốt |
| Bắt đầu có nguy cơ vỡ đồ | +11 | Chốt |
| Tỉ lệ thành công Cường hóa từng cấp | Chưa có | TBD |
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
| Tỉ lệ từng bậc Rarity (Thư Mời Hoàng Gia) | Chưa có, phải công bố trong game | TBD |
| Ngưỡng Pity | Chưa có | TBD |

## 7. Mốc Quest và Thành tựu
Các mốc ghi trong [08](08_Quests_Achievements_Collections.md) (ví dụ 50,000 Gold/ngày, 1,000,000 Gold) là **placeholder**, cần chỉnh sau khi có số liệu mô phỏng ở mục 3 và 4.
