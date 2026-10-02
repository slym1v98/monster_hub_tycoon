# SUB-GDD 2: HUB INFRASTRUCTURE & ECONOMY

## 1. CHUỖI CUNG ỨNG & CÔNG TRÌNH (TECH TREE)

### 1.0. Dòng tiền và dòng hàng
*   **Nguồn Gold vào nền kinh tế (Faucet):** (1) quái rơi Gold trực tiếp vào túi Trainer; (2) Thương nhân trả Gold cho Trainer khi mua nguyên liệu; (3) NPC Tổng tài thỉnh thoảng donate cho Trainer hết tiền (§1.5). HUB **không bán gì ra bên ngoài**: mọi Gold của Giám đốc đến từ Trainer (thuế, dịch vụ, trang bị, lãi, phí).
*   **Trạm Giao Thương:** chợ của HUB. Giám đốc đặt **yêu cầu mua** cho từng loại nguyên liệu (mức cần giữ trong kho + giá mua). Trainer bán theo yêu cầu và chịu Thuế giao dịch. Trạm có **gian hàng HUB** để Giám đốc chào bán trang bị cho Trainer.
*   **Thương nhân (NPC lưu động):** đi khắp nơi tìm Trainer, mua nguyên liệu **rẻ hơn giá Trạm 5-10%** (tùy khối lượng), rồi mở gian hàng tại Trạm bán cho HUB **đắt hơn giá Trạm 5-10%**. Nhờ đó Trainer luôn bán được hàng, và HUB mua được thứ quên đặt nhưng chịu giá cao. Thương nhân thỉnh thoảng phá sản, người mới tới thay.
*   **Gia công:** Nhà máy Tinh chế biến nguyên liệu thô thành phôi. Lò Phản Ứng biến phôi thành vật liệu nâng cấp (Đá Cường hóa, Nước Cất, Đá Tiến Hóa).
*   **Chế tạo trang bị (3 xưởng theo nhóm slot, xem [05](05_Itemization_Gear_System.md)):** Lò Rèn (đồ Monster), Xưởng Dệt (đồ Tiện ích của Trainer), Tiệm Kim Hoàn (đồ Hào quang). Trang bị bán qua gian hàng HUB ở Trạm.
*   **Hàng tiêu hao:** mỗi quầy tự chế từ nguyên liệu của mình: Bệnh Viện (Thuốc, Vắc-xin, Thuốc An Thần), Nhà Hàng (đồ ăn, nước uống, Bánh thưởng), Quán Bar (Rượu), Xưởng Công Cụ (Áo mưa, Mặt nạ, Bẫy, Bóng, Sách), Nhà Máy Nước Ngọt (bình nước buff chỉ số Monster, bán ở Tiệm Tạp hóa). Ngủ ở Nhà Trọ và chữa HP không cần nguyên liệu. Hết nguyên liệu thì hết hàng.
*   **Sản xuất theo tồn kho mục tiêu:** Giám đốc đặt mức tồn kho cho từng món ("giữ 20 Thuốc"). Xưởng tự sản xuất khi dưới mức nếu đủ nguyên liệu.

**Trạng thái Domain (prototype, 2026-10-02):** `HubWorld` hiện nối Trainer bán nguyên liệu theo ID, yêu cầu mua có target/bid của Trạm, thuế, tồn kho, Thương nhân có tiền/sức chứa hữu hạn và tuyến ghé, thay Merchant sau phá sản, cùng job sản xuất giữ nguyên liệu, trừ chi phí khi bắt đầu nếu Kho bạc đủ tiền và phát demand khi thiếu. Thời lượng job áp dụng multiplier theo cấp Producer (Prototype: 1.0, 0.9, 0.8, 0.7, 0.6); kết quả được làm tròn lên phút. Trainer đang chờ được thử bán ngay khi Merchant thay thế đến, kể cả khi chạm hạn chờ cùng phút. Hành vi này được báo cáo bằng `tools/Game.Sim core` và `market`; các số dùng ở đó là Prototype/TBD, chưa cân bằng.

**Còn TBD:** `SimpleFarmResolver` tạm gán mọi loot nguyên liệu thành Quặng tier 1 vì GDD chưa có bảng loot theo từng Zone/đối tượng; đây không phải quyết định danh mục loot. Loot thật phải thay theo mô hình farm/chiến đấu. Mục tiêu mua, giá đặt của Giám đốc, vốn/chu kỳ Merchant và hiệu suất/thời gian sản xuất vẫn cần cân bằng trong workbook. Đường cong thời lượng job theo cấp Producer hiện là giả định prototype để hiện thực hóa luật GDD và cần cân bằng. Trainer chưa tự chọn và mua hàng tiêu hao/trang bị; phần mô phỏng quầy hiện giữ catalog, công thức và tồn kho, chưa mô phỏng hành vi tiêu dùng hay áp dụng hiệu ứng. Stress do thuế giao dịch vượt 30% và do chờ Merchant theo GDD 03 cũng chưa nối vào FSM. Nếu Merchant không mua hết, hàng còn trong balo; Trainer chờ theo giới hạn cấu hình Prototype/TBD (mặc định 210 phút) rồi tiếp tục với phần chưa bán.

```
Trainer --thô--> Trạm Giao Thương (yêu cầu mua, thuế) <-- Thương nhân (mua rẻ của Trainer, bán đắt cho HUB)
                    |
                    +--> Nhà máy Tinh chế --phôi--> Lò Rèn / Xưởng Dệt / Kim Hoàn --> gian hàng HUB
                    |                         \---> Lò Phản Ứng --> Đá Cường hóa, Nước Cất, Đá Tiến Hóa
                    +--> Bệnh Viện / Nhà Hàng / Bar / Xưởng Công Cụ / Nhà Máy Nước Ngọt (tự chế tiêu hao)
```

### 1.1. Bảng công trình
Mỗi loại chỉ xây **1 công trình**; vị trí đặt chỉ để trang trí, không ảnh hưởng số liệu.

| Công trình | Chức năng | Cấp tối đa | Ghi chú |
|---|---|---|---|
| Tòa Thị Chính | Cấp bằng Rebirth, tuyển dụng, mở khóa xây dựng/nâng cấp công trình khác | 25 | Mặc định (Đổ nát) |
| Ký túc xá | Quyết định dân số tối đa (§1.3) | 5 | Mặc định (Đổ nát), mới |
| Trạm Giao Thương | Yêu cầu mua nguyên liệu, Thuế giao dịch, gian hàng HUB và Thương nhân | 5 | Mặc định (Đổ nát) |
| Bệnh Viện Thú Y | Chữa HP Monster, hồi phục Monster mới bắt, đổi Monster, bán Thuốc/Vắc-xin (§1.4) | 25 | Mặc định (Đổ nát) |
| Nhà máy Tinh chế | Nguyên liệu thô -> phôi | 5 | |
| Lò Phản Ứng | Phôi -> Đá Cường hóa, Nước Cất, Đá Tiến Hóa | 5 | |
| Lò Rèn | Chế đồ Monster, Cường hóa (+1 đến +20), Sửa đồ Monster | 5 | |
| Xưởng Dệt | Chế đồ Tiện ích Trainer, Sửa đồ Tiện ích | 5 | Mới |
| Tiệm Kim Hoàn | Chế đồ Hào quang, Nâng Sao, Tinh Luyện (Nước Cất) | 5 | |
| Nhà Trọ | Hồi Thể lực | 25 | |
| Nhà Hàng | Hồi No nê và Nước | 25 | |
| Quán Bar | Giảm Stress, bán Rượu | 25 | |
| Xưởng Công Cụ | Áo mưa, Mặt nạ, Bẫy, Bóng bắt thú, Sách Chiến Thuật | 25 | |
| Nhà Máy Nước Ngọt | Chế bình nước buff tạm HP/ATK/DEF/CRIT... cho Monster | 25 | Mới |
| Tiệm Tạp hóa | Bán bình nước buff của Nhà Máy Nước Ngọt | 25 | Mới |
| Ngân Hàng Gene | Lưu trữ Monster dư (thu phí), quầy bán Monster tịch thu, nhận Gene Fragments | 25 | Nhóm Sink, §1.4 |
| Phòng Thí Nghiệm Tiến Hóa | Giám định IVs, tăng tư chất (Rarity), Tiến hóa Monster | 25 | |
| Học Viện | Đào tạo Class cho Trainer (xem [03](03_Trainer_AI_System.md)) | 25 | |
| Sàn Chứng Khoán HUB Street | IPO, giao dịch cổ phiếu (xem §3) | 25 | |
| Cổng Dịch Chuyển | Fast-travel giữa HUB và các Zone | 25 | |
| Bảng Truy Nã | Treo thưởng nguyên liệu (xem [01](01_World_Map_Environment.md)) | - | Prop |
| Bảo Tàng Khảo Cổ | Ghép Cổ vật (xem [08](08_Quests_Achievements_Collections.md)) | 25 | Sau Early Access |
| Đấu Trường Nội Bộ | PvP nội bộ (xem [06](06_Events_PVE_PVP.md)) | 25 | Sau Early Access |

**Cấp và Tòa Thị Chính:**
*   Công trình 25 cấp: cấp công trình **không vượt cấp Tòa Thị Chính**. Mỗi cấp nhỏ tăng dần chất lượng, giá và sức chứa; món/tính năng mới mở ở các mốc riêng của từng công trình (ví dụ Nhà Hàng Lv1-6 chỉ bán 1 món, Lv7 mở món thứ 2).
*   Công trình 5 cấp: cấp N cần Tòa Thị Chính ở tier N (Lv ≥ 5N - 4). *(Khởi điểm.)*
*   Tòa Thị Chính chia 5 tier, mỗi tier 5 cấp, khớp 5 Zone. Lên Lv6 cần mở Zone 2; tương tự, lên tier N+1 cần mở Zone N+1.
*   Mỗi công trình có **5 model theo tier** + 1 model **Tàn phá** (§1.6). 4 công trình mặc định có thêm model **Đổ nát**; công trình khác xây mới từ đầu.

**Mở khóa theo cấp Tòa Thị Chính** *(khởi điểm, chỉnh khi cân bằng)*:

| Tòa Thị Chính | Công trình mở khóa |
|---|---|
| Lv1 | Ký túc xá, Trạm Giao Thương, Bệnh Viện Thú Y (dựng lại từ Đổ nát) |
| Lv2 | Nhà Trọ, Nhà Hàng |
| Lv3 | Nhà máy Tinh chế, Xưởng Công Cụ |
| Lv4 | Quán Bar, Lò Rèn |
| Lv5 | Xưởng Dệt, Bảng Truy Nã |
| Tier 2 (Lv6-10, cần Zone 2) | Lò Phản Ứng, Cổng Dịch Chuyển, Nhà Máy Nước Ngọt, Tiệm Tạp hóa, Ngân Hàng Gene |
| Tier 3 (Lv11-15, cần Zone 3) | Tiệm Kim Hoàn, Phòng Thí Nghiệm Tiến Hóa, Học Viện, Sàn Chứng Khoán |

### 1.2. Xây dựng, nâng cấp và vận hành
*   **Chi phí xây/nâng cấp:** Gold + phôi (Gỗ, Đá, Sắt) + thời gian (vài ngày in-game). Trong lúc nâng cấp, công trình vẫn chạy ở cấp cũ.
*   **Chi phí vận hành:** trả hằng ngày, tăng theo cấp. Không đủ tiền thì công trình chuyển sang **Thiếu bảo trì**: hiệu suất và sức chứa giảm một nửa, chất lượng giảm, cho tới khi trả đủ. Nợ vận hành không cộng dồn.
*   **Tắt điện:** Giám đốc chủ động đóng cửa một công trình: không tốn vận hành, dịch vụ ngừng, giá cổ phiếu công trình đó giảm (Short Selling, §3).

### 1.3. Dân số và mở Zone
Dân số do **Ký túc xá** quyết định, tăng theo mốc mở Zone:

| Mở Zone | Điều kiện | Dân số tối đa |
|---|---|---|
| 1 | 5 Trainer Rank I + Ký túc xá Lv1 | 10 |
| 2 | 7/10 Trainer Rank II + Ký túc xá Lv2 | 15 |
| 3 | 12/15 Trainer Rank III + Ký túc xá Lv3 | 20 |
| 4 | 17/20 Trainer Rank IV + Ký túc xá Lv4 | 25 |
| 5 | 22/25 Trainer Rank V + Ký túc xá Lv5 | 30 |

### 1.4. Bệnh Viện Thú Y và Ngân Hàng Gene
*   **Chỉ Monster có HP chiến đấu.** Bệnh Viện chữa HP Monster (phí theo HP). Monster ngất (0 HP) tốn thêm phí hồi sức và chữa lâu hơn.
*   **Hai khu:** khu cấp cứu (Monster đang dùng, sức chứa theo §1.5) và khu hồi phục **100 giường** cho Monster mới bắt. Monster bắt được đều đưa về Bệnh Viện; lần hồi phục đầu tiên lâu gấp nhiều lần chữa thường. Trainer đổi Monster tại Bệnh Viện.
*   Hết giường hồi phục: Trainer ở lại HUB la hét tới khi được phục vụ, hoặc Giám đốc yêu cầu Trainer gửi Monster vào Ngân Hàng Gene. Bắt được con đủ hiếm thì Trainer luôn ưu tiên đưa vào Bệnh Viện. Cơ chế này giữ Monster ưu tú và loại Monster kém.
*   **Ngân Hàng Gene (Sink):** Monster cũ được gửi vào (thu phí) hoặc phân giải thành Gene Fragments (bán cho Trạm, nguyên liệu của Phòng Thí Nghiệm Tiến Hóa).
*   Phí thu **mỗi Payday, sau khi trả lương** (mặc định 20 Gold/Monster/ngày, tính cho 30 ngày). Thu hằng ngày không hoạt động vì Trainer hết tiền mặt mỗi ngày. Nếu Trainer không đủ phí ở Payday, Giám đốc **tịch thu** 1 Monster. Monster tịch thu thuộc HUB: bán lại cho Trainer khác (quầy Monster ở Ngân Hàng Gene) hoặc phân giải. Xem [13_Balance_Parameters](13_Balance_Parameters.md) §13.

### 1.5. Công trình dịch vụ: sức chứa, xếp hàng và giá
*   **Sức chứa = số chỗ dùng cùng lúc** (giường, bàn). Thời gian phục vụ ở các công trình gần như bằng nhau và ngắn, để Trainer quay lại farm nhanh. Hết chỗ thì Trainer đứng đợi.
*   Sức chứa được thiết kế **luôn thiếu nhẹ so với dân số**: ví dụ Nhà Trọ Lv5 (dân số tối đa 10) có 9 giường, Lv25 (dân số 30) có 25 giường. Công thức khởi điểm: **số chỗ = 5 + 0.8 x cấp**.
*   **Nhu cầu không được đáp ứng** (chưa có công trình, hết hàng, quá tải): Trainer ở lỳ trong HUB tới khi được đáp ứng. Không có debuff hay cái chết; cái giá là mất ngày công farm.
*   **Trainer hết tiền:** xin ứng lương; NPC **Tổng tài** thỉnh thoảng đi dạo trong HUB và donate (lối thoát thường gặp nhất); hoặc Giám đốc donate không hoàn lại.
*   **Giá hợp lý + Stress:** mỗi món có *giá hợp lý* theo chất lượng (cấp công trình). Hàng thiết yếu (ăn, uống, ngủ, chữa): AI luôn mua, nhưng mỗi lần trả trên giá hợp lý thì Stress tăng theo mức chênh. Hàng không thiết yếu (trang bị, bình buff, Bar): xác suất mua giảm khi giá tăng. Tính cách quyết định độ nhạy giá (Tư bản nhạy nhất). Stress cao dẫn tới Bar, Đình công và Thanh Tra.

### 1.6. Hư hại
*   Siege thua hoặc World Boss làm công trình chuyển sang **Hư hại**: chạy yếu như Thiếu bảo trì cho tới khi Giám đốc trả phí sửa (Gold + phôi, tỉ lệ với cấp) và chờ sửa xong.
*   Xác suất rất nhỏ công trình bị **tụt cấp**. Khi đó, vật phẩm trong kho đòi cấp cao hơn cũng bị hư hại; hư hại càng nhiều càng tốn tiền khôi phục.

### 1.7. Chi phí nâng cấp (số liệu mô phỏng cũ)
> Các số dưới đây tính trên mô hình cũ (3 cấp, Tòa Thị Chính quyết định dân số) và cần mô phỏng lại theo hệ cấp mới. Giữ lại công thức và các tỉ lệ làm điểm xuất phát.

Công thức chi phí: **Chi phí nâng cấp = Lợi ích mỗi tháng x Số tháng hoàn vốn mục tiêu** (xem [13_Balance_Parameters](13_Balance_Parameters.md) §11). Mỗi Payday là 1 "tháng" in-game. Mục tiêu hoàn vốn cũ: 2 tháng cho bước đầu, 4 tháng cho bước sau. Vì chi phí nâng cấp lớn hơn nhiều so với quỹ lương, **mỗi lần nâng cấp là một quyết định có rủi ro Payday** (xem §2.0).

Doanh thu dịch vụ ròng đo được (Trainer Common, mỗi tháng): Bệnh Viện 2,524; Nhà Hàng 1,800; Nhà Trọ 1,012; Quán Bar 928. Một tháng doanh thu dịch vụ của 10 Trainer Common khoảng 93,600 Gold. Chi phí vận hành cũ khoảng 50 Gold/công trình/ngày (10% lợi nhuận tháng của HUB 30 Trainer với 16 công trình).

## 2. QUẢN TRỊ KHỦNG HOẢNG TÀI CHÍNH
*   **Chu kỳ Lương (Payday):** Mỗi 30 ngày in-game, HUB trả lương theo hợp đồng cho mọi Trainer (xem [03](03_Trainer_AI_System.md) §2).
*   **Trả thiếu:** Kho bạc không đủ thì quỹ lương được **chia đều theo tỉ lệ**, nhưng chỉ cần thiếu là **toàn bộ Trainer đình công**. Phần thiếu thành nợ lương cộng dồn.
*   **Thang vỡ nợ (không có game over, Kho bạc không bao giờ âm):**
    *   *Vỡ nợ Payday 1:* Đình công, nợ lương cộng dồn.
    *   *Vỡ nợ Payday 2:* Stress tăng mạnh.
    *   *Vỡ nợ Payday 3 trở đi, chế độ "cấn nợ":* Trainer vẫn farm nhưng ít hơn; nhậu ở Quán Bar bằng tiền túi (doanh thu giúp HUB có tiền trả lương); dùng miễn phí dịch vụ như Nhà Hàng, giá trị dịch vụ trừ vào nợ lương.
    *   Trainer không bỏ đi vì nợ lương.
*   **Hai cơ chế vay nợ (tách biệt):**
    *   *Vay Từ Trainer (Reverse Loan):* Giám đốc vay Gold từ Trainer Rank V (xem [03](03_Trainer_AI_System.md)), tối đa khoảng 50% tiền mặt của mỗi người, lãi 5%/Payday, hạn 1-2 Payday. Quá hạn: Trainer đó dùng dịch vụ HUB miễn phí, giá trị trừ vào nợ cho tới khi hết (cùng cơ chế cấn nợ).
    *   *Cho Trainer Vay (Vay Nặng Lãi):* Trainer hết tiền (hoặc muốn mua cổ phiếu, mua đồ) vay Gold từ HUB. Giám đốc đặt lãi suất. Số dư Gold của Trainer có thể xuống ÂM (nợ xấu); khi đó lương và tiền bán nguyên liệu được trừ nợ trước. Lãi suất, hạn mức và hậu quả quá hạn: xem [13_Balance_Parameters](13_Balance_Parameters.md) §12 (mặc định hạn mức 2 lần lương tháng, lãi 10%/Payday).

### 2.0. Rủi ro Payday (quy tắc thiết kế, từ mô phỏng)
*   Khủng hoảng Payday **không** do kinh tế tự nhiên tạo ra (tiền Trainer quay lại HUB). Nó đến từ quyết định tái đầu tư của Giám đốc và các cú sốc (Siege, Thanh Tra phạt, Boss làm Hư hại công trình, chi phí Black Friday).
*   Giám đốc cần giữ dự trữ khoảng **1 lần quỹ lương** trở lên. UI phải luôn hiển thị *"Payday sau X ngày, cần Y Gold, hiện có Z Gold"* để người chơi quyết định có chủ ý (rủi ro công bằng, không bất ngờ). Lương theo hợp đồng cố định nên quỹ lương luôn biết trước.
*   Chi phí một cú sốc cỡ **10 ngày lợi nhuận** là đủ gây Đình công nếu dự trữ mỏng, nhưng không phá sản người chơi giữ dự trữ đủ. Số liệu: [13_Balance_Parameters](13_Balance_Parameters.md) §10.

### 2.1. Các loại thuế
| Loại thuế | Áp dụng cho | Giá trị mặc định |
|---|---|---|
| Thuế giao dịch | Mỗi lần Trainer bán nguyên liệu ở Trạm Giao Thương. Giám đốc tự chỉnh. | 20% (FTUE) |
| Thuế Tự Do Tài Chính | Lợi nhuận chứng khoán của Trainer (xem §3) | Nặng, xem bảng tham số |

*   Thuế giao dịch vượt 30% làm Stress của Trainer tăng theo mức vượt (xem [03](03_Trainer_AI_System.md) §1).
*   **Ngưỡng Thanh Tra Lao Động** ([06](06_Events_PVE_PVP.md)): bị phạt khi Thuế giao dịch **> 30%** hoặc Stress đỏ (≥ 80). Mặc định 20% của FTUE nằm dưới ngưỡng để người chơi có khoảng tăng thuế.
*   Cổ vật *Đồng Tiền Hai Mặt* (Round-up 10%) chỉ áp dụng cho Thuế giao dịch.

### 2.2. Danh tiếng HUB
*   Chỉ số tổng thể ngoài tiền, tính từ chất lượng dịch vụ (cấp công trình, nội thất), mức giá so với giá hợp lý, Stress trung bình và số lần vỡ nợ.
*   Danh tiếng cao: ứng viên tuyển thường có Rarity cao hơn, Traffic tăng. Danh tiếng thấp: ứng viên kém, Thanh Tra hay ghé. Đây là thế cân bằng dài hạn với việc bóc lột ngắn hạn. Trong Early Access, Danh tiếng đóng vai trò của Prestige (Prestige từ PvP là sau Early Access).

## 3. SÀN CHỨNG KHOÁN HUB STREET
*   **Cơ chế:** Công trình đạt cấp yêu cầu được IPO (Phát hành cổ phiếu). Cấp của Sàn quyết định số công trình niêm yết cùng lúc và mở các công cụ thao túng. Giám đốc giữ 51% (khóa, không bán được); 49% bán cho Trainer, tiền vào Kho bạc. Cứ 15 ngày chia cổ tức dựa trên doanh thu. Mỗi giao dịch của Trainer trả phí sàn cho HUB. Sàn mở cửa ban ngày (xem [01](01_World_Map_Environment.md)).
*   **Giá:** mỗi ngày giá = giá hôm qua x (1 + nhiễu ngẫu nhiên ±3% + 0.5 x thay đổi Traffic + k x lệnh mua/bán ròng / số cổ phiếu lưu hành). Traffic = doanh thu ngày của công trình so với trung bình 15 ngày. Không có sổ lệnh: lệnh khớp ngay ở giá hiện tại.
*   **Hành vi AI theo Rarity và Tính cách (xem [03](03_Trainer_AI_System.md)):** Trainer Common có xu hướng all-in/vay nợ mua cổ phiếu. Trainer Ultimate mua Blue-chip. Tính cách *Tư bản* hay lướt sóng, *Nhát gan* dễ panic sell; panic sell hàng loạt kéo giá sập thêm.
*   **Thao túng thị trường:** Giám đốc mua/bán thêm phần cổ phiếu nổi ở giá hiện tại.
    *   *Pump & Dump:* Mở sự kiện giảm giá lấy Traffic -> Giá cổ phiếu tăng -> Giám đốc bán tháo chốt lời.
    *   *Short Selling:* Cố tình Tắt điện công trình (§1.2) -> Giá sập -> Giám đốc gom mua giá đáy.
*   **Thuế Tự Do Tài Chính:** Đánh thuế nặng vào AI giàu từ chứng khoán, tránh việc chúng lười biếng bỏ farm.
