# TÀI LIỆU TECH STACK & KIẾN TRÚC KỸ THUẬT

**Engine:** Unity 6.3 LTS (6000.3) hoặc mới hơn. Render pipeline: **URP** (cần cho shader Cutout, bóng đổ của Sprite và Bake lighting, xem [11_Game_Assets](designs/11_Game_Assets.md)).
**Target Platform:** Android, **target API Level 36** (Google Play yêu cầu từ 2026-08-31 cho app mới và bản cập nhật; xác nhận lại khi phát hành), định dạng Android App Bundle (AAB)

## 1. Kiến Trúc Cốt Lõi (Core Architecture)

Để bóc tách hoàn toàn logic nền kinh tế và AI ra khỏi giao diện, dự án sẽ áp dụng **Clean Architecture** kết hợp với **Domain-Driven Design (DDD)**.

* **Domain Layer (Entities & Use Cases):** Chứa toàn bộ core logic (Công thức tính lương, Thuế, Lò rèn, Sàn chứng khoán). Hoàn toàn độc lập, viết bằng C# thuần, không phụ thuộc vào thư viện `UnityEngine`. Điều này cho phép Unit Test hàng triệu vòng lặp kinh tế chỉ trong vài giây mà không cần Play Mode.
* **Interface Adapters (Controllers & Presenters):** Cầu nối giữa Domain và giao diện. Áp dụng pattern **MVP (Model-View-Presenter)** để bind dữ liệu kinh tế lên màn hình giám đốc.
* **Dependency Injection (DI):**
  * *Package sử dụng:* **VContainer**.
  * *Lý do:* Giải quyết triệt để sự phụ thuộc chéo giữa các hệ thống (Ví dụ: Class `TradingPost` cần gọi `TreasurySystem` mà không phải dùng Singleton bừa bãi).

## 2. Hệ Thống Dữ Liệu Phẳng (Flattened Data Management)

Thay vì sử dụng cấu trúc cây đệ quy (recursive tree) rất dễ gây tràn bộ nhớ và chậm truy xuất khi AI quét điều kiện, toàn bộ dữ liệu (Inventory, Gene Quái vật, Bảng xếp hạng trang bị) sẽ được **phẳng hóa (flattened)**.

* **Dữ liệu tĩnh (Static Data / Master Data):**
  * *Định dạng:* CSV hoặc Google Sheets tải xuống dưới dạng JSON.
  * *Lưu trữ trong Unity:* Sử dụng **ScriptableObjects** để lưu trữ chỉ số cơ bản của quái vật, giá trị trang bị, cấu hình map. Đọc cực nhanh vào bộ nhớ tại thời điểm runtime.

* **Dữ liệu động (Runtime Data & Save Game):**
  * *Package:* **MessagePack** cho C#. (Chọn MessagePack thay vì Newtonsoft.Json vì tốc độ serialize/deserialize nhanh hơn JSON nhiều lần trên thiết bị di động).
  * *Lưu trữ cục bộ:* Ghi file nhị phân (Binary) hoặc JSON mã hóa xuống `Application.persistentDataPath` của thiết bị Android.
  * *Database nội bộ:* Nếu lịch sử giao dịch Chứng khoán và Dòng tiền quá lớn, có thể tích hợp **SQLite** local để AI query (truy vấn) trạng thái thị trường hiệu quả hơn khi cần tra cứu lịch sử lớn. Với tra cứu trạng thái thị trường theo thời gian thực, dùng `Dictionary`/index trong bộ nhớ (O(1)) thay vì loop qua List; SQLite chỉ dùng cho truy vấn lịch sử.

## 3. Hệ Thống AI & Mô Phỏng (AI & Simulation Layer)

Quản lý 30 Trainer + 90 Monster + Tương tác công trình trên Mobile đòi hỏi tối ưu CPU (chống nóng máy và hao pin Android).

* **Kiến trúc Hệ thống (Tick System):** Không sử dụng `Update()` của Unity MonoBehaviour cho từng nhân vật. Tạo ra một **Global Tick Manager**. Mỗi chu kỳ (ví dụ: 0.2 giây / tick), Manager sẽ phân bổ tín hiệu đến các AI để chúng tự kiểm tra trạng thái.
* **Hành vi AI (FSM thuần C#):** Tự viết một Finite State Machine (FSM) nhẹ gọn bằng C# thuần ở tầng Domain (các trạng thái như "Đình công", "Đi Farm", "Ngủ"). Không dùng plugin Behavior Tree/Visual Scripting (NodeCanvas, Behavior Designer) vì chúng phụ thuộc `UnityEngine`, phá vỡ nguyên tắc Domain thuần C# và khả năng Unit Test không cần Play Mode.

* **Reactive Programming:** Domain phát sự kiện bằng C# thuần (event/delegate hoặc event bus tự viết), không phụ thuộc thư viện Unity. **R3** (bản kế nhiệm của UniRx, vốn đã ngừng phát triển) chỉ dùng ở lớp Interface Adapters/Presentation để chuyển sự kiện Domain thành luồng cập nhật UI. Ví dụ: Khi Gold của kho bạc thay đổi, Domain bắn sự kiện, Presenter dùng R3 cập nhật UI; hành vi "Đòi tăng lương" của Trainer do FSM trong Domain xử lý khi nhận sự kiện đó.

## 4. UI & Tương Tác (Presentation Layer)

* **Hệ thống UI:** Sử dụng **Unity UI (UGUI)** với TextMeshPro để render các chỉ số sát thương, bảng biểu chứng khoán rõ nét trên màn hình Android. UI Toolkit không dùng trong giai đoạn Early Access.
* **Object Pooling:** Bắt buộc áp dụng Pool cho Damage Text (Số sát thương nhảy lên), Hiệu ứng kỹ năng, và Item Drop (Tài nguyên rơi ra) để tránh Garbage Collection (GC Spikes) làm khựng game trên các máy Android cấu hình yếu.

## 4b. Cấu trúc project (đề xuất khi khởi tạo)

Chia theo Assembly Definition để trình biên dịch chặn phụ thuộc sai chiều:

* `Game.Domain`: C# thuần, bật *No Engine References*. Entities, Use Cases, FSM, công thức kinh tế.
* `Game.Application`: giao diện (interface/port) giữa Domain và bên ngoài, Tick Manager (logic).
* `Game.Infrastructure`: MessagePack/Save, SQLite, ScriptableObjects loader, Remote Config.
* `Game.Presentation`: MonoBehaviour, UGUI, Presenters, R3, Billboard/Shader, NavMesh.
* `tests/Game.Domain.Tests`: Unit Test (xUnit) cho Domain. `tools/Game.Sim`: console runner mô phỏng cân bằng (xem Roadmap Bước 5). Cả hai hiện chạy bằng .NET SDK, chưa nằm trong project Unity.

## 5. Môi trường Build & CI/CD (Pipeline)

Môi trường phát triển và xuất file cần được tự động hóa để tiết kiệm thời gian test.

* **Môi trường Dev:** Khuyến nghị (không bắt buộc) dùng máy hiệu năng cao, ví dụ Mac chip Apple Silicon, để đảm bảo tốc độ compile C# và build file Android (APK/AAB) cực nhanh, đồng thời sẵn sàng mở rộng (scale) sang iOS/Xcode về sau mà không cần đổi máy.
* **Version Control:** Git (GitHub/GitLab) với file `.gitignore` tiêu chuẩn cho Unity.
* **CI/CD Pipeline:** Sử dụng **GitHub Actions** hoặc **GameCI**. Thiết lập pipeline tự động: Mỗi khi push code lên nhánh `main`, hệ thống tự động chạy Unit Test (Domain Layer) -> Build ra file `.apk` -> Đẩy lên Firebase App Distribution để test nội bộ trên điện thoại thực tế.

## 6. Backend & Services (Tùy chọn mở rộng)

Game là Offline/Idle, PvP nằm ngoài Early Access nên không cần server riêng. Việc tích hợp một Backend as a Service (BaaS) nhẹ nhàng vẫn cần thiết cho dòng game Tycoon để cân bằng kinh tế. Phạm vi Early Access chỉ gồm Remote Config và Analytics (PvP, leaderboard liên server cần backend riêng, làm ở giai đoạn sau):

* **Dịch vụ:** **Unity Gaming Services (UGS)** hoặc **Firebase**.
* **Tính năng thiết yếu:**
  * *Remote Config:* Cho phép bạn (Giám đốc thực sự) đổi giá Gem của Bùa Bảo Hộ, hoặc giảm tỉ lệ rớt nguyên liệu trực tiếp từ Server mà không cần bắt người dùng tải lại bản cập nhật trên Google Play.
  * *Analytics:* Cắm các cờ (Events) theo dõi xem người chơi thường tiêu sạch tiền ở công trình nào nhất (Lò rèn hay Chứng khoán) để điều chỉnh GDD.
