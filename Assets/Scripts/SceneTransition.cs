using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 黒フェード付きの非同期シーン遷移。
/// 未配置なら初回呼び出し時に自動生成する。
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const float DefaultFadeDuration = 0.3f;

    private static SceneTransition instance;

    private CanvasGroup fadeGroup;
    private bool isTransitioning;
    private float fadeDuration = DefaultFadeDuration;

    /// <summary>指定シーンへフェード付きで非同期遷移する。</summary>
    public static void Load(string sceneName)
    {
        EnsureInstance().StartTransition(sceneName);
    }

    private static SceneTransition EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject go = new GameObject("SceneTransition");
        instance = go.AddComponent<SceneTransition>();
        DontDestroyOnLoad(go);
        instance.BuildFadeOverlay();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup == null)
        {
            BuildFadeOverlay();
        }
    }

    private void BuildFadeOverlay()
    {
        GameObject canvasObject = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject fadeObject = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        fadeObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = fadeObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = fadeObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        fadeGroup = fadeObject.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    private void StartTransition(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;
        fadeGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"SceneTransition: シーン '{sceneName}' をロードできません。");
            yield return Fade(1f, 0f);
            fadeGroup.blocksRaycasts = false;
            isTransitioning = false;
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            yield return null;
        }

        yield return Fade(1f, 0f);

        fadeGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadeGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        fadeGroup.alpha = to;
    }
}
