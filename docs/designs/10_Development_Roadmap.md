# SUB-GDD 10: DEVELOPMENT ROADMAP (LỘ TRÌNH PHÁT TRIỂN)

Tài liệu này vạch ra lộ trình 6 bước (ước tính 14 tuần) để phát triển **Monster HUB Tycoon** từ mức GDD đến bản phát hành Early Access trên Android (Unity Engine). Chiến lược áp dụng là tiếp cận "Từ dưới lên" (Bottom-up), ưu tiên tính toàn vẹn của dữ liệu và hệ thống AI trước khi đắp đồ họa.

## PHẠM VI RELEASE

*   **Early Access (14 tuần):** Vòng lặp lõi; Trainer/Monster/Gear; kinh tế HUB (công trình, Payday, khủng hoảng, Đình công, vay nợ); Sàn Chứng Khoán; Quest chính + KPI Daily/Weekly; sự kiện Định kỳ (Black Friday, Breeding Season); tiến trình offline; monetization cơ bản (Ads tự nguyện, Gacha, Bùa Bảo Hộ, Tycoon Club, Battle Pass); Intro cutscene và FTUE; Remote Config + Analytics.
*   **Sau Early Access:** PvE (Monster Siege, Tháp Vô Tận); sự kiện Đột xuất (Monster Flu, Thanh Tra); Thành tựu ẩn; Sách Đỏ Quái Vật và Bảo Tàng Khảo Cổ; toàn bộ PvP (cần backend riêng, xem [06](06_Events_PVE_PVP.md) §3); PC/iOS.

## BƯỚC 1: KHỞI TẠO KIẾN TRÚC & DỮ LIỆU CỐT LÕI (TUẦN 1 - 2)
**Mục tiêu:** Đặt nền móng vững chắc bằng Clean Architecture và Domain-Driven Design (DDD) để chống nợ kỹ thuật (Technical Debt) khi scale lên hàng chục AI.
*   **Thiết lập Unity & DI:** Khởi tạo project (Unity 6.3 LTS, URP) theo cấu trúc Assembly Definition tại [00_Tech_Stack](../00_Tech_Stack.md) §4b. Cài đặt các package cốt lõi: `VContainer` (Dependency Injection), `R3` (Reactive Programming, chỉ dùng ở lớp Presentation).
*   **Flattened Data Model:** Viết class C# thuần (Domain Layer) cho cấu trúc dữ liệu phẳng của `Trainer`, `Monster`, `Inventory`, và `Building`. (Không kế thừa `MonoBehaviour`).
*   **Master Data:** Thiết lập CSV/Google Sheets chứa chỉ số nền (Base Stats), công thức lương, giá trị đồ đạc và parse thành `ScriptableObjects` để game đọc nhanh tại Runtime.

## BƯỚC 2: PROTOTYPE VÒNG LẶP KINH TẾ LÕI (TUẦN 3 - 4)
**Mục tiêu:** Làm cho game "chạy được" trên giao diện hình khối (Whiteboxing) để kiểm chứng luồng luân chuyển của dòng tiền (Gold).
*   **AI State Machine (FSM):** Lập trình FSM thuần C# (tầng Domain) cho 1 Trainer: `Đi farm` -> `Tụt HP` -> `Về HUB bán đồ` -> `Trả tiền Bệnh viện` -> `Lặp lại`.
*   **Hệ thống Thời gian:** Lập trình Global Tick Manager và đồng hồ đếm ngược chu kỳ 30 ngày (Payday).
*   **Giao dịch:** Code logic Trạm Giao Thương (thu mua nguyên liệu thô) và logic Kho bạc HUB (cộng/trừ Gold, trả lương).
*   **Prototype hiệu năng 2.5D:** Dựng sớm 30 Trainer + 90 Monster (sprite Billboard, bóng đổ, NavMesh) trên thiết bị Android tầm trung thật. Tiêu chí chấp nhận: tối thiểu 30 FPS ổn định, mục tiêu 60 FPS ở Bước 6. Nếu không đạt, giảm bóng thời gian thực/đèn động trước khi đi tiếp.
*   **Art spike (1 tuần, cuối Bước 2):** Thử quy trình art bằng AI để chọn công cụ (xem [14_AI_Art_Pipeline](14_AI_Art_Pipeline.md) §4).
*   **Kiểm chứng:** Chạy mô phỏng không có đồ họa để xem Kho bạc của Giám đốc tăng lên hay phá sản sau chu kỳ 30 ngày đầu tiên.

## BƯỚC 3: ĐỒ HỌA & BẢN CẮT DỌC - VERTICAL SLICE (TUẦN 5 - 7)
**Mục tiêu:** Hoàn thiện 1 "lát cắt" của game với đầy đủ hình ảnh, âm thanh và UI cho giai đoạn Tân thủ (FTUE).
*   **Tích hợp Art:** Đưa asset Chibi Pixel Art 2.5D vào project (Sprite nhân vật có Billboard + Cutout Shader, môi trường 3D Tile/Block Zone 1 kèm NavMesh, Animation chiến đấu cơ bản).
*   **Hệ thống Tương tác (Swap):** Code cơ chế `1 Active + 2 Reserve` cho Monster. AI tự động đổi quái dự bị khi HP < 15%.
*   **Hệ thống Trang bị (Cơ bản):** Lập trình hệ thống Inventory và module Cường hóa (+1 đến +20). AI quét shop và tự động trừ tiền mua đồ có Gear Score cao hơn.
*   **UI/UX:** Xây dựng Dashboard Quản lý (Hiển thị Gold, Đồng hồ Payday, Dân số, Bảng theo dõi AI Trainer).

## BƯỚC 4: SCALE HỆ THỐNG & TÍNH NĂNG NÂNG CAO (TUẦN 8 - 10)
**Mục tiêu:** Scale hệ thống để đáp ứng quy mô 30 Trainer và tích hợp các tính năng End-game.
*   **Mở rộng Thế giới:** Code logic khóa/mở Zone 1->5 dựa trên Milestone (Rank I-V của Trainer, xem [03](03_Trainer_AI_System.md)). Áp dụng hiệu ứng Thời tiết và Ngày/Đêm.
*   **Công trình Tiêu thụ (End-game Sinks):** Lập trình Phòng Thí Nghiệm Tiến Hóa (đột biến Rarity Monster) và Tinh Luyện Trang bị tại Tiệm Kim Hoàn.
*   **HUB Street (Chứng khoán):** Lập trình thuật toán biến động giá cổ phiếu dựa trên Traffic ảo. Code hành vi mua/bán cổ phiếu của AI theo Tính cách.
*   **Sự kiện Định kỳ & Quest:** Code Black Friday, Breeding Season; Quest chính và KPI Daily/Weekly.
*   **Khủng hoảng & Quán Bar:** Code luồng sự kiện Đình công khi vỡ nợ, luồng AI tiêu tiền ở Quán Bar để HUB gỡ gạc, và tính năng Vay nặng lãi.

## BƯỚC 5: CÂN BẰNG KINH TẾ & MONETIZATION (TUẦN 11 - 12)
**Mục tiêu:** Xử lý rủi ro lạm phát và tích hợp các điểm chạm thương mại.
*   **Balancing (Cân bằng):** Chạy hàng triệu vòng lặp giả lập bằng console runner dùng chính Domain C# (xuất CSV, phân tích bằng Excel/Python) để tinh chỉnh:
    *   Hệ số tiền rớt từ quái vật.
    *   Chi phí Bệnh viện và sửa chữa Độ bền (đảm bảo AI luôn nghèo đi).
    *   Tỉ lệ thành công của Cường hóa/Tiến hóa.
*   **Monetization (Kiếm tiền):** 
    *   Tích hợp SDK Quảng cáo (AdMob/Unity Ads) cho các điểm chạm Tự nguyện (Bailout TV, Tiếp tế Hàng không).
    *   Tích hợp Google Play Billing cho Gacha (Thư mời), Thẻ Tháng và Bùa Bảo Hộ.

## BƯỚC 6: BETA TESTING, ANALYTICS & XUẤT BẢN (TUẦN 13 - 14)
**Mục tiêu:** Đánh bóng (Polishing), theo dõi hành vi người chơi và Release.
*   **Analytics:** Tích hợp Firebase Analytics. Đặt các tracking event: `first_payday`, `stock_market_crash`, `trainer_strike_count` để phân tích điểm kẹt của người chơi.
*   **Tối ưu Hiệu năng (Optimization):** Áp dụng Object Pooling toàn diện cho Damage Text, VFX và Vật phẩm rớt. Profiling để đảm bảo 30 AI và 90 Monster chạy mượt ở 60 FPS trên máy Android tầm trung.
*   **Nội dung hoàn thiện:** Tích hợp Intro cutscene, âm thanh/BGM, flavor text (Lore).
*   **Build & Submit:** Build định dạng `.aab` (Android App Bundle), hoàn thiện trang Google Play Store và phát hành bản Early Access.

## RỦI RO & GIẢ ĐỊNH

*   Lộ trình 14 tuần chỉ tính công việc code. Sản xuất art (khoảng 16 công trình × 4 model 3D, sprite Trainer/Monster/Trang bị, VFX, âm thanh) chạy song song, ưu tiên công cụ AI kèm chỉnh tay (xem [14_AI_Art_Pipeline](14_AI_Art_Pipeline.md)), và phải khóa danh mục asset trước Bước 3. Một **art spike 1 tuần** (14 §4) chạy cuối Bước 2 để quyết định công cụ.
*   Bước 3 (Vertical Slice, 3 tuần) là bước nặng nhất: art, Swap, Inventory, Cường hóa, UI. Nếu art chưa sẵn sàng, dùng placeholder và dời tích hợp art sang Bước 4.
