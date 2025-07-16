using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LoginControl : MonoBehaviour
{
    [Header("Componentes de UI")]
    [SerializeField] private InputField nameInputField;
    [SerializeField] private Button continueButton;
    [SerializeField] public Button guestButton;

    [Header("Perfiles de Usuario")]
    [SerializeField] private Transform userButtonsContainer;
    [SerializeField] private GameObject userButtonPrefab;

    [Header("Navegación")]
    [SerializeField] private string sceneNameToLoad;

    // Ya no necesitamos la referencia al panel de login, se gestiona solo

    void Start()
    {
        // --- CAMBIO IMPORTANTE: Cargamos los datos directamente al inicio ---
        UserManager.LoadData();

        // El resto de la configuración
        continueButton.onClick.AddListener(ConfirmNameAndContinue);
        guestButton.onClick.AddListener(ContinueAsGuest);
        nameInputField.onValueChanged.AddListener(ValidateInput);
        nameInputField.onSubmit.AddListener(OnInputSubmit);
        ValidateInput("");

        // Llenamos la lista de usuarios guardados
        PopulateUserList();
    }

    private void PopulateUserList()
    {
        foreach (Transform child in userButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        List<string> users = UserManager.GetUsers();
        Debug.Log($"Mostrando {users.Count} botones de usuario.");

        foreach (string username in users)
        {
            GameObject buttonGO = Instantiate(userButtonPrefab, userButtonsContainer);
            buttonGO.name = $"Button_{username}";
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = username;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => LoginAsExistingUser(username));
        }
    }

    private void ValidateInput(string text)
    {
        continueButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    private void OnInputSubmit(string text)
    {
        if (continueButton.interactable)
        {
            ConfirmNameAndContinue();
        }
    }

    public void LoginAsExistingUser(string username)
    {
        Debug.Log($"Iniciando sesión como usuario existente: {username}");
        UserManager.SetCurrentUser(username);
        LoadGameScene();
    }

    public void ConfirmNameAndContinue()
    {
        EventSystem.current.SetSelectedGameObject(null);
        string playerName = nameInputField.text;
        if (string.IsNullOrWhiteSpace(playerName)) return;

        UserManager.SaveUser(playerName);
        UserManager.SetCurrentUser(playerName);
        LoadGameScene();
    }

    public void ContinueAsGuest()
    {
        EventSystem.current.SetSelectedGameObject(null);
        UserManager.SetCurrentUser("Invitado");
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.LogError("El nombre de la escena a cargar no está especificado.");
            return;
        }
        SceneManager.LoadScene(sceneNameToLoad);
    }
}
