# SUB-GDD 7: MONETIZATION & ECONOMY BALANCE (MÔ HÌNH KIẾM TIỀN)

## 1. HỆ THỐNG TIỀN TỆ (DUAL-CURRENCY)
*   **Gold (Tiền Tệ Mềm):** Tiền lưu thông nội bộ (trả lương, mua bán với AI). Kiếm miễn phí 100% qua vòng lặp kinh tế.
*   **Gem (Tiền Tệ Cứng):** Tiền nạp thực tế (IAP). Dùng để mua vật phẩm thao túng cấp Giám đốc (God-mode tools).

## 2. IN-APP PURCHASES & GACHA
Tích hợp Gacha tự nhiên vào bối cảnh Tuyển dụng nhân sự:
*   **Thư Mời Hoàng Gia (Premium Recruitment):** Dùng Gem mở Gacha để chiêu mộ Trainer hạng Epic -> Ultimate, kèm các Tính cách hiếm có lợi cho doanh nghiệp. *Có cơ chế Bảo hiểm (Pity).* Tỉ lệ rơi từng bậc Rarity và ngưỡng Pity phải được công bố trong game (xem [13_Balance_Parameters](13_Balance_Parameters.md)). Trainer nhận được sinh ngẫu nhiên từ các thành phần (giới tính, avatar, tính cách, chỉ số theo Rarity), không phải danh sách cố định.
*   **Bùa Bảo Hộ Cường Hóa (Protection Scrolls):** Vật phẩm bắt buộc phải có để đập đồ từ +11 -> +20 mà không bị vỡ. *Luồng:* Giám đốc mua Bùa bằng Gem (IAP) từ nhà cung cấp, rồi tự đặt giá bán lại cho AI bằng Gold. AI chỉ trả Gold, không bao giờ trả Gem; AI mua Bùa khi giá thấp hơn lợi ích kỳ vọng, hết Bùa thì liều đập. Ngoài Gem, Giám đốc nhận một ít Bùa miễn phí từ Quest chính, KPI tuần, Battle Pass miễn phí và Gem nhỏ giọt từ thành tựu, để người không nạp vẫn trải nghiệm được. Nguồn thu IAP ổn định nhất giai đoạn late-game.
*   **Công Cụ Can Thiệp (God-Tools):** 
    *   *Roi Kỷ Luật:* Xóa 100% thanh Stress và kết thúc Đình công của toàn bộ HUB ngay lập tức. **Giới hạn:** có cooldown (khởi điểm 3 ngày in-game) và không xóa nợ lương: tiền lương còn thiếu vẫn phải trả ở Payday kế tiếp.
    *   *Đồng Hồ Cát:* Tua nhanh 8h/24h (giờ thực, bằng chính mô phỏng như offline) để thu hoạch lợi nhuận tức thì. Xem [00_Master_GDD](00_Master_GDD.md) §5. **Giới hạn:** tối đa 1 lần dùng 24h mỗi ngày thực; phần tua tự dừng ngay trước Payday kế tiếp, để người chơi tự xử lý khi kho bạc không đủ trả lương; phần chưa tua được giữ lại và chạy tiếp sau Payday.

## 3. THẺ ĐĂNG KÝ & BATTLE PASS (RETENTION REVENUE)
*   **Thẻ Cổ Đông (Tycoon Club - Monthly Sub):** Giá $4.99/tháng.
    *   Đặc quyền: **Thư ký** tự chào hàng trang bị cho Trainer theo quy tắc Giám đốc đặt (người chơi thường phải tự chào hàng từng Trainer, xem [03](03_Trainer_AI_System.md) §1.1) và tự xử lý các việc lặp lại, kể cả trong mô phỏng offline; miễn phí 1 Gói cứu trợ vỡ nợ mỗi tháng, tắt quảng cáo, khung Avatar VIP. Pay-for-convenience, không tạo sức mạnh trực tiếp.
*   **Sổ Tay Thị Trưởng (Mayor's Ledger - Battle Pass):** Mùa giải 30 ngày.
    *   *Nhiệm vụ:* Hướng đến tương tác kinh tế (VD: Ép AI tiêu 1 triệu Gold, Thu 500k tiền thuế).
    *   *Phần thưởng Premium ($9.99):* Cung cấp các Bộ Trang Bị Hào Quang (Aura) độc quyền với ngoại hình đặc biệt và chỉ số thấp. Giám đốc không tự dùng được, chỉ bán cho AI bằng Gold qua cửa hàng HUB, và AI mua thì nhận buff thật. Nhờ vậy Aura không tạo sức mạnh trực tiếp cho người chơi.

## 4. QUẢNG CÁO TỰ NGUYỆN (REWARDED VIDEO ADS)
Loại bỏ quảng cáo Pop-up ép buộc, chuyển thành các "Khoản đầu tư thiên thần" hợp ngữ cảnh:
*   **Quỹ Cứu Trợ Tivi (Bailout TV):** Khi kho bạc HUB cạn kiệt sắp vỡ nợ, xem 1 QC 30s để nhận ngay một khoản tài trợ Gold khẩn cấp.
*   **Tiếp Tế Hàng Không (Sponsorship Airdrop):** Trong trận đánh World Boss, xem QC để mở rương tiếp tế, nhận Buff 50% DMG cho toàn bộ AI trong 10 phút.
*   **Đính Chính Báo Chí:** Khi chứng khoán sập, xem QC để phát hành bài báo giả, giúp giá cổ phiếu phục hồi ngay 20%.

## 5. THỜI TRANG CẢNH QUAN (REAL ESTATE COSMETICS)
Doanh thu từ nhóm người chơi "Cá Voi" thích trang trí (Cosmetics):
*   **Skin Công trình:** Đổi giao diện Tòa Thị Chính thành Tòa Nhà Cyberpunk, Lâu đài Gothic.
*   **Nội thất Dịch vụ:** Mua sắm Bàn ghế VIP, Sàn nhảy cho Quán Bar bằng Gem. Nội thất xịn giúp thanh Stress của AI tụt nhanh hơn khi chúng sử dụng dịch vụ, gián tiếp tối ưu hóa thời gian đi farm (Pay-for-convenience).

## 6. TUÂN THỦ CHÍNH SÁCH GOOGLE PLAY (CẦN KIỂM TRA LẠI KHI SUBMIT)
Kết quả tra cứu ngày 2026-10-01, chưa phải tư vấn pháp lý; chính sách có thể thay đổi:
*   **Gacha (Thư Mời Hoàng Gia):** Google Play (Payments policy) yêu cầu công bố tỉ lệ nhận vật phẩm ngẫu nhiên trước và gần thời điểm mua. Hiển thị tỉ lệ từng bậc Rarity và Pity ngay trên màn hình mua.
*   **Quảng cáo có thưởng:** phần thưởng ngẫu nhiên phải công bố tỉ lệ trước khi chiếu quảng cáo; không dùng phần thưởng là tiền thật quy đổi trực tiếp. Phần thưởng Bailout TV và Airdrop hiện đều là Gold/Buff trong game, phù hợp.
*   **Đăng ký (Tycoon Club):** Google đã siết yêu cầu về hủy đăng ký năm 2026; cần giao diện hủy rõ ràng và mô tả đặc quyền, giá, chu kỳ trước khi mua.
*   **Nội dung AI:** xem [14_AI_Art_Pipeline](14_AI_Art_Pipeline.md) §5.

Nguồn: [Payments - Play Console Help](https://support.google.com/googleplay/android-developer/answer/9858738), [Policies for ad units that offer rewards - AdMob Help](https://support.google.com/admob/answer/7313578), [Google Play now requires disclosure of loot box odds - Fenwick](https://www.fenwick.com/insights/publications/google-play-now-requires-disclosure-of-loot-box-odds).
