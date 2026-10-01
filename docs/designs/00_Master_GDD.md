# GAME DESIGN DOCUMENT: MONSTER HUB TYCOON

## 1. TỔNG QUAN DỰ ÁN (EXECUTIVE SUMMARY)
*   **Tên dự án:** Monster HUB Tycoon
*   **Thể loại:** Management Simulation / Idle RPG / Dark-Capitalist Tycoon
*   **Phong cách Đồ họa:** Chibi Pixel Art 2.5D: môi trường 3D low-poly/voxel, nhân vật 2D sprite pixel art, camera isometric (chi tiết tại [11_Game_Assets](11_Game_Assets.md)). Tươi sáng, dễ thương nhưng tương phản với nội tại kinh tế khốc liệt.
*   **Nền tảng:** Android (Early Access). PC/iOS là hướng mở rộng sau Early Access.
*   **Chế độ mạng:** Offline. PvP (xem [06](06_Events_PVE_PVP.md) §3) nằm ngoài Early Access.
*   **Tóm tắt cốt lõi:** Người chơi vào vai Giám đốc HUB nằm giữa vùng hoang dã. Game tự trị (autonomous). Người chơi không điều khiển nhân vật đánh quái mà điều khiển "Dòng tiền". Chiêu mộ Trainer, cung cấp dịch vụ độc quyền, bòn rút tài sản của họ thông qua chuỗi cung ứng, chứng khoán và các dịch vụ rủi ro cao.

## 2. VÒNG LẶP CỐT LÕI (CORE LOOP)
1. **Thu thập (Farming):** Trainer (AI) tự động ra ngoài đánh quái, nhặt nguyên liệu.
2. **Giao thương (Trading):** Trainer về HUB bán nguyên liệu lấy Gold. HUB (Người chơi) chế tạo nguyên liệu này thành hàng hóa/dịch vụ.
3. **Tiêu dùng (Sinks):** Trainer dùng Gold mua dịch vụ sinh tồn (ăn, ngủ, y tế) và nâng cấp trang bị từ HUB với giá cắt cổ.
4. **Chu kỳ bóc lột:** HUB trả lương mỗi 30 ngày in-game -> Thu lại qua Sàn Chứng Khoán, Lãi vay, Phí sửa chữa đồ và Quán Bar.

## 3. FTUE - 15 PHÚT ĐẦU TIÊN (TRẢI NGHIỆM NGƯỜI CHƠI MỚI)
*   **Bối cảnh:** Ruin-to-Riches. FTUE bắt đầu ngay sau Intro Cutscene 15 giây (xem [09](09_Story_Lore.md) §2): cảnh cuối của Intro là HUB hoang tàn, bước đầu tiên của FTUE là dựng lại Tòa Thị Chính. Người chơi tiếp quản một HUB đổ nát với số vốn ít ỏi.
*   **Hành động:** 
    * Dựng lại Tòa Thị Chính Lv1 và Ký túc xá Lv1. Chiêu mộ 5 Trainer hạng Common ngẫu nhiên. Phân phát Monster Mặc định (Slime/Sâu) và đuổi ra Zone 1.
    * Dọn dẹp phế tích lấy vật liệu, dựng lại Trạm Giao Thương và Bệnh Viện Thú Y (4 công trình mặc định ở trạng thái Đổ nát, xem [02](02_HUB_Economy_Infrastructure.md) §1.1).
*   **A-ha Moment:** Thiết lập Thuế 20%. Chứng kiến AI đi farm về bị thương, bán nguyên liệu lấy tiền rồi lập tức khóc lóc nộp lại tiền đó cho Bệnh Viện của bạn. Đồng hồ 30 Ngày Trả Lương bắt đầu đếm ngược.

## 4. ĐỊNH HƯỚNG KIẾN TRÚC KỸ THUẬT (TECHNICAL GUIDELINES)
*   **Kiến trúc:** Domain-Driven Design (DDD) & Clean Architecture. Tách biệt hoàn toàn Core Logic của nền kinh tế khỏi Presentation/UI Layer.
*   **Dữ liệu:** Cấu trúc dữ liệu của các thực thể (Gene quái vật, Inventory) phải được **phẳng hóa (flattened arrays/objects)**. Loại bỏ cấu trúc cây đệ quy sâu để tối ưu hóa hiệu năng (O(1) hoặc O(N) tuyến tính) cho hàng trăm AI State Machine hoạt động cùng lúc.
*   **Mô phỏng:** Domain mô phỏng theo **sự kiện rời rạc** tính bằng phút in-game. Trainer chạy FSM có thời lượng (đi, farm, về, xếp hàng, dùng dịch vụ); kết quả farm và chiến đấu tính bằng công thức trong Domain. Unity chỉ diễn hoạt lại (NavMesh, hoạt ảnh trận). Online, offline, Đồng Hồ Cát và `tools/Game.Sim` dùng **chung một mô hình**, chỉ khác bước thời gian. Seed RNG được lưu trong save.

## 5. THỜI GIAN & TIẾN TRÌNH OFFLINE
*   **Thang thời gian (giá trị khởi điểm, chỉnh khi cân bằng):** 1 ngày in-game = 15 phút thực (12 giờ ban ngày 06:00-18:00 = 7.5 phút). 1 "tháng" = 30 ngày = Payday = 7.5 giờ thực. Cổ tức chứng khoán mỗi 15 ngày. Logic chạy theo Global Tick 0.2 giây (xem [00_Tech_Stack](../00_Tech_Stack.md)).
*   **Tiến trình offline:** Khi mở lại app, Domain **tự động mô phỏng mọi thứ** (cùng mô hình với online: farm, dịch vụ, sản xuất theo tồn kho mục tiêu, yêu cầu mua ở Trạm, Thương nhân) **tới Payday gần nhất thì dừng**. Payday luôn chờ người chơi xử lý khi mở app. Thời gian offline vượt quá Payday bị bỏ. Không có trần 8 giờ; tối đa 1 tháng in-game (khoảng 7.5 giờ thực).
*   **Đồng Hồ Cát:** Tua nhanh 8 giờ hoặc 24 giờ **thực** bằng chính mô phỏng, cũng dừng ngay trước Payday kế tiếp; phần chưa tua được giữ lại, chạy tiếp sau khi người chơi xử lý Payday.

## 6. ĐIỀU KHIỂN CỦA GIÁM ĐỐC (DIRECTOR ACTIONS)
Người chơi không điều khiển hành động của AI. Các đòn bẩy sau là toàn bộ cách tác động lên nền kinh tế (làm nguồn cho thiết kế UI/UX):

| Nhóm | Đòn bẩy | Tài liệu |
|---|---|---|
| Giá & thuế | Thuế giao dịch, giá dịch vụ, giá Bùa Bảo Hộ, giá phòng, Cà phê ép xung, giá vé Cổng, lãi suất cho vay | [02](02_HUB_Economy_Infrastructure.md), [05](05_Itemization_Gear_System.md) |
| Chuỗi cung ứng | Yêu cầu mua ở Trạm (mức giữ + giá), tồn kho mục tiêu từng món, mua từ Thương nhân, chào hàng trang bị cho Trainer, mua lại đồ cũ | [02](02_HUB_Economy_Infrastructure.md) §1.0 |
| Nhân sự | Tuyển dụng, đàm phán/ép lương, ứng lương, donate, đào tạo Học Viện, trục xuất, thứ tự gửi Monster vào Gene Bank | [03](03_Trainer_AI_System.md) |
| Công trình | Xây, nâng cấp, tắt điện, sửa Hư hại, IPO | [02](02_HUB_Economy_Infrastructure.md) |
| Hướng dẫn farm | Bảng Truy Nã (giá mua x3), Cổng Dịch Chuyển | [01](01_World_Map_Environment.md) |
| Sự kiện | Còi World Boss, sự kiện giảm giá lấy Traffic | [01](01_World_Map_Environment.md), [02](02_HUB_Economy_Infrastructure.md) |
| Tài chính | Vay từ Trainer, cho Trainer vay, thao túng chứng khoán, tịch thu Monster tại Ngân Hàng Gene | [02](02_HUB_Economy_Infrastructure.md) |
| Công cụ trả phí | Roi Kỷ Luật, Đồng Hồ Cát, Gacha | [07](07_Monetization_Model.md) |
