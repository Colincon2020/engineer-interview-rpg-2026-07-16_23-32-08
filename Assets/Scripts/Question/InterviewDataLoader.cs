using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Question 配下の面接 JSON を読み込むユーティリティ。
/// パスは Resources からの相対（拡張子なし）。例: Question/hr_interviewer
/// </summary>
public static class InterviewDataLoader
{
    public const string MetaResourcePath = "Question/interview_meta";
    public const string HrResourcePath = "Question/hr_interviewer";
    public const string TechResourcePath = "Question/tech_interviewer";
    public const string CeoResourcePath = "Question/ceo_interviewer";

    /// <summary>共通メタ（採点・ランク・年収ルール）を読み込む。</summary>
    public static InterviewMetaFileData LoadMeta()
    {
        return LoadJson<InterviewMetaFileData>(MetaResourcePath);
    }

    /// <summary>面接官種別から対応 JSON を読み込む。</summary>
    public static InterviewerFileData LoadInterviewer(InterviewerType type)
    {
        return type switch
        {
            InterviewerType.Hr => LoadJson<InterviewerFileData>(HrResourcePath),
            InterviewerType.Technical => LoadJson<InterviewerFileData>(TechResourcePath),
            InterviewerType.President => LoadJson<InterviewerFileData>(CeoResourcePath),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "未知の面接官種別です。"),
        };
    }

    /// <summary>人事面接官 JSON を読み込む。</summary>
    public static InterviewerFileData LoadHr() => LoadInterviewer(InterviewerType.Hr);

    /// <summary>技術面接官 JSON を読み込む。</summary>
    public static InterviewerFileData LoadTechnical() => LoadInterviewer(InterviewerType.Technical);

    /// <summary>社長面接官 JSON を読み込む。</summary>
    public static InterviewerFileData LoadPresident() => LoadInterviewer(InterviewerType.President);

    /// <summary>
    /// JSON の質問データ本体を出題順で返す。
    /// randomize が true の場合は出題前にシャッフルする。
    /// </summary>
    public static List<InterviewQuestionData> ToQuestionDataList(
        InterviewerFileData fileData,
        int? askCount = null)
    {
        var result = new List<InterviewQuestionData>();
        if (fileData?.interviewer?.questions == null)
        {
            return result;
        }

        var source = new List<InterviewQuestionData>(fileData.interviewer.questions);
        if (fileData.interviewer.randomize)
        {
            Shuffle(source);
        }

        int take = askCount
            ?? (fileData.interviewer.askCount > 0
                ? fileData.interviewer.askCount
                : source.Count);
        take = Mathf.Clamp(take, 0, source.Count);

        for (int i = 0; i < take; i++)
        {
            InterviewQuestionData q = source[i];
            if (q == null || string.IsNullOrEmpty(q.question))
            {
                continue;
            }

            result.Add(q);
        }

        return result;
    }

    /// <summary>
    /// JSON の質問文だけを既存の <see cref="InterviewQuestion"/> リストへ変換する。
    /// randomize が true の場合は出題前にシャッフルする。
    /// </summary>
    public static List<InterviewQuestion> ToInterviewQuestions(
        InterviewerFileData fileData,
        int? askCount = null)
    {
        var result = new List<InterviewQuestion>();
        foreach (InterviewQuestionData q in ToQuestionDataList(fileData, askCount))
        {
            result.Add(new InterviewQuestion(q.question));
        }

        return result;
    }

    /// <summary>
    /// 選択肢の加点（choice.score × difficultyWeight）を整数で返す。
    /// </summary>
    public static int CalculateChoiceScore(
        InterviewQuestionData question,
        InterviewChoiceData choice,
        ScoringRuleData scoring)
    {
        if (choice == null)
        {
            return 0;
        }

        float weight = scoring != null
            ? scoring.GetDifficultyWeight(question?.difficulty)
            : 1f;
        return Mathf.RoundToInt(choice.score * weight);
    }

    /// <summary>
    /// 各面接の通過ライン。passRules が無ければ finalPassMinScore(200)/maxTotalScore(625) で按分。
    /// </summary>
    public static int GetStagePassScore(int maxScore, ScoringRuleData scoring = null)
    {
        if (maxScore <= 0)
        {
            return 0;
        }

        PassRulesData rules = scoring?.passRules;
        int numerator = rules != null && rules.stagePassRatioNumerator > 0
            ? rules.stagePassRatioNumerator
            : (rules != null && rules.finalPassMinScore > 0 ? rules.finalPassMinScore : 200);
        int denominator = rules != null && rules.stagePassRatioDenominator > 0
            ? rules.stagePassRatioDenominator
            : (scoring != null && scoring.maxTotalScore > 0 ? scoring.maxTotalScore : 625);

        return Mathf.CeilToInt(maxScore * (float)numerator / denominator);
    }

    /// <summary>各面接の通過判定。</summary>
    public static bool IsStagePass(int score, int maxScore, ScoringRuleData scoring = null)
    {
        return score >= GetStagePassScore(maxScore, scoring);
    }

    /// <summary>最終合否（合計スコアが C 以上か）。</summary>
    public static bool IsFinalPass(int totalScore, ScoringRuleData scoring = null)
    {
        int threshold = scoring?.passRules != null && scoring.passRules.finalPassMinScore > 0
            ? scoring.passRules.finalPassMinScore
            : 200;
        return totalScore >= threshold;
    }

    /// <summary>
    /// hintSkill 文字列（例: "SQL", "C#", "JAVA"）を <see cref="SkillType"/> に変換する。
    /// 未対応・空の場合は false。
    /// </summary>
    public static bool TryParseHintSkill(string hintSkill, out SkillType skillType)
    {
        skillType = default;
        if (string.IsNullOrWhiteSpace(hintSkill))
        {
            return false;
        }

        switch (hintSkill.Trim())
        {
            case "JAVA":
            case "Java":
                skillType = SkillType.Java;
                return true;
            case "SQL":
            case "Sql":
                skillType = SkillType.Sql;
                return true;
            case "C#":
            case "CSharp":
                skillType = SkillType.CSharp;
                return true;
            case "C++":
            case "Cpp":
                skillType = SkillType.Cpp;
                return true;
            case "C":
                skillType = SkillType.C;
                return true;
            case "アセンブリ":
            case "Assembly":
                skillType = SkillType.Assembly;
                return true;
            case "Python":
                skillType = SkillType.Python;
                return true;
            case "VBA":
            case "Vba":
                skillType = SkillType.Vba;
                return true;
            case "Swift":
                skillType = SkillType.Swift;
                return true;
            case "JavaScript":
            case "JS":
                skillType = SkillType.JavaScript;
                return true;
            default:
                Debug.LogWarning($"未対応の hintSkill です: {hintSkill}");
                return false;
        }
    }

    /// <summary>
    /// プレイヤーのスキルがヒント表示条件を満たすか。
    /// technical / mixed 向け。hintSkill が無い質問は常に false。
    /// </summary>
    public static bool ShouldShowHint(InterviewQuestionData question, Player player)
    {
        if (question == null || player == null)
        {
            return false;
        }

        if (!TryParseHintSkill(question.hintSkill, out SkillType skillType))
        {
            return false;
        }

        return player.GetSkillLevel(skillType) >= question.hintRequiredLevel;
    }

    private static T LoadJson<T>(string resourcePath) where T : class
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"面接 JSON が見つかりません: Resources/{resourcePath}.json");
            return null;
        }

        T data = JsonUtility.FromJson<T>(asset.text);
        if (data == null)
        {
            Debug.LogError($"面接 JSON のパースに失敗しました: Resources/{resourcePath}.json");
        }

        return data;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
