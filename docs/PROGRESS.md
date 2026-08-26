# PROGRESS — Nhật ký chức năng đã hoàn thành

> **Xong một task trong [TASKS.md](TASKS.md) → thêm một mục vào đây, cùng commit.**
>
> File này trả lời câu hỏi: *"chức năng X đã có chưa, nằm ở đâu, dùng thế nào?"*
> Đọc file này **trước khi viết code mới** để khỏi làm lại thứ đã có.
>
> Mục mới ghi ở **trên cùng** (mới nhất trước).

---

## Mẫu — copy nguyên khối này

```markdown
### T-000 · Tên chức năng
**Người làm:** @tên · **Ngày:** YYYY-MM-DD · **Commit:** `abc1234`

**Làm gì:**
Một đến ba câu, mô tả hành vi mà người chơi hoặc lập trình viên khác quan sát được.
Không mô tả lại code.

**File chính:**
- `Assets/_LAC/Scripts/.../ClassName.cs` — vai trò
- `Assets/_LAC/Data/.../Something.asset` — vai trò

**Dùng thế nào:**
Cách gọi/kết nối hệ thống này. Ví dụ ngắn nếu cần.

**Ảnh hưởng mạng:** *(bắt buộc điền — ghi "không" nếu thuần cục bộ)*
Cái gì host quyết, cái gì client tự chạy, đồng bộ gì.

**Đã kiểm chứng:**
- [ ] Chạy được trong Unity, không lỗi console
- [ ] Test với giả lập trễ 100ms
- [ ] Test 2 người chơi

**Cạm bẫy / còn nợ:**
Điều người sau cần biết. Ghi "không" nếu sạch.
```

---

## Nhật ký

<!-- MỤC MỚI NHẤT Ở NGAY DƯỚI DÒNG NÀY -->

### T-000 · Khởi tạo repo và tài liệu
**Người làm:** @HyKiet · **Ngày:** 2026-08-26 · **Commit:** `—`

**Làm gì:**
Dựng khung làm việc chung cho 3 người qua GitHub. Đặt ra cấu trúc thư mục `Assets/_LAC/`, bộ tài liệu trong `docs/`, và [CLAUDE.md](../CLAUDE.md) làm điểm vào cho mọi công cụ AI.

**File chính:**
- `CLAUDE.md` — file AI đọc đầu tiên; ba luật sắt, cấu trúc, quy trình
- `docs/ARCHITECTURE.md` — bản đồ hệ thống
- `docs/TASKS.md` — kế hoạch có ô tick
- `docs/NETCODE.md` — luật đồng bộ
- `docs/CONVENTIONS.md` — quy ước git và code
- `.gitignore` / `.gitattributes` — LFS + merge YAML của Unity

**Dùng thế nào:**
Người mới (hoặc AI mới) đọc `CLAUDE.md` → `ARCHITECTURE.md` → `TASKS.md`, nhận một task chưa có người, điền tên vào, làm.

**Ảnh hưởng mạng:** không.

**Đã kiểm chứng:**
- [x] Cấu trúc thư mục đã tạo
- [ ] Cả 3 máy clone và mở được

**Cạm bẫy / còn nợ:**
`docs/GDD.md` là bản gốc và có chỗ đã lỗi thời — bảng đối chiếu ở mục 8 của `CLAUDE.md`. Khi mâu thuẫn thì `CLAUDE.md` thắng.
