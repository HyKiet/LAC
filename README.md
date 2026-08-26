# LẠC — Children of the Dragon

**Arena survival roguelite 2D trên nền thần thoại Việt Nam.** Ba anh hùng, ba lối chơi. 15 phút một ván, co-op 2 người qua Steam.

> Đồ án tốt nghiệp · Unity 6000.5.6f1 · URP 2D · Mirror + Steamworks.NET

---

## Cài một lần

```bash
git lfs install
git clone https://github.com/HyKiet/LAC.git
```

Cấu hình công cụ merge của Unity — **bắt buộc**, không có thì conflict scene phải sửa tay:

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver \
  '"C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

Mở dự án bằng Unity Hub — **phải đúng bản 6000.5.6f1**.

Mở thư mục bằng VS Code, nó sẽ hỏi cài extension → bấm **Install All**. Sau đó bấm `Alt+C` là tick được task.

---

## Bắt đầu làm

1. Đọc [CLAUDE.md](CLAUDE.md) — 10 phút, hiểu đủ để bắt đầu
2. Mở [docs/TASKS.md](docs/TASKS.md), chọn một task còn ghi *"chưa ai nhận"*
3. Điền tên mình vào, push ngay để hai người kia không làm trùng
4. Làm xong: tick ô + viết một dòng `>` nói chức năng đó làm gì
5. Commit `feat(T-11): dash có i-frame` rồi push lên `dev`

---

## Ba file cần biết

| | |
|---|---|
| [CLAUDE.md](CLAUDE.md) | **Đọc đầu tiên.** Game là gì, ba luật sắt, quy trình |
| [docs/TASKS.md](docs/TASKS.md) | Việc — ai làm gì, xong gì |
| [docs/GDD.md](docs/GDD.md) | Chi tiết thiết kế (bản gốc, có chỗ đã lỗi thời) |

*Cần biết code nằm đâu, ai gọi ai → [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)*

---

## Ba luật sắt

1. **Không có "chế độ chơi đơn" riêng.** Chơi đơn = Mirror host mode với 1 client.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn.
3. **Không `UnityEngine.Random` trong gameplay.** Dùng `RunRandom` có seed.

Chi tiết ở [CLAUDE.md](CLAUDE.md) mục 3. Vi phạm là hỏng game.

---

## Nhóm

`@HyKiet` code · sản phẩm · marketing — `@dev2` code — `@dev3` code — `@artist` pixel art · VFX · âm thanh
