using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// スキル一覧の各行（SkillText）配下の LevelText に、
/// <see cref="Player"/> の習熟度を表示する。
/// 練習などでレベルが変わると自動で更新する。
/// </summary>
public class SkillSheetUI : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [Tooltip("スキル行の親。未設定ならこのオブジェクト以下を検索する。")]
    [SerializeField]
    private Transform skillRowsRoot;

    private readonly Dictionary<SkillType, TMP_Text> levelTexts = new Dictionary<SkillType, TMP_Text>();

    private void Awake()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (skillRowsRoot == null)
        {
            skillRowsRoot = transform;
        }

        BuildMapping();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.StateChanged += Refresh;
            player.SkillLeveledUp += OnSkillLeveledUp;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.StateChanged -= Refresh;
            player.SkillLeveledUp -= OnSkillLeveledUp;
        }
    }

    /// <summary>表示対象の Player を差し替える（シーン初期化用）。</summary>
    public void Bind(Player target)
    {
        if (player == target)
        {
            Refresh();
            return;
        }

        if (player != null && isActiveAndEnabled)
        {
            player.StateChanged -= Refresh;
            player.SkillLeveledUp -= OnSkillLeveledUp;
        }

        player = target;

        if (player != null && isActiveAndEnabled)
        {
            player.StateChanged += Refresh;
            player.SkillLeveledUp += OnSkillLeveledUp;
        }

        Refresh();
    }

    /// <summary>全スキルの LevelText を現在値で更新する。</summary>
    public void Refresh()
    {
        if (levelTexts.Count == 0)
        {
            BuildMapping();
        }

        if (player == null)
        {
            foreach (TMP_Text levelText in levelTexts.Values)
            {
                if (levelText != null)
                {
                    levelText.text = "0";
                }
            }
            return;
        }

        foreach (KeyValuePair<SkillType, TMP_Text> pair in levelTexts)
        {
            if (pair.Value != null)
            {
                pair.Value.text = player.GetSkillLevel(pair.Key).ToString();
            }
        }
    }

    private void OnSkillLeveledUp(SkillType skill, int newLevel)
    {
        if (levelTexts.TryGetValue(skill, out TMP_Text levelText) && levelText != null)
        {
            levelText.text = newLevel.ToString();
        }
    }

    /// <summary>
    /// SkillText (TMP)_* の表示名から SkillType を特定し、
    /// 子の LevelText (TMP) を紐づける。
    /// </summary>
    private void BuildMapping()
    {
        levelTexts.Clear();

        if (skillRowsRoot == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = skillRowsRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI skillNameText in texts)
        {
            if (skillNameText == null || !skillNameText.name.StartsWith("SkillText", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryParseSkillName(skillNameText.text, out SkillType skill))
            {
                Debug.LogWarning($"SkillSheetUI: 未知のスキル名です: '{skillNameText.text}' ({skillNameText.name})");
                continue;
            }

            Transform levelTransform = skillNameText.transform.Find("LevelText (TMP)");
            if (levelTransform == null)
            {
                Debug.LogWarning($"SkillSheetUI: LevelText (TMP) が見つかりません: {skillNameText.name}");
                continue;
            }

            TMP_Text levelText = levelTransform.GetComponent<TMP_Text>();
            if (levelText == null)
            {
                continue;
            }

            levelTexts[skill] = levelText;
        }
    }

    private static bool TryParseSkillName(string displayName, out SkillType skill)
    {
        skill = default;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        string trimmed = displayName.Trim();
        foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
        {
            if (string.Equals(type.GetDisplayName(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                skill = type;
                return true;
            }
        }

        return false;
    }
}
