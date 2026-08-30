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
- Nếu cần regenerate, dùng `DigitalDisco.ttf` regular, atlas 512×512, padding 5, SDFAA, Dynamic và
  multi-atlas; giữ nguyên asset/GUID hiện tại nếu đang repair reference chung.
- Content validator hoặc review phải bắt explicit TMP font khác chuẩn trong scene/prefab production.
- MainMenu Scene và gameplay overlay dùng cùng font family; đây không làm hai navigation system trở
  thành một hệ UI.

## License và attribution

- Tác giả: jeti.
- License công bố: CC BY 4.0.
- Cho phép dùng cho dự án cá nhân hoặc thương mại với yêu cầu ghi credit.
- Nguồn: [Digital Disco trên DaFont](https://www.dafont.com/digital-disco.font).
- License: [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/).

Credit phát hành phải có tối thiểu: `Digital Disco font by jeti — CC BY 4.0`.
