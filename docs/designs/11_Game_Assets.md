# SUB-GDD 11: GAME ASSETS & 2.5D ISOMETRIC ART DIRECTION

## TỔNG QUAN THỊ GIÁC (VISUAL IDENTITY)
Dự án **Monster HUB Tycoon** áp dụng triết lý thiết kế "Phân tách Không gian" (Spatial Separation) nhằm nhấn mạnh vai trò Giám đốc của người chơi:
1.  **Không gian Quản trị (UI/UX 2D):** Bảng điều khiển (Dashboard) hiển thị dưới dạng 2D phẳng (Flat Design/Pixel UI) đè lên trên cùng màn hình (Screen Space - Overlay), tạo cảm giác người chơi đang ngồi trước một hệ thống quản lý tài chính thực thụ.
2.  **Không gian Thực địa (World 2.5D Isometric):** Thế giới thực nơi AI sinh sống. Môi trường, địa hình và công trình được dựng bằng **3D Low-poly/Voxel**, trong khi Nhân vật (Trainer, Monster, Enemy) là các **Sprite 2D Pixel Art** dựng đứng trong không gian 3D. Mọi thứ được quan sát qua lăng kính Camera Đẳng cự (Isometric).

---

## 1. KỸ THUẬT UNITY 2.5D ISOMETRIC (TECHNICAL ART IMPLEMENTATION)
Sự kết hợp giữa 3D Environment và 2D Sprite yêu cầu các thiết lập kỹ thuật (Tech Art) khắt khe để đảm bảo hình ảnh không bị gãy hoặc sai lệch phối cảnh:

*   **Hệ thống Camera Isometric:**
    *   Sử dụng **Orthographic Camera** (Loại bỏ hiệu ứng tụ điểm/phối cảnh xa gần).
    *   *Góc xoay tiêu chuẩn (True Isometric Rotation):* X: 35.264, Y: 45, Z: 0. Thiết lập này tạo ra lưới quy hoạch (Grid) hình thoi hoàn hảo, giúp việc kéo thả công trình trên nền đất vô cùng chuẩn xác.
*   **Sprite 2D trong Không gian 3D (Z-Buffer & Depth Sorting):**
    *   Tuyệt đối **KHÔNG** dùng `Sorting Layer` của hệ thống 2D. 
    *   Sử dụng **Alpha Clipping (Cutout Shader)** cho các Sprite 2D. Thuật toán này ép các nhân vật 2D ghi thông tin vào bộ đệm chiều sâu (Z-Buffer) của 3D. Nhờ đó, khi AI đi vòng ra sau một công trình 3D, chúng sẽ tự nhiên bị che khuất mà không cần code phức tạp tính toán trục Y.
*   **Kỹ thuật Billboarding & Bóng đổ (Shadows):**
    *   Mọi nhân vật 2D được gắn script `Billboard` và trừ hao góc nghiêng camera để luôn hướng mặt về phía người chơi.
    *   Sử dụng Custom Shader cho phép các Sprite 2D này **đổ bóng 3D (Cast Shadows)** lên mặt đất và công trình 3D khi có ánh sáng chiếu vào.
*   **Tìm đường & Địa hình (Pathfinding):**
    *   Môi trường dùng hệ thống lưới 3D có độ nhấp nhô (đồi núi, vực thẳm).
    *   AI Trainer và Monster sử dụng **NavMesh 3D** của Unity để tìm đường thay vì Grid 2D, giúp chúng di chuyển mượt mà qua các góc dốc, gầm cầu.

---

## 2. QUY TRÌNH SẢN XUẤT NHÂN VẬT & TRANG BỊ (MODULAR 2D WORKFLOW)
Để đáp ứng hệ thống **18 Slot Trang Bị** có thể thay đổi linh hoạt mà không làm phình to dung lượng bộ nhớ:
*   **Không dùng Sprite Sheet tĩnh:** Không vẽ frame-by-frame cho từng bộ quần áo.
*   **Hệ thống Paper-doll (Spine 2D / Unity 2D Animation):**
    *   Vẽ một "Phôi gốc" (Base Body) dạng Chibi trần.
    *   Vẽ tách rời các bộ phận (Mũ, Áo, Balo, Vũ khí).
    *   Sử dụng tính năng **Sprite Swap** để tự động đắp các mảnh trang bị này lên khung xương (Bone) của nhân vật ngay tại Runtime.
*   **Đổi màu động (Color Tinting):** Vẽ 1 form Quái vật cơ bản dưới dạng Trắng đen (Grayscale). Dùng Material Shader đổi màu theo hệ (Đỏ = Lửa, Xanh lá = Cỏ) để tiết kiệm 66% công sức vẽ. Vẽ form thứ 2 riêng biệt chỉ khi quái vật Tiến hóa.

---

## 3. DANH MỤC TÀI NGUYÊN CẦN SẢN XUẤT (ASSET LIST)

### A. Giao Diện Giám Đốc (UI/UX 2D)
*   **Style:** Dark Mode kết hợp viền Kim loại/Vàng (Gold).
*   **Assets:**
    *   Các Panel báo cáo, Khung Avatar, Cửa sổ Chứng khoán, Đồng hồ Payday.
    *   Hơn 100+ Icons hệ thống (16x16px): Gỗ, Đá, Gold, Gem, Bùa Bảo Hộ, Các loại Thuốc, Cổ vật.
    *   30 Avatar Chibi (Portraits) vẽ tay độ phân giải cao cho các Trainer.

### B. Môi trường & Công trình (3D Low-poly)
*   **Tile/Block 3D:** Khối đất, đá, nước để xếp Map theo dạng Grid vuông. Bề mặt bọc Texture Pixel Art.
*   **5 Biome Environments:** HUB (Đất/Cỏ), Zone 2 (Núi lửa/Dung nham), Zone 3 (Băng/Tuyết), Zone 4 (Đầm lầy độc), Zone 5 (Hư vô).
*   **Công trình HUB:**
    *   Tòa Thị Chính, Nhà Hàng, Nhà Trọ, Lò Rèn, Bệnh Viện, Trạm Giao Thương, Quán Bar.
    *   *Yêu cầu:* Mỗi công trình cần 4 Model 3D tương ứng 4 trạng thái (Đổ nát -> Lvl 1 -> Lvl 2 -> Lvl Max). Công trình cấp càng cao thì model 3D càng vươn cao lên theo trục Y để thể hiện sự phát triển.

### C. Nhân vật, Quái vật & Kẻ địch (2D Pixel Art Sprites)
*   **Trainer:** 2 Base model (Nam/Nữ). Animation Set: *Idle, Walk, Run, Attack, Gather, Defeat, Sleep, Sit.* (Render 4 hoặc 8 hướng cho isometric).
*   **Trang bị (Modular Assets):** 
    *   Trainer: Nón, Áo, Balo, Giày, Bình nước, Găng, Còi, Huy hiệu, Áo choàng, Kính, Vệ tinh.
    *   Monster: Vũ khí, Giáp, Vòng cổ, Lục lạc, Guốc, Lõi nguyên tố.
    *   *(Mỗi loại thiết kế tối thiểu 3 Tier ngoại hình: Rác/Thường -> Xịn -> Thần Thoại rực sáng).*
*   **Monster/Enemy:** 10-15 Khung quái cơ bản (Slime, Cáo, Chó sói, Khủng long, Golem, Rồng). Có 2 form: Chibi (Bình thường) và Hầm hố (Tiến hóa).

### D. Hiệu Ứng Hình Ảnh (VFX 3D/2D Hỗn hợp)
*   **Chiến đấu:** Sử dụng Particle System 3D. Cầu lửa, Luồng sét, Chất độc bay theo quỹ đạo Parabol 3D nhưng sử dụng Sprite 2D làm Particle.
*   **Kinh tế:** Hạt Gold nảy lên và bay vào kho (Tweening), Text sát thương & Text Viện phí nổi lên mặt màn hình (World-space to Screen-space UI).
*   **Đập đồ:** Màn hình lóe sáng rực rỡ (Cường hóa thành công), hoặc rung bần bật + mảnh vỡ kính văng tung tóe (Cường hóa vỡ).

### E. Âm thanh & Nhạc nền (Audio - SFX & BGM)
*   **SFX (Gây nghiện kinh tế):** 
    *   Tiếng `KACHING!` ròn rã của máy tính tiền.
    *   Tiếng búa đập kim loại nhịp nhàng (Lò rèn).
    *   Tiếng chuông còi báo động chói tai (Khủng hoảng/Đình công/Boss).
*   **BGM (Nhạc nền):**
    *   *HUB Ban ngày:* Nhịp độ nhanh, hối hả, mang âm hưởng tư bản/jazz nhịp beat nhanh.
    *   *HUB Ban đêm:* Lofi/Chiptune thư giãn, chậm rãi.
    *   *Wilderness (Map farm):* Âm hưởng thám hiểm, thay đổi nhạc cụ theo từng Zone.

---

## 4. CHIẾN LƯỢC TỐI ƯU CẤU HÌNH MOBILE (ANDROID OPTIMIZATION)
Vì xử lý môi trường 3D kết hợp hàng trăm Sprite 2D, Art Team phải tuân thủ các quy tắc Tối ưu hóa hiệu năng sau cho Engine:

1.  **Bake Lighting:** Ánh sáng tổng thể ban ngày (Directional Light) và bóng đổ của các công trình TĨNH bắt buộc phải được Bake (Nướng) sẵn vào Texture/Lightmap.
2.  **Đèn Động Hạn Chế (Real-time Lights Limit):** Chỉ dùng nguồn sáng động vào ban đêm (Đèn đường của HUB, Đuốc trên tay AI Trainer).
3.  **Culling Hệ Thống:** Kích hoạt tính năng **Occlusion Culling** (Ẩn các model 3D bị che lấp) và **Frustum Culling** (Ẩn các AI và quái vật nằm ngoài khung hình Camera Isometric). Giúp giải phóng CPU để tập trung tính toán logic kinh tế ngầm.
4.  **Texture Atlas:** Đóng gói toàn bộ các mảnh trang bị (Mũ, áo, balo...) vào một hoặc hai Texture Atlas lớn để giảm Draw Calls xuống mức tối thiểu.