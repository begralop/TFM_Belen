using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Es una buena práctica asegurarse de que el namespace de Oculus está presente.
// Aunque no suele ser necesario si el paquete está bien instalado.
// using OVR; 

public class LoginControl : MonoBehaviour
{
    [Header("Componentes de UI")]
    [Tooltip("Arrastra aquí el objeto Input Field estándar.")]
    [SerializeField] private InputField nameInputField; // CAMBIADO: De TextMeshProUGUI a InputField

    [Tooltip("El botón para confirmar el nombre y continuar.")]
    [SerializeField] private Button continueButton;

    [Tooltip("El botón para continuar como invitado.")]
    [SerializeField] public Button guestButton;

    [Header("Paneles a Controlar")]
    [Tooltip("El panel que contiene toda la UI de login.")]
    [SerializeField] private GameObject loginPanel;

    [Tooltip("El contenedor con el resto del juego.")]
    [SerializeField] private GameObject playContainer;

    private const string PlayerNameKey = "PlayerName";

    void Start()
    {

        // Configuramos la UI inicial
        // El botón de continuar empieza desactivado
        playContainer.SetActive(false);
        loginPanel.SetActive(true);

        // Asignamos los listeners a los botones
        continueButton.onClick.AddListener(ConfirmNameAndContinue);
        guestButton.onClick.AddListener(ContinueAsGuest);

        // Añadimos un listener al InputField para que reaccione cuando el texto cambia
        nameInputField.onValueChanged.AddListener(ValidateInput);
    }

    // Este método se llama cada vez que el texto en el InputField cambia
    private void ValidateInput(string text)
    {
        // Activa el botón de continuar solo si hay texto escrito
        continueButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    // Lógica del botón "Continuar"
    public void ConfirmNameAndContinue()
    {
        // CAMBIADO: Obtenemos el texto del InputField
        string playerName = nameInputField.text;

        // Comprobación final por si acaso
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Intento de continuar sin un nombre válido.");
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        Debug.Log($"Nombre del jugador '{playerName}' guardado en PlayerPrefs.");

        loginPanel.SetActive(false);
        playContainer.SetActive(true);
    }

    // Lógica del botón "Invitado"
    public void ContinueAsGuest()
    {
        Debug.Log("Botón de invitado pulsado. Ocultando login y mostrando el juego.");

        // Guardamos "Invitado" como nombre por defecto
        PlayerPrefs.SetString(PlayerNameKey, "Invitado");
        PlayerPrefs.Save();

        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("La referencia a loginPanel no está asignada en el Inspector.");
        }

        if (playContainer != null)
        {
            playContainer.SetActive(true);
        }
        else
        {
            Debug.LogError("La referencia a playContainer no está asignada en el Inspector.");
        }
    }
}