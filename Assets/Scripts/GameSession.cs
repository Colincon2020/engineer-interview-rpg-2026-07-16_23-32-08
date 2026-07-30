using UnityEngine;

/// <summary>
/// シーンをまたいで保持するセッションデータ。
/// タイトルでの主人公選択結果や面接スコアなどを ActionScene 以降へ渡す。
/// </summary>
public static class GameSession
{
    /// <summary>タイトルで選択された主人公の性別。</summary>
    public static PlayerGender SelectedGender { get; private set; } = PlayerGender.Male;

    /// <summary>性別が一度でも選択されたか。</summary>
    public static bool HasSelectedGender { get; private set; }

    /// <summary>特訓終了時点のメンタル（面接画面表示用）。</summary>
    public static int Mental { get; private set; } = Player.MaxMental;

    /// <summary>人事面接の獲得スコア。</summary>
    public static int HrScore { get; private set; }

    /// <summary>人事面接の満点。</summary>
    public static int HrMaxScore { get; private set; }

    /// <summary>人事面接の通過ライン。</summary>
    public static int HrPassScore { get; private set; }

    /// <summary>人事面接を最後まで実施したか。</summary>
    public static bool HasCompletedHrInterview { get; private set; }

    /// <summary>人事面接に通過したか。</summary>
    public static bool HasPassedHrInterview { get; private set; }

    /// <summary>タイトル画面で選択した性別を保存する。</summary>
    public static void SetSelectedGender(PlayerGender gender)
    {
        SelectedGender = gender;
        HasSelectedGender = true;
    }

    /// <summary>特訓終了時点のメンタルを保存する。</summary>
    public static void SetMental(int mental)
    {
        Mental = Mathf.Clamp(mental, 0, Player.MaxMental);
    }

    /// <summary>人事面接の結果を保存する。</summary>
    public static void SetHrInterviewResult(int score, int maxScore, int passScore, bool passed)
    {
        HrScore = score;
        HrMaxScore = maxScore;
        HrPassScore = passScore;
        HasCompletedHrInterview = true;
        HasPassedHrInterview = passed;
    }

    /// <summary>人事面接の結果だけをクリアする（不合格で特訓へ戻るときなど）。</summary>
    public static void ClearHrInterviewResult()
    {
        HrScore = 0;
        HrMaxScore = 0;
        HrPassScore = 0;
        HasCompletedHrInterview = false;
        HasPassedHrInterview = false;
    }

    /// <summary>セッション状態を初期化する（タイトルへ戻ったときなど）。</summary>
    public static void Reset()
    {
        SelectedGender = PlayerGender.Male;
        HasSelectedGender = false;
        Mental = Player.MaxMental;
        ClearHrInterviewResult();
    }
}
