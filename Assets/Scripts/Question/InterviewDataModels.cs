using System;
using UnityEngine;

/// <summary>面接 JSON 共通のメタ情報。</summary>
[Serializable]
public class InterviewFileMeta
{
    public string gameTitle;
    public string dataName;
    public string version;
    public string updatedAt;
    public string interviewerId;
    public string note;
}

/// <summary>選択肢1件分。</summary>
[Serializable]
public class InterviewChoiceData
{
    public string id;
    public string text;
    public int score;
    public bool isCorrect;
    public string reaction;
}

/// <summary>質問1件分。</summary>
[Serializable]
public class InterviewQuestionData
{
    public string id;
    public int no;
    public string category;
    public string difficulty;
    public string question;
    public string hintSkill;
    public int hintRequiredLevel;
    public string modelAnswer;
    public InterviewChoiceData[] choices;
}

/// <summary>面接官1人分のデータ本体。</summary>
[Serializable]
public class InterviewerBodyData
{
    public string id;
    public string name;
    public string displayName;
    public string theme;
    public string difficulty;
    public float difficultyWeight;
    public int questionCount;
    public int askCount;
    public bool randomize;
    public int maxScore;
    public InterviewQuestionData[] questions;
}

/// <summary>hr / tech / ceo 面接官 JSON のルート。</summary>
[Serializable]
public class InterviewerFileData
{
    public InterviewFileMeta meta;
    public InterviewerBodyData interviewer;
}

/// <summary>難易度と重みの対応（JsonUtility は Dictionary 非対応のため配列で持つ）。</summary>
[Serializable]
public class DifficultyWeightEntry
{
    public string difficulty;
    public float weight;
}

/// <summary>年収計算ルール。</summary>
[Serializable]
public class SalaryRuleData
{
    public string formula;
    public string unit;
    public int min;
    public int max;
}

/// <summary>ランク判定1件分。</summary>
[Serializable]
public class RankRuleData
{
    public string rank;
    public int minScore;
    public string label;
}

/// <summary>採点ルール。</summary>
[Serializable]
public class ScoringRuleData
{
    public int maxScorePerQuestion;
    public DifficultyWeightEntry[] difficultyWeights;
    public int maxTotalScore;
    public string formula;
    public SalaryRuleData salary;
    public RankRuleData[] ranks;
    public string hintRule;

    /// <summary>難易度文字列（易 / 中 / 高）に対応する重みを返す。未定義なら 1。</summary>
    public float GetDifficultyWeight(string difficulty)
    {
        if (difficultyWeights == null || string.IsNullOrEmpty(difficulty))
        {
            return 1f;
        }

        foreach (DifficultyWeightEntry entry in difficultyWeights)
        {
            if (entry != null && entry.difficulty == difficulty)
            {
                return entry.weight;
            }
        }

        return 1f;
    }
}

/// <summary>interview_meta.json のルート。</summary>
[Serializable]
public class InterviewMetaFileData
{
    public InterviewFileMeta meta;
    public ScoringRuleData scoring;
}
