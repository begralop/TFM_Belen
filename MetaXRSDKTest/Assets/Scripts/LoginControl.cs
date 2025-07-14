using UnityEngine;
using UnityEngine.UI; // Necesario para el componente Button
using TMPro; // Necesario para TextMeshPro Input Field

public class LoginControl : MonoBehaviour
{
    [Header("Paneles de Juego")]
    public GameObject PanelLogin;
    public GameObject PlayContainer;
    public Button guestButton;

    void Start()
    {
        PlayContainer.SetActive(false);
        PanelLogin.SetActive(true);

        if (guestButton != null)
        {
            guestButton.onClick.AddListener(ContinueAsGuest);
        }
    }

    public void ContinueAsGuest()
    {
        Debug.Log("Botón de invitado pulsado. Ocultando PanelLogin y mostrando PlayContainer.");

        if (PanelLogin != null)
        {
            PanelLogin.SetActive(false); // Oculta el panel de login
        }
        else
        {
            Debug.LogError("La referencia a PanelLogin no está asignada en el Inspector.");
        }

        if (PlayContainer != null)
        {
            PlayContainer.SetActive(true); // Muestra el contenedor del juego
        }
        else
        {
            Debug.LogError("La referencia a PlayContainer no está asignada en el Inspector.");
        }
    }
}