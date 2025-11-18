using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject welcomeUI;

    [Header("Buttons")]
    [SerializeField] private Button enterButton;
    private void Start()
    {
        enterButton.onClick.AddListener(OnClickEnter);
    }

    private void OnClickEnter()
    {
        welcomeUI.SetActive(false);
    }
}
