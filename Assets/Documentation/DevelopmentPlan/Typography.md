# Typography Standard

Ngày chốt: 2026-08-22

## Font family chuẩn

Toàn bộ UI/text của project dùng **Digital Disco**:

- Regular/source chính: `Assets/Fonts/DigitalDisco.ttf`.
- Thin variant: `Assets/Fonts/DigitalDisco-Thin.ttf`, chỉ dùng khi art direction yêu cầu weight mảnh.
- TMP runtime asset mặc định: `Assets/Fonts/DigitalDisco SDF v3.asset`.

`TMP Settings.asset` phải trỏ `m_defaultFontAsset` tới `DigitalDisco SDF v3`. Text prefab/scene có
explicit font reference cũng phải dùng asset này; không để LiberationSans hoặc font template trở thành
visual font của game. Fallback font chỉ được thêm khi cần glyph mà Digital Disco không hỗ trợ và phải
được review để tránh thay đổi style ngoài ý muốn.

## Authoring rules

- Tạo text mới bằng TextMeshPro và giữ default DigitalDisco.
- Không tạo một SDF asset riêng cho từng scene.
- Content validator hoặc review phải bắt explicit TMP font khác chuẩn trong scene/prefab production.
- MainMenu Scene và gameplay overlay dùng cùng font family; đây không làm hai navigation system trở
  thành một hệ UI.

### Atlas Population Mode — Static (đổi 2026-09-07)

`DigitalDisco SDF v3.asset` dùng **Static** atlas population (trước đó Dynamic). Lý do đổi:

- Game full English, không cần hiển thị tiếng Việt cho người chơi — đã xác nhận trực tiếp với chủ dự án
  (2026-09-07).
- Dynamic mode khiến font asset tự động thêm glyph mới và bị đánh dấu dirty **mỗi khi một tổ hợp ký tự
  chưa từng render xuất hiện** (kể cả trong lúc test bằng tool/Play Mode), gây diff khổng lồ liên tục
  trong git dù không ai chủ động sửa font.
- Đã xác nhận (bằng `FontEngine.TryGetGlyphIndex` trực tiếp trên `DigitalDisco.ttf`, đối chiếu công khai
  với charmap 354 glyph trên DaFont) rằng font **không có** khối Unicode `U+1EA0–U+1EF9` (toàn bộ tổ hợp
  dấu sắc/huyền/hỏi/ngã/nặng mở rộng của tiếng Việt như ả, ế, ộ, ữ...), nên Dynamic cũng không thực sự
  "bảo vệ" khỏi glyph thiếu — nó chỉ hoãn lỗi tới runtime thay vì báo ngay lúc bake.

**Bộ ký tự đã bake tĩnh (143 ký tự)**: toàn bộ ASCII in được (`U+0020–U+007E`) cộng thêm các ký tự kiểu
chữ đã dùng trong UI hiện có (dấu ngoặc kép thông minh, en/em dash, dấu ba chấm, độ, bullet) và một số
ký tự Latin có dấu cơ bản mà font hỗ trợ (á à ã ă â é è ê í ì ĩ ó ò õ ô ú ù..., không dùng vì UI tiếng
Anh nhưng không gây hại khi giữ lại).

**Nếu sau này cần thêm ký tự mới không có trong bộ trên** (ví dụ ký hiệu đặc biệt cho UI mới): asset sẽ
**không tự thêm** vì đã Static — chữ thiếu sẽ hiện ô trống. Phải chủ động gọi lại
`TMP_FontAsset.TryAddCharacters(...)` (hoặc mở Font Asset Creator ở chế độ Additional Glyphs) cho ký tự
mới rồi save lại asset, không được âm thầm đổi ngược về Dynamic để né vấn đề.

Nếu sau này project cần hỗ trợ tiếng Việt thật (đổi ngôn ngữ, phụ đề...), phải quay lại đánh giá: hoặc
đổi hẳn sang font khác có phủ Unicode Latin Extended Additional, hoặc thêm Fallback Font riêng cho tiếng
Việt trong `TMP_Settings`/per-font fallback — không cố nhồi glyph vào `DigitalDisco.ttf` (font không có
glyph nguồn để bake).

## License và attribution

- Tác giả: jeti.
- License công bố: CC BY 4.0.
- Cho phép dùng cho dự án cá nhân hoặc thương mại với yêu cầu ghi credit.
- Nguồn: [Digital Disco trên DaFont](https://www.dafont.com/digital-disco.font).
- License: [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/).

Credit phát hành phải có tối thiểu: `Digital Disco font by jeti — CC BY 4.0`.
