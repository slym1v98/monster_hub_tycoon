# Art spike: bản giao việc (O3)

Chuẩn bị cho art spike 1 tuần ở [14_AI_Art_Pipeline](../designs/14_AI_Art_Pipeline.md) §4. Người chạy thử chỉ cần làm theo tài liệu này và điền bảng kết quả.

## 1. Style bible (bản nháp, cần chốt trước khi sinh ảnh)
*   **Phong cách:** Chibi pixel art, tươi sáng và dễ thương, tương phản với nội tại kinh tế khốc liệt ([00_Master_GDD](../designs/00_Master_GDD.md)).
*   **Nhân vật:** sprite 2D đứng trong thế giới 3D, camera isometric (X 35.264, Y 45). Tỉ lệ đầu : thân khoảng 1 : 1.5. Viền tối 1 pixel.
*   **Độ phân giải đề xuất:** Trainer 64x64 mỗi khung, Monster 48x48 đến 64x64, icon 16x16, avatar 128x128. *(giá trị đề xuất, chốt sau khi thử trong Unity)*
*   **Bảng màu:** giới hạn 24-32 màu mỗi nhóm asset; mỗi hệ nguyên tố một màu chủ đạo (xem [04](../designs/04_Monster_System.md)). Monster vẽ dạng grayscale để tô màu bằng shader.
*   **UI:** dark mode viền kim loại/vàng ([11](../designs/11_Game_Assets.md) §3A).
*   **Cấm:** tên nghệ sĩ hoặc IP có sẵn trong prompt.

## 2. Danh mục asset của spike
| # | Asset | Chi tiết | Ghi chú |
|---|---|---|---|
| 1 | Trainer base trần (nam) | 4 hướng; Idle, Walk, Attack | Khung xương cho Unity 2D Animation |
| 2 | Bộ phận trang bị | Nón, Áo, Balo, Giày; mỗi loại 3 Tier (Thường, Xịn, Thần Thoại) = 12 mảnh | Phải khớp vị trí xương |
| 3 | Monster Slime | Chibi + Tiến hóa; grayscale | Tô màu theo hệ |
| 4 | Monster Sói | Chibi + Tiến hóa; grayscale | |
| 5 | Công trình Nhà Trọ | 4 trạng thái (Đổ nát, Lvl 1, 2, 3), cùng silhouette cơ sở | 3D low-poly/voxel |
| 6 | Icon | 10 icon 16x16 (Gold, Gem, Gỗ, Đá, Thuốc, Bùa Bảo Hộ...) | |
| 7 | Avatar | 3 avatar 128x128 | |

## 3. Mẫu prompt (điền vào, giữ nguyên phần phong cách)
```
[PHONG CÁCH] chibi pixel art, {độ phân giải}, bảng màu giới hạn, viền tối 1px, nền trong suốt, góc nhìn isometric
[ĐỐI TƯỢNG] {mô tả: ví dụ "nhà trọ gỗ nhỏ, mái cỏ, đổ nát"}
[RÀNG BUỘC] cùng tỉ lệ và bảng màu với ảnh tham chiếu; không chữ; không logo; không phong cách của tác giả hoặc game có sẵn
[THAM CHIẾU] ảnh gốc đã duyệt: {tên file}
```

## 4. Phiếu chấm điểm (điền sau khi chạy)
| Tiêu chí | Đạt khi | Gemini | Meowa | PixelLab/khác |
|---|---|---|---|---|
| Nhất quán tư thế | 4 hướng cùng tỉ lệ, cùng bảng màu | | | |
| Paper-doll | Mảnh trang bị ghép vào base không lệch, không viền thừa | | | |
| Nhất quán Tier | 3 Tier cùng loại nhận ra là một món | | | |
| Nhất quán 4 trạng thái công trình | Cùng silhouette cơ sở, cao dần theo cấp | | | |
| Độ phân giải pixel sạch | Không mờ, không nhiễu, đúng lưới pixel | | | |
| Giấy phép | Cho dùng thương mại bằng văn bản | | | |
| Công chỉnh tay mỗi asset | Dưới 30 phút (ngưỡng đề xuất) | | | |
| Chi phí | Credit/giờ chấp nhận được | | | |

Kết luận cần ghi: công cụ nào dùng cho nhóm asset nào; phần nào phải thuê họa sĩ; số tuần art thực tế cho Early Access.
