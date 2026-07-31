using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内定時の年収を計算するユーティリティ。
/// 面接スコアとスキルスコアから年収を算出する。
/// </summary>
public static class SalaryCalculator
{
    /// <summary>基準年収（万円）。</summary>
    private const int BaseSalary = 150;

    /// <summary>面接スコア1点あたりの年収加算（万円）。</summary>
    private const float InterviewScoreMultiplier = 0.5f;

    /// <summary>スキルスコア1点あたりの年収加算（万円）。</summary>
    private const float SkillScoreMultiplier = 0.5f;

    /// <summary>言語ごとの需要重み係数。</summary>
    private static readonly Dictionary<SkillType, float> SkillDemandWeights = new Dictionary<SkillType, float>
    {
        { SkillType.Python, 1.5f },      // AI/データ分析需要高
        { SkillType.JavaScript, 1.3f },  // Web需要高
        { SkillType.Java, 1.2f },        // エンタープライズ需要
        { SkillType.CSharp, 1.2f },      // ゲーム/業務アプリ
        { SkillType.Sql, 1.1f },         // 基本スキル
        { SkillType.Swift, 1.3f },       // iOS需要
        { SkillType.Cpp, 1.4f },         // 組み込み/ゲーム
        { SkillType.C, 1.2f },           // 組み込み
        { SkillType.Vba, 0.8f },         // 需要低め
        { SkillType.Assembly, 1.5f },    // 希少スキル
    };

    /// <summary>
    /// 年収を計算する。
    /// </summary>
    /// <param name="interviewScore">面接の合計スコア（0〜400想定）。</param>
    /// <param name="skillLevels">スキルレベルのディクショナリ。</param>
    /// <returns>年収（万円）。</returns>
    public static int CalculateSalary(int interviewScore, IReadOnlyDictionary<SkillType, int> skillLevels)
    {
        // 面接スコアからの加算
        float interviewBonus = interviewScore * InterviewScoreMultiplier;

        // スキルスコアからの加算（重み係数適用）
        float skillScore = CalculateWeightedSkillScore(skillLevels);
        float skillBonus = skillScore * SkillScoreMultiplier;

        // 合計年収を計算
        int totalSalary = Mathf.RoundToInt(BaseSalary + interviewBonus + skillBonus);

        return totalSalary;
    }

    /// <summary>
    /// GameSessionから年収を計算する。
    /// </summary>
    /// <returns>年収（万円）。</returns>
    public static int CalculateSalaryFromSession()
    {
        // 全面接のスコアを合計
        int totalInterviewScore = GameSession.HrScore + GameSession.TechScore + GameSession.PresidentScore;

        // GameSessionからスキルレベルを取得
        var skillLevels = new Dictionary<SkillType, int>();
        foreach (SkillType skillType in System.Enum.GetValues(typeof(SkillType)))
        {
            int level = GameSession.GetSkillLevel(skillType);
            if (level > 0)
            {
                skillLevels[skillType] = level;
            }
        }

        return CalculateSalary(totalInterviewScore, skillLevels);
    }

    /// <summary>
    /// 重み係数を適用したスキルスコアを計算する。
    /// </summary>
    private static float CalculateWeightedSkillScore(IReadOnlyDictionary<SkillType, int> skillLevels)
    {
        if (skillLevels == null)
        {
            return 0f;
        }

        float totalScore = 0f;
        foreach (var kvp in skillLevels)
        {
            float weight = GetDemandWeight(kvp.Key);
            totalScore += kvp.Value * weight;
        }

        return totalScore;
    }

    /// <summary>
    /// 指定スキルの需要重み係数を取得する。
    /// </summary>
    private static float GetDemandWeight(SkillType skillType)
    {
        return SkillDemandWeights.TryGetValue(skillType, out float weight) ? weight : 1.0f;
    }

    /// <summary>
    /// デバッグ用：年収計算の内訳を取得する。
    /// </summary>
    public static string GetSalaryBreakdown(int interviewScore, IReadOnlyDictionary<SkillType, int> skillLevels)
    {
        float interviewBonus = interviewScore * InterviewScoreMultiplier;
        float skillScore = CalculateWeightedSkillScore(skillLevels);
        float skillBonus = skillScore * SkillScoreMultiplier;
        int totalSalary = Mathf.RoundToInt(BaseSalary + interviewBonus + skillBonus);

        return $"基準年収: {BaseSalary}万円\n" +
               $"面接ボーナス: +{interviewBonus:F0}万円 (スコア{interviewScore} × {InterviewScoreMultiplier})\n" +
               $"スキルボーナス: +{skillBonus:F0}万円 (スコア{skillScore:F1} × {SkillScoreMultiplier})\n" +
               $"合計年収: {totalSalary}万円";
    }
}
