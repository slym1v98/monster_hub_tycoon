# SUB-GDD 4: MONSTER GENETICS & COMBAT

## 1. DỮ LIỆU, CẤP & TIỀM NĂNG (IVs)
*   Mỗi Monster có bộ Gen cố định ngẫu nhiên (Flattened Data). 
*   **Base Stats:** HP, ATK, DEF, ASPD, CRIT. Chỉ Monster có HP chiến đấu (Trainer không có HP).
*   **Cấp Monster không có EXP riêng**, suy ra từ Rank và cấp của Trainer: **Monster Lv = 20 x (Rank - 1) + ⌈Trainer Lv / 5⌉**. Trainer Rank I Lv1-100 -> Monster Lv1-20; Rank II -> Lv21-40; ... Rank V -> Lv81-100. Rebirth không làm Monster tụt cấp; Monster mới về đội nhận ngay cấp tương ứng.
*   **Rarity** quy định chỉ số khởi đầu và hệ số scale, không chặn cấp.
*   **IVs (D -> SSS):** Quyết định tỉ lệ tăng trưởng. Hệ số khởi điểm: D 0.8, C 0.9, B 1.0, A 1.1, S 1.2, SS 1.3, SSS 1.5. Quái "Rác" = IV D và C.
*   IV chưa biết khi bắt; AI trả tiền Giám định IVs ở Phòng Thí Nghiệm Tiến Hóa. Quái "Rác" thường bị phân giải thành Gene Fragments.

## 2. CƠ CHẾ CHIẾN ĐẤU & ĐỘI HÌNH
*   **Chiến đấu trên bản đồ:** Monster tự tung skill như game RPG ngay tại Zone, không chuyển cảnh. Khi Trainer chạm trán quái, Domain tính trọn trận ngay lập tức (skill, hồi chiêu, khắc chế hệ, Swap, Rebellion) và trả về một "kịch bản trận" cùng kết quả (HP mất, độ bền hao, loot, EXP, cơ hội bắt). Unity diễn lại kịch bản đó; khi Trainer nằm ngoài khung hình chỉ cần kết quả.
*   **1 Active + 2 Reserve:** Trainer mang tối đa 3 Monster.
*   **Swap (Đổi thú):** HP < 15%, tự động thu hồi con Active, tung con Reserve ra. Tối ưu thời gian farm.
*   **Ngất:** Monster 0 HP chỉ ngất, không chết. Cả 3 Monster cạn HP thì Trainer về HUB. Chữa Monster ngất ở Bệnh Viện tốn thêm phí hồi sức và lâu hơn.
*   **Bắt thú:** cần Bóng. Trainer tự chọn con đáng bắt (Rarity cao hơn hoặc mạnh hơn con yếu nhất trong đội) và ném Bóng khi quái yếu. Tỉ lệ = f(HP quái còn lại, Tier Bóng/Bẫy, Khéo léo, Class Trapper, Rarity quái). Monster bắt được đưa về Bệnh Viện Thú Y hồi phục; Trainer đổi Monster tại đó (xem [02](02_HUB_Economy_Infrastructure.md) §1.4).
*   **Bag Synergy (Cộng hưởng Balo):** Quái dự bị vai trò Tank (Buff HP), DPS (Buff Crit), Support (Buff Regen) cho quái Active. Khuyến khích AI mua Sách Chiến Thuật ghép đội hình.
*   **Hệ nguyên tố (9):** Lửa, Nước, Cỏ, Sét, Băng, Độc, Đất, Ánh sáng, Bóng tối. Vai trò (Tank/DPS/Support) là khái niệm riêng, không phải hệ.
*   **Hệ Tương Khắc:** Giữ tam giác Lửa > Cỏ > Nước > Lửa; Ánh sáng & Bóng tối khắc nhau. Các hệ còn lại theo logic của Pokémon (Sét khắc Nước, Băng khắc Cỏ/Đất, Độc khắc Cỏ, Đất khắc Lửa/Sét/Độc, Nước và Cỏ khắc Đất). Khác Pokémon: **không có miễn nhiễm (0x)**, thay bằng kháng (0.5x), để AI luôn gây được sát thương.
*   **Bảng sát thương nguyên tố** (hàng = hệ tấn công, cột = hệ phòng thủ; **2** = x2, **½** = x0.5, trống = x1; hệ số là giá trị khởi điểm):

| Tấn công \ Phòng thủ | Lửa | Nước | Cỏ | Sét | Băng | Độc | Đất | Sáng | Tối |
|---|---|---|---|---|---|---|---|---|---|
| **Lửa** | ½ | ½ | 2 | | 2 | | | | |
| **Nước** | 2 | ½ | ½ | | | | 2 | | |
| **Cỏ** | ½ | 2 | ½ | | | ½ | 2 | | |
| **Sét** | | 2 | ½ | ½ | | | ½ | | |
| **Băng** | ½ | ½ | 2 | | ½ | | 2 | | |
| **Độc** | | | 2 | | | ½ | ½ | | |
| **Đất** | 2 | | ½ | 2 | | 2 | | | |
| **Ánh sáng** | | | | | | | | ½ | 2 |
| **Bóng tối** | | | | | | | | 2 | ½ |

*   **Hệ theo Zone (khung ban đầu):** Đồng Cỏ (Cỏ, Nước, Đất), Núi Lửa (Lửa, Đất), Hầm Băng (Băng, Nước), Đầm Lầy (Độc, Nước, Cỏ), Vực Thẳm (Bóng tối). Sét và Ánh sáng xuất hiện qua thời tiết/ban ngày-đêm và sự kiện.
*   **Bình nước buff:** mua ở Tiệm Tạp hóa, tăng tạm HP/ATK/DEF/CRIT... cho Monster (xem [12](12_Item_Catalog.md)).

## 3. TĂNG TƯ CHẤT, TIẾN HÓA & SỰ PHẢN KHÁNG
*   **Rarity Monster:** dùng cùng 5 bậc với Trainer (Common < Rare < Epic < Legendary < Ultimate). IVs (D -> SSS) là tiềm năng tăng trưởng, độc lập với Rarity.
*   **Tăng tư chất (tăng Rarity):** tại Phòng Thí Nghiệm Tiến Hóa, dùng vật phẩm tăng Rarity, Monster cần Lv ≥ 40. Tỉ lệ thành công theo thang 60 / 40 / 25 / 10% ([13](13_Balance_Parameters.md) §4). Tỉ lệ thất bại cao (Ép mua Bùa Bảo Hộ bằng Gold).
*   **Tiến hóa (đổi hình thái, giống đổi class):** tối đa 2 lần; tùy loài có nhánh (chọn vai trò Tank/DPS/Support và bộ skill) hoặc không; có loài không tiến hóa. Tốn Đá Tiến Hóa + Lõi Đột Biến (Gene Fragments thay được một phần), có tỉ lệ thất bại (Bùa Bảo Hộ giữ vật liệu). Giữ nguyên Rarity, cấp, IV, trang bị; đổi vai trò, skill và sprite.
*   **Rebellion (Bất tuân):** Khi **Điểm Quản Lý** của Monster > **Điểm Lãnh đạo** của Trainer.
    *   *Điểm Quản Lý* = Cấp Monster + 20 x bậc Rarity Monster (Common = 0 ... Ultimate = 4).
    *   *Cấp quy đổi của Trainer* = 20 x (Rank - 1) + Lv / 5 (cùng thang với cấp Monster, để Rebirth không gây bất tuân).
    *   *Điểm Lãnh đạo* = 20 + 20 x bậc Rarity Trainer + Cấp quy đổi + thưởng (Khóa Giao Tiếp, Học Viện).
    *   Một bậc Rarity tương đương 20 cấp độ: Monster cùng bậc luôn nghe lời; hơn 1 bậc vừa đủ; hơn 2 bậc cần thưởng Lãnh đạo. Xem [13_Balance_Parameters](13_Balance_Parameters.md) §13.
    *   *Hậu quả:* Bỏ đánh, ngủ gật, cắn diện rộng kéo aggro quái rừng. Khởi điểm: xác suất bỏ đánh mỗi trận tỉ lệ với mức thiếu điểm.
    *   *Giải pháp:* AI phải chạy về HUB mua "Bánh thưởng" cao cấp hoặc học Khóa Giao Tiếp Thú Cưng với giá đắt đỏ.
