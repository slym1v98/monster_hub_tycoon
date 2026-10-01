# SUB-GDD 2: HUB INFRASTRUCTURE & ECONOMY

## 1. CHUỖI CUNG ỨNG & CÔNG TRÌNH (TECH TREE)
*   **Đầu vào (Faucet):** Trạm Giao Thương (Nơi thu mua nguyên liệu thô, thiết lập % Thuế giao dịch).
*   **Gia công (Processing):** Nhà máy Tinh chế, Lò Phản ứng (Biến nguyên liệu thô thành phôi liệu tự động).
*   **Đầu ra (Sinks):**
    *   *Dịch vụ Sinh tồn:* Nhà Trọ (Hồi Thể lực), Nhà Hàng (Hồi No Nê), Bệnh Viện Thú Y (Đa giường bệnh, thu phí trên từng HP), Quán Bar (Giảm Stress).
    *   *Trang bị:* Xưởng Công Cụ, Tiệm Kim Hoàn, Lò Rèn, Phòng Thí Nghiệm Tiến Hóa. Bán giá x5 x10 lần vốn.
    *   *Lưu trữ:* Ngân Hàng Gene (Thu phí lưu trữ quái dư, xem §1.2).

### 1.1. Bảng công trình
Mọi công trình có 4 trạng thái: Đổ nát -> Lvl 1 -> Lvl 2 -> **Lvl 3 (Max)**. Công trình mới mở khóa ở trạng thái Đổ nát và phải xây lại.

| Công trình | Chức năng | Ghi chú |
|---|---|---|
| Tòa Thị Chính | Cấp bằng Rebirth, tuyển dụng, mở khóa Zone và giấy phép xây dựng | Có sẵn từ FTUE |
| Trạm Giao Thương | Thu mua nguyên liệu thô, Thuế giao dịch | |
| Ngân Hàng Gene | Lưu trữ Monster dư (thu phí), nhận Gene Fragments | Nhóm Sink, §1.2 |
| Nhà máy Tinh chế | Tinh chế nguyên liệu thô thành phôi | |
| Lò Phản Ứng | Biến nguyên liệu thô thành phôi liệu tự động | |
| Nhà Trọ | Hồi Thể lực | |
| Nhà Hàng | Hồi No Nê | |
| Bệnh Viện Thú Y | Hồi HP, bán Thuốc và Vắc-xin | |
| Quán Bar | Giảm Stress, bán rượu | |
| Xưởng Công Cụ | Bán tiện ích sinh tồn (Áo mưa, Mặt nạ, Bình nước), Bẫy, Bóng bắt thú | |
| Lò Rèn | Cường hóa (+1 đến +20), Sửa chữa độ bền, chế tạo Set Bonuses | |
| Tiệm Kim Hoàn | Nâng Sao, Tinh Luyện (Nước Cất), trang bị Hào quang | |
| Phòng Thí Nghiệm Tiến Hóa | Tiến hóa Monster, Giám định IVs | |
| Học Viện | Đào tạo Class cho Trainer (xem [03](03_Trainer_AI_System.md)) | |
| Sàn Chứng Khoán HUB Street | IPO, giao dịch cổ phiếu (xem §3) | |
| Cổng Dịch Chuyển | Fast-travel giữa HUB và các Zone | |
| Bảng Truy Nã | Treo thưởng nguyên liệu (xem [01](01_World_Map_Environment.md)) | Prop, không có 4 trạng thái |
| Bảo Tàng Khảo Cổ | Ghép Cổ vật (xem [08](08_Quests_Achievements_Collections.md)) | Sau Early Access |
| Đấu Trường Nội Bộ | PvP nội bộ (xem [06](06_Events_PVE_PVP.md)) | Sau Early Access |

### 1.2. Ngân Hàng Gene
*   Thu phí lưu trữ theo ngày cho mỗi Monster dư mà Trainer gửi. Đây là nguồn Sink, không phải Faucet.
*   Nếu Trainer không trả phí quá số ngày quy định (xem [13_Balance_Parameters](13_Balance_Parameters.md)), Giám đốc được quyền **tịch thu** Monster đó.

## 2. QUẢN TRỊ KHỦNG HOẢNG TÀI CHÍNH
*   **Chu kỳ Lương (Payday):** Mỗi 30 ngày in-game, HUB trả lương cho tối đa 30 Trainer.
*   **Đình Công (Strike):** Nếu HUB vỡ nợ không trả nổi lương, AI đình công và chui vào Quán Bar xài tiền túi. HUB thu lại tiền từ Quán Bar để... trả lương cho chúng.
*   **Hai cơ chế vay nợ (tách biệt):**
    *   *Vay Từ Trainer (Reverse Loan):* Giám đốc vay Gold từ Trainer Rank V (xem [03](03_Trainer_AI_System.md)) khi kho bạc cạn. Nếu không trả nổi, AI xiết nợ bằng cách xài dịch vụ HUB miễn phí.
    *   *Cho Trainer Vay (Vay Nặng Lãi):* Trainer hết tiền (hoặc muốn mua cổ phiếu, mua đồ) vay Gold từ HUB. Giám đốc đặt lãi suất. Số dư Gold của Trainer có thể xuống ÂM (nợ xấu); khi đó lương và tiền bán nguyên liệu được trừ nợ trước. Lãi suất, hạn mức và hậu quả quá hạn: xem [13_Balance_Parameters](13_Balance_Parameters.md).

### 2.1. Các loại thuế
| Loại thuế | Áp dụng cho | Giá trị mặc định |
|---|---|---|
| Thuế giao dịch | Mỗi lần Trainer bán nguyên liệu ở Trạm Giao Thương. Giám đốc tự chỉnh. | 20% (FTUE) |
| Thuế Tự Do Tài Chính | Lợi nhuận chứng khoán của Trainer (xem §3) | Nặng, xem bảng tham số |

*   **Ngưỡng Thanh Tra Lao Động** ([06](06_Events_PVE_PVP.md)): bị phạt khi Thuế giao dịch **> 30%** hoặc Stress đỏ. Mặc định 20% của FTUE nằm dưới ngưỡng để người chơi có khoảng tăng thuế.
*   Cổ vật *Đồng Tiền Hai Mặt* (Round-up 10%) chỉ áp dụng cho Thuế giao dịch.

## 3. SÀN CHỨNG KHOÁN HUB STREET
*   **Cơ chế:** Công trình đạt Lvl 3 (Max) được IPO (Phát hành cổ phiếu). Giám đốc giữ 51%. Cứ 15 ngày chia cổ tức dựa trên doanh thu. Sàn mở cửa ban ngày (xem [01](01_World_Map_Environment.md)).
*   **Hành vi AI theo Rarity và Tính cách (xem [03](03_Trainer_AI_System.md)):** Trainer Common có xu hướng all-in/vay nợ mua cổ phiếu. Trainer Ultimate mua Blue-chip. Tính cách *Tư bản* hay lướt sóng, *Nhát gan* dễ panic sell.
*   **Thao túng thị trường:**
    *   *Pump & Dump:* Mở sự kiện giảm giá lấy Traffic -> Giá cổ phiếu tăng -> Giám đốc bán tháo chốt lời.
    *   *Short Selling:* Cố tình tắt điện/ngưng bảo trì công trình -> Giá sập -> Giám đốc gom mua giá đáy.
*   **Thuế Tự Do Tài Chính:** Đánh thuế nặng vào AI giàu từ chứng khoán, tránh việc chúng lười biếng bỏ farm.
