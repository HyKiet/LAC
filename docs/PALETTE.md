# Bảng màu Đông Hồ — đã chốt (T-17)

**Tài sản:** `Assets/_LAC/Art/Palettes/DongHo24.asset` (đọc được từ mã) ·
`DongHo24.gpl` (Aseprite, Krita) · `DongHo24.png` (bảng tra) · `DongHo24.json` (nguồn)

**Mật độ pixel đã chốt: 32 px, PPU 32.** Một ô lát = 32 px = 1 đơn vị Unity.
Nhân vật cao khoảng 26 px ≈ 0.81 đơn vị.

---

## 1. Bảng màu bắt nguồn từ đâu

Tranh Đông Hồ truyền thống chỉ dùng **năm màu gốc**, đều lấy từ vật liệu tự nhiên.
Bảng 24 màu dưới đây là năm nhóm đó, mỗi nhóm kéo thành một dải sáng tối, cộng một
nhóm nâu pha:

| Nhóm | Vật liệu gốc | Số màu | Dùng cho |
|---|---|---|---|
| **Điệp** | vỏ điệp nghiền, quét lên giấy dó | 4 | da, vải sáng, điểm bắt sáng |
| **Than** | than lá tre, than rơm | 4 | nét viền, bóng, nền sân |
| **Hoè** | nụ hoa hoè | 4 | nhạc cụ, kim loại, số sát thương |
| **Chàm** | lá chàm và gỉ đồng | 5 | quái, hiệu ứng người chơi, nước |
| **Nâu** | đất pha mực | 3 | áo, gỗ, gạch |
| **Son** | sỏi son | 4 | **chỉ dành cho đòn tấn công của địch** |

### 24 màu

| # | Tên | Hex | Nhóm |
|---|---|---|---|
| 1 | DiepSang | `#F4EADA` | Điệp |
| 2 | Diep | `#E0CFAF` | Điệp |
| 3 | DiepBong | `#BFA981` | Điệp |
| 4 | DiepSau | `#94805C` | Điệp |
| 5 | ThanNhat | `#6E6555` | Than |
| 6 | Than | `#47403A` | Than |
| 7 | ThanDam | `#2B2724` | Than |
| 8 | ThanSau | `#15130F` | Than |
| 9 | HoeSang | `#FBDD82` | Hoè |
| 10 | Hoe | `#EDBB3E` | Hoè |
| 11 | HoeDam | `#C08D20` | Hoè |
| 12 | HoeSau | `#8A5F14` | Hoè |
| 13 | ChamSang | `#9CCFC0` | Chàm |
| 14 | GiDong | `#4FA694` | Chàm |
| 15 | Cham | `#2F7480` | Chàm |
| 16 | ChamDam | `#1C4B5C` | Chàm |
| 17 | ChamSau | `#112E3E` | Chàm |
| 18 | NauSang | `#B37F4F` | Nâu |
| 19 | Nau | `#7F5432` | Nâu |
| 20 | NauDam | `#50331E` | Nâu |
| 21 | SonSang | **`#FF7A55`** | **Son — ĐÒN ĐỊCH** |
| 22 | Son | `#E23B26` | **Son — ĐÒN ĐỊCH** |
| 23 | SonDam | `#A82018` | **Son — ĐÒN ĐỊCH** |
| 24 | SonSau | `#6B1113` | **Son — ĐÒN ĐỊCH** |

---

## 2. Màu dành riêng cho đòn địch: `SonSang #FF7A55`

Mục 2.1 của CLAUDE.md yêu cầu dành riêng **một** màu. Ở đây dành riêng **cả nhóm son
bốn màu**, vì đòn địch cũng cần sáng tối để đọc ra hình khối; một màu phẳng không đủ.
`SonSang` là màu chuẩn, ba màu còn lại là dải đổ bóng của nó.

Đổi lại: **nhân vật, quái, ô lát, giao diện và mọi hiệu ứng của người chơi không được
dùng bất kỳ màu nào trong nhóm son.** Còn 20 màu, thừa cho pixel art ở mật độ này.
`PaletteData.IsReservedForEnemy` cho phép tự kiểm bằng mã.

### Vì sao là `SonSang` chứ không phải `Son`

Đo trên nền sân đình (`ThanDam #2B2724`):

| | trên nền sân | mô phỏng mù màu đỏ-lục |
|---|---|---|
| `Son #E23B26` | 3.45 : 1 | 4.36 : 1 |
| **`SonSang #FF7A55`** | **5.76 : 1** | **6.77 : 1** |

Và đây là con số quan trọng: **`Son` so với `Cham` chỉ đạt 1.24 : 1 về độ chói.** Hai
màu đó chỉ khác nhau ở sắc, không khác ở độ sáng — đúng kiểu cặp màu mà khoảng 8% nam
giới không phân biệt được. Nếu chỉ dựa vào "đỏ khác lam" thì với một phần người chơi,
đòn địch và hiệu ứng người chơi là cùng một thứ.

**Quy tắc rút ra: đòn địch phải khác hiệu ứng người chơi ở ĐỘ CHÓI, không chỉ ở sắc.**
Đòn địch vẽ đặc bằng `SonSang`, ở lớp trên cùng. Hiệu ứng người chơi vẽ bằng nhóm chàm,
alpha thấp, chế độ additive, lớp dưới.

---

## 3. Trần độ mờ của sóng âm — con số mà T-16 phải chờ

Sóng âm vẽ additive nên **các lớp cộng dồn độ sáng**. Đo độ tương phản giữa đòn địch
`SonSang` và vùng sóng âm `ChamSang` chồng nhau trên nền sân:

| alpha mỗi lớp | 1 lớp | 2 lớp | 3 lớp | 4 lớp |
|---|---|---|---|---|
| 0.08 | 4.61 | 3.62 | 2.83 | 2.23 |
| 0.10 | 4.35 | 3.20 | 2.36 | 1.77 |
| 0.12 | 4.09 | 2.83 | 1.98 | 1.42 |
| 0.16 | 3.62 | 2.23 | 1.42 | 1.04 |
| 0.20 | 3.20 | 1.77 | 1.05 | — |

Lấy ngưỡng 3 : 1 làm mức "còn đọc được":

| Số lớp chồng nhau | alpha tối đa mỗi lớp |
|---|---|
| 1 | 0.220 |
| 2 | 0.110 |
| 3 | **0.070** |
| 4 | 0.055 |
| 5 | 0.040 |

**Chốt: `SoundWave._peakAlpha = 0.07`, trần Inspector `0.22`.**

0.07 là mức an toàn khi ba lớp chồng nhau — mỗi lần khai hoả đã phát ba vòng lệch pha,
nên ba lớp là tình huống thường, không phải trường hợp xấu nhất hiếm gặp. Trần 0.22 là
mức mà **một** lớp đơn độc đã chạm ngưỡng; quá đó thì dù chỉ một vòng cũng nuốt mất
đòn địch, nên Inspector không cho vượt.

Giá trị cũ 0.11 (ghi ở T-16 là "an toàn tạm thời, không phải kết luận") thực tế **quá
cao**: ở ba lớp nó chỉ còn 2.36 : 1.

---

## 4. Nền sân phải tối — một ràng buộc, không phải lựa chọn thẩm mỹ

Toàn bộ phép tính ở mục 2 và 3 giả định nền sân có độ chói khoảng **0.021**
(`ThanDam #2B2724`). Bản vẽ đầu của tileset Sân Đình dùng `Nau #7F5432` cho thân gạch;
trên màn hình nhân vật gần như biến mất, và mọi con số tương phản ở trên đều sai.

**Ô lát nền phải nằm trong nhóm than, không được dùng nhóm nâu làm màu chủ đạo.**
Nâu chỉ được dùng làm điểm lốm đốm thưa. Đây là điều kiện để bảng màu hoạt động.

---

## 5. Việc còn lại cho hoạ sĩ

Bảng màu đã chốt, không đổi nữa. Ba việc tiếp theo dùng nguyên bảng này:

- **T-33** — sprite Gióng, sprite Tấm, 40 icon thẻ
- **T-47** — sprite bốn quái còn lại, Chằn Tinh, trống đồng, hai tileset còn lại
- Đòn tấn công của địch: chưa có quái nào đánh tầm xa, nên `SonSang` **chưa từng xuất
  hiện trên màn hình**. Con số 5.76 : 1 là tính toán, chưa phải quan sát. Phải kiểm lại
  bằng ảnh chụp khi có quái bắn đầu tiên.

Nạp `DongHo24.gpl` vào Aseprite: *Palette → Load Palette*. Đừng lấy màu bằng cách hút
từ ảnh chụp màn hình — sóng âm additive làm sai màu gốc.
