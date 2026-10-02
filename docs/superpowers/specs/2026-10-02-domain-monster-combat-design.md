# Sub-project 3: Monster, chiến đấu và hành vi farm

Ngày: 2026-10-02  
Phạm vi kế thừa: `Game.Domain` sau sub-project 1 (lõi thời gian/Trainer) và sub-project 2 (chuỗi cung ứng prototype).  
Nguồn quyết định: [03_Trainer_AI_System.md](../../designs/03_Trainer_AI_System.md), [04_Monster_System.md](../../designs/04_Monster_System.md), [01_World_Map_Environment.md](../../designs/01_World_Map_Environment.md), [02_HUB_Economy_Infrastructure.md](../../designs/02_HUB_Economy_Infrastructure.md), [12_Item_Catalog.md](../../designs/12_Item_Catalog.md), [13_Balance_Parameters.md](../../designs/13_Balance_Parameters.md).

Quy ước code: tên type, hàm, tham số và giả mã bằng tiếng Anh; chú thích/comment bằng tiếng Việt có dấu. Domain thuần C#, tất định theo seed, không phụ thuộc Unity.

## 1. Mục tiêu

Thay farm resolver và HP Monster gộp tạm bằng một vòng farm có đội Monster riêng, encounter được giải trọn trận, kết quả có thể phát lại và loot có danh tính nguyên liệu theo Zone. Mô hình dùng chung cho Game.Sim và HubWorld. Số chưa được GDD xác nhận phải mang trạng thái `Prototype` hoặc `TBD` trong workbook; không gọi là đã cân bằng.

Kết quả cần đạt:

- Mỗi Trainer có tối đa ba Monster: một Active và tối đa hai Reserve; Monster có gen, hệ, vai trò, Rarity, IV, cấp và HP riêng.
- Cấp Monster suy ra từ Rank/cấp Trainer; Rebirth không làm Monster tụt cấp; Monster mới nhận cấp tương ứng với Trainer.
- Encounter xử lý skill, cooldown, hệ tương khắc, tự động Swap, ngất và Rebellion; trả về log sự kiện có thứ tự cùng kết quả tất định.
- Trainer chọn Zone hợp lệ theo Rank/thu nhập kỳ vọng, chịu chi phí di chuyển và farm; vật liệu rơi theo catalog/Zone và được đưa vào supply chain hiện có.
- Capture cần Bóng, xét Bẫy/Class/Khéo léo/Rarity/HP mục tiêu; Monster bắt được đi qua hồi phục Thú Y trước khi được đưa vào roster.
- Kết quả và mọi số liệu runtime liên quan có thể quan sát trong Game.Sim và ánh xạ tới workbook.

## 2. Phạm vi và ranh giới

### Trong phạm vi

1. **Monster, roster và cấp độ:** dữ liệu phẳng, catalog cấu hình loài/skill, sinh gen có seed, quy đổi cấp Trainer và Monster, HP hiện tại/tối đa, Active/Reserve, ngất, mặc định một Monster Soul-bound khi Trainer được tạo.
2. **Combat resolver:** encounter PvE tại Zone, chọn skill và mục tiêu, hồi chiêu, sát thương/crit, khắc hệ chín nguyên tố, Rebellion và Swap khi HP dưới 15%; kết quả bao gồm nhật ký replay, HP còn lại, ngất, loot/EXP và kết quả trận.
3. **Zone/farm:** năm Zone với Rank tối thiểu, thời gian di chuyển và hồ sơ encounter/drop theo dữ liệu; lựa chọn Zone theo thu nhập kỳ vọng mỗi giờ, cộng thiên vị Truy Nã cho tính cách Tư bản khi có lệnh; trừ nhu cầu lúc đi/farm; Gold, EXP và nguyên liệu typed; balo tính theo tổng đơn vị; quy tắc farm ban đêm/kính nhìn đêm.
4. **Bắt thú và hồi phục:** tiêu hao Bóng/Bẫy từ quầy hiện có, chọn mục tiêu đáng thay, thử bắt, thêm Monster vào khu hồi phục Thú Y có 100 giường, thanh toán giá và thời gian hồi phục, sau đó cho phép đổi roster tại HUB. Khi hết giường, Trainer chờ hoặc gửi Monster vào Ngân Hàng Gene theo luật GDD.
5. **Vòng đời Monster và Ngân Hàng Gene:** inventory/roster/storage; gửi và rút Monster; giám định IV; phân giải Monster lấy Gene Fragments; tăng Rarity từ Lv40 theo tỉ lệ GDD; tiến hóa tối đa hai lần với nhánh loài/role/skill; resale Monster bị tịch thu. Phí Payday và kết quả tịch thu có contract Domain ở đây, còn thu phí/tịch thu thực tế nối với payroll ở sub-project 5.
6. **Rebellion trong trận:** điểm Quản Lý và Lãnh đạo theo GDD với cấp quy đổi; khả năng bỏ lượt/ngủ gật/tấn công diện rộng được điều khiển bởi các tham số prototype và seed; Bánh thưởng/Khóa Giao Tiếp tác động theo chỉ số.
7. **Tích hợp hành vi cần thiết:** inventory tối thiểu cho vật phẩm Monster (Potion, Bánh thưởng, Bóng/Bẫy, Sách Chiến Thuật, Bình nước buff, vật phẩm tăng Rarity, vật liệu Tiến hóa, Bùa Bảo Hộ) và mua/sử dụng tại HUB khi cần, theo tồn kho và giá supply chain; quyết định mua không tạo hàng hoặc Gold.
8. **Tính tất định, sự kiện Domain, Game.Sim, tests và workbook.**

### Ngoài phạm vi

- Trang bị, 30 slot, Gear Score, độ bền và hao mòn; combat chỉ phát ra dữ kiện để sub-project 4 nối durability sau này.
- Bảng skill đầy đủ cho toàn bộ loài và nội dung art/sprite; Domain dùng catalog data-driven đủ để mô phỏng các loài/nhánh được khai báo.
- Weather/event-driven encounter, Nightmare, The Void, Bounty Board payouts/AI herd, World Boss và hậu quả hư hại công trình; sub-project 6 sẽ nối các nguồn modifier này.
- Rebirth command và cấp Tòa Thị Chính: Rank/Lv hiện tại là đầu vào; transition Rebirth thuộc progression sub-project 6.
- PvP, Arena, Museum và các mục sau Early Access.
- Cân bằng cuối. Công thức/giá trị thiếu trong GDD được tách thành cấu hình Prototype/TBD.

## 3. Hành vi và mô hình dữ liệu

### 3.1 Monster và đội hình

Monster giữ ID ổn định, species ID, element, role, rarity, IV, base/growth stats (HP, ATK, DEF, ASPD, CRIT), level, current HP, skill loadout/cooldowns, status và trạng thái recovery. Gen được tạo một lần từ `SimRandom`; không sinh lại khi truy vấn.

Trainer bắt đầu với một Monster Soul-bound cùng Rarity. Roster có tối đa ba thành viên, một Active. Reserve có thể cấp Bag Synergy theo role khi có Sách Chiến Thuật. Mọi quyết định tie-break theo ID ổn định.

Theo GDD: `MonsterLevel = 20 * (Rank - 1) + ceil(TrainerLevel / 5)`. Cấp quy đổi dùng cho Rebellion là cùng thang cấp: `20 * (Rank - 1) + TrainerLevel / 5`; cách làm tròn số nguyên phải được ghi rõ trong implementation và workbook. Cần test biên Trainer Lv 1, 5, 6, 100 ở mọi Rank. Monster hiện có không bị giảm cấp khi Rank đổi; Monster mới dùng cấp Trainer hiện tại.

Base stat tables, rarity scaling, IV generation distribution, skill sets, role synergy magnitudes, and Trainer attribute generation are not fully specified by GDD. Provide small data-driven defaults with `Prototype` status. Keep formula inputs explicit, not embedded in species-specific branches.

### 3.2 Encounter và replay

Encounter resolver là hàm Domain thuần trên trạng thái đội hình, đối thủ, cấu hình và seed stream. Nó trả về `BattleResult` cùng danh sách action/event theo thứ tự (thời điểm/turn, actor, skill/action, target, damage/heal, effectiveness, HP sau hành động, Swap/faint/Rebellion flags). Unity có thể dựng lại animation từ log; mô phỏng offline chỉ cần áp dụng trạng thái cuối.

Damage/crit, attack cadence, cooldown clock, target selection, tie-break, number of opponents and encounter frequency được cấu hình. GDD khóa bảng 9 hệ và các quan hệ 2x/0.5x/1x, một Active + hai Reserve, Swap ở dưới 15% HP, không chết khi HP về 0, cả đội ngất thì Trainer về HUB. GDD không khóa công thức damage, crit, cooldown, skill priority hoặc encounter rate; các giá trị khởi đầu mang `Prototype`.

Một encounter tiêu hao durability chỉ được biểu diễn thành event/result hook nếu slot gear tồn tại; sub-project 4 sẽ nối thực thể trang bị và áp hao mòn. Không đưa durability giả vào trạng thái Monster.

### 3.3 Zone, lựa chọn và loot

Zone catalog có stable ID, required Rank, walk minutes, encounter profile, material family/tier weights, Gold/EXP profile và điều kiện thời gian. Zone N ưu tiên vật liệu tier N và cho phép spillover tier thấp theo bảng dữ liệu. Trainer xét các Zone đã mở và đủ Rank, chọn thu nhập kỳ vọng mỗi giờ cao nhất; tính cách Tư bản có multiplier/bonus riêng cho bounty khi hệ bounty được nối.

Di chuyển trừ Stamina/Satiety/Hydration theo thời gian và hệ số hiện có. Farm ban đêm áp loot/EXP x2 nếu có Night Vision; không có thì hành vi về HUB lúc Dusk giữ nguyên. Trainer nhặt Gold theo Luck và vật liệu theo MaterialPickRate; typed loot được chuyển vào balo/supply chain hiện hữu, không tự tạo hoặc xóa hàng. Loot vượt sức chứa được xử lý theo một luật rõ ràng và có event observable.

GDD không cho số walk time theo từng Zone, encounter/drop rates, Luck formula, EXP curve, giá trị vật liệu theo Zone, tie-break thu nhập hoặc xử lý loot dư. Đây là tham số Prototype/TBD trong workbook.

### 3.4 Capture và Veterinary Hospital

Trainer chỉ thử bắt khi có Bóng; Bẫy và Class Trapper tác động xác suất theo config. AI chọn mục tiêu có giá trị cao hơn hoặc mạnh hơn Monster yếu nhất; việc so sánh được triển khai bằng một hàm `ReplacementScore` có test và dữ liệu rõ ràng. Nếu bắt thành công, Monster không lập tức tham chiến: nó vào Veterinary Hospital recovery, trả phí và thời gian hồi phục, rồi mới có thể thay thành viên roster tại HUB. Thất bại tiêu hao vật phẩm theo cấu hình và ghi event.

GDD chỉ định các yếu tố trong xác suất bắt và recovery tại Thú Y, nhưng không chốt công thức, HP threshold “quái yếu”, có tiêu hao Bóng/Bẫy khi thất bại hay không, phí/thời gian, hoặc tiêu chí thay đội. Các luật này là Prototype/TBD. Không được cho vật phẩm âm thầm xuất hiện; purchase/use phải qua stock và ledger của sub-project 2.

Veterinary Hospital có khu cấp cứu cho Monster thuộc đội và khu hồi phục 100 giường dành cho Monster mới bắt. Monster mới bắt hồi phục lần đầu lâu hơn nhiều lần; Monster ngất chịu thêm phí và thời gian so với chữa thường. Nếu khu hồi phục đầy, Trainer chờ phục vụ hoặc gửi Monster dư vào Gene Bank; Monster bắt được có Rarity đủ cao được ưu tiên ở lại Bệnh Viện. Luật tie-break/ưu tiên còn thiếu được cấu hình và ghi là Prototype.

### 3.5 Giám định, phân giải, tăng Rarity và tiến hóa

- IV được tạo cố định khi sinh Monster nhưng bị ẩn cho đến khi Trainer trả phí giám định ở Phòng Thí Nghiệm Tiến Hóa. Monster IV D/C có thể phân giải thành Gene Fragments; Fragments được bán cho Trạm hoặc làm một phần nguyên liệu thay thế cho tiến hóa/tăng Rarity.
- Tăng Rarity giữ loài, cấp, IV và vai trò; yêu cầu cấp Monster ≥40. Áp tỉ lệ khởi điểm 60/40/25/10% theo bậc trong GDD/13. Thất bại tiêu hao vật phẩm/nguyên liệu, trừ khi có Bùa Bảo Hộ.
- Tiến hóa tối đa hai lần, phụ thuộc loài và có thể mở nhánh lựa chọn role/skill. Thành công giữ Rarity, level, IV và gear references; cập nhật form/species branch, role và skill loadout. Đá Tiến Hóa + Lõi Đột Biến là input; Gene Fragments có thể thay một phần. Bùa Bảo Hộ giữ vật liệu theo luật catalog.
- Mức Gene Fragments từ phân giải, giá giám định/phân giải, vật liệu/cost, xác suất tiến hóa, level gate và loài có nhánh đều chưa khóa đầy đủ; mọi số còn thiếu là Prototype/TBD trong workbook. Không cho IV hay vật liệu đổi ngầm.

Ngân Hàng Gene lưu Monster dư và thu phí theo Payday sau khi trả lương. GDD khởi điểm là 20 Gold/Monster/ngày nhân 30 ngày; thiếu phí thì tịch thu một Monster. Bank sở hữu Monster tịch thu và HUB có thể resale cho Trainer khác hoặc phân giải. Sức chứa Bank và chọn Monster nào bị tịch thu chưa có quy tắc, nên phải cấu hình Prototype. Sub-project 3 định nghĩa ownership, storage, fee quote, confiscation result và sự kiện; sub-project 5 phải gọi các contract này đúng thứ tự payroll/Payday để hoàn thành tích hợp tài chính.

## 4. Kiến trúc và tích hợp

- Thay `FarmResult`/`SimpleFarmResolver` bằng pipeline có các trách nhiệm tách biệt: `ZoneCatalog`/ZoneSelector, `EncounterGenerator`, `BattleResolver`, `LootResolver`, và `CaptureResolver`. Các interfaces phải cho phép `HubWorld` tiếp tục là entry point.
- Thay `Trainer.TeamHp`/`TeamHpMax` bằng roster Monster; duy trì view aggregate chỉ khi consumer hiện tại cần chuyển tiếp, và xóa nó khi không còn tham chiếu.
- Mở rộng event queue cho arrival/encounter/combat/capture/vet completion sao cho mỗi Trainer có tối đa một event cá nhân hợp lệ; stale event token bị bỏ qua như pattern hiện tại.
- Tích hợp typed loot với material inventory và giao dịch/market trong `HubWorld.Supply`; mọi giao dịch giữ nguyên quy tắc thuế/ledger.
- Tích hợp vật phẩm chiến đấu tối thiểu qua supply inventory/stall, không hard-code số lượng stock trong resolver.
- `Game.Sim` có scenario(s) riêng cho combat/team/capture và scenario tổng hợp; in encounter, win/loss, swaps, faint/recovery, capture, zone, loot, EXP, Monster HP, vật phẩm mua/dùng, và reconcile tiền/hàng.
- Mọi xác suất và số balance có stable ID, đơn vị, status và nguồn trong `MonsterHUB_Balance.xlsx`, với runtime defaults trong config/catalog Domain tương ứng.

## 5. Các quyết định prototype được phép trong implementation

Các mục sau không cần chờ khóa cân bằng cuối, nhưng phải được ghi cả ở spec/README/report và workbook với status `Prototype` hoặc `TBD`:

- Bộ loài/skill nhỏ đủ đại diện 5 Zone và 9 hệ; distribution của IV/Rarity/gen.
- Công thức stat growth, damage/crit, cooldown/action order, encounter rate và opponent strength.
- Trainer Dexterity/Luck/Leadership/Endurance defaults và cách chúng tác động vào kết quả.
- EXP cần lên cấp, mức income kỳ vọng dùng để chọn Zone, drop weights, Gold/Luck formula.
- Capture odds, HP threshold, item consumption on failure, replacement score, vet price/time.
- IV appraisal/salvage yields, evolution branch catalog, evolution odds/cost, Gene Bank storage/confiscation choice where not specified.
- Potion, Bánh thưởng, Bóng/Bẫy, Sách Chiến Thuật, buff bottle, rarity items, evolution materials và Bùa Bảo Hộ: nhu cầu mua, giới hạn tồn, giá/hiệu ứng nếu GDD chưa định lượng.
- Phần chưa thể mô phỏng do chưa có project Unity hoặc dữ liệu art không thuộc tiêu chí Domain.

Không được thay đổi các quyết định GDD đã khóa: hệ/khắc hệ, cấp quy đổi, roster tối đa ba, Swap threshold, quy tắc ngất, Zone Rank gate, night-vision loot/EXP x2, capture cần Bóng, Trainer không có HP.

## 6. Tiêu chí chấp nhận

1. Một Monster được sinh từ cùng seed/inputs cho cùng gen; roster giữ invariant tối đa ba và đúng một Active khi không rỗng.
2. Unit tests bao phủ công thức level qua biên Rank/Trainer level, stat derivation, bảng 9x9 hệ, sát thương/crit boundary, cooldown/action ordering, Swap <15%, faint (không chết), cả đội ngất, và Rebellion dùng cấp quy đổi.
3. Combat resolver là tất định: cùng seed, đội hình, Zone và config cho cùng BattleResult, event log và state cuối; log đủ dữ liệu replay.
4. Zone selector loại đúng Zone khóa/Rank cao, chọn thu nhập kỳ vọng tối đa theo tie-break ổn định; travel tiêu nhu cầu; Night Vision áp x2 loot/EXP.
5. Loot có stable material IDs, tuân thủ tỷ lệ/pickup/balo, được ghi vào supply chain hiện có; không tạo Gold/hàng ngoài nguồn đã định nghĩa.
6. Capture yêu cầu Bóng và tồn kho; xác suất nhận đúng các đầu vào; successful capture đi qua Veterinary recovery; roster chỉ đổi khi Monster sẵn sàng và người chơi/AI thực hiện thay.
7. Tương tác vật phẩm chiến đấu kiểm tra tồn kho, trừ tiền và hàng qua market ledger; không có số dư âm hoặc tiêu thụ không có nguồn.
8. `Game.Sim` xuất đủ chỉ số để kiểm tra trận, farm, hồi phục và dòng hàng/Gold; runtime dưới ngưỡng đã định bởi performance prototype hoặc ghi rõ nếu vượt.
9. Workbook có mọi tham số runtime liên quan với ID, đơn vị, Prototype/TBD/Locked và nguồn GDD/code; README và open issues phản ánh đúng trạng thái.
10. Vòng đời Monster chạy được qua bắt → hồi phục → roster/storage → giám định → phân giải hoặc tăng Rarity/tiến hóa; Gene Fragments và ownership/fee/confiscation có ledger/event rõ, không nhân đôi Monster hoặc vật liệu.
11. Bộ test/build đầy đủ của repository xanh; không còn đường chạy mặc định dựa vào HP gộp hoặc loot Quặng tier 1 giả.

## 7. Câu hỏi cần giữ mở

Các câu này không ngăn triển khai prototype, nhưng cần câu trả lời trước cân bằng cuối:

- GDD dùng cả “Trainer cấp quy đổi” với phép chia 5 và Monster level với `ceil`; điểm Rebellion có giữ phân số hay làm tròn xuống?
- “Thu nhập kỳ vọng/giờ” gồm những thành phần và chi phí nào: loot bán được, giá bounty, travel, nhu cầu, hồi phục, vật phẩm và nguy cơ ngất?
- Trainer có ba Monster ngay từ đầu hay nhận thêm theo tiến trình? GDD hiện nói Monster mặc định một con và roster tối đa ba.
- Monster bắt được thay thế ai khi roster đầy; ai ra quyết định thay và có thể giữ Monster mới trong kho không?
- Skill/encounter sử dụng bao nhiêu hành động/turn và hồi chiêu đo theo turn hay phút in-game?
- World Boss có trong EA nhưng nằm trong event/progression scope; nó sẽ dùng cùng resolver hay cần raid resolver riêng?

Các câu trả lời không được GDD khóa có thể tiếp tục dùng Prototype sau khi người dùng duyệt spec; các lựa chọn tác động lớn tới gameplay cần ghi lại trong workbook và open issues.
