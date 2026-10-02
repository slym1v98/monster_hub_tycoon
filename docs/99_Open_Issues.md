# CÁC VẤN ĐỀ CÒN MỞ (OPEN ISSUES)

Rà soát toàn bộ tài liệu ngày 2026-10-01. Khi chốt một mục, sửa tài liệu gốc rồi xóa mục đó khỏi file này.

## Đã chốt (2026-10-01)

| Mục | Quyết định | Đã sửa tại |
|---|---|---|
| Đồ họa | 2.5D Isometric: môi trường 3D, nhân vật sprite 2D. | README, 00_Master_GDD, 10_Roadmap |
| Nền tảng | Android trước; PC/iOS sau Early Access. | README, 00_Master_GDD |
| PvP/Backend | PvP ngoài Early Access; game Offline; backend chỉ Remote Config + Analytics. | 00_Tech_Stack §6, 06 §3, 10_Roadmap |
| Bùa Bảo Hộ | Giám đốc mua bằng Gem (IAP), bán lại cho AI bằng Gold. | 07, 04, 08, 12 |
| Phân cấp | Rarity: Common < Rare < Epic < Legendary < Ultimate (cả Trainer và Monster). Rank I-V = 1 + số Rebirth. Tinh Luyện: Normal -> Mythic. | 03, 04, 01, 02, 05 |
| Kiến trúc | Domain: FSM và sự kiện thuần C#. Stack: Unity 6.3 LTS, URP, VContainer, MVP, MessagePack, R3, UGUI. Target API 36. | 00_Tech_Stack, 10_Roadmap, README |
| Tên Zone | Đồng Cỏ, Núi Lửa, Hầm Băng, Đầm Lầy, Vực Thẳm. Nightmare/The Void dùng lại tile Zone 5. | 01, 11 |
| Tính cách | 4 tính cách: Háo chiến, Nhát gan, Tham ăn, Tư bản ("Tham lam" đổi thành Tư bản). Có bảng hành vi. | 03, 02 |
| Công trình | Bảng 19 công trình, tối đa Lvl 3; Phòng Thí Nghiệm Tiến Hóa là tên chuẩn; 18 công trình có 4 trạng thái (16 trong Early Access). | 02, 11, 10 |
| Trang bị | 30 slot (12 Trainer + 3 Monster x 6). Slot "Bùa" đổi thành "Trang Sức". | 05, 11 |
| Vay nợ | Tách "Vay Từ Trainer" và "Cho Trainer Vay". | 02 |
| Class | Mục đổi tên "Đào tạo & Nghề nghiệp", Học Viện thêm vào tech tree. | 03, 02 |
| Thuế | Phân loại thuế; ngưỡng phạt Thanh Tra 30% (mặc định FTUE 20%). | 02, 06 |
| Lore/FTUE | HUB giữa vùng hoang dã; Intro 15 giây chạy trước FTUE. | 09, 00_GDD |
| Aura | Chỉ số thấp, chỉ Giám đốc bán cho AI, không tự dùng. | 07 |
| Animation | Unity 2D Animation, không dùng Spine. | 11 |
| Thời gian | 1 ngày = 15 phút thực (Payday mỗi 7.5 giờ thực). Offline: mô phỏng đầy đủ tới Payday gần nhất rồi dừng. Đồng Hồ Cát tính giờ thực, dừng trước Payday. | 00_GDD §5, 13 |
| Giám đốc | Bảng đòn bẩy của Giám đốc. | 00_GDD §6 |
| Thanh trạng thái | 5 thanh (HP, Thể lực, No nê, Nước, Stress); chỉ số "Thể lực" đổi thành "Sức bền". | 03 |
| Hệ nguyên tố | 8 hệ; "hệ Tank/DPS/Support" đổi thành "vai trò". | 04 |
| Rebellion | Điểm Quản Lý = Cấp độ + (bậc Rarity x K). | 04 |
| Vật phẩm | Danh mục tiêu hao và nguyên liệu. | 12 |
| Gacha | Trainer sinh ngẫu nhiên từ thành phần; tỉ lệ và Pity phải công bố. | 07 |
| Ngân Hàng Gene | Chuyển sang nhóm Sink, thêm luật phí và tịch thu. | 02 |
| Phạm vi EA | Chia Early Access / sau Early Access. | 10 |
| Roadmap | Thêm prototype hiệu năng, sửa phần mô phỏng cân bằng dùng Domain C#, thêm rủi ro. | 10 |
| Khắc chế nguyên tố | 9 hệ (thêm Đất), bảng 2 / 0.5 / 1 theo logic Pokémon, không có miễn nhiễm. | 04, 13 |
| Công cụ trả phí | Roi Kỷ Luật có cooldown, không xóa nợ lương; Đồng Hồ Cát 1 lần/ngày, dừng trước Payday. | 07, 13 |
| Chính sách Google Play | Công bố tỉ lệ Gacha và phần thưởng ngẫu nhiên, yêu cầu hủy đăng ký; ghi vào mục tuân thủ. | 07 §6 |
| Sản xuất art | Dùng công cụ AI (Gemini image, Meowa, ...) kèm chỉnh tay; kế hoạch, khối lượng, art spike, pháp lý. | 14, 10 |
| Mô phỏng kinh tế sơ bộ | Dựng `src/Game.Domain` + `tools/Game.Sim`; giá trị khởi điểm cho lương, giá dịch vụ, Cường hóa, Gacha, Pity. | 13, README |
| Rủi ro Payday | Khủng hoảng đến từ tái đầu tư quá tay và cú sốc (ngưỡng dự trữ khoảng 1x quỹ lương); UI hiển thị dự báo Payday; mô phỏng thêm chính sách dự trữ và cú sốc, 15 unit test. | 02 §2.0, 13 §10, tests |
| Nâng cấp công trình & thang nâng cấp | Công thức chi phí = lợi ích/tháng x tháng hoàn vốn (2 và 4 tháng); Tòa Thị Chính 10/20/30 Trainer; vận hành 50 Gold/công trình/ngày; giá trị khởi điểm cho Nâng Sao, Tinh Luyện, Tiến hóa; `UpgradeLadder` + test. | 02 §1.3, 13 §4 và §11 |
| Hiệu ứng nâng cấp công trình dịch vụ | Công trình dịch vụ: sức chứa 10/20/30 Trainer/ngày; công trình mở khóa: tính năng theo cấp; chi phí mở khóa 1 và 2 tháng doanh thu dịch vụ; doanh thu dịch vụ đo được theo loại. | 02 §1.3, 13 §11 |
| Quán Bar | Bar chiếm khoảng 10% doanh thu dịch vụ: Stress +25/ngày, giá Bar 800/lần, Trainer vào Bar trước khi mua trang bị. Bar bị giới hạn bởi tiền mặt của Trainer, không phải giá. | 13 §8, 02 §1.3 |
| Vay nợ và cổ phiếu | Mô hình vay (hạn mức, lãi, quá hạn) và `StockMarket`; mặc định hạn mức 2 lần lương, lãi 10%/Payday; biến động 3%/ngày; 27 test. | 13 §12 |
| Tính cách, Ngân Hàng Gene, Rebellion, tài chính | Hệ số tính cách khởi điểm và kết quả; Ngân Hàng Gene thu phí mỗi Payday (20 Gold/Monster/ngày); một bậc Rarity = 20 cấp độ (K=20); cổ tức 10% và thuế tài chính 30%; điều kiện "Chủ Nợ Máu Lạnh" theo số dư ròng -2 lương; 39 test. | 03, 04, 02 §1.2, 08, 13 §13 |
| Kỹ thuật khác | Sửa câu O(1)/SQLite; Mac thành khuyến nghị; thêm cấu trúc Assembly Definition. | 00_Tech_Stack |
| Brainstorm mô phỏng Domain | Sự kiện rời rạc theo phút in-game, một mô hình cho online/offline/Sim; faucet = Gold quái rơi + Thương nhân + Tổng tài, HUB không bán ra ngoài; Thương nhân trung gian; lương hợp đồng; không game over, thang vỡ nợ và cấn nợ; dây chuyền Nhà máy -> 3 xưởng theo nhóm slot + Lò Phản Ứng; quầy tự chế tiêu hao; Tòa Thị Chính 25 cấp, Ký túc xá quyết định dân số theo Zone; công trình 5/25 cấp, model tier + Tàn phá; sức chứa = chỗ cùng lúc; ở lỳ khi thiếu dịch vụ; giá hợp lý + Stress; Danh tiếng; Bệnh Viện 2 khu; Trainer 4 thanh, chỉ Monster có HP; cấp Monster theo Rank/cấp Trainer; tăng tư chất tách khỏi Tiến hóa; Rebellion dùng cấp quy đổi; chỉ Giám đốc chào hàng trang bị (Tycoon Club có Thư ký); 30 loại nguyên liệu; World Boss vào EA. | 00, 01, 02, 03, 04, 05, 07, 08, 10, 11, 12, 13, 14 |

**Các giá trị tôi tự đặt, cần bạn xác nhận hoặc chỉnh:**
- Tên 5 bậc Rarity; Rank = 1 + số Rebirth; tên Mythic.
- Ngưỡng phạt Thanh Tra 30%.
- Vị trí chức năng trong bảng công trình (ví dụ Giám định IVs ở Phòng Thí Nghiệm Tiến Hóa, Bẫy/Bóng ở Xưởng Công Cụ, Thuốc ở Bệnh Viện).
- Thêm hệ Đất (9 hệ) và hệ số khắc chế khởi điểm 2 / 0.5 / 1.
- Cooldown Roi Kỷ Luật 3 ngày in-game và quy tắc "tua dừng trước Payday".
- Bảng "Thanh trạng thái" và vật phẩm trong `12_Item_Catalog` là mô tả khởi điểm.
- (Brainstorm) Trainer không bỏ đi vì nợ lương; Tổng tài donate là lối thoát hay gặp nhất khi Trainer hết tiền; chỉ số kiếm Gold là May mắn; Balo tính theo tổng số đơn vị.
- (Brainstorm) Công trình 5 cấp: cấp N cần Tòa Thị Chính tier N; bảng mở khóa theo cấp Tòa Thị Chính ở 02 §1.1; mỗi Zone mở 5 cấp Tòa Thị Chính.
- (Brainstorm) Sức chứa = 5 + 0.8 x cấp; ngưỡng về HUB theo tính cách (15/50/30/25%); hệ số IV; bảng Tính cách x Class (90/65/30%).

---

## Còn mở (làm được trong repo, chưa làm)

| # | Mức | Vấn đề | Việc tiếp theo |
|---|---|---|---|
| O6 | P1 | Số liệu nâng cấp và chi phí mở khóa (02 §1.7, 13 §11) tính trên mô hình cũ 3 cấp. Domain supply chain đã có đường sản xuất 5 cấp, nhưng hiệu suất 5 mức và thời lượng/chi phí job hiện là prototype, chưa được cân bằng cùng quy mô công trình/dân số hoàn chỉnh. | Chạy lại khi cấp công trình, dân số, công suất xưởng và hành vi mua hàng đã được mô phỏng; cập nhật workbook rồi đồng bộ GDD. |
| O7 | P1 | Lõi mới (sub-project 1) đã giải quyết: lương hợp đồng, chi phí vận hành, Stress theo giá, số công trình. Hạn mức vay 2 lương và Gene Bank thu theo Payday (sub-project 5), `RebellionModel` dùng cấp quy đổi Trainer (sub-project 3 — đã hoàn tất, RebellionCombatConfig dùng cấp quy đổi). | Sửa khi làm sub-project 5. |
| O8 | P2 | Thanh Tra thuộc sau Early Access (10) nhưng Stress đỏ, thuế > 30% và Danh tiếng đều tham chiếu tới nó. | Quyết định: đưa Thanh Tra rút gọn vào EA, hoặc thay hậu quả trong EA bằng Danh tiếng. |
| O9 | P2 | Tiệm Tạp hóa và Xưởng Công Cụ cùng là cửa hàng tiêu hao; có thể gộp để bớt model. | Quyết định khi khóa danh mục asset (trước Roadmap Bước 3). |
| O10 | P2 | Tiến hóa (đổi hình thái): tỉ lệ, chi phí, mốc cấp chưa có số GDD. Workbook đã ghi các tham số EvolutionCatalog (success_chance, min_level, gene_fragments) với trạng thái Prototype. | Cân bằng khi đủ dữ liệu GDD/asset. |
| O11 | P1 | Mô phỏng supply chain mới chỉ có FarmResult gán tạm loot thành Quặng tier 1; chưa có loot theo Zone/encounter và Trainer chưa tự mua hàng từ quầy. Stress do thuế giao dịch vượt 30% và chờ Merchant chưa nối vào FSM. Đường cong thời lượng Producer hiện là giả định prototype, chưa cân bằng. Domain Monster/combat/loot đã có cấu trúc (Monster Catalog, CombatConfig, CaptureResolver, ZoneCatalog, LootResolver, DefaultExpeditionResolver) nhưng loot chưa hoàn thiện. | Chốt loot khi làm farm/chiến đấu tiếp; cân bằng đường cong thời lượng theo cấp Producer; ở phần AI/Stress, nối lựa chọn mua, hiệu ứng tiêu hao và Stress do thuế/chờ; chạy lại `Game.Sim market` với ma trận nguyên liệu và quầy thật. |

## Hoãn (cần việc ngoài repo; đã chuẩn bị sẵn tài liệu)

| # | Mức | Vì sao chưa làm được | Đã chuẩn bị | Việc tiếp theo |
|---|---|---|---|---|
| O3 | P1 | Cần chạy thử công cụ AI tạo ảnh (Gemini, Meowa, ...), tôi không chạy được. | [art_spike_brief](reports/art_spike_brief.md): style bible nháp, danh mục asset, mẫu prompt, phiếu chấm điểm. | Chạy art spike 1 tuần, điền phiếu chấm điểm, quyết định công cụ và mức chỉnh tay, rồi chốt tiến độ art. Làm cuối Bước 2 Roadmap. |
| O4 | P1 | Cần Unity 6.3 LTS và thiết bị Android thật; repo chưa có project Unity. | [perf_prototype_plan](reports/perf_prototype_plan.md): cảnh thử 30 Trainer + 90 Monster, 5 biến thể, số cần đo, tiêu chí đạt. | Dựng project Unity (Roadmap Bước 1), chạy theo kế hoạch trên 2 thiết bị. Làm ở Bước 2. |
| O5 | P2 | Cần xác nhận điều khoản bằng văn bản từ công cụ và đọc chính sách Google Play vào thời điểm submit. | [legal_checklist](reports/legal_checklist.md), kèm mẫu email gửi Meowa. | Gửi email, lưu điều khoản; kiểm tra lại danh sách trước khi submit (Roadmap Bước 6). |
