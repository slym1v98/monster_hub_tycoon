# Danh sách kiểm tra pháp lý và chính sách (O5)

Không phải tư vấn pháp lý. Các việc dưới đây cần người có thẩm quyền xác nhận.

## 1. Trước khi dùng công cụ AI cho asset phát hành
| Việc | Trạng thái | Ai làm |
|---|---|---|
| Xin điều khoản dịch vụ bằng văn bản của Meowa (trang giá ghi "commercial ownership" nhưng không có trang điều khoản) | Chưa | Chủ dự án |
| Xác nhận gói Gemini có billing (bản miễn phí cho Google dùng prompt/kết quả) | Chưa | Chủ dự án |
| Lưu bản chụp điều khoản của mỗi công cụ tại ngày sử dụng | Chưa | Chủ dự án |
| Ghi nhật ký prompt và nguồn cho mỗi asset | Chưa (tạo thư mục `docs/art_log` khi bắt đầu) | Art |

Mẫu email gửi Meowa:
> Subject: Commercial licence terms for generated assets
> Hello, we plan to ship a mobile game (Android, Google Play) using sprites and tilesets generated with Meowa. Your pricing page states "commercial ownership of generated assets", but we could not find public Terms of Service. Could you share the full terms, confirm that this ownership covers paid commercial distribution, and tell us whether assets generated on a given plan remain usable after the subscription ends?

## 2. Trước khi submit lên Google Play
*   Chính sách Payments: công bố tỉ lệ Gacha trước và gần lúc mua ([07](../designs/07_Monetization_Model.md) §6).
*   Quảng cáo có thưởng: công bố tỉ lệ phần thưởng ngẫu nhiên; không thưởng tiền thật.
*   Đăng ký (Tycoon Club): giao diện hủy rõ ràng, mô tả giá và chu kỳ.
*   Target API 36 (xem [00_Tech_Stack](../00_Tech_Stack.md)); đọc lại yêu cầu hiện hành vào thời điểm submit.
*   Khai báo nội dung AI nếu chính sách nền tảng yêu cầu tại thời điểm đó.
*   Chính sách dữ liệu và Data safety form (Firebase Analytics, AdMob).
