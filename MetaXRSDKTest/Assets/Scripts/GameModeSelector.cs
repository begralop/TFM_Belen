using UnityEngine;
using UnityEngine.UI; // Aseg�rate de tener esta l�nea para los botones

public class GameModeSelector : MonoBehaviour
{
    [Header("Paneles y Contenedores")]
    [Tooltip("Arrastra aqu� el panel principal que contiene los botones 'Juego libre' y 'Por niveles'.")]
    public GameObject panelSeleccionModo;

    [Tooltip("Arrastra aqu� el objeto 'PlayContainer' que contiene todos los elementos del juego.")]
    public GameObject playContainer;

    [Tooltip("Arrastra aqu� el panel que mostrar� la selecci�n de niveles.")]
    public GameObject panelDeNiveles;

    void Start()
    {
        // Al iniciar, nos aseguramos de que solo el panel de selecci�n est� visible.
        panelSeleccionModo.SetActive(true);
        playContainer.SetActive(false);
        panelDeNiveles.SetActive(false);
    }

    /// <summary>
    /// Este m�todo se llamar� al pulsar el bot�n "Juego libre".
    /// </summary>
    public void IniciarJuegoLibre()
    {
        Debug.Log("Iniciando Juego Libre...");
        panelSeleccionModo.SetActive(false); // Ocultamos el men� principal
        playContainer.SetActive(true);       // Mostramos el contenedor del juego
    }

    /// <summary>
    /// Este m�todo se llamar� al pulsar el bot�n "Por niveles".
    /// </summary>
    public void MostrarPanelDeNiveles()
    {
        Debug.Log("Mostrando Panel de Niveles...");
        panelSeleccionModo.SetActive(false); // Ocultamos el men� principal
        panelDeNiveles.SetActive(true);      // Mostramos el panel de niveles
    }

    /// <summary>
    /// (Opcional) M�todo para un bot�n de "Atr�s" en el panel de niveles.
    /// </summary>
    public void VolverAlMenuPrincipal()
    {
        Debug.Log("Volviendo al men� principal...");
        playContainer.SetActive(false);
        panelDeNiveles.SetActive(false);
        panelSeleccionModo.SetActive(true); // Mostramos de nuevo el men� principal
    }
}