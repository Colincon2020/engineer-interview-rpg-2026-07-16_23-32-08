using UnityEngine;

/// <summary>
/// 練習アクションのルールと実行ロジック。
/// 状態（ライフ・メンタル・スキル）は <see cref="Player"/> が保持し、本クラスは消費順と上昇値だけを担う。
///
/// 仕様（7/16作戦会議メモより）:
///  - 練習は1日に何回でも可能（資源がある限り）。
///  - 1回につきライフポイントを <see cref="LifeCost"/> 消費し、スキルが <see cref="SkillGain"/> 上昇する。
///  - ライフが尽きた状態で続けると、代わりにメンタルを消費する。
///  - メンタルは回復しない。練習中にメンタルが 0 になると <see cref="EndSceneName"/> へ遷移する。
/// </summary>
public static class Practice
{
    /// <summary>練習1回で消費するライフポイント。</summary>
    public const int LifeCost = 1;

    /// <summary>ライフ尽きたあとの練習1回で消費するメンタル。</summary>
    public const int MentalCost = 1;

    /// <summary>練習1回でスキルが上昇する値（上昇値）。</summary>
    public const int SkillGain = 1;

    /// <summary>メンタルが尽きたときに遷移するシーン名。</summary>
    public const string EndSceneName = "EndScene";

    /// <summary>練習で消費した資源の種類。</summary>
    public enum CostType
    {
        None,
        Life,
        Mental,
    }

    /// <summary>練習の実行結果。</summary>
    public readonly struct Result
    {
        public readonly bool Success;
        public readonly CostType CostType;
        public readonly int NewSkillLevel;

        public Result(bool success, CostType costType, int newSkillLevel)
        {
            Success = success;
            CostType = costType;
            NewSkillLevel = newSkillLevel;
        }

        public static Result Failed => new Result(false, CostType.None, 0);
    }

    /// <summary>
    /// 指定スキルを練習する。ライフがあればライフを、無ければメンタルを消費し、
    /// スキルを <see cref="SkillGain"/> だけ上昇させる。
    /// </summary>
    public static Result TryExecute(Player player, SkillType skill)
    {
        if (player == null)
        {
            Debug.LogWarning("Practice.TryExecute: Player が null です。");
            return Result.Failed;
        }

        if (player.IsWeekFinished)
        {
            Debug.LogWarning("特訓期間は終了しています。面接へ進んでください。");
            return Result.Failed;
        }

        CostType costType;
        if (player.LifePoints > 0)
        {
            if (!player.TryConsumeLifeForPractice(LifeCost))
            {
                return Result.Failed;
            }

            costType = CostType.Life;
        }
        else if (player.Mental > 0)
        {
            // ライフが尽きた状態での練習はメンタルを削る。
            if (!player.TryConsumeMentalForPractice(MentalCost))
            {
                return Result.Failed;
            }

            costType = CostType.Mental;
        }
        else
        {
            Debug.Log("ライフもメンタルも尽きています。就寝してください。");
            return Result.Failed;
        }

        int newLevel = player.AddSkillLevel(skill, SkillGain);

        // 練習でメンタルを使い切ったらゲームオーバーとしてエンドシーンへ。
        if (costType == CostType.Mental && player.Mental <= 0)
        {
            SceneTransition.Load(EndSceneName);
        }

        return new Result(true, costType, newLevel);
    }
}
