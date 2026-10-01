# Monster HUB Tycoon

Game **Management Simulation / Idle RPG / Dark-Capitalist Tycoon** với phong cách Chibi Pixel Art 2.5D (môi trường 3D low-poly, nhân vật là sprite 2D pixel art, camera isometric). Người chơi vào vai Giám đốc HUB giữa vùng hoang dã. Game chạy tự động: bạn không điều khiển nhân vật đánh quái mà điều khiển **dòng tiền**. Bạn chiêu mộ Trainer, cung cấp dịch vụ độc quyền và thu lại tài sản của họ qua chuỗi cung ứng, chứng khoán và các dịch vụ rủi ro cao.

> Trạng thái: giai đoạn thiết kế. Repo có tài liệu thiết kế và mô hình kinh tế sơ bộ bằng C# (`src/Game.Domain`, `tools/Game.Sim`), chưa có project Unity.

## Vòng lặp cốt lõi

1. **Thu thập:** Trainer (AI) tự ra ngoài đánh quái và nhặt nguyên liệu.
2. **Giao thương:** Trainer về HUB bán nguyên liệu lấy Gold. HUB chế tạo nguyên liệu thành hàng hóa và dịch vụ.
3. **Tiêu dùng:** Trainer mua dịch vụ sinh tồn (ăn, ngủ, y tế) và nâng cấp trang bị từ HUB.
4. **Chu kỳ bóc lột:** HUB trả lương mỗi 30 ngày in-game, rồi thu lại qua Sàn Chứng Khoán, lãi vay, phí sửa đồ và Quán Bar.

## Tech stack

- **Engine:** Unity 6.3 LTS trở lên, URP
- **Nền tảng:** Android (target API Level 36, định dạng AAB). PC/iOS là hướng mở rộng sau Early Access.
- **Kiến trúc:** Clean Architecture + DDD. Domain Layer viết bằng C# thuần, không phụ thuộc `UnityEngine`.
- **Khác:** VContainer (DI), MVP, FSM thuần C# cho AI, R3 (chỉ ở lớp Presentation), MessagePack, ScriptableObjects, Global Tick Manager cho AI.

Chi tiết xem [docs/00_Tech_Stack.md](docs/00_Tech_Stack.md).

## Mô phỏng kinh tế

```
dotnet run --project tools/Game.Sim                  # kịch bản FTUE, HUB đầy đủ, Cường hóa, Gacha
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll stress      # kịch bản chịu tải
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll calibrate   # dò tham số
```

```
dotnet test tests/Game.Domain.Tests                       # unit test Domain
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll crisis      # rủi ro Payday theo dự trữ và cú sốc
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll ladders     # chi phí kỳ vọng Nâng Sao, Tinh Luyện, Tiến hóa
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll upgrades    # chi phí nâng cấp công trình theo hoàn vốn
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll bar         # thị phần doanh thu Quán Bar
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll loans       # vay nợ: hạn mức x lãi suất
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll stock       # tần suất sự kiện cổ phiếu
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll personality # hành vi theo tính cách
dotnet tools/Game.Sim/bin/Debug/net8.0/Game.Sim.dll genebank    # phí và tịch thu Ngân Hàng Gene
```

Logic nằm ở `src/Game.Domain` (C# thuần, không phụ thuộc Unity). Xem [13_Balance_Parameters](docs/designs/13_Balance_Parameters.md).

## Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [00_Tech_Stack.md](docs/00_Tech_Stack.md) | Tech stack và kiến trúc kỹ thuật |
| [00_Master_GDD.md](docs/designs/00_Master_GDD.md) | Game Design Document tổng quan |
| [01_World_Map_Environment.md](docs/designs/01_World_Map_Environment.md) | Bản đồ thế giới và môi trường |
| [02_HUB_Economy_Infrastructure.md](docs/designs/02_HUB_Economy_Infrastructure.md) | Kinh tế và hạ tầng HUB |
| [03_Trainer_AI_System.md](docs/designs/03_Trainer_AI_System.md) | Hệ thống AI của Trainer |
| [04_Monster_System.md](docs/designs/04_Monster_System.md) | Hệ thống Monster |
| [05_Itemization_Gear_System.md](docs/designs/05_Itemization_Gear_System.md) | Vật phẩm và trang bị |
| [06_Events_PVE_PVP.md](docs/designs/06_Events_PVE_PVP.md) | Sự kiện, PvE, PvP |
| [07_Monetization_Model.md](docs/designs/07_Monetization_Model.md) | Mô hình kiếm tiền |
| [08_Quests_Achievements_Collections.md](docs/designs/08_Quests_Achievements_Collections.md) | Nhiệm vụ, thành tựu, bộ sưu tập |
| [09_Story_Lore.md](docs/designs/09_Story_Lore.md) | Cốt truyện và bối cảnh |
| [10_Development_Roadmap.md](docs/designs/10_Development_Roadmap.md) | Lộ trình phát triển |
| [11_Game_Assets.md](docs/designs/11_Game_Assets.md) | Tài nguyên và định hướng nghệ thuật |
| [12_Item_Catalog.md](docs/designs/12_Item_Catalog.md) | Danh mục vật phẩm |
| [13_Balance_Parameters.md](docs/designs/13_Balance_Parameters.md) | Bảng tham số cân bằng |
| [14_AI_Art_Pipeline.md](docs/designs/14_AI_Art_Pipeline.md) | Quy trình sản xuất art bằng AI |
| [99_Open_Issues.md](docs/99_Open_Issues.md) | Các vấn đề còn mở cần quyết định |
