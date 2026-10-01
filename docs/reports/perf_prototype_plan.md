# Prototype hiệu năng 2.5D: kế hoạch kiểm chứng (O4)

Chuẩn bị cho Roadmap Bước 2. Cần Unity 6.3 LTS (URP) và ít nhất một thiết bị Android thật; chưa có project Unity trong repo.

## 1. Cảnh thử
*   Mặt đất 3D tile (khoảng 40x40 ô), 8 công trình placeholder (hộp có 4 mức cao), NavMesh bake.
*   **30 Trainer + 90 Monster** (120 sprite Billboard, Cutout Shader, đổ bóng bật) di chuyển ngẫu nhiên giữa các công trình bằng NavMesh, cập nhật bằng Global Tick 0.2s (không `Update()` mỗi nhân vật).
*   Ban đêm: 12 đèn động (đèn đường, đuốc), tối đa theo URP.
*   UI Dashboard (UGUI) cập nhật mỗi tick: Gold, Payday, bảng AI.
*   Object pool cho Damage Text: 60 text/giây.

## 2. Biến thể cần đo
| Biến thể | Thay đổi |
|---|---|
| B0 | Cấu hình đầy đủ ở trên |
| B1 | Tắt đổ bóng thời gian thực của sprite |
| B2 | Giảm đèn động xuống 4 |
| B3 | Giảm số Monster hiển thị (chỉ Active, 30 thay vì 90) |
| B4 | Dùng Billboard không đổ bóng + blob shadow |

## 3. Số cần ghi
FPS trung bình và thấp 1% (5 phút), thời gian CPU/GPU mỗi khung, draw call, bộ nhớ, nhiệt độ sau 15 phút, mức pin hao trong 15 phút, GC alloc mỗi khung.

## 4. Thiết bị
Tối thiểu 2 máy: một máy tầm trung (Snapdragon 6/7 hoặc Dimensity 7000 trở xuống, RAM 4-6 GB) và một máy cấu hình thấp (RAM 3-4 GB). Ghi tên máy, phiên bản Android, độ phân giải.

## 5. Tiêu chí
*   Đạt: B0 hoặc biến thể tắt tính năng ít nhất vẫn giữ tối thiểu 30 FPS ổn định, nhiệt độ và pin chấp nhận được.
*   Không đạt: chỉ biến thể bỏ cả đổ bóng lẫn nhiều Monster mới đạt 30 FPS. Khi đó đề xuất đổi sang 2D thuần (đã chốt 2.5D ở mục "Đã chốt" của [Open Issues](../99_Open_Issues.md), nên quyết định này cần bàn lại).
