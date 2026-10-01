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

### 1.3. Nâng cấp công trình (đề xuất từ mô phỏng)
Công thức chi phí: **Chi phí nâng cấp = Lợi ích mỗi tháng x Số tháng hoàn vốn mục tiêu** (xem [13_Balance_Parameters](13_Balance_Parameters.md) §11). Mục tiêu hoàn vốn: 2 tháng cho Lvl 1 -> 2, 4 tháng cho Lvl 2 -> 3. Mỗi Payday là 1 "tháng" in-game.

| Công trình | Lvl 1 | Lvl 2 | Lvl 3 (Max) | Lợi ích | Ghi chú |
|---|---|---|---|---|---|
| Tòa Thị Chính | 10 Trainer | 20 Trainer | 30 Trainer | Giới hạn dân số | Nâng cấp lớn nhất, đắt nhất |
| Trạm Giao Thương, Nhà máy Tinh chế, Lò Phản Ứng | hiệu suất 0.85 | 0.92 | 0.99 | Giá trị thu về từ nguyên liệu | Lợi ích tăng theo số Trainer |

Mẫu chi phí (Trainer Common, 10 Trainer, đơn vị Gold khởi điểm): Tòa Thị Chính Lvl 1 -> 2 khoảng 158,000 (5.5 lần quỹ lương hiện có); Trạm/Nhà máy +0.07 hiệu suất khoảng 17,000 (0.6 lần quỹ lương) với 10 Trainer, 100,000 (1.2 lần) với 30 Trainer. Vì chi phí lớn hơn nhiều so với quỹ lương, **mỗi lần nâng cấp là một quyết định có rủi ro Payday** (xem §2.0).

Chi phí vận hành: khoảng **50 Gold/công trình/ngày** (tương đương 10% lợi nhuận tháng của HUB 30 Trainer với 16 công trình); chi phí tăng theo cấp. Hiệu ứng nâng cấp của Nhà Trọ, Nhà Hàng, Bệnh Viện, Quán Bar, Lò Rèn, Tiệm Kim Hoàn, Xưởng Công Cụ, Học Viện, Phòng Thí Nghiệm *chưa chốt* (xem Open Issues O7).

## 2. QUẢN TRỊ KHỦNG HOẢNG TÀI CHÍNH
*   **Chu kỳ Lương (Payday):** Mỗi 30 ngày in-game, HUB trả lương cho tối đa 30 Trainer.
*   **Đình Công (Strike):** Nếu HUB vỡ nợ không trả nổi lương, AI đình công và chui vào Quán Bar xài tiền túi. HUB thu lại tiền từ Quán Bar để... trả lương cho chúng.
*   **Hai cơ chế vay nợ (tách biệt):**
    *   *Vay Từ Trainer (Reverse Loan):* Giám đốc vay Gold từ Trainer Rank V (xem [03](03_Trainer_AI_System.md)) khi kho bạc cạn. Nếu không trả nổi, AI xiết nợ bằng cách xài dịch vụ HUB miễn phí.
    *   *Cho Trainer Vay (Vay Nặng Lãi):* Trainer hết tiền (hoặc muốn mua cổ phiếu, mua đồ) vay Gold từ HUB. Giám đốc đặt lãi suất. Số dư Gold của Trainer có thể xuống ÂM (nợ xấu); khi đó lương và tiền bán nguyên liệu được trừ nợ trước. Lãi suất, hạn mức và hậu quả quá hạn: xem [13_Balance_Parameters](13_Balance_Parameters.md).

### 2.0. Rủi ro Payday (quy tắc thiết kế, từ mô phỏng)
*   Khủng hoảng Payday **không** do kinh tế tự nhiên tạo ra (tiền Trainer quay lại HUB). Nó đến từ quyết định tái đầu tư của Giám đốc và các cú sốc (Siege, Thanh Tra phạt, Boss hỏng công trình, chi phí Black Friday).
*   Giám đốc cần giữ dự trữ khoảng **1 lần quỹ lương** trở lên. UI phải luôn hiển thị *"Payday sau X ngày, cần Y Gold, hiện có Z Gold"* để người chơi quyết định có chủ ý (rủi ro công bằng, không bất ngờ).
*   Chi phí một cú sốc cỡ **10 ngày lợi nhuận** là đủ gây Đình công nếu dự trữ mỏng, nhưng không phá sản người chơi giữ dự trữ đủ. Số liệu: [13_Balance_Parameters](13_Balance_Parameters.md) §10.

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
