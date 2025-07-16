using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Necesario para controlar el foco de la UI
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class LoginControl : MonoBehaviour
{
    [Header("Componentes de UI")]
    [SerializeField] private InputField nameInputField;
    [SerializeField] private Button continueButton;
    [SerializeField] public Button guestButton;

    [Header("Paneles a Controlar")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject playContainer;

    [Header("Perfiles de Usuario (Nuevo)")]
    [Tooltip("El objeto que contendr� los botones de los perfiles guardados.")]
    [SerializeField] private Transform userButtonsContainer;

    [Tooltip("El prefab del bot�n que se usar� para cada perfil.")]
    [SerializeField] private GameObject userButtonPrefab;

    [Header("Navegaci�n")]
    [Tooltip("El nombre exacto de la escena a la que se navegar�.")]
    [SerializeField] private string sceneNameToLoad;

    void Start()
    {
        // Configuramos la UI inicial
        playContainer.SetActive(false);
        loginPanel.SetActive(true);

        // Asignamos los listeners a los botones principales
        continueButton.onClick.AddListener(ConfirmNameAndContinue);
        guestButton.onClick.AddListener(ContinueAsGuest);

        // Listener para el InputField
        nameInputField.onValueChanged.AddListener(ValidateInput);
        nameInputField.onSubmit.AddListener(OnInputSubmit);

        ValidateInput("");

        // --- NUEVA FUNCI�N ---
        // Llenamos la lista de usuarios guardados
        PopulateUserList();
    }

    // --- NUEVA FUNCI�N ---
    // Crea los botones para los usuarios existentes
    private void PopulateUserList()
    {
        // Primero, limpiamos cualquier bot�n que pudiera existir de antes
        foreach (Transform child in userButtonsContainer)
        {
            Destroy(child.gameObject);
        }

        // Obtenemos la lista de usuarios desde nuestro UserManager
        List<string> users = UserManager.GetUsers();
        Debug.Log($"Se encontraron {users.Count} usuarios guardados.");

        // Por cada usuario en la lista, creamos un bot�n
        foreach (string username in users)
        {
            // Creamos una instancia del prefab del bot�n
            GameObject buttonGO = Instantiate(userButtonPrefab, userButtonsContainer);
            buttonGO.name = $"Button_{username}";

            // Obtenemos el texto del bot�n y le ponemos el nombre del usuario
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = username;
            }

            // Obtenemos el componente Button y le a�adimos un listener
            Button userButton = buttonGO.GetComponent<Button>();
            if (userButton != null)
            {
                // �Importante! Le decimos que al hacer clic, llame a LoginAsExistingUser
                // y le pasamos el nombre de ESTE usuario en concreto.
                userButton.onClick.AddListener(() => LoginAsExistingUser(username));
            }
        }
    }

    private void ValidateInput(string text)
    {
        continueButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    // Se llama cuando el usuario pulsa "Enter" en el teclado virtual
    private void OnInputSubmit(string text)
    {
        // Si el bot�n de continuar est� activo (lo que significa que hay texto v�lido)
        if (continueButton.interactable)
        {
            Debug.Log("InputField 'onSubmit' event triggered. Calling ConfirmNameAndContinue.");
            // Llama a la misma funci�n que el bot�n de continuar
            ConfirmNameAndContinue();
        }
    }

    // --- NUEVA FUNCI�N ---
    // Se llama cuando se pulsa un bot�n de perfil existente
    public void LoginAsExistingUser(string username)
    {
        Debug.Log($"Iniciando sesi�n como usuario existente: {username}");

        // Establecemos este usuario como el actual
        UserManager.SetCurrentUser(username);

        // Ocultamos el panel de login y mostramos el del juego
        loginPanel.SetActive(false);
        playContainer.SetActive(true);
    }

    // L�gica del bot�n "Continuar" (para nuevos usuarios)
    public void ConfirmNameAndContinue()
    {
        EventSystem.current.SetSelectedGameObject(null);
        string playerName = nameInputField.text;

        if (string.IsNullOrWhiteSpace(playerName)) return;

        // Guardamos el nuevo usuario y lo establecemos como actual
        UserManager.SaveUser(playerName);
        UserManager.SetCurrentUser(playerName);
        Debug.Log($"Nuevo usuario '{playerName}' guardado y establecido como actual.");
        LoadGameScene();
        loginPanel.SetActive(false);
    }

    // L�gica del bot�n "Invitado"
    public void ContinueAsGuest()
    {
        EventSystem.current.SetSelectedGameObject(null);
        Debug.Log("Continuando como invitado.");

        // Establecemos "Invitado" como usuario actual
        UserManager.SetCurrentUser("Invitado");
        LoadGameScene();
    }

    // Carga la escena especificada en el Inspector
    private void LoadGameScene()
    {
        if (string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.LogError("El nombre de la escena a cargar no est� especificado en el Inspector.");
            return;
        }
        Debug.Log($"Cargando escena: {sceneNameToLoad}");
        SceneManager.LoadScene(sceneNameToLoad);
        loginPanel.SetActive(false);
    }
}