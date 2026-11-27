# ORYNTHALS Trailer — Video nền cho cảnh thoại

Chỉ cần **một video nền** dùng chung cho mọi câu thoại. Mỗi lần chuyển scene: hiện video nền → hiện chữ thoại và giọng đọc → cắt sang clip gameplay.

Style theo game: nền tối nâu đen giống UI của game, hạt sáng pixel màu vàng ấm, điểm thêm vài hạt màu của 3 hệ
(xanh nước, nâu đất, xanh bạc hà của gió). Giữa màn hình để trống cho chữ.

## Bước 1 — ChatGPT tạo ảnh

Tải kèm `Marketing/References/ref_ui_frame.png` (khung UI của game) để ChatGPT lấy đúng tông màu.

```
Use the attached image only as a colour reference (dark brown-black, warm gold,
sapphire blue accents). Create a background for a fantasy game trailer title card,
16:9, in detailed pixel art style.

Deep dark brown-black background with a soft vignette, slightly lighter warm glow in
the centre. Floating pixel-art particles scattered across the frame: mostly small warm
gold and ember-orange glowing pixels and short sparks, plus a few elemental motes —
tiny sapphire-blue water droplets, small brown stone fragments and little mint-green
leaves. Low density, many particles slightly out of focus for depth, larger and blurrier
near the edges, smaller and sharper toward the centre.

The centre of the image is calm and mostly empty (space for text).
Square crisp pixels, cozy medieval fantasy mood, cinematic.
No text, no letters, no logo, no frame, no UI, no characters, no objects.
```

## Bước 2 — Google Flow tạo video

- Chọn **Frames to Video**.
- Đưa **cùng một ảnh** vào cả **khung đầu** và **khung cuối** để video lặp lại liền mạch.
- Chọn **16:9, 8 giây**.

```
The glowing pixel particles drift slowly upward and slightly sideways, gently
flickering and twinkling. Gold embers rise softly, small blue droplets, stone fragments
and green leaves float and turn slowly. The warm glow in the centre breathes gently
brighter and dimmer. Calm, magical, storytelling mood. Seamless loop, the last frame
matches the first. Static camera, no camera movement. Keep the pixel art style.
No text, no characters, no objects, no speech.
```

## Bước 3 — Khi dựng phim

- Video 8 giây được lặp lại, nên đặt nối tiếp nhiều lần nếu câu thoại dài.
- **Mỗi câu thoại** cắt ra một đoạn 3–6 giây từ video nền, mỗi lần lấy ở **vị trí khác nhau** để các cảnh không lặp y hệt.
- Chuyển cảnh: video nền fade từ đen vào → chữ hiện → giọng đọc → dứt câu thì cắt sang clip gameplay.

| Thiết lập chữ | Giá trị |
|---|---|
| Font | **Digital Disco** (`Assets/Fonts/DigitalDisco.ttf`) |
| Màu | Vàng nhạt `#F3E2B0`, viền nâu đen `#1A110A` dày 3–4 px |
| Cỡ | Khoảng 56–64 px ở 1080p |
| Vị trí | **Giữa màn hình** |
| Hiệu ứng | Fade in 0,4 giây, giữ trong lúc đọc, fade out 0,4 giây |

| # | Chữ thoại | Đoạn video nền dài |
|---|---|---|
| 01 | Countless journeys... and countless legends yet to be written. | 6 giây |
| 02 | And some... are ready to take the first step. | 4 giây |
| 03 | A new land. | 3 giây |
| 04 | A new way of life. | 3 giây |
| 05 | (không có thoại, cắt thẳng sang gameplay) | — |
| 06 | Newfound strength. | 3 giây |
| 07 | Face the dangers that lie ahead. | 3 giây |
| 08 | Gather resources. Build your strength. | 4 giây |
| 09 | Countless dangers... await your arrival. | 4 giây |
| 10 | Conquer the impossible... and write your own legend. | 5 giây |
| 11 | ORYNTHALS (logo game) | 5 giây |
