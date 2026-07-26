using UnityEngine;

/// <summary>
/// 人事面接官。人柄＝メンタルの高さで受け答えの成否が決まる。質問は3問。
/// メンタルが高いほど落ち着いて答えられる、というイメージ。
/// </summary>
public class HrInterviewer : Interviewer
{
    public override InterviewerType Type => InterviewerType.Hr;
    public override string DisplayName => "人事面接官";
    public override int QuestionCount => 3;

    protected override float GetAnswerSuccessRate(Player player)
    {
        if (player == null || Player.MaxMental <= 0)
        {
            return BaselineSuccessRate;
        }

        // メンタルの割合（0〜1）を下駄の上に載せる。
        float mentalRatio = (float)player.Mental / Player.MaxMental;
        return BaselineSuccessRate + (1f - BaselineSuccessRate) * mentalRatio;
    }
}
