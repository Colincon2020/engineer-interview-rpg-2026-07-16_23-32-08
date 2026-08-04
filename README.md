# エンジニア転生 〜1週間の特訓〜

転生したエンジニアが **1週間の特訓** でプログラミングスキルを磨き、最終日に **人事 → 技術 → 社長** の3段階面接へ挑む 2D 育成／面接シミュレーションゲームです。社長面接まで突破すると内定を獲得し、算出された年収が [unityroom](https://unityroom.com/) のランキングへ送信されます。

別名（面接データ上のタイトル）: **まっしろから始めるエンジニア転生記**

## 概要

| 項目 | 内容 |
|------|------|
| ジャンル | 2D 育成 RPG／面接シミュレーション |
| エンジン | Unity 6（`6000.5.3f1`） |
| レンダリング | Universal Render Pipeline (URP) |
| 入力 | Input System（新入力システム） |
| ランキング | unityroom スコアボード連携（年収を送信） |
| 想定ビルド | WebGL（unityroom 公開）／ Windows / Mac（要モジュール） |

プレイヤーは限られた **スタミナ（ライフポイント）** と **メンタル** を使い分けながらスキルを伸ばし、人事・技術・社長の3段階面接を突破して内定と年収を勝ち取ります。

## ゲームの流れ

```
タイトル（StartScence）
    ↓ Enter → 性別選択 → Enter
特訓（ActionScene / 7日間）
    ↓ 最終日に就寝で終了
人事面接（InterviewScene）
    ↓ 通過                     ↓ 不合格 → 特訓へ戻る
技術面接（TechInterviewScene）
    ↓ 通過                     ↓ 不合格 → 特訓へ戻る
社長面接（PresidentInterviewScene）
    ↓ 通過 → 内定パネル（年収表示＋ランキング送信）
    ↓ 不合格 → 特訓へ戻る

※ 特訓中にメンタルが尽きるとゲームオーバー（EndScene）
```

1. **タイトル（StartScence）**
   Enter でスタート後、主人公の **性別（男性／女性）** を選択して特訓へ。性別は立ち絵・顔アップ・面接キャラ画像に反映される。
2. **特訓フェーズ（7日間 / ActionScene）**
   ドロップダウンで言語を選んで練習し、習熟度を上げる。スタミナに応じて背景（朝／夕／夜）と表情が変化する。夜（スタミナ1以下）になると就寝ボタンが出現し、就寝で翌日へ進みスタミナが回復する（メンタルは回復しない）。
3. **面接フェーズ（3段階）**
   人事 → 技術 → 社長の順に、それぞれ専用シーンで挑戦。各段階で通過ラインを超えれば次の面接へ、下回ると特訓（ActionScene）へ戻される。
4. **結果**
   社長面接を通過すると内定パネルが表示され、年収を提示。その年収を unityroom のランキングへ送信する。

## 主なシステム

### リソース

| リソース | 上限 | 回復 |
|----------|------|------|
| スタミナ（ライフポイント） | 5 / 日 | 就寝で最大まで回復 |
| メンタル | 10（通算） | 回復なし |

- 練習は 1 回あたりスタミナ 1 を消費し、対象スキルが +1。
- スタミナが尽きたあとも練習を続けると、代わりにメンタルを 1 消費する。
- 練習中にメンタルが 0 になるとゲームオーバー（`EndScene` へ遷移）。
- 特訓終了時点のメンタルとスキルは `GameSession` に保持され、面接へ引き継がれる。

### 練習できるスキル（10種）

Java / SQL / C# / C++ / C / アセンブリ / Python / VBA / Swift / JavaScript

各言語には年収計算用の **需要重み係数** が設定されている（例: Python・アセンブリ 1.5 / C++ 1.4 / JavaScript・Swift 1.3 …）。

### 面接（3段階）

| 面接 | シーン | データファイル | 満点 | 通過ライン（約50%） |
|------|--------|----------------|------|----------------------|
| 人事 | `InterviewScene` | `hr_interviewer.json` | 30 | 15 |
| 技術 | `TechInterviewScene` | `tech_interviewer.json` | 75 | 38 |
| 社長 | `PresidentInterviewScene` | `ceo_interviewer.json` | 200 | 100 |

- 質問は `Assets/Resources/Question/` 配下の JSON から読み込む。1問につき3択を表示。
- 採点は `選択肢スコア × 質問の難易度ウェイト`（易 1.0 / 中 1.5 / 高 2.0）で加算。
- 通過ラインは `ceil(満点 × 152 / 305)`（各面接おおよそ 50%）。合計満点は 305。
- 得点が通過ラインに達するとスコア表示が赤白に点滅する。回答ごとに面接官のリアクション・ボイスが再生される。
- **技術面接のヒント**: 質問の `hintSkill` に対応するスキルを **レベル20以上** まで鍛えていると、正解の選択肢が赤字で表示される。
- 各段階で不合格になると結果をクリアして特訓（`ActionScene`）へ戻る。「降参」ボタンでも特訓へ戻れる。

### 年収とランキング

社長面接を通過すると `SalaryCalculator` が年収を算出し、内定パネルに表示する。

```
年収(万円) = 150（基準）
           + 面接合計スコア × 1.025
           + Σ(スキルレベル × 需要重み係数) × 4.5
```

- 面接合計スコアは人事・技術・社長の獲得スコアの合算。
- 算出した年収（万円）を `UnityroomApiClient` 経由で unityroom のスコアボード **No.1** に送信する（`Assets/unityroom/`）。

> 補足: `interview_meta.json` にも年収・ランク（S/A/B/C/D）の定義があり、`3,000,000 + (totalScore / 305) × 7,000,000` 円という別式やランク表を持つ。現在の内定画面の年収表示は上記 `SalaryCalculator`（万円ベース）を使用しており、メタ JSON のランク定義は採点ルールの参照用データとして残っている。

## プロジェクト構成

```
EngineerTensei/
├── Assets/
│   ├── GameAssets/          # キャラクター・背景などの画像素材
│   ├── Resources/Question/  # 面接 JSON（メタ・各面接官）
│   ├── unityroom/           # unityroom ランキング送信 API
│   ├── Scenario/            # 仕様・作戦会議メモ
│   ├── Scenes/              # Unity シーン
│   │   ├── StartScence.unity            # タイトル・性別選択
│   │   ├── ActionScene.unity            # 特訓（7日間）
│   │   ├── InterviewScene.unity         # 人事面接
│   │   ├── TechInterviewScene.unity     # 技術面接
│   │   ├── PresidentInterviewScene.unity # 社長面接
│   │   └── EndScene.unity               # ゲームオーバー
│   └── Scripts/             # C# スクリプト
│       ├── Player.cs
│       ├── Practice.cs
│       ├── GameSession.cs
│       ├── SkillType.cs
│       ├── SalaryCalculator.cs
│       ├── SkillSheetUI.cs / SkillDropdown.cs
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
| `Player.cs` | 日数・スタミナ・メンタル・スキル習熟度の管理と各種イベント発火 |
| `Practice.cs` | 練習の消費ルール（スタミナ→メンタル）と実行ロジック |
| `GameSession.cs` | シーンをまたぐ状態（性別・メンタル・スキル・各面接結果）の保持 |
| `SkillType.cs` | スキル種別と表示名 |
| `SalaryCalculator.cs` | 面接スコアとスキルから年収を算出（需要重み付き） |
| `SkillSheetUI.cs` / `SkillDropdown.cs` | スキルシート表示・練習対象の選択 |
| `StartSceneController.cs` | タイトル表示・性別選択・特訓へ遷移 |
| `ActionSceneController.cs` | 特訓 UI・背景／表情切替・就寝・面接への遷移 |
| `InterviewSceneController.cs` | 面接進行（出題・採点・合否・演出）。`interviewType` で人事／技術／社長を切替 |
| `EndSceneController.cs` | ゲームオーバー画面とタイトルへの復帰 |
| `InterviewDataLoader.cs` | 面接 JSON の読み込み・採点・通過ライン計算 |
| `Interviewer` / `HrInterviewer` / `TechnicalInterviewer` / `PresidentInterviewer` | 面接官種別・データ定義 |
| `SceneTransition.cs` | シーン遷移の共通処理 |
| `UnityroomApiClient`（`Assets/unityroom/`） | ランキングへのスコア送信 |

## 開発環境のセットアップ

1. [Unity Hub](https://unity.com/download) をインストールする。
2. **Unity 6.5**（エディタバージョン `6000.5.3f1`）を入れる。
3. 必要に応じてビルドサポートを追加する。
   - WebGL Build Support（unityroom 公開向け）
   - Windows / Mac Build Support など
4. 本リポジトリをクローンし、Unity Hub からプロジェクトを開く。

```bash
git clone <repository-url>
```

`Library/` や `Temp/` などは `.gitignore` 済みです。初回起動時に Unity が再生成します。

## 操作

| 画面 | 操作 | 内容 |
|------|------|------|
| タイトル | Enter / Space | 性別選択へ進む |
| 性別選択 | ← → / 1・2 / A・D | 男性・女性を選択 |
| 性別選択 | Enter | 決定して特訓開始 |
| 特訓 | 練習ボタン | 選択中の言語を練習 |
| 特訓 | 就寝ボタン（夜のみ表示） | 翌日へ進む |
| 面接 | 回答ボタン（3択） | 選択肢を回答 |
| 面接 | 降参ボタン | 特訓へ戻る |
| 内定パネル | Enter / Space | タイトルへ戻る |
| ゲームオーバー | Enter | タイトルへ戻る |

## データ編集のヒント

面接の質問や採点ルールを変えたい場合は、次の JSON を編集してください。

- `Assets/Resources/Question/interview_meta.json` … 採点・難易度ウェイト・ランク・通過ライン・年収ルール
- `Assets/Resources/Question/hr_interviewer.json` … 人事（満点30）
- `Assets/Resources/Question/tech_interviewer.json` … 技術（満点75）
- `Assets/Resources/Question/ceo_interviewer.json` … 社長（満点200）

各面接官 JSON は `maxScore`・`questions[]`（`difficulty` / `choices[].score` / `hintSkill` など）を持ちます。通過ラインは満点と `interview_meta.json` の `passRules`（`152 / 305`）から自動計算されます。

仕様メモは `Assets/Scenario/` にあります。

## 今後の予定（メモ）

- 特訓イベントの追加
- 背景・演出バリエーションの拡充
- バランス調整（通過ライン・年収係数）

## ライセンス

未設定です。公開・再配布する場合は別途ライセンスを決めます。
