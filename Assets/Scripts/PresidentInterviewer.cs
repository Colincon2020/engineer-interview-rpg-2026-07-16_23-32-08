using UnityEngine;

/// <summary>
/// 社長面接官（ラスボス）。人柄（メンタル）と技術（スキル）の両方を総合的に見る。質問は10問。
/// 最終関門なので、どちらか一方だけ高くても満点は取りにくい。
/// </summary>
public class PresidentInterviewer : Interviewer
{
    [Header("総合評価")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("正答率に占めるメンタル評価の比重。残りが技術の比重になる。")]
    private float mentalWeight = 0.5f;

    [SerializeField]
    [Tooltip("技術評価が最大になるスキル合計値。")]
    private int skillForTopScore = 30;

    public override InterviewerType Type => InterviewerType.President;
    public override string DisplayName => "社長面接官";
    public override int QuestionCount => 10;

    protected override float GetAnswerSuccessRate(Player player)
    {
        if (player == null)
        {
            return BaselineSuccessRate;
        }

        float mentalRatio = Player.MaxMental > 0
            ? (float)player.Mental / Player.MaxMental
            : 0f;
        float skillRatio = skillForTopScore > 0
            ? Mathf.Clamp01((float)player.GetTotalSkillLevel() / skillForTopScore)
            : 0f;

        // メンタルと技術を比重に応じて合成した総合到達度（0〜1）。
        float weight = Mathf.Clamp01(mentalWeight);
        float overallRatio = mentalRatio * weight + skillRatio * (1f - weight);
        return BaselineSuccessRate + (1f - BaselineSuccessRate) * overallRatio;
    }
}
