# LẠC — Children of the Dragon

**Arena survival roguelite 2D trên nền thần thoại Việt Nam.** Ba anh hùng, ba lối chơi. 15 phút một ván, co-op 2 người qua Steam.

> Đồ án tốt nghiệp · Unity 6000.5.6f1 · URP 2D · Mirror + Steamworks.NET

---

## Bắt đầu

### 1. Cài đặt một lần

```bash
git lfs install
git clone https://github.com/HyKiet/LAC.git
cd LAC
```

Cấu hình công cụ merge của Unity (**bắt buộc**, không có thì conflict scene phải sửa tay):

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver \
  '"C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

### 2. Mở dự án

Unity Hub → Add → chọn thư mục. **Bắt buộc dùng Unity 6000.5.6f1.** Bản khác sẽ nâng cấp file project và gây conflict cho cả nhóm.

### 3. Nhận việc

1. Đọc [CLAUDE.md](CLAUDE.md) — bắt buộc, kể cả khi bạn là người
2. Mở [docs/TASKS.md](docs/TASKS.md), chọn một task chưa có người
3. Điền tên mình vào, push ngay để hai người kia không làm trùng
4. Tạo nhánh: `git checkout -b feat/T-102-ten-task`
5. Làm. Xong thì tick ô + ghi [docs/PROGRESS.md](docs/PROGRESS.md) + mở PR

---

## Tài liệu

| File | Nội dung |
|---|---|
| [CLAUDE.md](CLAUDE.md) | **Đọc đầu tiên.** Game là gì, ba luật sắt, cấu trúc, quy trình |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Bản đồ hệ thống, ai gọi ai |
| [docs/TASKS.md](docs/TASKS.md) | Kế hoạch 16 tuần có ô tick |
| [docs/PROGRESS.md](docs/PROGRESS.md) | Nhật ký chức năng đã xong |
| [docs/NETCODE.md](docs/NETCODE.md) | Luật đồng bộ — đọc trước khi động vào mạng |
| [docs/CONVENTIONS.md](docs/CONVENTIONS.md) | Quy ước git, code, và cách làm việc với AI |
| [docs/GDD.md](docs/GDD.md) | Game Design Document gốc (có chỗ đã lỗi thời — xem mục 8 của CLAUDE.md) |

---

## Ba luật sắt

1. **Không có "chế độ chơi đơn" riêng.** Chơi đơn = Mirror host mode với 1 client.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn.
3. **Không `UnityEngine.Random` trong gameplay.** Dùng `RunRandom` có seed.

Chi tiết: [CLAUDE.md](CLAUDE.md) mục 3.

---

## Nhóm

| | |
|---|---|
| @HyKiet | Code · sản phẩm · marketing |
| @dev2 | Code |
| @dev3 | Code |
| @artist | Pixel art · VFX · âm thanh |
