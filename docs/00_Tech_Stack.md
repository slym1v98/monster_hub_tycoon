
# TÀI LIỆU TECH STACK & KIẾN TRÚC KỸ THUẬT

**Engine:** Unity LTS (Đề xuất 2022.3 LTS hoặc mới hơn)
**Target Platform:** Android (Mục tiêu: API Level 34+, định dạng Android App Bundle - AAB)

## 1. Kiến Trúc Cốt Lõi (Core Architecture)

Để bóc tách hoàn toàn logic nền kinh tế và AI ra khỏi giao diện, dự án sẽ áp dụng **Clean Architecture** kết hợp với **Domain-Driven Design (DDD)**.

* **Domain Layer (Entities & Use Cases):** Chứa toàn bộ core logic (Công thức tính lương, Thuế, Lò rèn, Sàn chứng khoán). Hoàn toàn độc lập, viết bằng C# thuần, không phụ thuộc vào thư viện `UnityEngine`. Điều này cho phép Unit Test hàng triệu vòng lặp kinh tế chỉ trong vài giây mà không cần Play Mode.
* **Interface Adapters (Controllers & Presenters):** Cầu nối giữa Domain và giao diện. Áp dụng pattern **MVVM (Model-View-ViewModel)** hoặc **MVP (Model-View-Presenter)** để bind dữ liệu kinh tế lên màn hình giám đốc.
* **Dependency Injection (DI):**
* *Package sử dụng:* **VContainer** hoặc **Zenject (Extenject)**.
* *Lý do:* Giải quyết triệt để sự phụ thuộc chéo giữa các hệ thống (Ví dụ: Class `TradingPost` cần gọi `TreasurySystem` mà không phải dùng Singleton bừa bãi).



## 2. Hệ Thống Dữ Liệu Phẳng (Flattened Data Management)

Thay vì sử dụng cấu trúc cây đệ quy (recursive tree) rất dễ gây tràn bộ nhớ và chậm truy xuất khi AI quét điều kiện, toàn bộ dữ liệu (Inventory, Gene Quái vật, Bảng xếp hạng trang bị) sẽ được **phẳng hóa (flattened)**.

* **Dữ liệu tĩnh (Static Data / Master Data):**
* *Định dạng:* CSV hoặc Google Sheets tải xuống dưới dạng JSON.
* *Lưu trữ trong Unity:* Sử dụng **ScriptableObjects** để lưu trữ chỉ số cơ bản của quái vật, giá trị trang bị, cấu hình map. Đọc cực nhanh vào bộ nhớ tại thời điểm runtime.


* **Dữ liệu động (Runtime Data & Save Game):**
* *Package:* **MessagePack** cho C# hoặc **Newtonsoft.Json**. (Đề xuất MessagePack vì tốc độ serialize/deserialize nhanh hơn JSON nhiều lần trên thiết bị di động).
* *Lưu trữ cục bộ:* Ghi file nhị phân (Binary) hoặc JSON mã hóa xuống `Application.persistentDataPath` của thiết bị Android.
* *Database nội bộ:* Nếu lịch sử giao dịch Chứng khoán và Dòng tiền quá lớn, có thể tích hợp **SQLite** local để AI query (truy vấn) trạng thái thị trường hiệu quả hơn (O(1) thay vì loop qua List).



## 3. Hệ Thống AI & Mô Phỏng (AI & Simulation Layer)

Quản lý 30 Trainer + 90 Monster + Tương tác công trình trên Mobile đòi hỏi tối ưu CPU (chống nóng máy và hao pin Android).

* **Kiến trúc Hệ thống (Tick System):** Không sử dụng `Update()` của Unity MonoBehaviour cho từng nhân vật. Tạo ra một **Global Tick Manager**. Mỗi chu kỳ (ví dụ: 0.2 giây / tick), Manager sẽ phân bổ tín hiệu đến các AI để chúng tự kiểm tra trạng thái.
* **Hành vi AI (Behavior Tree / FSM):**
* *Giải pháp 1 (Custom C#):* Tự viết một Finite State Machine (FSM) nhẹ gọn bằng C# thuần ở tầng Domain.
* *Giải pháp 2 (Visual Scripting):* Sử dụng plugin **NodeCanvas** hoặc **Behavior Designer** để Designer có thể tự vẽ sơ đồ hành vi "Đình công", "Đi Farm", "Ngủ" mà không cần Dev phải sửa code liên tục.


* **Reactive Programming:** Sử dụng **UniRx (Reactive Extensions for Unity)**. Cực kỳ hiệu quả cho game Tycoon. Ví dụ: Khi Gold của kho bạc thay đổi, UniRx sẽ tự động bắn event (bắn luồng tín hiệu) cập nhật UI và trigger hành vi "Đòi tăng lương" của Trainer mà không cần viết hàm Check() liên tục.

## 4. UI & Tương Tác (Presentation Layer)

* **Hệ thống UI:** Tạm thời sử dụng **Unity UI (UGUI)** với TextMeshPro để render các chỉ số sát thương, bảng biểu chứng khoán rõ nét trên màn hình Android. (Nếu team có kinh nghiệm, có thể sử dụng UI Toolkit mới của Unity).
* **Object Pooling:** Bắt buộc áp dụng Pool cho Damage Text (Số sát thương nhảy lên), Hiệu ứng kỹ năng, và Item Drop (Tài nguyên rơi ra) để tránh Garbage Collection (GC Spikes) làm khựng game trên các máy Android cấu hình yếu.

## 5. Môi trường Build & CI/CD (Pipeline)

Môi trường phát triển và xuất file cần được tự động hóa để tiết kiệm thời gian test.

* **Môi trường Dev:** Ưu tiên sử dụng kiến trúc máy tính hiệu năng cao (như các dòng máy Mac sử dụng chip Apple Silicon) để đảm bảo tốc độ compile C# và build file Android (APK/AAB) cực nhanh, đồng thời sẵn sàng mở rộng (scale) sang iOS/Xcode về sau mà không cần đổi máy.
* **Version Control:** Git (GitHub/GitLab) với file `.gitignore` tiêu chuẩn cho Unity.
* **CI/CD Pipeline:** Sử dụng **GitHub Actions** hoặc **GameCI**. Thiết lập pipeline tự động: Mỗi khi push code lên nhánh `main`, hệ thống tự động chạy Unit Test (Domain Layer) -> Build ra file `.apk` -> Đẩy lên Firebase App Distribution để test nội bộ trên điện thoại thực tế.

## 6. Backend & Services (Tùy chọn mở rộng)

Dù là game Offline/Idle, việc tích hợp một Backend as a Service (BaaS) nhẹ nhàng là rất cần thiết cho dòng game Tycoon để cân bằng kinh tế:

* **Dịch vụ:** **Unity Gaming Services (UGS)** hoặc **Firebase**.
* **Tính năng thiết yếu:**
* *Remote Config:* Cho phép bạn (Giám đốc thực sự) đổi giá bán Bùa bảo hộ, hoặc giảm tỉ lệ rớt nguyên liệu trực tiếp từ Server mà không cần bắt người dùng tải lại bản cập nhật trên Google Play.
* *Analytics:* Cắm các cờ (Events) theo dõi xem người chơi thường tiêu sạch tiền ở công trình nào nhất (Lò rèn hay Chứng khoán) để điều chỉnh GDD.
