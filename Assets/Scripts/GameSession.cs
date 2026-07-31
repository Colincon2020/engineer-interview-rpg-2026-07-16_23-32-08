using System.Collections.Generic;
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

    /// <summary>特訓終了時点のスキルレベル（面接のヒント判定用）。</summary>
    private static Dictionary<SkillType, int> skillLevels = new Dictionary<SkillType, int>();

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

    /// <summary>技術面接の獲得スコア。</summary>
    public static int TechScore { get; private set; }

    /// <summary>技術面接の満点。</summary>
    public static int TechMaxScore { get; private set; }

    /// <summary>技術面接の通過ライン。</summary>
    public static int TechPassScore { get; private set; }

    /// <summary>技術面接を最後まで実施したか。</summary>
    public static bool HasCompletedTechInterview { get; private set; }

    /// <summary>技術面接に通過したか。</summary>
    public static bool HasPassedTechInterview { get; private set; }

    /// <summary>社長面接の獲得スコア。</summary>
    public static int PresidentScore { get; private set; }

    /// <summary>社長面接の満点。</summary>
    public static int PresidentMaxScore { get; private set; }

    /// <summary>社長面接の通過ライン。</summary>
    public static int PresidentPassScore { get; private set; }

    /// <summary>社長面接を最後まで実施したか。</summary>
    public static bool HasCompletedPresidentInterview { get; private set; }

    /// <summary>社長面接に通過したか。</summary>
    public static bool HasPassedPresidentInterview { get; private set; }

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

    /// <summary>特訓終了時点のスキルレベルを保存する。</summary>
    public static void SetSkillLevels(IReadOnlyDictionary<SkillType, int> levels)
    {
        skillLevels.Clear();
        if (levels != null)
        {
            foreach (var kvp in levels)
            {
                skillLevels[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>指定スキルのレベルを取得する（未設定は0）。</summary>
    public static int GetSkillLevel(SkillType skill)
    {
        return skillLevels.TryGetValue(skill, out int level) ? level : 0;
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

    /// <summary>技術面接の結果を保存する。</summary>
    public static void SetTechInterviewResult(int score, int maxScore, int passScore, bool passed)
    {
        TechScore = score;
        TechMaxScore = maxScore;
        TechPassScore = passScore;
        HasCompletedTechInterview = true;
        HasPassedTechInterview = passed;
    }

    /// <summary>技術面接の結果だけをクリアする。</summary>
    public static void ClearTechInterviewResult()
    {
        TechScore = 0;
        TechMaxScore = 0;
        TechPassScore = 0;
        HasCompletedTechInterview = false;
        HasPassedTechInterview = false;
    }

    /// <summary>社長面接の結果を保存する。</summary>
    public static void SetPresidentInterviewResult(int score, int maxScore, int passScore, bool passed)
    {
        PresidentScore = score;
        PresidentMaxScore = maxScore;
        PresidentPassScore = passScore;
        HasCompletedPresidentInterview = true;
        HasPassedPresidentInterview = passed;
    }

    /// <summary>社長面接の結果だけをクリアする。</summary>
    public static void ClearPresidentInterviewResult()
    {
        PresidentScore = 0;
        PresidentMaxScore = 0;
        PresidentPassScore = 0;
        HasCompletedPresidentInterview = false;
        HasPassedPresidentInterview = false;
    }

    /// <summary>セッション状態を初期化する（タイトルへ戻ったときなど）。</summary>
    public static void Reset()
    {
        SelectedGender = PlayerGender.Male;
        HasSelectedGender = false;
        Mental = Player.MaxMental;
        skillLevels.Clear();
        ClearHrInterviewResult();
        ClearTechInterviewResult();
        ClearPresidentInterviewResult();
    }
}
