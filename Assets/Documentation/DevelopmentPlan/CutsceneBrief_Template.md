# Mẫu mô tả Cutscene (điền cho Cảnh 2, tái dùng cho các cảnh sau)

> Copy file này, điền vào các mục bên dưới rồi gửi lại. Càng cụ thể (đặc biệt phần thoại và
> đường di chuyển) thì dựng Timeline theo quy trình trong
> [CutsceneTimeline.md](../CutsceneTimeline.md) càng nhanh và ít phải hỏi lại.

## 1. Bối cảnh

- **Scene/Map:** MapNhat
- **Kích hoạt:** tự động khi vào MapNhat lần đầu
- **Vị trí diễn ra trong map:** _(mô tả hoặc chụp ảnh khu vực trong Scene view — gần cổng làng?
  quảng trường? nhà TruongLang? ...)_

## 2. Nhân vật

| Nhân vật | Vị trí xuất hiện lúc bắt đầu cảnh | Trạng thái ban đầu |
|---|---|---|
| Player | _(ở đâu khi cảnh bắt đầu — vừa vào map, hay đã đứng sẵn một chỗ?)_ | _(đi vào / đã đứng sẵn / idle hướng nào)_ |
| TruongLang | _(đã ở đó chờ sẵn, hay đi vào giữa cảnh?)_ | _(idle hướng nào / ẩn chờ đi vào)_ |

## 3. Diễn biến & đường di chuyển

Mô tả theo trình tự thời gian, mỗi actor di chuyển **theo trục thẳng** (không đi chéo — nếu cần rẽ,
ghi rõ điểm rẽ). Có thể chụp ảnh Scene view rồi vẽ mũi tên đường đi như đã làm ở Cảnh 1.

1. _(vd: Player đi từ điểm A tới điểm B theo hướng phải...)_
2. _(vd: TruongLang đã đứng sẵn, quay mặt hướng nào...)_
3. _(vd: sau thoại, cả 2 cùng đi về đâu / hay ở lại tại chỗ...)_

## 4. Camera

- Góc quay: _(giữ camera gameplay bình thường, hay cắt cảnh riêng qua Cinemachine như Cảnh 1?)_

Nếu cắt cảnh riêng, điền đúng 2 số sau — lấy bằng cách: đặt `CinemachineCamera` trong Scene view ở
góc bạn muốn, rồi đọc trực tiếp trong Inspector (không cần mô tả bằng lời):

| Thông số | Giá trị Cảnh 1 (tham khảo) | Giá trị Cảnh 2 |
|---|---|---|
| **Position** (Transform, X/Y/Z) | `(-17.41, -5.37, -10)` | |
| **Lens > Orthographic Size** | `8` | |

- Position: kéo GameObject camera trong Scene view tới góc muốn quay, copy 3 số X/Y/Z trong
  Inspector (Z luôn để `-10` cho 2D — camera phải lùi ra sau mặt phẳng sprite).
- Orthographic Size: số càng nhỏ càng zoom gần (8 ≈ đủ lấy 2 nhân vật đứng cạnh nhau + một phần bối
  cảnh xung quanh như Cảnh 1). Kéo thanh Size trong Inspector tới khi thấy khung hình ưng ý trong
  Scene view rồi đọc số ra.
- Cách nhanh nhất: chụp ảnh Inspector của `CinemachineCamera` (phần Transform + phần Lens) tại đúng
  góc bạn muốn, gửi ảnh đó — tôi đọc số trực tiếp từ ảnh, không cần gõ tay.

## 5. Thoại (theo đúng thứ tự sẽ hiện ra)

| # | Người nói | Đứng bên nào (trái/phải đối phương) | Nội dung thoại |
|---|---|---|---|
| 1 | | | |
| 2 | | | |
| 3 | | | |

_(thêm dòng nếu cần. "Đứng bên nào" quyết định chiều bong bóng thoại — xem "Quy tắc Flip" trong
CutsceneTimeline.md)_

## 6. Kết thúc cảnh

- Sau khi thoại xong: _(actor rời đi đâu, hay ở lại tại chỗ và trả quyền điều khiển cho Player?)_
- Có fade to/from black không?
- Sau cutscene: _(trở lại gameplay bình thường tại MapNhat, hay chuyển tiếp sang đâu khác?)_

## 7. Ghi chú khác

_(bất kỳ điều gì khác cần biết — âm thanh, hiệu ứng đặc biệt, timing đặc biệt...)_
