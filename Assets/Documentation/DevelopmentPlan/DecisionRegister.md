# Architecture and Game-Design Decision Register

File này ghi các quyết định ảnh hưởng kiến trúc. `Proposed` là mặc định khuyến nghị để team có thể
tiếp tục thiết kế; phải đổi thành `Accepted` trước phase implementation liên quan.

| ID | Quyết định | Trạng thái | Mặc định đề xuất | Cần chốt trước |
|---|---|---|---|---|
| D-001 | Vai trò DemoScene | Accepted từ yêu cầu | Integration playground; feature được đóng prefab/installer rồi kéo sang scene thật | Phase 0 |
| D-002 | Số save slot | Accepted từ yêu cầu | Chính xác 3 slot | Phase 2 |
| D-003 | Continue behavior | Proposed | Mở danh sách save hợp lệ; thêm Continue Last sau | Phase 3 |
| D-004 | New Game overwrite | Proposed | Slot có dữ liệu cần confirm rõ; không overwrite một click | Phase 2 |
| D-005 | Save khi combat | Proposed | Chỉ manual save ngoài combat/danger; UI giải thích lý do | Phase 9 |
| D-006 | Inventory pause world | Proposed | Pause world ở bản offline đầu | Phase 1 |
| D-007 | Dialogue pause world | Proposed | World chạy nhưng player input khóa; đánh giá nguy cơ bị tấn công | Phase 5/6 |
| D-008 | Tutorial skip | Proposed | Cho skip có confirm; vẫn cần Tutorial Quest để mở Main Quest | Phase 5 |
| D-009 | Tutorial Quest bắt buộc | Accepted từ yêu cầu | Không bắt buộc sandbox; bắt buộc để nhận Main Quest | Phase 6 |
| D-010 | Player death | Open | Load active slot, respawn checkpoint hay mất tài nguyên? | Phase 1/3 |
| D-011 | Initial save timing | Proposed | Ghi save sau New Game restore thành công | Phase 3 |
| D-012 | Autosave | Proposed | Chưa có ở foundation; manual save trước | Phase 9/10 |
| D-013 | Character creation scope | Open | Tối thiểu character name; appearance nếu hệ thống sẵn sàng | Phase 3 |
| D-014 | Obtain objective semantics | Open | Chốt per objective: lifetime acquired hoặc possess-at-turn-in | Phase 6 |
| D-015 | Resource respawn clock | Proposed | Lưu in-game/world timestamp; offline passage chưa áp dụng | Phase 8 |
| D-016 | Save format | Proposed | JSON versioned + backup; cân nhắc compression/encryption sau | Phase 2 |
| D-017 | Return Main Menu dirty state | Proposed | Prompt Save / Leave Without Saving / Cancel | Phase 9 |
| D-018 | Settings ownership | Accepted kiến trúc | Shared SettingsService, hai navigation UI riêng | Phase 1 |
| D-019 | Production world scene topology | Open | Chưa chốt một hay nhiều scene; save luôn dùng area/scene ID ổn định | Trước production world |
| D-020 | Data loading backend | Proposed | Domain phụ thuộc Resolver interface; Resources chỉ là backend migration ban đầu | Phase 4 |
| D-021 | Definition authoring reference | Proposed | Typed asset reference trong Inspector, stable ID tại save/runtime boundary | Phase 4–7 |

## Quy tắc cập nhật

Mỗi decision khi Accepted cần ghi:

- Ngày và người/nhóm chốt.
- Lý do.
- Hệ thống/tài liệu bị ảnh hưởng.
- Có cần migration data hoặc UI không.

Ví dụ:

```text
D-005 — Accepted — 2026-xx-xx
Manual save bị khóa khi player đang trong combat hoặc trong danger area.
Lý do: tránh restore enemy/projectile transient state phức tạp ở version đầu.
Ảnh hưởng: CombatState query, Pause save button disabled reason, QA save matrix.
```

## Những quyết định không được hard-code trước khi chốt

- Hình phạt khi chết.
- Save/load trong combat.
- Offline resource regeneration.
- Character appearance serialization.
- Daily Quest reset clock.

Code foundation nên cung cấp extension point nhưng không tự chọn gameplay rule thay designer.
