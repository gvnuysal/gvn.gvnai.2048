# 2048 (Unity, C#)

`2048_Unity_Oyun_Analizi.pdf` belgesine göre geliştirilmiş klasik 2048: 4x4 tahta, tek oyunculu ve çevrimdışı.
Hedef platformlar sırasıyla **iOS**, **Android** ve **Windows** masaüstü.

MVP'ye ek olarak şu özellikler var:
- Swipe ile kontrol (dokunmatik ve mouse sürükleme)
- Taş animasyonları: kayma, birleşme ve yeni taş
- Kalıcı **Best** skor
- 2048'den sonra **Continue**
- Tek adımlık **Undo**

## Sürümler

| | |
|---|---|
| Unity | **6000.3.25f1** (Unity 6.3 LTS) |
| Input | **Input System 1.20.0**. Active Input Handling = *Input System Package (New)* |
| UI | uGUI + TextMeshPro (`com.unity.ugui` 2.0.0) |
| Test | Unity Test Framework 1.6.0 (NUnit) + `dotnet test` (net9.0) |
| Xcode | 26.5 (iOS 26.5 Simulator) |

## Projeyi açma

1. Unity Hub'da **Add → Add project from disk** ile bu klasörü ekle ve Unity 6000.3.25f1 ile aç.
   Gerekli modüller: iOS, Android (SDK/NDK ve OpenJDK dahil) ve Windows Build Support (Mono).
2. `Assets/Scenes/Game.unity` sahnesini aç ve **Play**'e bas.
3. Sahneyi veya prefab'ı sıfırdan üretmek için **2048 → Setup Project** menüsünü kullan.
   Bu menü PlayerSettings'i, `Tile.prefab` dosyasını, `Game.unity` sahnesini ve tüm Inspector bağlantılarını otomatik kurar.

## Kontroller

| Platform | Giriş |
|---|---|
| iOS / Android | Parmakla kaydırma (swipe). Bir hareket = bir hamle. |
| Masaüstü / Editor | Ok tuşları veya **WASD**. Mouse ile sürükleme de çalışır. |
| Butonlar | **Undo** (tek adım geri), **Restart**. Sonuç ekranında **Continue** / **Restart**. |

## Build

Tüm build'ler menüden (**2048 → Build → …**) veya komut satırından alınabilir:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath . -executeMethod Game2048.Editor.BuildScripts.<Metot> -logFile Logs/build.log
```

| Metot | Çıktı |
|---|---|
| `BuildiOSSimulator` | `Builds/iOS-Sim` (Xcode projesi, Simulator SDK, arm64) |
| `BuildiOSDevice` | `Builds/iOS` (Xcode projesi, cihaz). Signing Xcode'da Team seçilerek yapılır. |
| `BuildAndroidApk` / `BuildAndroidBundle` | `Builds/Android/2048.apk` / `.aab` (IL2CPP, ARM64) |
| `BuildWindows` | `Builds/Windows/2048.exe` (x64, Mono). macOS'tan da alınabilir. |
| `BuildMac` | `Builds/macOS/2048.app` |

**iOS Simulator için tek komut** (Unity build, xcodebuild, yükleme ve başlatma):

```bash
Tools/build-ios-sim.sh
```

`--setup` parametresi önce sahneyi yeniden üretir. Script'in çalışması için bir simülatörün açık (booted) olması gerekir.

## Testler

Kurallar `Assets/Scripts/Core` altında, Unity'ye bağımlı olmayan saf C# kodudur.
Kabul senaryolarının tamamı test ediliyor:
- [2,0,2,0] → [4,0,0,0]
- [2,2,2,2] → [4,4,0,0]
- [4,4,4,0] sağa → [0,0,4,8]
- Geçersiz hamle
- Game Over
- 2048 / You Win
- Restart
- Ek olarak Undo, Continue ve Best skor

```bash
dotnet test Tests/Core.Tests
```

Unity içindeki testler:
- EditMode: kuralların aynısı
- PlayMode: sahne yüklenir; sanal klavye, WASD ve mouse swipe ile oynanır; overlay ve Undo kontrol edilir

```bash
$UNITY -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
$UNITY -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
```

## Yapı

```
Assets/Scripts/Core/      BoardModel, GameSession, Direction, MoveResult, IRandom   (saf C#)
Assets/Scripts/Runtime/   GameManager, BoardView, TileView, InputHandler, GameUI,
                          TileColors, SafeAreaFitter, SquareFitter, PlayerPrefsScoreStore
Assets/Editor/            ProjectBootstrap (sahne/prefab/ayar üretimi), BuildScripts
Assets/Tests/             EditMode + PlayMode testleri
Tests/Core.Tests/         Unity'siz dotnet test projesi (Core ve EditMode testlerini link eder)
```

- **BoardModel**: `int[4,4]` tahta. Yönden bağımsız `SlideLine` fonksiyonu önce sıfırları çıkarır, sonra her taşı en fazla bir kez birleştirir, sonra boşlukları doldurur. `Move(Direction)` fonksiyonu bu kuralı her satır veya sütuna, yönün ön ucundan başlayarak uygular.
- **GameSession**: yeni oyun, geçerli hamle, taş üretme (%90 ihtimalle 2, %10 ihtimalle 4), skor, Best, Won/Lost durumları, Continue ve Undo. Geçersiz hamlede skor değişmez ve yeni taş eklenmez.
- **GameManager**: önce hamleyi modelde çözer, sonra görünümü günceller. Animasyon sürerken gelen yeni hamle önceki animasyonu anında bitirir.

## Dış varlıklar

- TextMeshPro Essential Resources: LiberationSans SDF fontu (Unity `com.unity.ugui` paketinden, SIL Open Font License).
- Yuvarlatılmış köşeli sprite (`Assets/Sprites/RoundedRect.png`) `ProjectBootstrap` tarafından kodla üretilir.

## Kapsam dışı (sonraki sürümler)

Ses, devam eden oyunun kaydedilip uygulama kapandıktan sonra sürdürülmesi, reklam ve çevrimiçi özellikler.
