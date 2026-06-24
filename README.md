# うんちキャッチャー / POOP PANIC 🐶💩

犬がフィールドでウンチをしてしまう前に回収して、公園をクリーンに保つカジュアルアクション。
犬は**フェイント**を仕掛けてくるので、本物を見抜いて回収するのがキモ。**3回ミスでゲームオーバー。**

> 企画の元ネタは [`game_design.txt`](game_design.txt) を参照。本リポジトリはその **MVP仕様** を Unity で実装したものです。

## 遊び方

- **地面をクリック / ドラッグ** → プレイヤーがその地点へ移動
- 犬の近く（一定範囲）に行くと**ウンチを自動回収**
- **落ちる前（空中）にキャッチ**すると高得点（+200）、着地後の回収は通常得点（+100）
- 連続成功で**コンボボーナス**（+50ずつ）
- ウンチを放置（約4秒）or 取り逃すと**ミス**。**3ミスで GAME OVER**（その場でリトライ可）

### 犬の性格（フェイントの個性）

| 性格 | 特徴 |
|------|------|
| のんびり犬 (Lazy) | 遅い・本物率高め・フェイント少なめ（初心者向け） |
| いたずら犬 (Mischief) | フェイント多発でプレイヤーを翻弄 |
| ダッシュ犬 (Dash) | 予兆の前に別の場所へ走ってからする |
| 群れ犬 (Pack) | 他の犬につられて一緒にしゃがむ |
| 普通犬 (Normal) | 標準 |

時間が経つほど、犬の数が増え、フェイント率が上がり、予兆の間隔が短くなります。

## 動かし方

このリポジトリは **そのまま開ける Unity プロジェクト**です（**Unity 6000.5.0f1 / Unity 6.5 で動作確認済み**。2022.3 LTS 以降でも動くはずです）。

### 必要なアセット（Asset Store）

見た目には以下の Asset Store アセットを使用します。**ライセンス上リポジトリには含めていない**（`.gitignore` で除外）ので、各自インポートしてください。Unity で開き、**Window ▸ Package Manager ▸ My Assets** から Download / Import します。

| 用途 | アセット | 使用パス |
|------|----------|----------|
| 犬モデル | ithappy「Animals FREE」 | `Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab` |
| 公園の地面・木・草 | Simple Nature Pack | `Assets/SimpleNaturePack/Prefabs/*` |

> アセットが無くても起動はできます（`Bootstrap` が自動でプリミティブの犬・地面に**フォールバック**）。アセットを入れると本物のモデルに差し替わります。
> インポート後は **PoopPanic ▸ Create Game Scene** でシーンを作り直すと参照が焼き込まれます。
>
> ⚠️ ithappy の犬マテリアルは **URP 用シェーダー**ですが、本プロジェクトは Built-in パイプラインのため、`Bootstrap` が実行時に犬テクスチャ付き **Standard マテリアルへ自動差し替え**します（マゼンタ回避）。

### A. Unity エディタで開いて遊ぶ（おすすめ）

1. Unity Hub → **Add** → このフォルダ（`dog-poop-game`）を追加して開く
2. `Assets/Scenes/Game.unity` を開く（無ければメニュー **PoopPanic ▸ Create Game Scene** で生成）
3. **Play** を押す

シーンには空の `Bootstrap` オブジェクトが1つあるだけで、`Bootstrap.cs` が実行時にカメラ・ライト・地面・プレイヤー・犬・UI を生成します。
HUD（スコア・ミス・タイム）は `OnGUI` 描画なのでフォントアセット不要です。

### B. ビルド済みの実行ファイルで遊ぶ

メニュー **PoopPanic ▸ Build Windows**（または下記コマンド）で `Builds/Windows/PoopPanic.exe` を生成して起動。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe" `
  -batchmode -quit -nographics -projectPath . `
  -executeMethod PoopPanic.BuildTool.BuildWindows
```

> ⚠️ スクリプトは `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` を使用しています（Unity 2022.2+ の API）。
> もっと古い Unity を使う場合は `FindObjectsOfType<T>()` に置き換えてください。

## 構成

```
Assets/
├── Scenes/Game.unity     # Bootstrap オブジェクトだけの起動シーン
├── Editor/BuildTool.cs   # シーン生成＆スタンドアロンビルド（メニュー PoopPanic ▸ …）
└── Scripts/
    ├── Bootstrap.cs      # 実行時にシーン一式を生成（エントリポイント）
    ├── GameConfig.cs     # チューニング値（スコア/速度/難易度など）を集約
    ├── GameManager.cs    # スコア・ミス・難易度・ゲームオーバー・HUD(OnGUI)
    ├── Player.cs         # クリック移動・自動回収
    ├── Dog.cs            # 状態機械（Wander→Tell→本物/フェイント）・性格
    └── Poop.cs           # 生成・落下・色変化・回収/放置ミス判定
```

調整したい数値は基本的に `GameConfig.cs` に集約しています。

## ロードマップ（設計書より、今後）

- 大型犬の陰に小型犬が隠れて見えづらくなるギミック
- アイテム（消臭スプレー / 掃除機 / 犬笛 / おやつ）
- Unity Asset Store の犬・公園アセットへの差し替え
- タイトル画面・ランキング・実績、Steam 向けビルド

## 開発メモ

見た目は Unity プリミティブ（カプセル・球・キューブ）でコミカルに組んだ仮アセットです。
まず「**フェイントを見抜いて回収する気持ちよさ**」を確認するためのプロトタイプという位置づけ。
