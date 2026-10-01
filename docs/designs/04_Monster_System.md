# SUB-GDD 4: MONSTER GENETICS & COMBAT

## 1. DỮ LIỆU & TIỀM NĂNG (IVs)
*   Mỗi Monster có bộ Gen cố định ngẫu nhiên (Flattened Data). 
*   **Base Stats:** HP, ATK, DEF, ASPD.
*   **IVs (D -> SSS):** Quyết định tỉ lệ tăng trưởng.
*   AI có thể trả tiền để Giám định IVs, nếu là quái "Rác", AI sẽ phân giải lấy Gene Fragments nộp cho HUB.

## 2. CƠ CHẾ CHIẾN ĐẤU & ĐỘI HÌNH
*   **1 Active + 2 Reserve:** Trainer mang tối đa 3 Monster.
*   **Swap (Đổi thú):** HP < 15%, tự động thu hồi con Active, tung con Reserve ra. Tối ưu thời gian farm.
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

## 3. ĐỘT BIẾN & SỰ PHẢN KHÁNG
*   **Phòng Thí Nghiệm Tiến Hóa (Bòn rút):** Max level -> Đốt Gold, Đá Tiến Hóa, Lõi Đột Biến để đổi ngoại hình & tăng Rarity. Tỉ lệ thất bại cao (Ép mua Bùa Bảo Hộ bằng Gold).
*   **Rarity Monster:** dùng cùng 5 bậc với Trainer (Common < Rare < Epic < Legendary < Ultimate). Tiến hóa tăng Rarity. IVs (D -> SSS) là tiềm năng tăng trưởng, độc lập với Rarity.
*   **Rebellion (Bất tuân):** Khi **Điểm Quản Lý** của Monster > "Điểm Lãnh đạo" của Trainer. *Điểm Quản Lý = Cấp độ Monster + (chỉ số bậc Rarity x K)*, với chỉ số bậc Common = 0 ... Ultimate = 4; K là tham số cân bằng.
    *   *Hậu quả:* Bỏ đánh, ngủ gật, cắn diện rộng kéo aggro quái rừng.
    *   *Giải pháp:* AI phải chạy về HUB mua "Bánh thưởng" cao cấp hoặc học Khóa Giao Tiếp Thú Cưng với giá đắt đỏ.
