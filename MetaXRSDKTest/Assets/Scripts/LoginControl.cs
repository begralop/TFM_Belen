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
    [SerializeField] private Transform loginButtonsContainer;
    [SerializeField] private Transform deleteButtonsContainer;
    [SerializeField] private GameObject userButtonPrefab;

    [Header("Navegación")]
    [SerializeField] private string sceneNameToLoad;

    void Start()
    {
        // ELIMINADO: Ya no necesitamos llamar a UserManager.LoadData() aquí.

        // El resto de la configuración
        continueButton.onClick.AddListener(ConfirmNameAndContinue);
        guestButton.onClick.AddListener(ContinueAsGuest);
        nameInputField.onValueChanged.AddListener(ValidateInput);
        ValidateInput("");

        // Llenamos las listas de usuarios. UserManager ya habrá cargado los datos.
        PopulateAllUserLists();
    }

    private void PopulateAllUserLists()
    {
        foreach (Transform child in loginButtonsContainer) Destroy(child.gameObject);
        foreach (Transform child in deleteButtonsContainer) Destroy(child.gameObject);

        List<string> users = UserManager.GetUsers();
        Debug.Log($"Actualizando listas con {users.Count} usuarios.");

        foreach (string username in users)
        {
            // Crear botón para el panel de LOGIN
            GameObject loginButtonGO = Instantiate(userButtonPrefab, loginButtonsContainer);
            loginButtonGO.GetComponentInChildren<TextMeshProUGUI>().text = username;
            loginButtonGO.GetComponent<Image>().color = GetColorForUsername(username);
            loginButtonGO.GetComponent<Button>().onClick.AddListener(() => LoginAsExistingUser(username));

            // Crear botón para el panel de BORRADO
            GameObject deleteButtonGO = Instantiate(userButtonPrefab, deleteButtonsContainer);
            deleteButtonGO.GetComponentInChildren<TextMeshProUGUI>().text = username;
            deleteButtonGO.GetComponent<Image>().color = GetColorForUsername(username);
            deleteButtonGO.GetComponent<Button>().onClick.AddListener(() => DeleteUserAndRefresh(username));
        }
    }

    private void DeleteUserAndRefresh(string username)
    {
        UserManager.DeleteUser(username);
        PopulateAllUserLists();
    }

    private Color GetColorForUsername(string username)
    {
        Random.InitState(username.GetHashCode());
        return new Color(Random.Range(0.4f, 1f), Random.Range(0.4f, 1f), Random.Range(0.4f, 1f));
    }

    private void ValidateInput(string text)
    {
        continueButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    public void LoginAsExistingUser(string username)
    {
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
