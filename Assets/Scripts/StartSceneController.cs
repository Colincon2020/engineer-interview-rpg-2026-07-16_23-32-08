using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 追加

public class StartSceneController : MonoBehaviour
{
    public GameObject[] titleElements;
    public GameObject[] genderSelectElements;

    void Update()
    {
        if (titleElements[0].activeSelf && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            ShowGenderSelect();
        }
    }

    public void ShowGenderSelect()
    {
        foreach (GameObject obj in titleElements)
        {
            obj.SetActive(false);
        }
        foreach (GameObject obj in genderSelectElements)
        {
            obj.SetActive(true);
        }
    }

    public void OnSelectMale()
    {
        GameDataManager.Instance.SelectedGender = GameDataManager.Gender.Male;
        SceneManager.LoadScene("ActionScene");
    }

    public void OnSelectFemale()
    {
        GameDataManager.Instance.SelectedGender = GameDataManager.Gender.Female;
        SceneManager.LoadScene("ActionScene");
    }
}