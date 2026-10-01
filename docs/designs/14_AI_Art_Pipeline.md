# SUB-GDD 14: AI ART PIPELINE (QUY TRÌNH SẢN XUẤT ART BẰNG AI)

Phương án sản xuất art cho Early Access bằng công cụ AI (ví dụ Google Gemini image/"Nano Banana", Meowa, và các công cụ tạo game asset khác). Tài liệu này là **kế hoạch cần kiểm chứng bằng art spike (§4)**, chưa phải cam kết: chất lượng, giá và điều khoản các công cụ thay đổi nhanh, cần kiểm tra lại trước khi dùng.

## 1. Khối lượng asset Early Access (từ [11_Game_Assets](11_Game_Assets.md))

| Hạng mục | Số lượng | Loại |
|---|---|---|
| Công trình HUB | 16 công trình x 4 trạng thái = 64 model | 3D low-poly/voxel |
| Tile/Block 3D cho 5 biome (Zone 2-5 và HUB/Đồng Cỏ) | khoảng 5 bộ | 3D + texture pixel |
| Trainer | 2 base model x 8 animation x 4-8 hướng | Sprite 2D (paper-doll) |
| Trang bị | 18 loại slot (12 Trainer + 6 Monster) x 3 Tier = 54 bộ phận | Sprite 2D rời để Sprite Swap |
| Monster/Enemy | 10-15 khung x 2 form (Chibi, Tiến hóa), tô màu theo hệ | Sprite 2D grayscale + shader tint |
| Avatar Trainer | 30 | Portrait 2D |
| Icon hệ thống | 100+ (16x16) | Pixel 2D |
| UI | Panel, khung, đồng hồ Payday, cửa sổ Chứng khoán | Pixel 2D |
| VFX | Chiến đấu, kinh tế, đập đồ | Particle + sprite |
| Audio | SFX kinh tế, BGM ngày/đêm/Zone | Âm thanh |

## 2. Công cụ ứng viên

| Nhu cầu | Ứng viên | Ghi chú cần kiểm tra |
|---|---|---|
| Ý tưởng, concept, avatar, icon, tham chiếu phong cách | Google Gemini image (Nano Banana / Nano Banana Pro) | Có thể giữ nhân vật nhất quán qua nhiều ảnh tham chiếu và tạo sprite sheet; dễ lệch tỉ lệ giữa các tư thế, nên tạo mỗi lần 1 nhân vật |
| Sprite, tileset, prop, UI, âm thanh trong một workspace | [Meowa](https://meowa.ai/) | Có workflow Pixel Art, HD Art, Music & Sound; kiểm tra định dạng xuất, độ phân giải và quyền thương mại |
| Sprite pixel art chuyên dụng | PixelLab, Ludo.ai | Gói tính phí theo số lượt tạo |
| Giữ nhất quán phong cách qua mô hình tự huấn luyện | Scenario | Huấn luyện trên art gốc của dự án |
| Model 3D từ ảnh/văn bản | Meshy, Tripo | Cần chỉnh topology/low-poly và texture thủ công để khớp phong cách voxel |

## 3. Quy trình đề xuất
1. **Style bible (người làm):** Chốt bảng màu, tỉ lệ Chibi, độ phân giải sprite, quy tắc viền và bóng. Mọi prompt dùng cùng bảng này.
2. **Tạo bản gốc nhất quán:** 1 Trainer base trần, 1 Monster grayscale, 1 công trình Lvl 1-3. Dùng làm ảnh tham chiếu cho cả dự án.
3. **Paper-doll:** Tạo từng bộ phận trang bị (Nón, Áo, Balo...) trên cùng khung xương để khớp Unity 2D Animation. Đây là điểm AI yếu nhất (bộ phận rời, nhất quán nhiều hướng), nên cần họa sĩ/pixel artist chỉnh tay.
4. **Hậu kỳ:** Chuẩn hóa bảng màu, cắt sprite, dọn nền, canh pivot, xuất atlas (xem [11](11_Game_Assets.md) §4 về Texture Atlas).
5. **3D:** Tạo model thô bằng AI rồi dựng lại lưới low-poly, bake texture pixel art. Giữ 4 trạng thái cùng silhouette cơ sở để khác biệt rõ khi lên cấp.
6. **Kiểm duyệt:** Mỗi asset phải qua kiểm tra nhất quán phong cách, độ phân giải, hiệu năng (số tam giác, atlas) trước khi vào project.

## 4. Art spike (1 tuần, trước Roadmap Bước 3)
Chạy thử để quyết định công cụ và mức độ chỉnh tay:
*   Sản xuất: 1 Trainer paper-doll (base + 4 bộ phận trang bị x 3 Tier, 4 hướng, Idle/Walk/Attack), 2 Monster (Chibi + Tiến hóa, tint theo hệ), 1 công trình (4 trạng thái), 10 icon, 3 avatar.
*   Đưa vào Unity: Billboard + Cutout Shader + NavMesh (cùng bài kiểm tra hiệu năng ở Roadmap Bước 2).
*   Tiêu chí đạt: giữ nhất quán qua mọi tư thế và Tier; ghép được vào Sprite Swap không lỗi; thời gian chỉnh tay mỗi asset đủ thấp để khối lượng §1 làm được trong lịch; giấy phép công cụ cho phép dùng thương mại.
*   Nếu không đạt ở paper-doll hoặc 3D: giữ AI cho concept/icon/avatar/UI, thuê họa sĩ cho phần còn lại.

## 5. Pháp lý và chính sách
*   Tác phẩm hoàn toàn do AI tạo ra thường không được bảo hộ bản quyền ở Mỹ vì thiếu tác giả là người; chỉ phần đóng góp sáng tạo của con người (chỉnh sửa, sắp xếp) có thể được bảo hộ. Điều này ảnh hưởng đến khả năng bảo vệ art của dự án nếu bị sao chép.
*   Steam yêu cầu khai báo nội dung do AI tạo xuất hiện trong game; Google Play chưa có quy định tổng quát tương đương nhưng thắt chặt ở các nhóm nhạy cảm. Theo dõi chính sách trước khi submit.
*   Lưu lại điều khoản thương mại của từng công cụ (tại thời điểm dùng) và nhật ký prompt/nguồn cho mỗi asset.
*   Không dùng tên/phong cách của nghệ sĩ hoặc IP có sẵn (ví dụ Pokémon) trong prompt.

Nguồn tham khảo: [Meowa](https://meowa.ai/), [Best AI Tools for Game Assets 2026 (Ludo.ai)](https://ludo.ai/compare/best-ai-game-asset-generators), [Gemini 3 Pro Image](https://aistudio.google.com/models/gemini-3-pro-image), [AI Game Assets: Copyright, Steam Disclosure (Promise Legal)](https://blog.promise.legal/ai-game-assets-copyright-steam-disclosure-2026/).
