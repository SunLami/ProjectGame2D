# Chuẩn kích thước và kế hoạch chuyển đổi popup

Đã bắt đầu triển khai. Inventory và Shop đang ở bước co đồng đều; kiểm thử trong Unity do chủ dự án thực hiện. Các kích thước mục tiêu dưới đây là đơn vị UI trên Canvas tham chiếu
1920×1080. Kích thước hiện tại ghi theo prefab hoặc builder. Scale ghi riêng khi đã xác định,
vì kích thước thực tế còn phụ thuộc Canvas và hierarchy.

## Chuẩn dùng chung

- Dùng Canvas tham chiếu 1920×1080. Các Canvas riêng có thể giữ sorting và lifecycle hiện có,
  nhưng dùng cùng quy tắc scale. Dùng `Scale With Screen Size` với `Expand` để toàn bộ vùng
  tham chiếu vẫn nằm trong viewport ở tỷ lệ hẹp hoặc rộng.
- Modal giữa màn hình neo trong safe area, chừa tối thiểu 24 đơn vị UI mỗi cạnh. Tại 16:9,
  modal lớn có vùng hiển thị mục tiêu 1600×900. Ở viewport nhỏ hơn, co đều toàn bộ khung.
- Khung gồm Backdrop stretch toàn màn hình, SafeArea root, Window căn giữa và Content.
  Header bám cạnh trên, footer bám cạnh dưới, thân stretch phần còn lại. Dùng layout group
  cho hàng/cột hoặc danh sách, ScrollRect cho nội dung biến đổi. Popup hội thoại neo dưới.
- Giai đoạn tương thích đặt toàn bộ board cũ trong một container. Tính hệ số fit bằng chiều
  nhỏ hơn trong hai tỷ lệ chiều rộng và chiều cao. Bố cục bên trong giữ đúng tỷ lệ cũ,
  nên có thể còn khoảng trống trong khung chuẩn. Khi chuyển xong từng popup, Content dùng
  scale 1 và các vùng con co giãn theo anchor/layout.
- Dùng chung kích thước ô, vùng icon và badge số cho GridSlot có cùng chức năng. Bắt đầu với
  ô 96×96, khoảng cách 16, riêng chiều cao và viewport do bố cục quyết định. Badge nằm trong
  góc dưới phải, nền tối đục, số trắng, không chặn raycast. Duyệt số 1, 10 và 99 trong cùng
  ảnh so sánh. Giữ quy tắc hiện hoặc ẩn số 1 của từng UI.
- Board có hoa văn viền cố định cần kiểm tra trước khi stretch. Dùng sprite 9-slice nếu viền
  hỗ trợ. Với board có sẵn bố cục hai cột trong ảnh, giữ tỷ lệ nguyên khung trong giai đoạn
  tương thích và chuyển sang các panel con khi cần bố cục linh hoạt.

## Từng cửa sổ

| UI | Hiện tại | Mục tiêu | Cách chuyển và kiểm tra |
|---|---|---|---|
| Inventory | 680×382.7, cha scale 0.80 | 1600×900 | Trái giữ character preview, equipment và stats. Phải dùng tab, grid scroll và footer. Kiểm tra đổi tab, trang bị, kéo thả, loadout và badge số. |
| Shop | 1536×1024, scale khoảng 0.39 | 1600×900 | Thân hai cột. Danh sách hàng và CompactInventory cuộn riêng. Nút mua/bán và số lượng bám footer. So sánh số 99 trực tiếp với Inventory, mua/bán bằng giao dịch thật. |
| Crafting | 1536×1024, scale khoảng 0.39 | 1600×900 | Giữ nhóm recipe mở rộng, vùng nguyên liệu và CompactInventory. Kiểm tra danh mục rỗng, tên dài và nút Craft khi thiếu nguyên liệu. |
| Farm Storage | 900×600 | 1600×900 | Hai cột chia đều, dùng cùng kích thước ô. Kiểm tra cuộn tới cuối, gửi/rút cả stack và kéo thả hai chiều. |
| Skill Panel | Composition 1720×900, board 1300×790 | 1600×900 | Chuyển cả board lẫn vùng tooltip vào safe area. Giữ tab, chi phí học và loadout. Kiểm tra mở từ Agnes, tooltip ở mép và kéo skill. |
| Character | 760×410 | 1600×900 | Preview và stats thành hai vùng, neo cột stat theo hàng. Kiểm tra giá trị lớn, thay trang bị và refresh preview. |
| Quest Log | 560×340 | 1600×900 | Hai cột danh sách và nội dung cuộn, action bám đáy. Kiểm tra nhiều mục tiêu, reward, Track và Abandon. |
| Full Map | Đo khung và viewport trên instance trước chuyển | 1600×900 | Neo nút đóng và cụm zoom độc lập với vùng ảnh. Kiểm tra pan, zoom và marker khớp ảnh. |
| Pause | 164×340, panel scale 0.65 | 560×720 | Dùng stack nút dọc, giữ vị trí chọn và Back. Kiểm tra chuyển Settings, Save và Return to Main Menu. |
| Settings | 200×300 trong GameplayUIRoot | 560×720 | Header và footer cố định, các nhóm slider/toggle cuộn. Áp dụng chung cấu trúc cho Main Menu và gameplay, giữ lifecycle riêng. |
| Save Slot Picker | Đo khung trên MainMenu và gameplay trước chuyển | 1440×810 ở Main Menu, 1440×810 trong gameplay | Giữ đúng ba slot, header và footer dùng chung. Kiểm tra Empty, Continue, Save As, Overwrite và Delete. |
| Quest Offer | Card 320×400 | 720×800 | Header/portrait cố định, objective và reward cuộn, Accept/Decline bám đáy. Kiểm tra nội dung dài và controller focus. |
| Xác nhận lưu, xóa save, bỏ quest, bỏ tutorial, vứt item | Panel lưu/vứt item đã đo 500×245; đo riêng các biến thể khác trước chuyển | 640×320 | Một cấu trúc header/body/actions; bước nhập số dùng cùng khung. Kiểm tra thông báo dài, Cancel, xác nhận và việc trả focus. |
| Dialogue NPC | 700×270, scale 0.50 | 960×280 | Neo dưới trong safe area. Vùng choice riêng, co phần body khi có choice. Kiểm tra hội thoại ngắn, dài và đủ bốn lựa chọn. |
| Fishing | Minigame 520×650, kết quả 620×180 | 560×720 cho minigame; 640×224 cho kết quả | Co toàn bộ track và vùng bắt cá cùng nhau để giữ hình học gameplay. Kiểm tra Waiting, Bite, minigame và kết quả. |
| World Message | 480×224 | 480×224 | Dùng chung safe area và thứ tự vẽ. Kiểm tra đóng modal và trả quyền điều khiển. |
| Boss Teleport | Vùng thẻ 1100×440 | 1280×560 | Dùng layout ngang, giữ nút đóng ngoài các thẻ. Kiểm tra focus, lựa chọn khả dụng và mở từ world tương ứng. |
| Dev Panel | 440×560 | Tối đa 560×720 | Áp dụng safe area cho công cụ QA, giữ danh sách cuộn. Kiểm tra bật qua Dev Mode. |

TutorialOverlayRoot, QuestTracker và tooltip item là overlay phụ. Chúng dùng cùng quy tắc
safe area và cỡ chữ, giữ kiểu neo riêng. Kiểm tra tooltip clamp sát bốn góc, tutorial và tracker
không đè lên vùng thao tác khi modal mở. Main Menu, Loading và Intro là bố cục toàn màn hình.
Chapter/scene transitions và HUD được kiểm tra hồi quy khi đổi Canvas.

## Trình tự thực hiện

1. Đo lại các instance chưa có số liệu và chụp baseline. Ghi CanvasScaler, scale của toàn bộ
   chuỗi cha, kích thước viewport thực tế, safe area, đối tượng xuất hiện chồng lớp và prefab override.
   Đo cho entry point DemoScene và production, kể cả Canvas riêng hoặc UI dựng lúc chạy.
2. Tạo khung dùng chung và chuyển Inventory + Shop trong DemoScene. Chụp cạnh nhau cùng một item
   và stack 99. Chốt kích thước ô, badge và typography sau khi xem ở kích thước thật. Đây là mẫu
   cho Crafting và Farm Storage.
3. Chuyển Crafting, Farm Storage, Character, Quest Log, Skill Panel và Full Map. Với từng UI,
   chuyển prefab nguồn cùng builder tương ứng, bỏ các override scene đã được thay thế sau khi
   kiểm tra các binding còn lại.
4. Chuyển Pause, Settings, Save Slot Picker, Quest Offer, các hộp xác nhận, Dialogue, Fishing,
   World Message, Boss Teleport và Dev Panel. Kiểm tra modal stack, Back/Escape và focus.
5. Kiểm tra trên MapNhat từ cả New Game và Continue. Kiểm tra thêm trên một world sử dụng
   gameplay UI chung. Dùng prefab/installer dùng chung theo quy trình DemoScene của dự án.

## Điều kiện hoàn tất cho mỗi UI

- Kiểm tra 1920×1080, 1280×720, 1920×1200 và 2560×1080. Khung và vùng thao tác nằm trong
  safe area, nền phủ viewport, không tràn chữ, không méo art và không che nút/cuộn/thanh trạng thái.
- Chạy ít nhất một luồng mở, thao tác và đóng bằng chuột cùng một luồng Back bằng bàn phím.
  Dùng nội dung rỗng, tên dài, danh sách dài và số lượng lớn để kiểm tra layout động.
- Xác minh prefab vẫn nối đúng controller, dữ liệu, input và callback. Mọi thay đổi thuộc
  presentation, sử dụng các service và quy tắc save/progression hiện hành.
- Lưu ảnh trước/sau và kết quả test của từng UI. Sau khi chủ dự án duyệt chuẩn kích thước,
  ghi quyết định vào Decision Register trước hoặc cùng đợt sửa prefab đầu tiên.


## Triển khai Inventory và Shop

`PopupViewportFit` áp dụng vùng mục tiêu 1600×900 theo hệ tham chiếu 1920×1080, giới hạn
trong giao của Canvas pixel rect và safe area. Component chỉ đổi vị trí và scale đồng đều của
board hiện có; không đổi tọa độ các object con, input hoặc callback. Bước này giữ CanvasScaler
hiện hành để các UI khác chưa chuyển vẫn dùng bố cục cũ.

Component được gắn trên InventoryPanel và ShopWindow của prefab nguồn. DemoScene và các
scene dùng những prefab này nhận cùng thay đổi. Khi màn hình là 1920×1080 và không có inset
hệ thống, board Inventory dự kiến khoảng 1599×900, board Shop khoảng 1350×900 vì giữ tỷ lệ
ảnh gốc. Kích thước ô và cỡ chữ dùng chung sẽ được chốt sau khi chủ dự án kiểm tra bước này.

## Đợt cửa sổ lớn tiếp theo

Crafting, Farm Storage, Character, Quest Log và Skill Panel đã nhận cùng vùng mục tiêu
1600×900 trong prefab nguồn và công cụ authoring tương ứng. Đây vẫn là bước co đồng đều
của board có sẵn. Skill Panel co theo cả Composition và tooltip bám theo hệ tọa độ của
Composition để giữ đúng vị trí khi board bị scale. Các layout con và kích thước ô dùng
chung sẽ được xử lý ở đợt chuyển anchor/layout.

Full Map hiện vẫn phủ toàn màn hình theo D-053. Chuyển Full Map sang khung giữa 1600×900
sẽ thay đổi vùng xem và trải nghiệm kéo bản đồ, nên hạng mục này cần quyết định riêng
giữa giữ toàn màn hình và áp dụng khung có lề. Các cửa sổ mới chuyển chưa qua kiểm thử
Unity. Phần kiểm thử do chủ dự án thực hiện.

## Đợt popup nhỏ

Đã gắn khung co đồng đều cho Pause, Settings gameplay, Quest Offer, Dialogue v2, Fishing
Minigame và Result, World Message, Boss Teleport, Dev Panel, cùng các hộp xác nhận trong
gameplay và Main Menu. Dialogue neo dưới và Dev Panel neo góc trên trái trong safe area.
Fishing Result giữ vị trí phía trên trung tâm. Boss Teleport dùng vùng 1280×700 cho toàn
panel vì vùng thẻ 1100×440 còn cần chỗ cho tiêu đề và nút xác nhận ở đáy.

Save Slot Picker và Settings của Main Menu vẫn cần chuyển bố cục toàn trang sang vùng
nội dung riêng. Full Map giữ quyết định D-053. Đợt này giữ bố cục con và chưa đổi CanvasScaler
toàn cục. Kiểm thử trong Unity do chủ dự án thực hiện.

## Save Slot Picker, Main Menu Settings và số lượng GridSlot

- Save Slot Picker dùng vùng 1440×810 ở Main Menu cho cả New Game và Continue, và 1440×810 trong gameplay. Main Menu giữ
  ba thẻ và bố cục gốc 1920×1080 làm hệ tọa độ nội dung. Gameplay dùng hệ tọa độ 776×430
  theo vùng tham chiếu 16:9 hiện tại. Back trong Main Menu dùng khung 440×124, nằm giữa bên dưới ba thẻ ở cả New Game và Continue. Khung tiêu đề mỗi slot dùng 358×126 và nhãn căn giữa phần mặt bảng. Khi chọn Save hoặc Load, khung Pause ẩn. Back hoặc Cancel đóng trình chọn slot và hiện lại Pause.
- Settings Main Menu dùng vùng 560×720. Bố cục gốc nằm trong rect 900×950 để tiêu đề và
  nội dung co cùng nhau. Settings gameplay đã dùng cùng vùng mục tiêu.
- Badge Inventory, Shop và Crafting được chỉnh theo tỷ lệ board sau khi fit, nhắm cỡ chữ
  hiệu dụng khoảng 24 đơn vị ở 1920×1080 và dùng cùng nền nâu đậm. Đây là kích thước dự
  kiến từ hình học prefab, chờ kiểm tra trong game với số 1, 10 và 99.
- Lưới hiện tại vẫn dùng kích thước gốc riêng: Inventory 40, Shop 104, Crafting 80,
  Farm Storage 48. Để áp dụng cell 96 và gap 16 thống nhất, cần chuyển viewport và viền
  panel sang layout động. Inventory có vùng ảnh chứa bố cục lưới cố định. Farm Storage
  có hai lớp ô trống và ô chứa item cần đổi cùng lúc để icon vẫn khớp viền.
- Kiểm thử Unity do chủ dự án thực hiện. Chú ý New Game, Continue, Save As, Overwrite,
  Delete và nút Back ở Settings. Full Map vẫn theo D-053.

## Đồng bộ GridSlot theo viewport

Inventory, Shop, Crafting và hai lưới Farm Storage giờ lấy ô mục tiêu 96 và khoảng cách 16
trên hệ tham chiếu 1920×1080. Chuyển các số này về tọa độ của board hiện có. Lưới dùng tối đa
sáu cột và giảm cột nếu viewport hẹp, các hàng còn lại cuộn trong viewport. Khi vừa thiếu
chỗ cho một cột, ô có thể co nhẹ để giữ cột đó. Nội dung Farm Storage và lớp ô trống dùng
cùng quy tắc cột và chiều cao cuộn. Farm Storage căn trái cả hai lớp để stack ít item vẫn
thẳng với viền ô trống.

Phần bố cục header, footer và panel art vẫn dùng board hiện có. Kiểm thử Unity do chủ dự án
thực hiện, tập trung vào cột cuối, hàng cuối, kéo thả và stack 99 trong từng lưới.

## Anchor cho Inventory và Shop

Inventory: tiêu đề/nút đóng bám trên, các tab và Quick Assign lần lượt bám trên/dưới,
stats và tiền bám đáy, khung và viewport của lưới co theo vùng bên phải.

Shop: tiêu đề hai cột và nút đóng bám trên. Viewport mua, vùng thả bán và CompactInventory
co theo hai cột. Tab, thông tin giao dịch, nút thao tác và tiền bám dưới. ScrollRect, mask,
thứ tự vẽ và binding vẫn dùng các object gốc.

Đợt này bảo toàn hình học ở board gốc 680×382.70334 và 1536×1024 bằng cách quy đổi anchor
và offset. Các builder tái tạo cùng quy tắc neo khi dựng lại prefab. Ảnh board và các chuỗi
nút/ô có art cố định vẫn dùng kích thước đã author, nên đổi mạnh tỷ lệ khung vẫn cần chuyển
ảnh panel sang các phần co giãn riêng. CanvasScaler toàn cục chưa chuyển.
Kiểm thử Unity do chủ dự án thực hiện.

## Anchor cho Crafting và Farm Storage

Crafting dùng anchor cho header, danh sách recipe, chi tiết và nguyên liệu, vùng túi và các
nút ở đáy. Farm Storage dùng hai cột co theo thân cửa sổ, mỗi cột giữ header và vùng cuộn
riêng. Cả hai builder giữ các anchor này khi dựng lại prefab. Nút đóng Farm Storage được
đưa vào trong viền để khớp vùng an toàn của board. Các nhóm khác giữ hình học ở kích thước gốc.

Ảnh nền panel và CanvasScaler toàn cục chưa chuyển. Kiểm thử Unity do chủ dự án thực hiện.

## CanvasScaler của các popup có Canvas riêng

Dialogue v2, Quest Offer, Farm Storage, Skill Panel, Inventory Discard, Fishing, World Message
và Boss Teleport dùng `Scale With Screen Size`, tham chiếu 1920×1080, chế độ Expand.
Prefab, builder và các popup tạo lúc chạy dùng cùng cấu hình. Khung popup vẫn tự fit theo safe area.

GameplayUIRoot còn ở 800×600 vì nó chia Canvas với HUD, QuestTracker và các overlay dùng hệ
tọa độ cũ. Cần chuyển các vùng đó trước khi đổi Canvas gốc. Main Menu vẫn giữ Canvas hiện có.
Các ảnh board Shop/Crafting/Farm Storage chứa viền và vách ngăn trong cùng sprite. Đổi ngay
sang 9-slice toàn board sẽ làm vách ngăn lệch khỏi vùng nội dung, nên giữ art hiện có cho đến
khi thay bằng khung ngoài và panel con riêng. Kiểm thử Unity do chủ dự án thực hiện.

## Hoàn tất đợt triển khai

- Canvas dùng chung GameplayUIRoot và Main Menu đã dùng 1920×1080 với Expand. Gameplay
  đặt HUD và overlay cũ vào một vùng tương thích lúc chạy để giữ độ lớn ở 16:9 và thứ tự
  sibling. Prefab và các builder vẫn dùng hierarchy authoring hiện có. Các popup đã chuyển
  tiếp tục fit theo kích thước hiển thị chuẩn; tooltip và kéo Full Map chuyển đổi pointer qua
  tọa độ địa phương sau khi scale. Full Map vẫn phủ toàn màn hình theo D-053.
- Inventory, Shop, Crafting và Farm Storage dùng lại sprite gốc. Khi đổi kích thước board,
  cơ chế vẽ chia ảnh thành các vùng viền, vách và thân để co giãn riêng, không cần tách file
  ảnh hoặc kéo lại các object chỉ vì viewport đổi. Bố cục gốc vẫn dùng mesh gốc của Image.
- Phần triển khai đã đưa vào nguồn. Chưa chạy kiểm thử Unity theo phân công của chủ dự án.
  Ưu tiên kiểm tra HUD không chồng, lệnh mở/đóng và drag ở Inventory, Full Map pan/zoom,
  Shop/Crafting không lệch vách ở tỷ lệ rộng, và luồng New Game/Continue.

Thanh vàng của Crafting neo ở đáy CompactInventory với offset 48 để biểu tượng và số vàng nằm giữa khung footer. Prefab và builder dùng cùng vị trí.

Nút X dùng sprite `map_close_button_v1` chung. Vị trí và kích thước của nút được author theo khung của Inventory, Character Stats, Quest Log, Skills, Settings, Shop, Crafting và Farm Storage. Căn nút theo góc trên phải của khung hoặc header tương ứng, giữ hình vuông và tránh chồng lên góc trang trí. Hộp lỗi Main Menu, Boss Teleport và Dev Panel dùng cùng sprite.
