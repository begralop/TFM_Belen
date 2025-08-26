using UnityEngine;
using UnityEngine.UI; // Asegúrate de tener esta línea para los botones

public class GameModeSelector : MonoBehaviour
{
    [Header("Paneles y Contenedores")]
    [Tooltip("Arrastra aquí el panel principal que contiene los botones 'Juego libre' y 'Por niveles'.")]
    public GameObject panelSeleccionModo;

    [Tooltip("Arrastra aquí el objeto 'PlayContainer' que contiene todos los elementos del juego.")]
    public GameObject playContainer;

    [Tooltip("Arrastra aquí el panel que mostrará la selección de niveles.")]
    public GameObject panelDeNiveles;

    void Start()
    {
        // Al iniciar, nos aseguramos de que solo el panel de selección esté visible.
        panelSeleccionModo.SetActive(true);
        playContainer.SetActive(false);
        panelDeNiveles.SetActive(false);
    }

    /// <summary>
    /// Este método se llamará al pulsar el botón "Juego libre".
    /// </summary>
    public void IniciarJuegoLibre()
    {
        Debug.Log("Iniciando Juego Libre...");
        panelSeleccionModo.SetActive(false); // Ocultamos el menú principal
        playContainer.SetActive(true);       // Mostramos el contenedor del juego
    }

    /// <summary>
    /// Este método se llamará al pulsar el botón "Por niveles".
    /// </summary>
    public void MostrarPanelDeNiveles()
    {
        Debug.Log("Mostrando Panel de Niveles...");
        panelSeleccionModo.SetActive(false); // Ocultamos el menú principal
        panelDeNiveles.SetActive(true);      // Mostramos el panel de niveles
    }

    /// <summary>
    /// (Opcional) Método para un botón de "Atrás" en el panel de niveles.
    /// </summary>
    public void VolverAlMenuPrincipal()
    {
        Debug.Log("Volviendo al menú principal...");
        playContainer.SetActive(false);
        panelDeNiveles.SetActive(false);
        panelSeleccionModo.SetActive(true); // Mostramos de nuevo el menú principal
    }
}