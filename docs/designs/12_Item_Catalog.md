# SUB-GDD 12: ITEM CATALOG (DANH MỤC VẬT PHẨM)

Danh mục các vật phẩm tiêu hao và nguyên liệu được nhắc trong các tài liệu khác. Cột "Giá" chưa có số, nằm trong [13_Balance_Parameters](13_Balance_Parameters.md). Trang bị (30 slot) xem [05](05_Itemization_Gear_System.md). Mỗi quầy tự chế hàng tiêu hao của mình từ nguyên liệu; hết nguyên liệu thì hết hàng (xem [02](02_HUB_Economy_Infrastructure.md) §1.0).

## 1. Vật phẩm tiêu hao bán cho AI

| Vật phẩm | Tác dụng | Chế và bán tại | Nguyên liệu | Nguồn tham chiếu |
|---|---|---|---|---|
| Thuốc (Potion) | Hồi HP cho Monster | Bệnh Viện Thú Y | Thảo dược | 03, 08 |
| Vắc-xin | Cắt dịch Monster Flu | Bệnh Viện Thú Y | Thảo dược | 06 |
| Thuốc An Thần | Giảm Stress khi vào Nightmare | Bệnh Viện Thú Y | Thảo dược | 01 |
| Đồ ăn, nước uống | Hồi No nê và Nước | Nhà Hàng | Thực phẩm | 01, 03 |
| Bánh thưởng | Giảm Rebellion của Monster | Nhà Hàng | Thực phẩm | 04 |
| Rượu | Giảm Stress | Quán Bar | Thực phẩm | 08 |
| Áo mưa | Chống Mưa bão | Xưởng Công Cụ | Phôi | 01 |
| Mặt nạ phòng độc | Chống Sương mù độc | Xưởng Công Cụ | Phôi | 01 |
| Bóng bắt thú, Bẫy | Bắt Monster (bắt buộc có Bóng) | Xưởng Công Cụ | Phôi, Gỗ | 03, 04, 06 |
| Sách Chiến Thuật | Ghép đội hình Bag Synergy | Xưởng Công Cụ | Phôi | 04 |
| Bình nước buff | Tăng tạm HP / ATK / DEF / CRIT... cho Monster | Chế tại Nhà Máy Nước Ngọt, bán tại Tiệm Tạp hóa | Thực phẩm | 04 |
| Khóa Giao Tiếp Thú Cưng | Tăng Điểm Lãnh đạo hiệu dụng | Học Viện | - | 04 |
| Cà phê ép xung | Thức thêm vài giờ ban đêm | Nhà Trọ | Thực phẩm | 01 |
| Nước Cất | Nguyên liệu Tinh Luyện, độc quyền, giá x10 | Chế tại Lò Phản Ứng, bán tại Tiệm Kim Hoàn | Phôi | 05 |
| Vật phẩm tăng Rarity | Tăng tư chất Monster (Lv ≥ 40) | Phòng Thí Nghiệm Tiến Hóa | Lõi Đột Biến, Gene Fragments | 04 |
| Bùa Bảo Hộ | Chống vỡ đồ khi Cường hóa +11 trở lên, giữ vật liệu khi Tiến hóa/tăng tư chất | Giám đốc mua bằng Gem (hoặc nhận thưởng), bán cho AI bằng Gold (xem [07](07_Monetization_Model.md)) | - | 04, 05, 07, 09 |

## 2. Nguyên liệu và vật phẩm chế tạo

**Nguyên liệu thô: 6 nhóm x 5 tier = 30 loại.** Zone N rơi chủ yếu tier N (Zone cao thỉnh thoảng rơi tier thấp). Đồ tier cao cần nguyên liệu tier cao.

| Nhóm | Dùng ở |
|---|---|
| Khoáng (đá, quặng) | Nhà máy Tinh chế -> Lò Rèn, Lò Phản Ứng; xây dựng |
| Gỗ | Xây dựng, Xưởng Công Cụ |
| Vải / Da | Nhà máy Tinh chế -> Xưởng Dệt |
| Đá quý | Nhà máy Tinh chế -> Tiệm Kim Hoàn |
| Thảo dược | Bệnh Viện Thú Y |
| Thực phẩm | Nhà Hàng, Quán Bar, Nhà Máy Nước Ngọt, Nhà Trọ |

| Vật phẩm | Nguồn | Dùng ở | Nguồn tham chiếu |
|---|---|---|---|
| Phôi liệu (Sắt, Vải, Đá quý mài...) | Nhà máy Tinh chế | 3 xưởng trang bị, Xưởng Công Cụ, Lò Phản Ứng, xây dựng | 02 |
| Đá Cường hóa | Lò Phản Ứng | Cường hóa tại Lò Rèn | 05 |
| Đá Tiến Hóa | Lò Phản Ứng | Tiến hóa tại Phòng Thí Nghiệm | 04 |
| Lõi Đột Biến | Farm | Tiến hóa, tăng tư chất | 04 |
| Tinh Thể Boss Thế Giới | World Boss (nguồn duy nhất) | Tinh Luyện | 01, 05 |
| Gene Fragments | Phân giải Monster (thường là IV D, C) | Bán cho Trạm; nguyên liệu Phòng Thí Nghiệm Tiến Hóa | 02, 04 |
| Cổ vật vỡ (Broken Relics) | Farm ngẫu nhiên | Ghép tại Bảo Tàng Khảo Cổ (sau Early Access) | 08 |

*Các giá trị tác dụng ở trên là mô tả khởi điểm lấy từ các tài liệu khác, cần xác nhận khi cân bằng.*

## 3. Trạng thái catalog trong Domain

Prototype `Game.Domain` có ID ổn định cho 30 nguyên liệu (6 nhóm × 5 tier), các nhóm sản phẩm, công thức Tinh chế/Lò Phản Ứng và công thức tiêu hao dùng thử. Công thức và số lượng đầu vào/đầu ra trong [workbook cân bằng](../balance/MonsterHUB_Balance.xlsx) là dữ liệu Prototype/TBD, chưa phải định mức sản xuất đã chốt. Các công thức trang bị được biểu diễn theo nhóm slot; chỉ mapping xưởng và quầy, chưa có crafting/equipment economy hoàn chỉnh.

**Loot chưa được đặc tả theo từng material ID:** farm prototype hiện trả Quặng tier 1 cho mọi lượt để kiểm tra luồng `Trainer → Trạm/Thương nhân → sản xuất`. GDD hiện chỉ nêu nhóm nguyên liệu và tier chủ yếu theo Zone, chưa có bảng xác suất/số lượng theo Zone, encounter hay loại quái. Vì vậy phép ánh xạ này là tạm; không suy ra rằng các Zone chỉ rơi Quặng. Chi tiết loot sẽ được chốt cùng mô hình farm/chiến đấu. Hiệu ứng vật phẩm và hành vi Trainer mua từ quầy cũng chưa được mô phỏng.

## 3. Hiệu ứng đã mô phỏng (Sub-project 3)
Thuốc (hồi HP), Bình nước buff (tăng tạm ATK/DEF/CRIT), Bánh thưởng (giảm Rebellion), Sách Chiến Thuật (Bag Synergy), Khóa Giao Tiếp (tăng Điểm Lãnh đạo) và Bóng/Bẫy (bắt thú) đã có hiệu ứng trong `Game.Domain`. Giá trị cụ thể là **Prototype** (workbook, sheet `Monster Lifecycle`, tiền tố `item_effect.`). Trang bị 30 slot thuộc Sub-project 4.
