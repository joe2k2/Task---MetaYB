using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Toggle showPasswordToggle;
    [SerializeField] private Image showPasswordImage;
    [SerializeField] private Sprite showSprite;
    [SerializeField] private Sprite hideSprite;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Task - Kutumb";

    [Header("Login Credentials")]
    [SerializeField] private string correctUsername = "admin";
    [SerializeField] private string correctPassword = "password123";

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);

        if (warningText != null)
        {
            warningText.text = "";
        }

        passwordInput.contentType = TMP_InputField.ContentType.Password;

        if (showPasswordToggle != null)
        {
            showPasswordToggle.isOn = false;
            showPasswordToggle.onValueChanged.AddListener(OnShowPasswordToggleChanged);
        }
    }

    private void OnShowPasswordToggleChanged(bool isOn)
    {
        if (showPasswordImage != null && showSprite != null && hideSprite != null)
        {
            showPasswordImage.sprite = isOn ? showSprite : hideSprite;
        }

        if (isOn)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
        }

        passwordInput.ForceLabelUpdate();
    }

    private void OnLoginButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowWarning("Please enter username and password");
            return;
        }

        if (username == correctUsername && password == correctPassword)
        {
            warningText.text = "";
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            ShowWarning("Invalid username or password");
        }
    }

    private void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }
    }
}
