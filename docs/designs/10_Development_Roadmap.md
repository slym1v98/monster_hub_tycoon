# SUB-GDD 10: DEVELOPMENT ROADMAP (LỘ TRÌNH PHÁT TRIỂN)

Tài liệu này vạch ra lộ trình 6 bước (ước tính 14 tuần) để phát triển **Monster HUB Tycoon** từ mức GDD đến bản phát hành Early Access trên Android (Unity Engine). Chiến lược áp dụng là tiếp cận "Từ dưới lên" (Bottom-up), ưu tiên tính toàn vẹn của dữ liệu và hệ thống AI trước khi đắp đồ họa.

## BƯỚC 1: KHỞI TẠO KIẾN TRÚC & DỮ LIỆU CỐT LÕI (TUẦN 1 - 2)
**Mục tiêu:** Đặt nền móng vững chắc bằng Clean Architecture và Domain-Driven Design (DDD) để chống nợ kỹ thuật (Technical Debt) khi scale lên hàng chục AI.
*   **Thiết lập Unity & DI:** Khởi tạo project (Unity 2022 LTS+). Cài đặt các package cốt lõi: `VContainer` hoặc `Zenject` (Dependency Injection), `UniRx` (Reactive Programming).
*   **Flattened Data Model:** Viết class C# thuần (Domain Layer) cho cấu trúc dữ liệu phẳng của `Trainer`, `Monster`, `Inventory`, và `Building`. (Không kế thừa `MonoBehaviour`).
*   **Master Data:** Thiết lập CSV/Google Sheets chứa chỉ số nền (Base Stats), công thức lương, giá trị đồ đạc và parse thành `ScriptableObjects` để game đọc nhanh tại Runtime.

## BƯỚC 2: PROTOTYPE VÒNG LẶP KINH TẾ LÕI (TUẦN 3 - 4)
**Mục tiêu:** Làm cho game "chạy được" trên giao diện hình khối (Whiteboxing) để kiểm chứng luồng luân chuyển của dòng tiền (Gold).
*   **AI State Machine (FSM):** Lập trình Cây hành vi cơ bản cho 1 Trainer: `Đi farm` -> `Tụt HP` -> `Về HUB bán đồ` -> `Trả tiền Bệnh viện` -> `Lặp lại`.
*   **Hệ thống Thời gian:** Lập trình Global Tick Manager và đồng hồ đếm ngược chu kỳ 30 ngày (Payday).
*   **Giao dịch:** Code logic Trạm Giao Thương (thu mua nguyên liệu thô) và logic Kho bạc HUB (cộng/trừ Gold, trả lương).
*   **Kiểm chứng:** Chạy mô phỏng không có đồ họa để xem Kho bạc của Giám đốc tăng lên hay phá sản sau chu kỳ 30 ngày đầu tiên.

## BƯỚC 3: ĐỒ HỌA & BẢN CẮT DỌC - VERTICAL SLICE (TUẦN 5 - 7)
**Mục tiêu:** Hoàn thiện 1 "lát cắt" của game với đầy đủ hình ảnh, âm thanh và UI cho giai đoạn Tân thủ (FTUE).
*   **Tích hợp Art:** Đưa asset Chibi Pixel Art vào project (Sprite nhân vật, Tilemap bản đồ Zone 1, Animation chiến đấu cơ bản).
*   **Hệ thống Tương tác (Swap):** Code cơ chế `1 Active + 2 Reserve` cho Monster. AI tự động đổi quái dự bị khi HP < 15%.
*   **Hệ thống Trang bị (Cơ bản):** Lập trình hệ thống Inventory và module Cường hóa (+1 đến +20). AI quét shop và tự động trừ tiền mua đồ có Gear Score cao hơn.
*   **UI/UX:** Xây dựng Dashboard Quản lý (Hiển thị Gold, Đồng hồ Payday, Dân số, Bảng theo dõi AI Trainer).

## BƯỚC 4: SCALE HỆ THỐNG & TÍNH NĂNG NÂNG CAO (TUẦN 8 - 10)
**Mục tiêu:** Scale hệ thống để đáp ứng quy mô 30 Trainer và tích hợp các tính năng End-game.
*   **Mở rộng Thế giới:** Code logic khóa/mở Zone 1->5 dựa trên Milestone (Rank của Trainer). Áp dụng hiệu ứng Thời tiết và Ngày/Đêm.
*   **Công trình Tiêu thụ (End-game Sinks):** Lập trình Lò Tiến Hóa (đột biến Rarity Monster) và Trục Tinh Luyện Trang bị.
*   **HUB Street (Chứng khoán):** Lập trình thuật toán biến động giá cổ phiếu dựa trên Traffic ảo. Code hành vi mua/bán cổ phiếu của AI theo Tính cách.
*   **Khủng hoảng & Quán Bar:** Code luồng sự kiện Đình công khi vỡ nợ, luồng AI tiêu tiền ở Quán Bar để HUB gỡ gạc, và tính năng Vay nặng lãi.

## BƯỚC 5: CÂN BẰNG KINH TẾ & MONETIZATION (TUẦN 11 - 12)
**Mục tiêu:** Xử lý rủi ro lạm phát và tích hợp các điểm chạm thương mại.
*   **Balancing (Cân bằng):** Chạy hàng triệu vòng lặp giả lập bằng Python/Excel để tinh chỉnh:
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
*   **Build & Submit:** Build định dạng `.aab` (Android App Bundle), hoàn thiện trang Google Play Store và phát hành bản Early Access.