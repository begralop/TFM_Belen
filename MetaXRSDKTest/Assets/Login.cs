using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Login : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    [Tooltip("El panel de login que se muestra al inicio.")]
    [SerializeField] private GameObject loginPanel;

    [Tooltip("El contenedor con los objetos del juego que se activará.")]
    [SerializeField] private GameObject playContainer;

    [Tooltip("El GameObject del teclado virtual.")]
    [SerializeField] private GameObject virtualKeyboard;

    [Tooltip("El campo de texto para el nombre de usuario.")]
    [SerializeField] private TMP_InputField nameInputField;

    // Variable para guardar el nombre del jugador actual
    private string currentPlayerName;

    void Start()
    {
        // Al empezar, nos aseguramos de que solo se vea el panel de login
        loginPanel.SetActive(true);
        playContainer.SetActive(false);
        virtualKeyboard.SetActive(false);
    }

    // --- MÉTODOS PÚBLICOS PARA LOS EVENTOS ONCLICK ---

    /// <summary>
    /// Muestra el teclado virtual. Lo llamaremos desde el InputField.
    /// </summary>
    public void ShowKeyboard()
    {
        virtualKeyboard.SetActive(true);
    }

    /// <summary>
    /// Inicia el juego usando el nombre del InputField.
    /// Lo llamaremos desde el botón "Crear usuario".
    /// </summary>
    public void CreateUserAndPlay()
    {
        string username = nameInputField.text;

        // Si el usuario no escribe nada, le ponemos un nombre por defecto.
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "Jugador";
        }

        StartGame(username);
    }

    /// <summary>
    /// Inicia el juego directamente como "Invitado".
    /// Lo llamaremos desde el botón "Jugar como invitado".
    /// </summary>
    public void PlayAsGuest()
    {
        StartGame("Invitado");
    }

    // --- LÓGICA PRIVADA ---

    /// <summary>
    /// Se encarga de ocultar la UI de login y mostrar el juego.
    /// </summary>
    private void StartGame(string playerName)
    {
        currentPlayerName = playerName;
        Debug.Log($"¡Bienvenido! Iniciando juego para: {currentPlayerName}");

        // Ocultamos los paneles de login y el teclado
        loginPanel.SetActive(false);
        virtualKeyboard.SetActive(false);

        // Activamos el contenedor del juego
        playContainer.SetActive(true);

        // Opcional: Aquí podrías llamar a otro script para pasarle el nombre del jugador
        // if (playContainer.GetComponent<GameManager>() != null)
        // {
        //     playContainer.GetComponent<GameManager>().InitializePlayer(currentPlayerName);
        // }
    }
}
