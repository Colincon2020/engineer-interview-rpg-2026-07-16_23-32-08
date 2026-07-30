using UnityEngine;
using System.Collections;

public class BlinkEffect : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            canvasGroup.alpha = 0.1f; // ここを0.35fから0.1fに変更
            yield return new WaitForSeconds(0.4f);
            canvasGroup.alpha = 1f;
        }
    }
}