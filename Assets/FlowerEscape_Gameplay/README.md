# Flower Escape – Màn gameplay 2 trạng thái, responsive (Bài thực hành 4)

Scene: `Assets/Scenes/FlowerEscape_Gameplay.unity`. Scene này tự được tạo lần đầu khi mở project. Muốn tạo lại thì vào **Tools > Flower Escape > Build Gameplay Scene**.

## 2 trạng thái
| Trạng thái | Nội dung | Vào bằng |
|---|---|---|
| **Combat** | Vùng chiến đấu (boss, quái, thuyền), bảng gear nhỏ ở dưới, cột booster, cột đạn bên phải | Bấm **START!** |
| **Xếp gear** | Bảng gear phóng to, thêm thanh mua gear (Refresh / START), nút +50 | Chạm vào **bảng gear** |

Khi Play có thể dùng phím tắt:
- **Space** hoặc **Tab**: đổi trạng thái.
- **1**: về Combat.
- **2**: sang Xếp gear.

Lúc chưa Play, muốn xem trước từng trạng thái thì chuột phải vào component `GameplayStateController` trong Inspector.

## Responsive (9:21 và 3:4)
- **Canvas Scaler**: chọn `Scale With Screen Size`, reference **450×1050** (khung 9:21 của Figma), chế độ **Expand**. Khung thiết kế luôn hiện trọn vẹn: màn 9:21 vừa khít, màn 3:4 rộng thêm hai bên.
- **Neo (anchor) theo vùng:**

  | Nhóm | Neo vào |
  |---|---|
  | Settings, ô Sun 165 | góc trên-trái |
  | Level / thanh tiến độ / Hard | trên-giữa |
  | Nút X, x2 | góc trên-phải |
  | Cột đạn | cạnh phải |
  | Bảng gear, thanh mua gear | đáy |

- **Bỏ quảng cáo**: banner quảng cáo ở đáy đã bị bỏ; bảng gear (Combat) và thanh mua gear (Xếp gear) hạ xuống sát đáy để dùng chỗ trống. Muốn hiện lại thì sửa `ShowAdBanner = true` trong `FlowerEscapeSceneBuilder.cs`.
- **Nền nối gương 2 bên**: trên màn rộng, ảnh nền được lật gương ra hai bên để không bị méo. Thanh tím đáy kéo full bề ngang và phủ tới mép dưới.
- **Safe Area**: tránh tai thỏ và thanh home.

Cách test: vào **Tools > Flower Escape > Add Game View Sizes**, rồi chọn *Flower Escape 9:21* hoặc *Flower Escape 3:4* ở Game view.

## Script
- `Runtime/GameplayStateController.cs`: chuyển trạng thái (mờ dần + phóng to/thu nhỏ bảng gear), xử lý Refresh.
- `Runtime/SafeArea.cs`: co UI vào vùng an toàn.
- `Runtime/UIPressFeedback.cs`: hiệu ứng nhấn nút.
- `Editor/FlowerEscapeSceneBuilder.cs` và `Editor/FlowerEscapeLayoutData.cs`: dựng scene từ 249 lớp ảnh Figma của 2 màn gốc.
- `Editor/GameViewSizeUtil.cs`: thêm độ phân giải test.
