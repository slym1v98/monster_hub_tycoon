# Monster HUB Tycoon

Game **Management Simulation / Idle RPG / Dark-Capitalist Tycoon** với phong cách Chibi Pixel Art. Người chơi vào vai Giám đốc HUB giữa vùng hoang dã. Game chạy tự động: bạn không điều khiển nhân vật đánh quái mà điều khiển **dòng tiền**. Bạn chiêu mộ Trainer, cung cấp dịch vụ độc quyền và thu lại tài sản của họ qua chuỗi cung ứng, chứng khoán và các dịch vụ rủi ro cao.

> Trạng thái: giai đoạn thiết kế. Repo hiện chỉ có tài liệu, chưa có code.

## Vòng lặp cốt lõi

1. **Thu thập:** Trainer (AI) tự ra ngoài đánh quái và nhặt nguyên liệu.
2. **Giao thương:** Trainer về HUB bán nguyên liệu lấy Gold. HUB chế tạo nguyên liệu thành hàng hóa và dịch vụ.
3. **Tiêu dùng:** Trainer mua dịch vụ sinh tồn (ăn, ngủ, y tế) và nâng cấp trang bị từ HUB.
4. **Chu kỳ bóc lột:** HUB trả lương mỗi 30 ngày in-game, rồi thu lại qua Sàn Chứng Khoán, lãi vay, phí sửa đồ và Quán Bar.

## Tech stack

- **Engine:** Unity LTS (đề xuất 2022.3 trở lên)
- **Nền tảng:** Android (API Level 34+, định dạng AAB)
- **Kiến trúc:** Clean Architecture + DDD. Domain Layer viết bằng C# thuần, không phụ thuộc `UnityEngine`.
- **Khác:** VContainer hoặc Zenject (DI), UniRx, MessagePack, ScriptableObjects, Global Tick Manager cho AI.

Chi tiết xem [docs/00_Tach_Stack.md](docs/00_Tach_Stack.md).

## Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [00_Tach_Stack.md](docs/00_Tach_Stack.md) | Tech stack và kiến trúc kỹ thuật |
| [00_Master_GDD.md](docs/designs/00_Master_GDD.md) | Game Design Document tổng quan |
| [01_World_Map_Environment.md](docs/designs/01_World_Map_Environment.md) | Bản đồ thế giới và môi trường |
| [02_HUB_Economy_Infrastructure.md](docs/designs/02_HUB_Economy_Infrastructure.md) | Kinh tế và hạ tầng HUB |
| [03_Trainer_AI_System.md](docs/designs/03_Trainer_AI_System.md) | Hệ thống AI của Trainer |
| [04_Monster_System.md](docs/designs/04_Monster_System.md) | Hệ thống Monster |
| [05_Itemization_Gear_System.md](docs/designs/05_Itemization_Gear_System.md) | Vật phẩm và trang bị |
