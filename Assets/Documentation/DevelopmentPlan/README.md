# Sandbox RPG Development Plan

Đây là bộ tài liệu nguồn chuẩn để phát triển luồng nền tảng của project. Khi một quyết định thay đổi,
cập nhật tài liệu liên quan trước hoặc cùng lúc với code.

## Tầm nhìn đã thống nhất

- Game offline sandbox RPG, không chơi theo màn.
- `DemoScene` là integration playground để dựng và test mọi chức năng, không phải scene production.
- `MainMenu` là scene riêng; các world scene production về sau nhận feature bằng prefab/installer.
- Pause/Inventory/Settings trong DemoScene/world scene chỉ là gameplay UI overlay.
- Có đúng ba save slot ở phiên bản đầu.
- New Game tạo nhân vật mới và spawn tại Tutorial Area.
- Continue tải slot cũ và spawn tại area/vị trí đã lưu.
- Tutorial không bắt buộc để khám phá sandbox, nhưng chuỗi Tutorial Quest là điều kiện mở Main Quest.
- Daily Quest nằm ngoài phạm vi nền tảng đầu tiên.

## Thứ tự đọc

1. [Roadmap](Roadmap.md): phase, dependency, deliverable và điều kiện hoàn thành.
2. [Runtime Architecture](RuntimeArchitecture.md): scene flow, manager boundary và lifecycle.
3. [DemoScene Workflow](DemoSceneWorkflow.md): chuẩn dựng, đóng gói và kéo feature sang scene khác.
4. [Data-Driven Development](DataDrivenDevelopment.md): definition/runtime/save, ID, catalog, handler và validation.
5. [Save and World Persistence](SaveAndWorldPersistence.md): ba slot, schema, atomic write và restore order.
6. [Tutorial and Quest Progression](TutorialAndQuestProgression.md): tutorial tự do, quest gate và dữ liệu lưu.
7. [UI and Interaction Flows](UIAndInteractionFlows.md): Main Menu Scene so với gameplay overlays.
8. [Quality Strategy](QualityStrategy.md): test matrix, failure cases và Definition of Done.
9. [Decision Register](DecisionRegister.md): các lựa chọn đã chốt, đề xuất và còn mở.
10. [GameStateManager Architecture](../GameStateManager.md): implementation state coordination hiện tại và migration đích.
11. [Phase 0 Baseline Report](Phase0BaselineReport.md): snapshot kiểm tra DemoScene, Build Settings và các việc còn lại.
12. [Phase 1 Implementation Report](Phase1ImplementationReport.md): trạng thái triển khai bootstrap, state, session và scene flow.
12a. [Phase 2 Implementation Report](Phase2ImplementationReport.md): trạng thái triển khai save slot repository/file foundation.
12b. [Phase 3 Implementation Report](Phase3ImplementationReport.md): trạng thái New Game/Continue, SpawnRegistry và Player restore.
12c. [Phase 4 Implementation Report](Phase4ImplementationReport.md): trạng thái Inventory/Equipment/Stat persistence.
13. [Typography Standard](Typography.md): Digital Disco là font family chuẩn, TMP default và attribution bắt buộc.
12. [Service Ownership and Lifecycle](ServiceOwnershipLifecycle.md): inventory singleton, scene reference và lifecycle đích.
13. [Input System Inventory](InputSystemInventory.md): action maps, scene bindings và migration gate cho MainMenu.
14. [Data Asset and Stable ID Inventory](DataAssetStableIdInventory.md): toàn bộ custom ScriptableObject/catalog và chất lượng ID hiện tại.
15. [Content Validation](ContentValidation.md): cách chạy validator, rule và severity hiện có.

## Quy tắc quản trị tài liệu

- Đây là source of truth bắt buộc trước mọi thay đổi code, prefab, scene, data asset hoặc save contract.
- Trước khi triển khai, phải đọc tài liệu subsystem liên quan và đối chiếu yêu cầu mới với Decision Register.
- Nếu yêu cầu mới xung đột kiến trúc, bỏ qua dependency hoặc gây rủi ro dữ liệu/progression, phải phản biện trước khi làm.
- Không âm thầm thay đổi quyết định đã Accepted; cập nhật tài liệu và Decision Register sau khi thống nhất.
- Code, test, Unity asset, trạng thái roadmap và tài liệu phải được cập nhật đồng bộ trong cùng thay đổi khi liên quan.
- Mỗi phase chỉ bắt đầu khi dependency của nó đã đạt acceptance criteria.
- Không dùng tên asset, quest hoặc area làm ID lưu nếu ID có thể bị đổi bởi designer.
- Không thêm dữ liệu vào save mà không cập nhật `saveVersion` và migration plan.
- Không cho UI trực tiếp sở hữu save/load, world restore hoặc `Time.timeScale`.
- Một phase được đánh dấu hoàn tất chỉ sau khi có test và play-mode verification, không chỉ vì compile được.

## Trạng thái hiện tại

- `GameStateManager` nền tảng đã tồn tại và điều phối Pause/Inventory/Settings trong gameplay.
- `DemoScene` đang là nơi integration chính thức; không được đổi thành GameScene trong roadmap.
- Phase 1 foundation đã migration sang `GameState.GameplayMenu`/`GameplayMenuPage`, bootstrap explicit
  MainMenu/DemoScene và khóa gameplay input theo state.
- Return Main Menu từ Pause Menu đã load qua SceneFlowService, clear session và teardown gameplay roots
  được đăng ký trong DemoScene `_SceneContext`.
- Save hiện mới có DTO inventory thử nghiệm, chưa phải hệ thống save slot hoàn chỉnh.
- Build Settings hiện dùng `MainMenu` index 0 và `DemoScene` index 1.

## Ngoài phạm vi nền tảng này

- Daily Quest generation/reset.
- Cloud save và đồng bộ nhiều thiết bị.
- Multiplayer.
- World streaming/additive scene production.
- Mod support.
- Procedural quest generation.

Các mục trên có thể thêm sau, nhưng kiến trúc ID/version/event trong bộ tài liệu này phải không chặn chúng.
