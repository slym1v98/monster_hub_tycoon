# SUB-GDD 3: TRAINER AI & PROGRESSION

## 1. QUẢN LÝ NHÂN SỰ
*   **Dân số:** Tối đa 30 Trainer, mở dần theo Ký túc xá và mốc Zone (xem [02](02_HUB_Economy_Infrastructure.md) §1.3).
*   **Rarity (Độ hiếm):** Common < Rare < Epic < Legendary < Ultimate.
*   **Rank (Cấp bậc, I -> V):** Rank = 1 + số lần Rebirth (tối đa Rank V). Rank quyết định Zone được vào (Zone N cần Rank >= N), điều kiện mở Zone của HUB và đủ điều kiện cho HUB vay Gold (Rank V). Rank độc lập với Rarity và Level.
*   **Chỉ số cơ bản:** Lãnh đạo (Giới hạn cấp Monster quản lý), Khéo léo (Tốc độ lột đồ/bắt thú), Sức bền (Giới hạn thanh Thể lực), May mắn (lượng Gold nhặt được).
*   **Thanh trạng thái (Needs):** Mỗi Trainer có 4 thanh. Trainer không có HP; HP thuộc về Monster (xem [04](04_Monster_System.md)).

| Thanh | Hồi phục tại | Trang bị liên quan |
|---|---|---|
| Thể lực | Nhà Trọ | - |
| No nê | Nhà Hàng | - |
| Nước | Nhà Hàng (cùng lượt ăn) | Bình nước |
| Stress (Tinh Thần) | Quán Bar, ngủ | Nón (Tỉnh táo) |

*   **Stress (0-100):**
    *   *Tăng:* mỗi giờ farm; trả giá trên giá hợp lý (xem [02](02_HUB_Economy_Infrastructure.md) §1.5); Thuế giao dịch vượt 30% (theo mức vượt); nợ lương; chờ lâu (hết chỗ, hết hàng); xung đột Class và tính cách.
    *   *Giảm:* Quán Bar, ngủ, Roi Kỷ Luật, nội thất Gem.
    *   *Stress đỏ:* ≥ 80 (Thanh Tra tính vào vi phạm). *Đạt 100:* bắt buộc vào Bar ngay; không vào được thì nghỉ làm tới khi Stress giảm.
*   **Tính cách (Personality):** 
    *   *Háo chiến:* Đánh nhanh, Monster dễ ngất.
    *   *Nhát gan:* An toàn, mau về HUB, panic sell chứng khoán.
    *   *Tham ăn:* Tụt No nê nhanh (Tốn tiền nhà hàng).
    *   *Tư bản:* Nhặt nhiều nguyên liệu, giảm giá dịch vụ HUB nhưng ép lương cao, nhạy giá nhất. Hay lướt sóng chứng khoán và chạy theo Bảng Truy Nã (thưởng x3).

| Tính cách | Farm | Chứng khoán | Khi vỡ nợ / Stress cao |
|---|---|---|---|
| Háo chiến | Đánh nhanh, Monster dễ ngất, tốn viện phí | Ít quan tâm | Dễ vào Bar |
| Nhát gan | Về HUB sớm, farm an toàn | Panic sell khi giá sập | Đình công sớm |
| Tham ăn | Tụt No nê nhanh, về Nhà Hàng nhiều | Ít quan tâm | Chi nhiều cho Nhà Hàng |
| Tư bản | Nhặt nhiều nguyên liệu, farm lâu | Lướt sóng, bầy đàn theo Bảng Truy Nã | Ép lương, đòi tăng lương |

### 1.1. Vòng ra quyết định (FSM)
Domain mô phỏng theo sự kiện rời rạc (phút in-game). Trainer chạy FSM: Ở HUB -> Đi tới Zone -> Farm -> Về HUB -> Xếp hàng -> Dùng dịch vụ -> ...
*   **Về HUB khi xảy ra điều kiện đầu tiên:** một thanh dưới ngưỡng theo tính cách; Balo đầy; cả 3 Monster cạn HP; trời tối mà không có kính nhìn đêm. Ngưỡng khởi điểm: Háo chiến 15%, Nhát gan 50%, Tham ăn 30% (No nê 50%), Tư bản 25%.
*   **Ở HUB, thứ tự cố định:** bán hàng -> hồi thanh thấp nhất trước -> Bar (nếu Stress cao) -> mua đồ. UI hiển thị lý do ("về HUB: đói").
*   **Nhặt đồ:** nguyên liệu có tỉ lệ bị bỏ lại, tùy tính cách (Tư bản nhặt nhiều nhất). Gold luôn được nhặt, số lượng theo chỉ số May mắn. Balo chứa theo tổng số đơn vị nguyên liệu.
*   **Ban đêm:** Trainer có slot Kính đạt Tier có thuộc tính Nhìn đêm thì farm đêm (loot/EXP x2) và ngủ bù ban ngày. Không có thì về Nhà Trọ lúc 18:00. Cà phê ép xung cho thức thêm vài giờ.
*   **Chọn Zone:** trong các Zone đủ Rank, chọn nơi có thu nhập kỳ vọng mỗi giờ cao nhất (tính cả Bảng Truy Nã; Tư bản có thêm thiên vị cho Truy Nã).
*   **Di chuyển:** mỗi Zone có thời gian đi bộ, tốn Thể lực/No nê/Nước. Mua vé Cổng Dịch Chuyển nếu giá vé < thu nhập mỗi giờ x số giờ tiết kiệm + chi phí nhu cầu tiết kiệm.
*   **Mua trang bị:** Trainer không tự mua; chỉ mua khi Giám đốc chào hàng (Tycoon Club có Thư ký tự chào hàng theo quy tắc Giám đốc đặt). Trainer đồng ý theo Gear Score, giá (giá hợp lý + Stress) và tiền còn lại. Đồ cũ biến mất, trừ khi HUB yêu cầu mua lại.

## 2. VÒNG ĐỜI & CHẾ ĐỘ ĐÃI NGỘ
*   **Tuyển dụng:** Ứng viên tới Tòa Thị Chính định kỳ; Rarity của ứng viên phụ thuộc Danh tiếng HUB (xem [02](02_HUB_Economy_Infrastructure.md) §2.2). Giám đốc trả phí tuyển bằng Gold rồi đàm phán lương. Gacha Thư Mời Hoàng Gia là kênh song song bằng Gem.
*   **Hợp đồng lương:** Trainer ký hợp đồng độc quyền: tự sở hữu loot nhưng chỉ bán cho HUB (hoặc Thương nhân). Lương là con số cố định trong hợp đồng. Đề nghị ban đầu = công thức theo chỉ số, Rarity và tính cách (neo quanh 30% thu nhập kỳ vọng). Giám đốc ép giá khi tuyển; ép quá tay thì ứng viên bỏ đi.
*   **Đòi tăng lương:** sau Rebirth và sau khi học xong Class; Tư bản đòi tăng định kỳ.
*   **Rời HUB:** chỉ vì lý do hợp đồng: (1) Giám đốc trục xuất (nếu Trainer đang nợ, HUB mất khoản nợ); (2) đòi tăng lương bị từ chối nhiều lần thì xin nghỉ, Trainer phải trả đền bù hợp đồng cho HUB. Stress, nợ hay nợ lương không làm Trainer bỏ đi.
*   **Hết tiền:** xin ứng lương, chờ NPC Tổng tài donate, hoặc Giám đốc donate (xem [02](02_HUB_Economy_Infrastructure.md) §1.5).
*   **Monster Mặc định (Default):** Có sẵn 1 Monster Soul-bound khi gia nhập (cùng bậc Rarity với Trainer, ví dụ Trainer Common nhận Monster Common; bị Debuff nếu khác bậc).
*   **Thăng Hạng (Rebirth):** Lv100 -> Tòa Thị Chính cấp bằng -> Reset Lv1, tăng trưởng chỉ số cơ bản nhân lên, đồng thời tăng 1 Rank. Monster không tụt cấp (xem [04](04_Monster_System.md) §1).
*   **Nhịp mục tiêu:** chậm, Zone 5 sau khoảng 6 tháng trở lên với người chơi đều đặn. Chỉ dùng để chỉnh đường cong EXP; mở Zone vẫn theo điều kiện Trainer.

## 3. ĐÀO TẠO & NGHỀ NGHIỆP (CLASS)
Giám đốc **không điều khiển hành động** của AI, chỉ ra quyết định chi tiền: bỏ tiền túi cho AI vào Học Viện để học Nghề, từ đó định hình đội hình. Mọi hành vi còn lại vẫn do AI tự quyết:
*   **Combat Medic (Bác Sĩ):** Sustain, tốn nhiều Potion, an toàn.
*   **Tactical Commander (Chỉ Huy):** Buff sát thương, clear nhanh, Monster ngất nhiều (tốn tiền Bệnh viện).
*   **Tech Engineer (Kỹ Sư):** Thủ vững, farm lâu dài, tốn tiền mua Tiện ích sinh tồn.
*   **Trapper (Đặt Bẫy):** Khống chế, tăng tỉ lệ bắt thú. Khách VIP mua bẫy/bóng từ HUB.
*   **Khóa học:** Giám đốc trả học phí -> Trainer tự động nhận 1 nhiệm vụ đào tạo; hoàn thành nhiệm vụ là tốt nghiệp. Tỉ lệ đậu theo bảng tương thích Tính cách x Class (hợp / bình thường / xung đột; khởi điểm 90% / 65% / 30%). Trượt: mất phí, +Stress, học lại được. Đậu: nhận Class và đòi tăng lương. Mỗi Trainer 1 Class; học Class mới thì thay Class cũ.

|  | Medic | Commander | Engineer | Trapper |
|---|---|---|---|---|
| Háo chiến | xung đột | hợp | bình thường | bình thường |
| Nhát gan | hợp | xung đột | hợp | bình thường |
| Tham ăn | bình thường | bình thường | xung đột | hợp |
| Tư bản | bình thường | hợp | bình thường | hợp |

*   *Lưu ý:* Xung đột tính cách còn làm Stress tăng lâu dài và Trainer thường xuyên vào Bar.
