using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面の制御。
/// タイトル表示 → 性別選択 → 特訓シーンへ進む。
/// </summary>
public class StartSceneController : MonoBehaviour
{
    [Header("シーン遷移")]
    [SerializeField]
    private string gameSceneName = "ActionScene";

    [Header("性別選択（未設定なら実行時に生成）")]
    [SerializeField]
    private Button maleButton;

    [SerializeField]
    private Button femaleButton;

    [SerializeField]
    private Image selectionHighlight;

    [SerializeField]
    private TMP_Text instructionText;

    [SerializeField]
    private Color selectedColor = new Color(1f, 0.92f, 0.4f, 1f);

    [SerializeField]
    private Color normalColor = Color.white;

    [Header("画面切替")]
    public GameObject[] titleElements;
    public GameObject[] genderSelectElements;

    private PlayerGender selectedGender = PlayerGender.Male;
    private bool isTransitioning;

    private void Awake()
    {
        EnsureSelectionUi();
        WireButtons();
        ApplySelectionVisual();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // タイトル表示中は Enter / Space で性別選択へ
        if (IsTitleVisible())
        {
            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame)
            {
                ShowGenderSelect();
            }

            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame
            || keyboard.aKey.wasPressedThisFrame)
        {
            SelectMale();
        }
        else if (keyboard.digit2Key.wasPressedThisFrame
                 || keyboard.rightArrowKey.wasPressedThisFrame
                 || keyboard.dKey.wasPressedThisFrame)
        {
            SelectFemale();
        }
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmAndStart();
        }
    }

    /// <summary>タイトル要素を隠し、性別選択 UI を表示する。</summary>
    public void ShowGenderSelect()
    {
        if (titleElements != null)
        {
            foreach (GameObject obj in titleElements)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        if (genderSelectElements != null)
        {
            foreach (GameObject obj in genderSelectElements)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    /// <summary>男性を選択する（UI ボタンからも呼べる）。</summary>
    public void SelectMale()
    {
        selectedGender = PlayerGender.Male;
        ApplySelectionVisual();
    }

    /// <summary>女性を選択する（UI ボタンからも呼べる）。</summary>
    public void SelectFemale()
    {
        selectedGender = PlayerGender.Female;
        ApplySelectionVisual();
    }

    /// <summary>シーン上の男性ボタンから呼ばれる。</summary>
    public void OnSelectMale()
    {
        selectedGender = PlayerGender.Male;
        ApplySelectionVisual();
        PersistGenderAndLoadScene(PlayerGender.Male);
    }

    /// <summary>シーン上の女性ボタンから呼ばれる。</summary>
    public void OnSelectFemale()
    {
        selectedGender = PlayerGender.Female;
        ApplySelectionVisual();
        PersistGenderAndLoadScene(PlayerGender.Female);
    }

    /// <summary>選択した性別を保存し、特訓シーンへ遷移する。</summary>
    public void ConfirmAndStart()
    {
        PersistGenderAndLoadScene(selectedGender);
    }

    private void PersistGenderAndLoadScene(PlayerGender gender)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        GameSession.SetSelectedGender(gender);

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SelectedGender = gender == PlayerGender.Female
                ? GameDataManager.Gender.Female
                : GameDataManager.Gender.Male;
        }

        SceneTransition.Load(gameSceneName);
    }

    private bool IsTitleVisible()
    {
        return titleElements != null
            && titleElements.Length > 0
            && titleElements[0] != null
            && titleElements[0].activeSelf;
    }

    private void WireButtons()
    {
        if (maleButton != null)
        {
            maleButton.onClick.RemoveListener(SelectMale);
            maleButton.onClick.AddListener(SelectMale);
        }

        if (femaleButton != null)
        {
            femaleButton.onClick.RemoveListener(SelectFemale);
            femaleButton.onClick.AddListener(SelectFemale);
        }
    }

    private void ApplySelectionVisual()
    {
        if (maleButton != null)
        {
            SetButtonColor(maleButton, selectedGender == PlayerGender.Male ? selectedColor : normalColor);
        }

        if (femaleButton != null)
        {
            SetButtonColor(femaleButton, selectedGender == PlayerGender.Female ? selectedColor : normalColor);
        }

        if (instructionText != null)
        {
            string genderLabel = selectedGender == PlayerGender.Female ? "女性" : "男性";
            instructionText.text = $"主人公: {genderLabel}\n←→ または 1/2 で選択 / Enter で開始";
        }
    }

    private static void SetButtonColor(Button button, Color color)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = color;
        button.colors = colors;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = color;
        }
    }

    /// <summary>Inspector 未設定時に、最低限の男女選択 UI を Canvas 上へ生成する。</summary>
    private void EnsureSelectionUi()
    {
        if (maleButton != null && femaleButton != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("StartSceneController: Canvas が見つからないため性別選択 UI を生成できません。");
            return;
        }

        GameObject panelObject = new GameObject("GenderSelectPanel", typeof(RectTransform));
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);
        panelRect.sizeDelta = new Vector2(520f, 140f);

        if (instructionText == null)
        {
            instructionText = CreateTmpLabel(panelObject.transform, "InstructionText", new Vector2(0f, 50f), new Vector2(500f, 50f));
        }

        if (maleButton == null)
        {
            maleButton = CreateChoiceButton(panelObject.transform, "MaleButton", "男性 (1)", new Vector2(-130f, -20f));
        }

        if (femaleButton == null)
        {
            femaleButton = CreateChoiceButton(panelObject.transform, "FemaleButton", "女性 (2)", new Vector2(130f, -20f));
        }
    }

    private static Button CreateChoiceButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(200f, 56f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text labelText = CreateTmpLabel(buttonObject.transform, "Label", Vector2.zero, new Vector2(180f, 40f));
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 28f;
        labelText.color = Color.black;

        return button;
    }

    private static TMP_Text CreateTmpLabel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
