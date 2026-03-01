# Vacuum Tool — Flow Guide

> Giải thích chi tiết cách Vacuum hoạt động trong code, dành cho người mới tìm hiểu Unity.

---

## Tổng quan

Vacuum là vũ khí 2 giai đoạn (2-phase):
- **Phase 1 (Suck):** Hút kẻ địch nhỏ vào đầu hút. Enemy bị kéo dần về + thu nhỏ → biến mất.
- **Phase 2 (Shoot):** Mỗi enemy đã hút được biến thành 1 viên đạn xoay tròn bắn ra theo hướng player đang nhìn.

---

## Các file liên quan

| File | Vai trò |
|------|---------|
| `Scripts/Tools/VacuumTool.cs` | Logic chính: detect enemy, trigger suck, bắn ra projectile |
| `Scripts/Tools/BaseTool.cs` | Class cha — quản lý cooldown, cung cấp `GetAttackDirection()`, animation callbacks |
| `Scripts/Tools/Configs/VacuumToolConfig.cs` | ScriptableObject chứa các số balance (suckDuration, shootSpeed,...) |
| `Scripts/Enemies/BaseEnemy.cs` | Hàm `GetSucked()` — enemy tự quản lý animation bị hút (smooth pull + scale down) |
| `Scripts/Player/PlayerToolHandler.cs` | Wire tool → animator, chuyển phase animation qua `SecondaryPhase` bool |
| `Scripts/Combat/ToolProjectile.cs` | Viên đạn bay thẳng, gây damage khi chạm enemy |
| `Scripts/Core/CameraShake.cs` | Rung camera khi bắn projectile |
| `Art/Animations/vacuum/` | 3 animation clips: `idle.anim`, `attack.anim` (phase 1), `phase2.anim` (phase 2) |

---

## Flow chi tiết từng bước

### Bước 0: Player equip Vacuum

```
Player nhặt Toolbox chứa Vacuum
  → GameManager gọi PlayerToolHandler.EquipTool(vacuumToolData)
    → PlayerToolHandler tìm VacuumTool component (đã gắn sẵn trên Player prefab)
    → Gọi VacuumTool.Initialize(toolData, playerController)
      → Đọc config từ VacuumToolConfig SO (suckDuration, suckRange, shootSpeed,...)
    → Swap RuntimeAnimatorController trên WeaponAnimator thành vacuumAnimator.controller
    → Wire 2 callback: OnRequestSecondaryAnimation, OnRequestPrimaryAnimation
```

**Giải thích Unity:**
- `RuntimeAnimatorController` là bộ điều khiển animation. Mỗi tool có 1 controller riêng.
  Nó chứa state machine: Idle → Phase1Attack → Phase2Attack, và các transition condition.
- Khi equip tool mới, ta "swap" controller trên Animator component để weapon visual play đúng animation.
- `OnRequestSecondaryAnimation` / `OnRequestPrimaryAnimation` là delegate (`System.Action`).
  Khi tool gọi delegate này, PlayerToolHandler sẽ set bool `SecondaryPhase` trên animator để Animator
  tự chuyển state theo transition đã setup sẵn trong controller.

---

### Bước 1: Player nhấn Attack (J) — Phase 1 bắt đầu

```
Player nhấn J (mỗi frame giữ J)
  → PlayerToolHandler.Update() kiểm tra Input.GetKey(KeyCode.J)
    → Gọi VacuumTool.CanAttack()
      → Return true nếu KHÔNG đang hút (_isSucking == false) VÀ KHÔNG đang bắn (_isShooting == false)
    → Gọi VacuumTool.Attack()
      → Set _isSucking = true
      → Set _suckTimer = suckDuration (ví dụ 1 giây)
      → Clear danh sách _suckedEnemies
      → Play _suckVFX (particle hút)
    → Gọi TriggerWeaponAttackAnimation()
      → Animator.SetTrigger("Attack") → plays attack.anim (vacuum shake/glow animation)
```

**Giải thích Unity:**
- `SetTrigger("Attack")` giống như nhấn nút 1 lần — animator tự chuyển từ Idle → Phase1Attack state.
  Trigger khác Bool ở chỗ: Trigger tự reset về false sau khi dùng, Bool phải set thủ công.
- `attack.anim` là animation clip cho vacuum lúc hút (ví dụ: weapon rung, phát sáng).
- `ParticleSystem.Play()` bắt đầu phát particle. Vì set Looping = true, nó sẽ phát liên tục cho tới
  khi gọi `.Stop()`.

---

### Bước 2: Mỗi frame trong Phase 1 — Detect & Suck enemies

```
VacuumTool.Update() chạy mỗi frame:
  │
  ├── Giảm _suckTimer -= Time.deltaTime
  │   (Time.deltaTime = thời gian từ frame trước đến frame này, thường 0.016s ở 60fps)
  │
  ├── Tính vùng detect hình chữ nhật phía trước player:
  │   origin = vị trí player + offset nhỏ phía trước (0.5 unit)
  │   direction = hướng player đang nhìn (Vector2.right hoặc Vector2.left)
  │   boxSize = (suckRange, 1) — chiều ngang = tầm hút, chiều cao = 1 unit
  │   boxCenter = origin + direction * (suckRange / 2)
  │
  │   Hình dung:
  │   Player [■]═══════════[box detect area]
  │                         suckRange units
  │
  ├── Physics2D.OverlapBoxAll(boxCenter, boxSize, angle=0, enemyLayer)
  │   → Unity kiểm tra tất cả Collider2D trong vùng chữ nhật này
  │   → Chỉ kiểm tra object thuộc _enemyLayer (tránh detect player, đạn, etc.)
  │   → Trả về mảng Collider2D[]
  │
  └── Với mỗi collider tìm được:
      ├── Lấy component BaseEnemy từ GameObject chứa collider
      ├── Kiểm tra: enemy.IsSmall == true? (chỉ hút enemy nhỏ, boss/lớn bỏ qua)
      ├── Kiểm tra: chưa có trong _suckedEnemies? (không hút trùng)
      │
      └── Nếu tất cả OK:
          ├── Add enemy.gameObject vào _suckedEnemies (tracking để bắn ra Phase 2)
          │
          └── Gọi enemy.GetSucked(transform, suckAnimDuration, suckPullSpeed, callback)
              │
              └── [CHẠY TRÊN ENEMY — BaseEnemy.GetSucked()]:
                  │
                  ├── Set _beingSucked = true (chống gọi trùng)
                  │
                  ├── Disable Rigidbody2D:
                  │   _rb.linearVelocity = Vector2.zero   ← dừng mọi chuyển động
                  │   _rb.gravityScale = 0                ← tắt trọng lực
                  │   (Nếu không tắt, enemy sẽ rơi xuống thay vì bay về vacuum)
                  │
                  ├── Disable BoxCollider2D:
                  │   _boxCollider.enabled = false
                  │   (Ngăn enemy gây contact damage cho player trong lúc bị hút)
                  │
                  └── StartCoroutine(SuckAnimationRoutine()):
                      │
                      ├── Ẩn health bar (không cần hiển thị nữa)
                      │
                      ├── Lưu originalScale = transform.localScale (thường là (1,1,1))
                      │
                      ├── ═══ VÒNG LẶP MỖI FRAME (duration = suckAnimDuration giây) ═══
                      │   │
                      │   ├── elapsed += Time.deltaTime — đếm thời gian đã trôi qua
                      │   ├── t = elapsed / duration — tỷ lệ hoàn thành (0.0 → 1.0)
                      │   │
                      │   ├── 📍 DI CHUYỂN: MoveTowards(current, target.position, pullSpeed * dt)
                      │   │   → target = VacuumTool's transform (on the Player GameObject)
                      │   │   → Di chuyển enemy TỐI ĐA pullSpeed*dt units mỗi frame
                      │   │   → KHÔNG vượt quá đích (an toàn, không overshoot)
                      │   │   → Đọc target.position MỖI FRAME vì player di chuyển
                      │   │
                      │   ├── 📏 THU NHỎ: localScale = Lerp(originalScale, Vector3.zero, t)
                      │   │   → t=0.0: scale = (1,1,1) — kích thước bình thường
                      │   │   → t=0.5: scale = (0.5,0.5,0.5) — nhỏ bằng nửa
                      │   │   → t=1.0: scale = (0,0,0) — biến mất hoàn toàn
                      │   │
                      │   └── yield return null — dừng ở đây, chạy tiếp frame sau
                      │       (Đây là cách coroutine hoạt động: mỗi yield = chờ 1 frame)
                      │
                      ├── ═══ HOÀN TẤT ═══
                      │   ├── transform.localScale = Vector3.zero (đảm bảo = 0)
                      │   ├── gameObject.SetActive(false) — ẩn enemy hoàn toàn
                      │   │   (SetActive(false) tắt toàn bộ: render, update, physics)
                      │   └── onAbsorbed?.Invoke() — gọi callback báo VacuumTool "xong rồi"
                      │
                      └── Callback → VacuumTool nhận biết enemy đã absorb
```

**Giải thích Unity:**
- `Coroutine` = hàm đặc biệt chạy "từng bước". Mỗi `yield return null` = tạm dừng, chạy tiếp frame sau.
  Giống 1 timer tự động — không block game, game vẫn chạy bình thường giữa các yield.
- `Vector3.MoveTowards(a, b, maxStep)` = di chuyển từ điểm a tới điểm b, tối đa maxStep mỗi lần.
  An toàn vì không bao giờ vượt quá b (không "trượt quá").
- Target là `VacuumTool.transform` (nằm trên Player GameObject). Không cần tạo child object
  riêng vì weapon visual hiện chỉ là 1 SpriteRenderer + Animator, không có hierarchy.
  Player transform luôn tồn tại và cập nhật vị trí mỗi frame.
- `Vector3.Lerp(a, b, t)` = nội suy tuyến tính. t=0 trả về a, t=1 trả về b, t=0.5 trả về điểm giữa.
- `transform.localScale` = kích thước hiển thị của GameObject. (1,1,1) = bình thường, (0,0,0) = biến mất.
- `gameObject.SetActive(false)` = tắt toàn bộ GameObject: không render, không chạy Update(), không physics.
  Khác với Destroy() — object vẫn tồn tại trong memory, có thể bật lại bằng SetActive(true).

---

### Bước 3: Hết thời gian hút → Chuyển Phase 2

```
_suckTimer <= 0:
  ├── _isSucking = false — kết thúc phase 1
  ├── Dừng _suckVFX.Stop() — tắt particle hút
  │
  ├── Gọi OnRequestSecondaryAnimation()
  │   → PlayerToolHandler.PlaySecondaryAnimation()
  │     → Animator.SetBool("SecondaryPhase", true)
  │       → Animator transition: Phase1Attack → Phase2Attack
  │       → phase2.anim bắt đầu play (vacuum "nhả" animation)
  │
  └── StartShooting()
      ├── Nếu _suckedEnemies.Count == 0 → không có gì để bắn
      │   → Gọi OnRequestPrimaryAnimation() → quay về Idle
      │   → Return (kết thúc ngay)
      │
      └── Nếu có enemy:
          ├── _isShooting = true
          ├── Play _burstVFX (particle nổ 1 lần)
          └── Chạy Coroutine ShootEnemiesSequence()
```

**Giải thích Unity:**
- Animator dùng `Bool` parameter (KHÔNG phải Trigger) cho SecondaryPhase vì ta cần GIỮ state
  Phase 2 suốt thời gian bắn. Bool giữ giá trị cho tới khi ta đổi lại thủ công.
- Transition condition trong Animator Controller: khi `SecondaryPhase == true`, animator tự
  chuyển từ Phase1Attack → Phase2Attack state (đã setup sẵn trong vacuumAnimator.controller).

---

### Bước 4: Bắn từng enemy ra (Phase 2)

```
ShootEnemiesSequence() coroutine:
  │
  └── Duyệt từng enemyObj trong _suckedEnemies:
      │
      ├── Null check: enemyObj != null? (enemy có thể bị destroy bởi hệ thống khác)
      │
      ├── Lấy vị trí spawn: origin = GetAttackOrigin() (phía trước player)
      │
      ├── Spawn _enemyProjectilePrefab tại origin:
      │   GameObject proj = Instantiate(prefab, position, rotation)
      │   → Tạo bản sao của prefab projectile trong scene
      │
      ├── Khởi tạo ToolProjectile component:
      │   toolProj.Initialize(direction, shootSpeed, shootDamage, pierce=false)
      │   → Set velocity = direction * speed (Rigidbody2D)
      │   → Set rotation hướng theo direction
      │   toolProj.SetRotating(true)
      │   → Bật xoay 720°/s cho visual effect
      │
      ├── Copy sprite từ enemy sang projectile:
      │   → Lấy SpriteRenderer.sprite từ enemy → gán sang projectile
      │   → Viên đạn sẽ trông GIỐNG CON ENEMY đang xoay tròn bay ra
      │
      ├── Camera shake nhẹ: CameraShake.Instance?.Shake(0.08f, 0.05f)
      │   → Rung camera 0.08 giây, biên độ 0.05 unit
      │
      ├── Destroy enemyObj: xóa enemy gốc khỏi scene
      │
      └── yield return WaitForSeconds(shootInterval):
          → Đợi 0.15 giây rồi bắn enemy tiếp theo
          → Tạo hiệu ứng "phun phun phun" thay vì bắn hết cùng lúc
```

**Giải thích Unity:**
- `Instantiate(prefab, pos, rot)` = clone prefab thành 1 GameObject mới trong scene.
  Prefab = template lưu trong Assets, instance = bản thực tế trong game.
- `WaitForSeconds(0.15f)` = coroutine dừng 0.15 giây. Game vẫn chạy bình thường
  giữa mỗi lần bắn — player thấy từng viên đạn bay ra tuần tự.
- Copy sprite: `SpriteRenderer.sprite` là hình ảnh hiển thị. Lấy từ enemy → gán sang
  projectile → viên đạn trông giống con enemy (visual feedback rõ ràng cho player).

---

### Bước 5: Bắn xong → Reset về Idle

```
Sau khi bắn hết:
  ├── Clear _suckedEnemies — xóa danh sách
  ├── _isShooting = false — cho phép Attack() lại
  │
  └── Gọi OnRequestPrimaryAnimation()
      → PlayerToolHandler.PlayPrimaryAnimation()
        → Animator.SetBool("SecondaryPhase", false)
          → Animator transition: Phase2Attack → Idle
          → Weapon visual quay về trạng thái nghỉ
```

---

## Edge Cases (Trường hợp đặc biệt)

| Tình huống | Xử lý |
|------------|--------|
| Enemy chết khi đang bị hút | `enemyObj != null` check trước khi bắn. Nếu null → skip. |
| 0 enemy hút được | `StartShooting()` gọi `OnRequestPrimaryAnimation()` rồi return ngay. |
| Player unequip giữa chừng | `OnUnequip()` gọi `StopAllCoroutines()`, reset mọi state, dừng VFX. |
| Enemy có `IsSmall = false` | `OverlapBox` tìm thấy nhưng skip vì `enemy.IsSmall == false`. |
| Player di chuyển khi hút | Enemy pull về player transform — tự động theo vị trí mới mỗi frame. |

---

## VFX Assets — Danh sách & Hướng dẫn Setup trong Unity Editor

### Asset 1: VacuumSuckVFX (ParticleSystem)

**Mục đích:** Hạt particle bay VÀO đầu hút vacuum trong Phase 1, tạo cảm giác "dòng gió hút".

**Cách tạo trong Unity:**
1. Click phải trong Hierarchy → Effects → Particle System
2. Đặt tên: `VacuumSuckVFX`
3. Save thành prefab: kéo từ Hierarchy → `Assets/_Game/Prefabs/VFX/VacuumSuckVFX.prefab`

**Settings trên ParticleSystem component:**

| Module | Setting | Value | Giải thích |
|--------|---------|-------|------------|
| **Main** | Duration | 1 | Bằng suckDuration — dùng Looping nên không quan trọng lắm |
| Main | Looping | ✅ On | Lặp liên tục suốt phase 1 |
| Main | Play On Awake | ❌ Off | QUAN TRỌNG: không tự phát khi spawn, code sẽ gọi `.Play()` |
| Main | Start Lifetime | 0.4 – 0.8 (random) | Mỗi hạt sống 0.4-0.8 giây rồi biến mất |
| Main | Start Speed | -3 to -5 (random) | **SỐ ÂM** = bay VÀO emitter (hút vào). Số dương = bay ra. |
| Main | Start Size | 0.1 – 0.3 (random) | Hạt nhỏ — gợi bụi/gió |
| Main | Start Color | `#AADDFF` (xanh nhạt) | Gợi luồng gió |
| Main | Simulation Space | World | Hạt đứng yên trong world khi player di chuyển (realistic hơn) |
| **Emission** | Rate over Time | 20–30 | Số hạt phát ra mỗi giây |
| **Shape** | Shape | Cone | Hình nón — particles bay từ miệng cone vào tâm |
| Shape | Angle | 25–35° | Góc mở nón. Nhỏ = hẹp tập trung, lớn = tỏa rộng |
| Shape | Radius | 1.5 | Kích thước miệng cone (≈ suckRange/2) |
| Shape | Emit from | Volume | Phát hạt từ bên trong cone, không chỉ bề mặt |
| **Size over Lifetime** | Enable | ✅ On | |
| Size over Lifetime | Curve | 1 → 0 (giảm dần) | Hạt nhỏ lại khi bay gần đầu hút |
| **Color over Lifetime** | Enable | ✅ On | |
| Color over Lifetime | Alpha | 1 → 0 (giảm dần) | Hạt mờ dần = biến mất mượt |
| **Renderer** | Material | Default-Particle | Material mặc định Unity (trắng, additive blend) |
| Renderer | Render Mode | Billboard | Hạt luôn quay mặt về camera (2D friendly) |
| Renderer | Sorting Layer | (same as weapon) | Hiển thị cùng layer với weapon sprite |

**Rotation Hướng nón:**
- Cone mở ra PHÍA TRƯỚC player (theo hướng nhìn)
- VFX có thể gắn làm child của weapon visual HOẶC spawn runtime tại `GetAttackOrigin()`
- Nếu gắn làm child: chú ý `flipX` không flip child → cần xử lý rotation bằng code
- Đơn giản nhất: gắn VFX là serialized field trên VacuumTool, đặt dưới Player hierarchy

---

### Asset 2: VacuumBurstVFX (ParticleSystem)

**Mục đích:** Burst nổ particles RA NGOÀI khi Phase 2 bắt đầu, tạo cảm giác "xả năng lượng" 1 lần.

**Cách tạo:** Tương tự, save thành `Assets/_Game/Prefabs/VFX/VacuumBurstVFX.prefab`

**Settings:**

| Module | Setting | Value | Giải thích |
|--------|---------|-------|------------|
| **Main** | Duration | 0.3 | Ngắn — chỉ nổ 1 lần |
| Main | Looping | ❌ Off | Không lặp — burst 1 phát |
| Main | Play On Awake | ❌ Off | Code gọi `.Play()` |
| Main | Start Lifetime | 0.2 – 0.5 | Hạt sống ngắn, biến mất nhanh |
| Main | Start Speed | 5 – 10 | Số DƯƠNG = bay RA nhanh |
| Main | Start Size | 0.15 – 0.4 | Hạt vừa — lớn hơn suck VFX 1 chút |
| Main | Start Color | `#FFAA33` (vàng/cam) | Gợi năng lượng/explosive |
| **Emission** | Rate over Time | 0 | Không phát liên tục |
| Emission | Bursts | Count = 15, Time = 0 | 15 hạt bắn ra CÁI RỤP cùng lúc |
| **Shape** | Shape | Cone | Hình nón RA phía trước |
| Shape | Angle | 20° | Hẹp hơn suck — tập trung theo hướng bắn |
| **Size over Lifetime** | Curve | 1 → 0 | Nhỏ dần |
| **Color over Lifetime** | Alpha | 1 → 0 | Mờ dần |

---

### ~~Asset 3: Nozzle Transform~~ (KHÔNG CẦN)

Weapon visual hiện tại chỉ là **1 GameObject duy nhất** (`_weaponVisualRoot`) với:
- `SpriteRenderer` — hiển thị hình weapon
- `Animator` — chạy animation clips
- Position cập nhật bởi `UpdateWeaponVisualTransform()` mỗi frame (theo `holdDistance`, `yOffset`, `facing`)
- `flipX` xử lý quay trái/phải (flip sprite, KHÔNG flip child Transform)

Nếu tạo child "Nozzle" trên weapon visual:
- `flipX` chỉ flip sprite, KHONG flip child Transform → Nozzle sẽ sai vị trí khi player quay trái
- Phải thêm code xử lý flip cho child → phức tạp không cần thiết

**Giải pháp:** Dùng `VacuumTool.transform` (= Player position) làm target.
Enemy pull về vị trí player, tự động đúng hướng bất kể player quay trái hay phải.

---

### Sơ đồ mối quan hệ giữa các file

```
┌──────────────────────┐         ┌──────────────────────┐
│  PlayerToolHandler   │         │  vacuumAnimator      │
│  ┌────────────────┐  │         │  .controller         │
│  │ _weaponAnimator ├──┼────────►  Idle                │
│  └────────────────┘  │         │  Phase1Attack        │
│  Wire callbacks:     │         │  Phase2Attack        │
│  OnRequestSecondary  │         └──────────────────────┘
│  OnRequestPrimary    │
└───────┬──────────────┘
        │ calls Attack()
        ▼
┌───────────────────┐    detect     ┌─────────────────────┐
│  VacuumTool       ├──────────────►│  BaseEnemy          │
│  _isSucking       │  GetSucked()  │  SuckAnimRoutine(): │
│  _isShooting      │◄──callback────│   MoveTowards()     │
│  _suckVFX.Play()  │              │   Scale lerp 1→0    │
│  _burstVFX.Play() │              └─────────────────────┘
│                   │    spawn      ┌─────────────────────┐
│  ShootSequence()  ├──────────────►│ToolProjectile       │
│  CameraShake      │              │ fly + rotate + dmg  │
└───────────────────┘              └─────────────────────┘

  ┌──────────────────┐
  │ VacuumToolConfig │  (ScriptableObject)
  │ suckDuration     │
  │ suckRange        │
  │ shootSpeed       │
  │ shootDamage      │
  │ shootInterval    │
  │ suckAnimDuration │  ← cho smooth pull
  │ suckPullSpeed    │  ← tốc độ kéo enemy
  └──────────────────┘
```
