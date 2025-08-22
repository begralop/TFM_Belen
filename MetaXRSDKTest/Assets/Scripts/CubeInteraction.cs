using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Text;

public class CubeInteraction : MonoBehaviour
{
    private GameGenerator gameGenerator;
    private HintSystem hintSystem; // Referencia al sistema de pistas
    private bool isPlaced = false; // Variable para asegurarse de que solo se cuente una vez
    private XRGrabInteractable grabInteractable; // Referencia al componente de agarre

    // NUEVO: StringBuilder para debug
    private StringBuilder debugInfo = new StringBuilder();

    void Start()
    {
        UpdateDebugInfo("=== INICIANDO CubeInteraction ===");

        // Buscar y asignar el componente GameGenerator
        gameGenerator = FindObjectOfType<GameGenerator>();
        if (gameGenerator != null)
        {
            UpdateDebugInfo($"GameGenerator encontrado");
        }
        else
        {
            UpdateDebugInfo($"ERROR: GameGenerator NO encontrado");
        }

        // Buscar y asignar el sistema de pistas
        hintSystem = FindObjectOfType<HintSystem>();
        if (hintSystem == null)
        {
            UpdateDebugInfo($"ERROR: No se encontró HintSystem para el cubo {gameObject.name}");
            Debug.LogWarning($"No se encontró HintSystem para el cubo {gameObject.name}");
        }
        else
        {
            UpdateDebugInfo($"HintSystem encontrado correctamente");
        }

        // Obtener el componente XRGrabInteractable
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Suscribirse a los eventos de agarre
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            UpdateDebugInfo($"Eventos de agarre configurados para {gameObject.name}");
            Debug.Log($"Eventos de agarre configurados para {gameObject.name}");
        }
        else
        {
            UpdateDebugInfo($"ERROR: No se encontró XRGrabInteractable en {gameObject.name}");
            Debug.LogWarning($"No se encontró XRGrabInteractable en {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse de los eventos para evitar errores
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    /// <summary>
    /// Se llama cuando se agarra el cubo
    /// </summary>
    void OnGrabbed(SelectEnterEventArgs args)
    {
        UpdateDebugInfo($"=== EVENTO OnGrabbed DISPARADO ===");
        UpdateDebugInfo($"CUBO AGARRADO: {gameObject.name}");
        Debug.Log($"CUBO AGARRADO: {gameObject.name}");

        // Verificar el estado del HintSystem
        if (hintSystem == null)
        {
            UpdateDebugInfo("ERROR: hintSystem es NULL en OnGrabbed");
            Debug.LogError("hintSystem es NULL en OnGrabbed");
            return;
        }

        UpdateDebugInfo($"HintSystem existe: {hintSystem != null}");

        bool hintsEnabled = hintSystem.AreHintsEnabled();
        UpdateDebugInfo($"Pistas habilitadas: {hintsEnabled}");

        if (hintsEnabled)
        {
            UpdateDebugInfo("Las pistas están ACTIVADAS, procesando...");

            // Extraer fila y columna del nombre del cubo
            string[] splitName = gameObject.name.Split('_');
            UpdateDebugInfo($"Nombre dividido en {splitName.Length} partes");

            if (splitName.Length >= 3)
            {
                UpdateDebugInfo($"Partes del nombre: [{string.Join(", ", splitName)}]");

                int row, col;
                bool rowParsed = int.TryParse(splitName[1], out row);
                bool colParsed = int.TryParse(splitName[2], out col);

                UpdateDebugInfo($"Row parseado: {rowParsed} (valor: {row})");
                UpdateDebugInfo($"Col parseado: {colParsed} (valor: {col})");

                if (rowParsed && colParsed)
                {
                    UpdateDebugInfo($"LLAMANDO A ShowGreenMagnetForCube({row},{col})");
                    Debug.Log($"Mostrando imán verde para cubo {gameObject.name} en posición ({row},{col})");

                    // LLAMADA CRÍTICA
                    hintSystem.ShowGreenMagnetForCube(row, col);

                    UpdateDebugInfo($"ShowGreenMagnetForCube LLAMADO EXITOSAMENTE");
                }
                else
                {
                    UpdateDebugInfo($"ERROR: No se pudo parsear row o col");
                }
            }
            else
            {
                UpdateDebugInfo($"ERROR: Nombre del cubo no tiene el formato esperado Cube_X_Y");
            }
        }
        else
        {
            UpdateDebugInfo("Las pistas están DESACTIVADAS, no se muestra imán");
        }
    }

    /// <summary>
    /// Se llama cuando se suelta el cubo
    /// </summary>
    void OnReleased(SelectExitEventArgs args)
    {
        UpdateDebugInfo($"=== EVENTO OnReleased DISPARADO ===");
        UpdateDebugInfo($"Cubo {gameObject.name} fue soltado");
        Debug.Log($"Cubo {gameObject.name} fue soltado");

        // Restaurar el color del imán cuando se suelta el cubo
        if (hintSystem != null)
        {
            bool hintsEnabled = hintSystem.AreHintsEnabled();
            UpdateDebugInfo($"Pistas habilitadas al soltar: {hintsEnabled}");

            if (hintsEnabled)
            {
                UpdateDebugInfo("Llamando a RestoreMagnetColors()");
                hintSystem.RestoreMagnetColors();
                UpdateDebugInfo("RestoreMagnetColors() llamado exitosamente");
            }
        }
        else
        {
            UpdateDebugInfo("ERROR: hintSystem es NULL en OnReleased");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isPlaced && other.CompareTag("refCube"))
        {
            isPlaced = true;
            if (gameGenerator != null)
            {
                gameGenerator.OnCubePlaced();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isPlaced && other.CompareTag("refCube"))
        {
            isPlaced = false;
            if (gameGenerator != null)
            {
                gameGenerator.OnCubeRemoved();
            }
        }
    }

    /// <summary>
    /// Actualiza la información de debug
    /// </summary>
    void UpdateDebugInfo(string message)
    {
        debugInfo.AppendLine($"[CubeInteraction {gameObject.name}] {message}");

        // Imprimir en la consola
        Debug.Log($"[CubeInteraction {gameObject.name}] {message}");

        // Si el gameGenerator tiene panel de debug, actualizar también ahí
        if (gameGenerator != null && gameGenerator.debugText != null)
        {
            gameGenerator.debugText.text += $"\n[CUBE] {message}";

            // Limitar el tamaño del texto
            string currentText = gameGenerator.debugText.text;
            string[] lines = currentText.Split('\n');
            if (lines.Length > 30)
            {
                gameGenerator.debugText.text = string.Join("\n", lines, lines.Length - 30, 30);
            }
        }
    }
}