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
| Kỹ thuật khác | Sửa câu O(1)/SQLite; Mac thành khuyến nghị; thêm cấu trúc Assembly Definition. | 00_Tech_Stack |

**Các giá trị tôi tự đặt, cần bạn xác nhận hoặc chỉnh:**
- Tên 5 bậc Rarity; Rank = 1 + số Rebirth; tên Mythic.
- 1 ngày = 10 phút thực (hệ quả: Payday mỗi 5 giờ thực, offline 8 giờ tương đương khoảng 1.6 Payday).
- Khi offline: người chơi thường không tự thu mua, Payday/Đình công/Thanh Tra chờ đến lúc mở lại app.
- Ngưỡng phạt Thanh Tra 30%.
- Vị trí chức năng trong bảng công trình (ví dụ Giám định IVs ở Phòng Thí Nghiệm Tiến Hóa, Bẫy/Bóng ở Xưởng Công Cụ, Thuốc ở Bệnh Viện).
- Bảng "Thanh trạng thái" và vật phẩm trong `12_Item_Catalog` là mô tả khởi điểm.

---

## Còn mở

| # | Mức | Vấn đề | Đề xuất |
|---|---|---|---|
| O1 | P1 | **Quan hệ khắc chế của Sét, Băng, Độc** chưa có (04 mới có Lửa > Cỏ > Nước > Lửa, Ánh sáng ↔ Bóng tối). | Thiết kế bảng khắc chế 8 hệ, ghi vào 13_Balance_Parameters §4. |
| O2 | P1 | **Toàn bộ con số kinh tế** còn TBD: lương theo Rarity, lãi suất hai loại vay, phí Ngân Hàng Gene, tỉ lệ Cường hóa/Nâng Sao/Tinh Luyện/Tiến hóa, hệ số K, tỉ lệ Gacha và Pity, mốc Quest/Thành tựu. | Dựng console runner mô phỏng bằng Domain C# (Roadmap Bước 2 và 5), điền vào `13_Balance_Parameters`. |
| O3 | P1 | **Khối lượng art Early Access chưa được lập kế hoạch:** 16 công trình x 4 model 3D, sprite Trainer/Monster/Trang bị (30 slot, mỗi loại tối thiểu 3 Tier), VFX, âm thanh, 30 avatar. Roadmap 14 tuần chỉ tính code. | Quyết định ai làm art (nhóm riêng/thuê ngoài), khóa danh mục asset trước Bước 3, đánh giá lại số tuần. |
| O4 | P1 | **Rủi ro hiệu năng 2.5D** (60 FPS, 120 thực thể, sprite đổ bóng, NavMesh, đèn động). Mới có tiêu chí chấp nhận ở Roadmap Bước 2, chưa kiểm chứng. | Chạy prototype hiệu năng trên thiết bị thật trước khi sản xuất art hàng loạt. |
| O5 | P2 | **Yêu cầu công bố tỉ lệ Gacha và chính sách cửa hàng ứng dụng** chưa được kiểm tra với điều khoản Google Play hiện hành. | Đọc chính sách Google Play Billing/Loot box trước Bước 5. |
| O6 | P2 | **Tương tác Tycoon Club / Bailout / Gem với cân bằng:** Roi Kỷ Luật xóa 100% Stress/Đình công toàn HUB và Đồng Hồ Cát tua 24h là công cụ trả tiền mạnh, chưa có giới hạn. | Đánh giá khi có số liệu cân bằng; cân nhắc cooldown. |
