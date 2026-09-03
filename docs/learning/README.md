# Ghi chú học code dự án LẠC

Thư mục này là “phụ đề” cho mã nguồn của LẠC, dành cho người đang học lại từ đầu. Tài liệu không thay thế code và cũng không cố bắt bạn học thuộc mọi dòng. Mục tiêu là giúp bạn trả lời được bốn câu hỏi khi mở một file:

1. File này chịu trách nhiệm gì?
2. Unity hoặc Mirror gọi nó vào lúc nào?
3. Nó nhận dữ liệu từ đâu và chuyển kết quả cho ai?
4. Vì sao dự án chọn cách làm này?

## Phạm vi đã quét

Phần code do nhóm sở hữu nằm trong `Assets/_LAC/`:

- 37 script C# và 1 shader, khoảng 3.600 dòng;
- scene chính `Assets/_LAC/Scenes/Arena.unity`;
- 6 prefab gameplay/VFX;
- 3 tài sản nhân vật, 1 tài sản quái, 1 bảng nhân vật và 1 bộ input;
- tài liệu sản phẩm, kiến trúc và kế hoạch trong `CLAUDE.md`, `README.md`, `docs/`.

Không học theo từng file trong `Assets/Mirror/`, `Library/`, `Temp/`, các file `.csproj` hay `.meta`:

- `Assets/Mirror/` là thư viện mạng bên thứ ba. Ta chỉ cần hiểu API mà LẠC đang dùng.
- `Library/`, `Temp/`, `Logs/` và các file project IDE do Unity sinh lại được.
- `.meta` chứa GUID để Unity nối tài sản, không phải logic game.
- `.unity`, `.prefab`, `.asset` là YAML do Unity tuần tự hoá. Nên chỉnh chúng qua Inspector, không học bằng cách sửa YAML tay.

## Nếu bạn chưa từng học code hoặc đã quên hoàn toàn

Đừng mở tài liệu số 01 hoặc đọc các file gameplay ngay. Hãy bắt đầu tại:

**[00 — Học code game từ con số 0](00_START_FROM_ZERO.md)**

Tài liệu số 00 không yêu cầu bạn biết C#, Unity, toán vector hay mạng. Nó giải thích bằng ví dụ đời thường, cho bạn viết những đoạn code đầu tiên và hướng dẫn đọc `ArenaBounds.cs` từng mảnh nhỏ.

## Thứ tự học đề xuất

1. [Học từ con số 0](00_START_FROM_ZERO.md): dành cho người chưa hiểu code là gì.
2. [Nền tảng C# và Unity](01_CSHARP_UNITY_FOUNDATIONS.md): chỉ đọc sau khi hoàn thành bài 00.
3. [Kiến trúc và các luồng chạy](02_ARCHITECTURE_AND_FLOWS.md): nhìn toàn cảnh trước khi đọc chi tiết.
4. [Giải thích từng file](03_FILE_BY_FILE.md): dùng như từ điển khi mở một script.
5. [Bài tập thực hành](04_PRACTICE_PLAN.md): chuyển từ “đọc hiểu” sang “tự viết và tự giải thích”.

Đừng đọc 37 file theo thứ tự bảng chữ cái. Một vòng học hợp lý là:

```text
PlayerInputReader
  -> PlayerMovement
  -> PlayerDash
  -> PlayerHealth
  -> DamageSystem
  -> Enemy
  -> WeaponAuto
  -> RunManager
  -> WaveManager
  -> EnemySpawner
```

Sau vòng này, quay lại các lớp hạ tầng (`GameEvents`, pool, registry, random), rồi mới đọc VFX/UI.

## Trạng thái thật của dự án tại thời điểm viết ghi chú

Đã có một lát cắt gameplay chạy được: host tự khởi động, sinh người chơi, sinh đợt quái, di chuyển/lướt, ba hình dạng vũ khí, sát thương do host quyết định, máu, VFX đánh trúng và HUD máu.

Chưa có các hệ thống được mô tả cho giai đoạn sau: thẻ nâng cấp, Hồn, Trống Đồng, AI Đạo Diễn, Steam lobby, hồi sinh, bảng đợt hoàn chỉnh, lưu tiến trình và âm thanh. Các thư mục tương ứng hiện chủ yếu là `.gitkeep`. Vì vậy `docs/ARCHITECTURE.md` vừa mô tả code hiện có, vừa mô tả đích kiến trúc tương lai; đừng nhầm sơ đồ thiết kế với chức năng đã triển khai.

## Ba nguyên tắc phải thuộc trước khi sửa code

- Chơi một người vẫn chạy Mirror host mode với một client. Không tạo `if (isSinglePlayer)`.
- Host giữ trạng thái thật; client dựng phần biểu diễn. Không gắn `NetworkIdentity` lên đạn hoặc quái.
- Mọi ngẫu nhiên ảnh hưởng gameplay đi qua `RunRandom`/`RandomStream`. `UnityEngine.Random` chỉ hợp lệ với VFX/âm thanh trang trí.

## Cách dùng ghi chú mà không tiếp tục phụ thuộc AI

Khi học một file, hãy làm theo vòng 20 phút:

1. Đọc mục của file trong `03_FILE_BY_FILE.md`.
2. Mở code và tự khoanh `field`, Unity callback, public method, helper method.
3. Che tài liệu đi và tự nói thành lời: “ai gọi class này, nó đổi dữ liệu gì, ai nhận kết quả?”.
4. Vẽ lại luồng bằng 5–8 ô, không nhìn mẫu.
5. Thay một giá trị an toàn trong Inspector, dự đoán kết quả, rồi Play để kiểm chứng.
6. Hoàn tác giá trị thử nghiệm nếu đó không phải thay đổi cân bằng đã được giao.

Bạn hiểu một class khi có thể giải thích nó bằng ngôn ngữ đời thường và đoán đúng chuyện gì xảy ra nếu bỏ một nhánh kiểm tra—not khi bạn nhớ cú pháp.
