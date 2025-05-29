using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI resourcesText;
    public Button restartButton;

    private GameManager manager;

    void Start()
    {
        manager = FindObjectOfType<GameManager>();
        restartButton?.onClick.AddListener(() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }

    void Update()
    {
        if (manager && resourcesText)
        {
            resourcesText.text =
                $"Красной базе доставлено: {manager.GetRedDelivered()}\n" +
                $"Синей базе доставлено: {manager.GetBlueDelivered()}";
        }
    }
}
