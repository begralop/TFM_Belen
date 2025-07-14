using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Es una buena pr�ctica asegurarse de que el namespace de Oculus est� presente.
// Aunque no suele ser necesario si el paquete est� bien instalado.
// using OVR; 

public class LoginControl : MonoBehaviour
{
    [Header("Componentes de UI")]
    [Tooltip("Arrastra aqu� el objeto Input Field est�ndar.")]
    [SerializeField] private InputField nameInputField; // CAMBIADO: De TextMeshProUGUI a InputField

    [Tooltip("El bot�n para confirmar el nombre y continuar.")]
    [SerializeField] private Button continueButton;

    [Tooltip("El bot�n para continuar como invitado.")]
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
        // El bot�n de continuar empieza desactivado
        playContainer.SetActive(false);
        loginPanel.SetActive(true);

        // Asignamos los listeners a los botones
        continueButton.onClick.AddListener(ConfirmNameAndContinue);
        guestButton.onClick.AddListener(ContinueAsGuest);

        // A�adimos un listener al InputField para que reaccione cuando el texto cambia
        nameInputField.onValueChanged.AddListener(ValidateInput);
    }

    // Este m�todo se llama cada vez que el texto en el InputField cambia
    private void ValidateInput(string text)
    {
        // Activa el bot�n de continuar solo si hay texto escrito
        continueButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    // L�gica del bot�n "Continuar"
    public void ConfirmNameAndContinue()
    {
        // CAMBIADO: Obtenemos el texto del InputField
        string playerName = nameInputField.text;

        // Comprobaci�n final por si acaso
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Intento de continuar sin un nombre v�lido.");
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        Debug.Log($"Nombre del jugador '{playerName}' guardado en PlayerPrefs.");

        loginPanel.SetActive(false);
        playContainer.SetActive(true);
    }

    // L�gica del bot�n "Invitado"
    public void ContinueAsGuest()
    {
        Debug.Log("Bot�n de invitado pulsado. Ocultando login y mostrando el juego.");

        // Guardamos "Invitado" como nombre por defecto
        PlayerPrefs.SetString(PlayerNameKey, "Invitado");
        PlayerPrefs.Save();

        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("La referencia a loginPanel no est� asignada en el Inspector.");
        }

        if (playContainer != null)
        {
            playContainer.SetActive(true);
        }
        else
        {
            Debug.LogError("La referencia a playContainer no est� asignada en el Inspector.");
        }
    }
}