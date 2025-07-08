using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("UI Elements")]
    public InputField nameInputField;
    public GameObject userButtonPrefab;
    public Transform userListContainer;
    public Button createUserButton;

    [Header("Contenedores de Estado")]
    public GameObject loginContenedor;
    public GameObject juegoContenedor;


    void Start()
    {
        // Al empezar, activamos el login y desactivamos el juego
        loginContenedor.SetActive(true);
        juegoContenedor.SetActive(false);

        // Preparamos el panel de login como siempre
        PopulateUserList();
        if (createUserButton != null && nameInputField != null)
        {
            createUserButton.interactable = false;
            nameInputField.onValueChanged.AddListener(ValidateInput);
        }
    }

    public void OnCreateUserAndPlay()
    {
        string newUsername = nameInputField.text;
        if (!string.IsNullOrEmpty(newUsername))
        {
            UserManager.SaveUser(newUsername);
            UserManager.SetCurrentUser(newUsername);

            // En lugar de cargar una escena, activamos el contenedor del juego
            StartGame();
        }
    }

    private void OnUserSelected(string username)
    {
        UserManager.SetCurrentUser(username);

        // En lugar de cargar una escena, activamos el contenedor del juego
        StartGame();
    }

    private void StartGame()
    {
        loginContenedor.SetActive(false);
        juegoContenedor.SetActive(true);

        // Opcional: Aquí podrías llamar a una función para iniciar/reiniciar el puzzle
        // FindObjectOfType<GameGenerator>().InitializePuzzle();
    }

    void ValidateInput(string input)
    {
        createUserButton.interactable = !string.IsNullOrEmpty(input);
    }

    private void PopulateUserList()
    {
        // (Este método se queda exactamente igual que antes, no hace falta cambiarlo)
        foreach (Transform child in userListContainer)
        {
            Destroy(child.gameObject);
        }

        string guestUsername = "Invitado";
        GameObject guestButton = Instantiate(userButtonPrefab, userListContainer);
        Image guestButtonImage = guestButton.GetComponent<Image>();
        if (guestButtonImage != null)
        {
            guestButtonImage.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }
        guestButton.GetComponentInChildren<TextMeshProUGUI>().text = guestUsername;
        guestButton.GetComponent<Button>().onClick.AddListener(() => OnUserSelected(guestUsername));

        List<string> users = UserManager.GetUsers();
        foreach (string username in users)
        {
            GameObject userButton = Instantiate(userButtonPrefab, userListContainer);
            Image buttonImage = userButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            }
            userButton.GetComponentInChildren<TextMeshProUGUI>().text = username;
            userButton.GetComponent<Button>().onClick.AddListener(() => OnUserSelected(username));
        }
    }
}