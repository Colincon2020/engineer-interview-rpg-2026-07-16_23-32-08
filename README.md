# エンジニア転生 〜1週間の特訓〜

転生したエンジニアが **1週間の特訓** でプログラミングスキルを磨き、最終日に面接へ挑む 2D 育成／面接シミュレーションゲームです。

別名（面接データ上のタイトル）: **まっしろから始めるエンジニア転生記**

## 概要

| 項目 | 内容 |
|------|------|
| ジャンル | 2D 育成 RPG／面接シミュレーション |
| エンジン | Unity 6（`6000.5.3f1`） |
| レンダリング | Universal Render Pipeline (URP) |
| 想定ビルド | Windows / Web / Mac（要モジュール） |

プレイヤーは限られた **ライフポイント** と **メンタル** を使い分けながらスキルを伸ばし、人事・技術・社長の面接を経てスコアと年収評価を受けます。

## ゲームの流れ

```
タイトル（StartScence）
    ↓ Enter
特訓（Training / Action）
    ↓ 7日経過
面接（InterviewScene）
    ↓
結果（EndScene）
```

1. **特訓フェーズ（7日間）**  
   各スキルを練習して習熟度を上げる。就寝で翌日へ進み、ライフは回復する（メンタルは回復しない）。
2. **面接フェーズ**  
   人事面接官 → 技術面接官 → 社長面接官の順に挑戦。
3. **結果**  
   スコアに応じたランクと年収が表示される。

## 主なシステム

### リソース

| リソース | 上限 | 回復 |
|----------|------|------|
| ライフポイント | 5 / 日 | 就寝で最大まで回復 |
| メンタル | 10（通算） | 回復なし |

- 練習は 1 回あたりライフ 1 を消費し、対象スキルが +1。
- ライフが尽きたあとも練習を続けるとメンタルを消費する。
- メンタルが 0 になるとゲームオーバー（`EndScene` へ遷移）。

### 練習できるスキル（10種）

Java / SQL / C# / C++ / C / アセンブリ / Python / VBA / Swift / JavaScript

### 面接

| 面接官 | データファイル | 内容の目安 |
|--------|----------------|------------|
| 人事 | `hr_interviewer.json` | 人柄・メンタル寄りの質問 |
| 技術 | `tech_interviewer.json` | 技術力を問う質問 |
| 社長 | `ceo_interviewer.json` | 多めの総合質問 |

質問は `Assets/Resources/Question/` 配下の JSON から読み込み、出題のランダム化や難易度ウェイトによる採点に対応しています。

採点の概要（`interview_meta.json`）:

- 合計スコア上限: 625
- 年収: `3,000,000 + (totalScore / 625) × 7,000,000` 円（最大 1,000 万円）
- ランク: S / A / B / C など

## プロジェクト構成

```
EngineerTensei/
├── Assets/
│   ├── GameAssets/          # キャラクター・背景などの画像素材
│   ├── Resources/Question/  # 面接 JSON（メタ・各面接官）
│   ├── Scenario/            # 仕様・作戦会議メモ
│   ├── Scenes/              # Unity シーン
│   │   ├── StartScence.unity
│   │   ├── TrainingScene.unity
│   │   ├── ActionScene.unity
│   │   ├── InterviewScene.unity
│   │   └── EndScene.unity
│   └── Scripts/             # C# スクリプト
│       ├── Player.cs
│       ├── Practice.cs
│       ├── SkillType.cs
│       ├── SkillSheetUI.cs
│       ├── *Interviewer.cs
│       ├── *SceneController.cs
│       └── Question/        # 面接データのモデル・ローダー
├── Packages/
├── ProjectSettings/
└── README.md
```

### 主要スクリプト

| スクリプト | 役割 |
|------------|------|
| `Player.cs` | 日数・ライフ・メンタル・スキル習熟度の管理 |
| `Practice.cs` | 練習の消費ルールと実行ロジック |
| `SkillType.cs` | スキル種別と表示名 |
| `SkillSheetUI.cs` | スキルシート UI |
| `InterviewDataLoader.cs` | 面接 JSON の読み込み |
| `HrInterviewer` / `TechnicalInterviewer` / `PresidentInterviewer` | 各面接官の進行 |
| `StartSceneController` / `ActionSceneController` | シーン遷移・画面制御 |

## 開発環境のセットアップ

1. [Unity Hub](https://unity.com/download) をインストールする。
2. **Unity 6.5**（エディタバージョン `6000.5.3f1`）を入れる。
3. 必要に応じてビルドサポートを追加する。
   - Windows Build Support
   - WebGL Build Support
   - Mac Build Support（IL2CPP）など
4. 本リポジトリをクローンし、Unity Hub からプロジェクトを開く。

```bash
git clone <repository-url>
```

`Library/` や `Temp/` などは `.gitignore` 済みです。初回起動時に Unity が再生成します。

## 操作（現状）

| 操作 | 内容 |
|------|------|
| Enter | タイトル画面からゲーム開始 |

※ 特訓・面接画面の操作はシーン上の UI に依存します。

## データ編集のヒント

面接の質問や採点ルールを変えたい場合は、次の JSON を編集してください。

- `Assets/Resources/Question/interview_meta.json` … 採点・ランク・年収
- `Assets/Resources/Question/hr_interviewer.json` … 人事
- `Assets/Resources/Question/tech_interviewer.json` … 技術
- `Assets/Resources/Question/ceo_interviewer.json` … 社長

仕様メモは `Assets/Scenario/シナリオ.txt` にあります。

## 今後の予定（メモ）

- タイトル画面の完成
- スコアランキング送信
- イベント追加
- 背景バリエーションの拡充

## ライセンス

未設定です。公開・再配布する場合は別途ライセンスを決めます。
