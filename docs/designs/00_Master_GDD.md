# GAME DESIGN DOCUMENT: MONSTER HUB TYCOON

## 1. TỔNG QUAN DỰ ÁN (EXECUTIVE SUMMARY)
*   **Tên dự án:** Monster HUB Tycoon
*   **Thể loại:** Management Simulation / Idle RPG / Dark-Capitalist Tycoon
*   **Phong cách Đồ họa:** Chibi Pixel Art (Tươi sáng, dễ thương nhưng tương phản với nội tại kinh tế khốc liệt).
*   **Nền tảng:** Mobile / PC
*   **Tóm tắt cốt lõi:** Người chơi vào vai Giám đốc HUB nằm giữa vùng hoang dã. Game tự trị (autonomous). Người chơi không điều khiển nhân vật đánh quái mà điều khiển "Dòng tiền". Chiêu mộ Trainer, cung cấp dịch vụ độc quyền, bòn rút tài sản của họ thông qua chuỗi cung ứng, chứng khoán và các dịch vụ rủi ro cao.

## 2. VÒNG LẶP CỐT LÕI (CORE LOOP)
1. **Thu thập (Farming):** Trainer (AI) tự động ra ngoài đánh quái, nhặt nguyên liệu.
2. **Giao thương (Trading):** Trainer về HUB bán nguyên liệu lấy Gold. HUB (Người chơi) chế tạo nguyên liệu này thành hàng hóa/dịch vụ.
3. **Tiêu dùng (Sinks):** Trainer dùng Gold mua dịch vụ sinh tồn (ăn, ngủ, y tế) và nâng cấp trang bị từ HUB với giá cắt cổ.
4. **Chu kỳ bóc lột:** HUB trả lương mỗi 30 ngày in-game -> Thu lại qua Sàn Chứng Khoán, Lãi vay, Phí sửa chữa đồ và Quán Bar.

## 3. FTUE - 15 PHÚT ĐẦU TIÊN (TRẢI NGHIỆM NGƯỜI CHƠI MỚI)
*   **Bối cảnh:** Ruin-to-Riches. Người chơi tiếp quản một HUB đổ nát với số vốn ít ỏi.
*   **Hành động:** 
    * Dựng lại Tòa Thị Chính Lvl 1. Chiêu mộ 5 Trainee ngẫu nhiên. Phân phát Monster Mặc định (Slime/Sâu) và đuổi ra Zone 1.
    * Dọn dẹp phế tích lấy vật liệu, xây Trạm Giao Thương và Bệnh Viện.
*   **A-ha Moment:** Thiết lập Thuế 20%. Chứng kiến AI đi farm về bị thương, bán nguyên liệu lấy tiền rồi lập tức khóc lóc nộp lại tiền đó cho Bệnh Viện của bạn. Đồng hồ 30 Ngày Trả Lương bắt đầu đếm ngược.

## 4. ĐỊNH HƯỚNG KIẾN TRÚC KỸ THUẬT (TECHNICAL GUIDELINES)
*   **Kiến trúc:** Domain-Driven Design (DDD) & Clean Architecture. Tách biệt hoàn toàn Core Logic của nền kinh tế khỏi Presentation/UI Layer.
*   **Dữ liệu:** Cấu trúc dữ liệu của các thực thể (Gene quái vật, Inventory) phải được **phẳng hóa (flattened arrays/objects)**. Loại bỏ cấu trúc cây đệ quy sâu để tối ưu hóa hiệu năng (O(1) hoặc O(N) tuyến tính) cho hàng trăm AI State Machine hoạt động cùng lúc.