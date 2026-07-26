using UnityEngine;

/// <summary>
/// 技術面接官。特訓で伸ばしたスキルの総量で受け答えの成否が決まる。質問は3問。
/// スキル合計が <see cref="skillForTopScore"/> に達すると正答率がほぼ最大になる。
/// </summary>
public class TechnicalInterviewer : Interviewer
{
    [Header("技術評価")]
    [SerializeField]
    [Tooltip("正答率がほぼ最大になるスキル合計値。特訓量に合わせて調整する。")]
    private int skillForTopScore = 30;

    public override InterviewerType Type => InterviewerType.Technical;
    public override string DisplayName => "技術面接官";
    public override int QuestionCount => 3;

    protected override float GetAnswerSuccessRate(Player player)
    {
        if (player == null || skillForTopScore <= 0)
        {
            return BaselineSuccessRate;
        }

        // スキル合計の到達度（0〜1）を下駄の上に載せる。
        float skillRatio = Mathf.Clamp01((float)player.GetTotalSkillLevel() / skillForTopScore);
        return BaselineSuccessRate + (1f - BaselineSuccessRate) * skillRatio;
    }
}
