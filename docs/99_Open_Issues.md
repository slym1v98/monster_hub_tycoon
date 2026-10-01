# CÁC VẤN ĐỀ CÒN MỞ (OPEN ISSUES)

Rà soát toàn bộ tài liệu ngày 2026-10-01. Khi chốt một mục, sửa tài liệu gốc rồi xóa mục đó khỏi file này.

## Đã chốt (2026-10-01)

| Mục | Quyết định | Đã sửa tại |
|---|---|---|
| Đồ họa | 2.5D Isometric: môi trường 3D, nhân vật sprite 2D. | README, 00_Master_GDD, 10_Roadmap |
| Nền tảng | Android trước; PC/iOS sau Early Access. | README, 00_Master_GDD |
| PvP/Backend | PvP ngoài Early Access; game Offline; backend chỉ Remote Config + Analytics. | 00_Tech_Stack §6, 06 §3, 10_Roadmap |
| Bùa Bảo Hộ | Giám đốc mua bằng Gem (IAP), bán lại cho AI bằng Gold. | 07, 04, 08, 12 |
| Phân cấp | Rarity: Common < Rare < Epic < Legendary < Ultimate (cả Trainer và Monster). Rank I-V = 1 + số Rebirth. Tinh Luyện: Normal -> Mythic. | 03, 04, 01, 02, 05 |
| Kiến trúc | Domain: FSM và sự kiện thuần C#. Stack: Unity 6.3 LTS, URP, VContainer, MVP, MessagePack, R3, UGUI. Target API 36. | 00_Tech_Stack, 10_Roadmap, README |
| Tên Zone | Đồng Cỏ, Núi Lửa, Hầm Băng, Đầm Lầy, Vực Thẳm. Nightmare/The Void dùng lại tile Zone 5. | 01, 11 |
| Tính cách | 4 tính cách: Háo chiến, Nhát gan, Tham ăn, Tư bản ("Tham lam" đổi thành Tư bản). Có bảng hành vi. | 03, 02 |
| Công trình | Bảng 19 công trình, tối đa Lvl 3; Phòng Thí Nghiệm Tiến Hóa là tên chuẩn; 18 công trình có 4 trạng thái (16 trong Early Access). | 02, 11, 10 |
| Trang bị | 30 slot (12 Trainer + 3 Monster x 6). Slot "Bùa" đổi thành "Trang Sức". | 05, 11 |
| Vay nợ | Tách "Vay Từ Trainer" và "Cho Trainer Vay". | 02 |
| Class | Mục đổi tên "Đào tạo & Nghề nghiệp", Học Viện thêm vào tech tree. | 03, 02 |
| Thuế | Phân loại thuế; ngưỡng phạt Thanh Tra 30% (mặc định FTUE 20%). | 02, 06 |
| Lore/FTUE | HUB giữa vùng hoang dã; Intro 15 giây chạy trước FTUE. | 09, 00_GDD |
| Aura | Chỉ số thấp, chỉ Giám đốc bán cho AI, không tự dùng. | 07 |
| Animation | Unity 2D Animation, không dùng Spine. | 11 |
| Thời gian | 1 ngày = 10 phút thực (khởi điểm). Offline: công thức xấp xỉ, tối đa 8 giờ. Đồng Hồ Cát tính giờ thực. | 00_GDD §5, 13 |
| Giám đốc | Bảng đòn bẩy của Giám đốc. | 00_GDD §6 |
| Thanh trạng thái | 5 thanh (HP, Thể lực, No nê, Nước, Stress); chỉ số "Thể lực" đổi thành "Sức bền". | 03 |
| Hệ nguyên tố | 8 hệ; "hệ Tank/DPS/Support" đổi thành "vai trò". | 04 |
| Rebellion | Điểm Quản Lý = Cấp độ + (bậc Rarity x K). | 04 |
| Vật phẩm | Danh mục tiêu hao và nguyên liệu. | 12 |
| Gacha | Trainer sinh ngẫu nhiên từ thành phần; tỉ lệ và Pity phải công bố. | 07 |
| Ngân Hàng Gene | Chuyển sang nhóm Sink, thêm luật phí và tịch thu. | 02 |
| Phạm vi EA | Chia Early Access / sau Early Access. | 10 |
| Roadmap | Thêm prototype hiệu năng, sửa phần mô phỏng cân bằng dùng Domain C#, thêm rủi ro. | 10 |
| Khắc chế nguyên tố | 9 hệ (thêm Đất), bảng 2 / 0.5 / 1 theo logic Pokémon, không có miễn nhiễm. | 04, 13 |
| Công cụ trả phí | Roi Kỷ Luật có cooldown, không xóa nợ lương; Đồng Hồ Cát 1 lần/ngày, dừng trước Payday. | 07, 13 |
| Chính sách Google Play | Công bố tỉ lệ Gacha và phần thưởng ngẫu nhiên, yêu cầu hủy đăng ký; ghi vào mục tuân thủ. | 07 §6 |
| Sản xuất art | Dùng công cụ AI (Gemini image, Meowa, ...) kèm chỉnh tay; kế hoạch, khối lượng, art spike, pháp lý. | 14, 10 |
| Kỹ thuật khác | Sửa câu O(1)/SQLite; Mac thành khuyến nghị; thêm cấu trúc Assembly Definition. | 00_Tech_Stack |

**Các giá trị tôi tự đặt, cần bạn xác nhận hoặc chỉnh:**
- Tên 5 bậc Rarity; Rank = 1 + số Rebirth; tên Mythic.
- 1 ngày = 10 phút thực (hệ quả: Payday mỗi 5 giờ thực, offline 8 giờ tương đương khoảng 1.6 Payday).
- Khi offline: người chơi thường không tự thu mua, Payday/Đình công/Thanh Tra chờ đến lúc mở lại app.
- Ngưỡng phạt Thanh Tra 30%.
- Vị trí chức năng trong bảng công trình (ví dụ Giám định IVs ở Phòng Thí Nghiệm Tiến Hóa, Bẫy/Bóng ở Xưởng Công Cụ, Thuốc ở Bệnh Viện).
- Thêm hệ Đất (9 hệ) và hệ số khắc chế khởi điểm 2 / 0.5 / 1.
- Cooldown Roi Kỷ Luật 3 ngày in-game và quy tắc "tua dừng trước Payday".
- Bảng "Thanh trạng thái" và vật phẩm trong `12_Item_Catalog` là mô tả khởi điểm.

---

## Còn mở

| # | Mức | Vấn đề | Đề xuất |
|---|---|---|---|
| O2 | P1 | **Toàn bộ con số kinh tế** còn TBD: lương theo Rarity, lãi suất hai loại vay, phí Ngân Hàng Gene, tỉ lệ Cường hóa/Nâng Sao/Tinh Luyện/Tiến hóa, hệ số K, tỉ lệ Gacha và Pity, mốc Quest/Thành tựu. Cần mô phỏng, mà chưa có code. | Dựng console runner mô phỏng bằng Domain C# (Roadmap Bước 2 và 5), điền vào `13_Balance_Parameters`. |
| O3 | P1 | **Art spike chưa chạy:** chưa biết công cụ AI nào đạt cho paper-doll 30 slot, 4-8 hướng và model 3D. Kế hoạch ở `14_AI_Art_Pipeline`. | Chạy art spike 1 tuần (14 §4), chọn công cụ và mức chỉnh tay, rồi chốt tiến độ art. |
| O4 | P1 | **Rủi ro hiệu năng 2.5D** (60 FPS, 120 thực thể, sprite đổ bóng, NavMesh, đèn động). Mới có tiêu chí chấp nhận ở Roadmap Bước 2, chưa kiểm chứng. | Chạy prototype hiệu năng trên thiết bị thật trước khi sản xuất art hàng loạt. |
| O5 | P2 | **Kiểm tra pháp lý chính thức:** mục tuân thủ ở 07 §6 và 14 §5 dựa trên tra cứu web, chưa phải tư vấn pháp lý; điều khoản từng công cụ AI chưa đọc. | Đọc điều khoản thương mại của công cụ được chọn và chính sách Google Play ngay trước khi submit. |
