using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryModeSystem : MonoBehaviour
{
    [Header("Botón Principal")]
    [Tooltip("Botón para activar/desactivar modo memoria")]
    public Button memoryModeMainButton;
    [Tooltip("Texto del botón principal")]
    public TextMeshProUGUI memoryModeMainButtonText;

    [Header("Panel de Control")]
    [Tooltip("Panel con controles de tiempo que aparece al activar el modo")]
    public GameObject memoryControlPanel;

    [Header("Botón de Activación Directa")]
    [Tooltip("Botón para activar directamente el modo memoria")]
    public Button activateMemoryButton;
    [Tooltip("Texto del botón de activación")]
    public TextMeshProUGUI activateButtonText;

    [Header("Control de Tiempo Visible")]
    [Tooltip("Texto que muestra el tiempo visible")]
    public TextMeshProUGUI visibleTimeText;
    [Tooltip("Botón para aumentar tiempo visible")]
    public Button visiblePlusButton;
    [Tooltip("Botón para disminuir tiempo visible")]
    public Button visibleMinusButton;

    [Header("Control de Tiempo Oculto")]
    [Tooltip("Texto que muestra el tiempo oculto")]
    public TextMeshProUGUI hiddenTimeText;
    [Tooltip("Botón para aumentar tiempo oculto")]
    public Button hiddenPlusButton;
    [Tooltip("Botón para disminuir tiempo oculto")]
    public Button hiddenMinusButton;

    [Header("Configuración")]
    [Tooltip("Tiempo mínimo en segundos")]
    public float minTime = 1f;
    [Tooltip("Tiempo máximo en segundos")]
    public float maxTime = 30f;
    [Tooltip("Incremento/decremento por click")]
    public float timeStep = 1f;

    [Header("Material Gris para Ocultar")]
    public Material grayMaterial;

    [Header("Configuración de Grid Aleatorio")]
    [Tooltip("Tamaño mínimo del grid aleatorio")]
    public int minGridSize = 2;
    [Tooltip("Tamaño máximo del grid aleatorio")]
    public int maxGridSize = 4;

    // Estado interno
    private bool memoryModeActive = false;
    private bool memoryModeEnabled = false;
    private bool memoryModePaused = false;  // NUEVO: Estado de pausa
    private float visibleTime = 2f;
    private float hiddenTime = 3f;

    // Variables para recordar el tamaño del grid actual
    private int currentRows = 0;
    private int currentColumns = 0;
    private bool needsRegeneration = false;

    // Referencias
    private GameGenerator gameGenerator;
    private HintSystem hintSystem;
    private Dictionary<GameObject, CubeMemoryController> cubeControllers = new Dictionary<GameObject, CubeMemoryController>();
    private Dictionary<GameObject, Dictionary<string, Material>> originalCubeMaterials = new Dictionary<GameObject, Dictionary<string, Material>>();

    void Start()
    {
        // Buscar referencias
        gameGenerator = FindObjectOfType<GameGenerator>();
        hintSystem = FindObjectOfType<HintSystem>();

        // Crear material gris por defecto si no está asignado
        if (grayMaterial == null)
        {
            grayMaterial = new Material(Shader.Find("Standard"));
            grayMaterial.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            grayMaterial.SetFloat("_Metallic", 0f);
            grayMaterial.SetFloat("_Glossiness", 0.1f);
        }

        // Configurar botones
        SetupButtons();

        // Actualizar displays iniciales
        UpdateDisplays();

        // Ocultar panel de control inicialmente
        if (memoryControlPanel != null)
            memoryControlPanel.SetActive(false);

        // Obtener el tamaño inicial del grid
        UpdateCurrentGridSize();
    }

    void UpdateCurrentGridSize()
    {
        GridCreator grid = FindObjectOfType<GridCreator>();
        if (grid != null)
        {
            currentRows = grid.rows;
            currentColumns = grid.columns;
        }
    }

    void SetupButtons()
    {
        // Botón principal
        if (memoryModeMainButton != null)
            memoryModeMainButton.onClick.AddListener(ToggleMemoryMode);

        // Botón de activación directa
        if (activateMemoryButton != null)
            activateMemoryButton.onClick.AddListener(ActivateMemoryDirect);

        // Botones de tiempo visible
        if (visiblePlusButton != null)
            visiblePlusButton.onClick.AddListener(() => AdjustVisibleTime(timeStep));

        if (visibleMinusButton != null)
            visibleMinusButton.onClick.AddListener(() => AdjustVisibleTime(-timeStep));

        // Botones de tiempo oculto
        if (hiddenPlusButton != null)
            hiddenPlusButton.onClick.AddListener(() => AdjustHiddenTime(timeStep));

        if (hiddenMinusButton != null)
            hiddenMinusButton.onClick.AddListener(() => AdjustHiddenTime(-timeStep));
    }

    void AdjustVisibleTime(float delta)
    {
        visibleTime = Mathf.Clamp(visibleTime + delta, minTime, maxTime);
        UpdateDisplays();

        if (memoryModeActive)
        {
            UpdateAllCubeControllers();
        }
    }

    void AdjustHiddenTime(float delta)
    {
        hiddenTime = Mathf.Clamp(hiddenTime + delta, minTime, maxTime);
        UpdateDisplays();

        if (memoryModeActive)
        {
            UpdateAllCubeControllers();
        }
    }

    void UpdateDisplays()
    {
        if (visibleTimeText != null)
            visibleTimeText.text = $"{visibleTime:F1}s";

        if (hiddenTimeText != null)
            hiddenTimeText.text = $"{hiddenTime:F1}s";

        if (memoryModeMainButtonText != null)
            memoryModeMainButtonText.text = memoryModeEnabled ? "Desactivar memoria" : "Activar memoria";

        if (activateButtonText != null)
            activateButtonText.text = memoryModeActive ? "Detener" : "Iniciar";

        UpdateButtonColor();
    }

    void UpdateButtonColor()
    {
        if (memoryModeMainButton != null)
        {
            Image buttonImage = memoryModeMainButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (memoryModeEnabled)
                {
                    buttonImage.color = HexToColor("008A06"); // Verde oscuro
                }
                else
                {
                    buttonImage.color = HexToColor("58DB5E"); // Verde claro
                }
            }
        }
        if (activateMemoryButton != null)
        {
            Image buttonImage = activateMemoryButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (memoryModeActive)
                {
                    buttonImage.color = HexToColor("0040B0"); // Azul oscuro
                }
                else
                {
                    buttonImage.color = HexToColor("85A5DB"); // Azul claro
                }
            }
        }
    }

    Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    void ToggleMemoryMode()
    {
        memoryModeEnabled = !memoryModeEnabled;

        if (memoryModeEnabled)
        {
            // Si las pistas están activas, desactivarlas
            if (hintSystem != null && hintSystem.AreHintsEnabled())
            {
                Debug.Log("Desactivando pistas para habilitar modo memoria");
                hintSystem.ForceDisableHints();
            }

            // Mostrar panel de control
            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(true);

            UpdateDebugInfo("Modo memoria habilitado - usa el botón Iniciar");
        }
        else
        {
            // Desactivar todo
            memoryModeEnabled = false;
            memoryModeActive = false;

            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(false);

            StopMemoryMode();
        }

        UpdateDisplays();
        Debug.Log($"Modo Memoria Habilitado: {memoryModeEnabled}");
    }

    public void OnGridSizeChanged(int newRows, int newColumns)
    {
        UpdateDebugInfo($"Grid cambió de {currentRows}x{currentColumns} a {newRows}x{newColumns}");

        bool sizeChanged = (currentRows != newRows || currentColumns != newColumns);

        if (sizeChanged)
        {
            // Actualizar el tamaño actual
            currentRows = newRows;
            currentColumns = newColumns;

            // Si el modo memoria está activo, detenerlo primero
            if (memoryModeActive)
            {
                memoryModeActive = false;
                StopMemoryMode();
                UpdateDebugInfo("Modo memoria detenido por cambio de grid");
            }

            // Limpiar todos los controladores de memoria existentes
            StopAllCubeControllers();
            cubeControllers.Clear();
            originalCubeMaterials.Clear();

            // Marcar que necesita regeneración cuando se vuelva a activar
            if (memoryModeEnabled)
            {
                needsRegeneration = true;
                UpdateDebugInfo($"Grid cambió a {newRows}x{newColumns} - Presiona 'Iniciar' para regenerar con el nuevo grid");
            }

            UpdateDisplays();
        }
    }


    // Agregar estos métodos en MemoryModeSystem.cs (después de los otros métodos públicos):

    /// <summary>
    /// Verifica si el modo memoria está actualmente activo (no solo habilitado)
    /// </summary>
    public bool IsMemoryModeActive()
    {
        return memoryModeActive;  // Retorna true si está activo, sin importar si está pausado
    }

    /// <summary>
    /// Verifica si el modo memoria está pausado
    /// </summary>
    public bool IsMemoryModePaused()
    {
        return memoryModeActive && memoryModePaused;
    }

    /// <summary>
    /// Verifica si el modo memoria está activo Y funcionando (no pausado)
    /// </summary>
    public bool IsMemoryModeRunning()
    {
        return memoryModeActive && !memoryModePaused;
    }
    // Y actualizar el método ActivateMemoryDirect para la lógica del botón Detener:

    public void ActivateMemoryDirect()
    {
        // NUEVO: Si hay un panel de resultado activo, simplemente cerrar sin mostrar nada
        if (gameGenerator != null && gameGenerator.IsResultPanelActive())
        {
            // Si intentamos detener el modo memoria con un panel activo, solo detenerlo sin mostrar nada
            if (memoryModeActive)
            {
                memoryModeActive = false;
                memoryModePaused = false; // Asegurarse de limpiar el estado de pausa
                StopMemoryMode();
                UpdateDebugInfo("Modo memoria DETENIDO - Panel de resultado activo, no se muestra panel adicional");
                UpdateDisplays();
                return;
            }
        }

        gameGenerator.CloseResultPanel();

        if (!memoryModeEnabled)
        {
            Debug.LogWarning("Primero debes habilitar el modo memoria");
            return;
        }

        // Toggle entre activar y desactivar
        if (memoryModeActive)
        {
            memoryModeActive = false;
            memoryModePaused = false; // Limpiar estado de pausa al detener
            StopMemoryMode();

            // IMPORTANTE: Verificar si el panel de resultado ya está activo
            if (gameGenerator != null)
            {
                gameGenerator.PauseTimer();

                // Solo mostrar el panel de modo memoria si NO hay otro panel activo
                if (!gameGenerator.IsResultPanelActive())
                {
                    gameGenerator.ShowMemoryModeStoppedPanel();
                    UpdateDebugInfo("Modo memoria DETENIDO - Mostrando panel de decisión");
                }
                else
                {
                    UpdateDebugInfo("Modo memoria DETENIDO - Panel de resultado ya activo, no se muestra panel adicional");
                }
            }
        }
        else
        {
            // SIEMPRE verificar el tamaño actual del grid antes de proceder
            GridCreator grid = FindObjectOfType<GridCreator>();
            if (grid != null)
            {
                bool gridChanged = (grid.rows != currentRows || grid.columns != currentColumns);
                if (gridChanged)
                {
                    UpdateDebugInfo($"Grid detectado diferente: actual {grid.rows}x{grid.columns} vs memoria {currentRows}x{currentColumns}");
                    currentRows = grid.rows;
                    currentColumns = grid.columns;

                    // Limpiar todos los cubos y magnetos existentes
                    GameObject[] existingCubes = GameObject.FindGameObjectsWithTag("cube");
                    GameObject[] existingMagnets = GameObject.FindGameObjectsWithTag("refCube");

                    foreach (GameObject cube in existingCubes)
                    {
                        Destroy(cube);
                    }
                    foreach (GameObject magnet in existingMagnets)
                    {
                        Destroy(magnet);
                    }

                    UpdateDebugInfo($"Limpiados {existingCubes.Length} cubos y {existingMagnets.Length} magnetos del grid anterior");
                    needsRegeneration = true;
                }
            }

            // Verificar si hay cubos Y si coinciden con el grid actual
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
            int expectedCubes = currentRows * currentColumns;

            bool needsGeneration = (cubes.Length == 0) || (cubes.Length != expectedCubes) || needsRegeneration;

            if (needsGeneration)
            {
                UpdateDebugInfo($"Regeneración necesaria: Cubos actuales={cubes.Length}, Esperados={expectedCubes}, Grid={currentRows}x{currentColumns}");

                // Limpiar cualquier cubo restante
                foreach (GameObject cube in cubes)
                {
                    Destroy(cube);
                }
                GameObject[] magnets = GameObject.FindGameObjectsWithTag("refCube");
                foreach (GameObject magnet in magnets)
                {
                    Destroy(magnet);
                }

                // Verificar si hay puzzle seleccionado
                GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
                if (curvedBackground != null)
                {
                    Image bgImage = curvedBackground.GetComponent<Image>();
                    if (bgImage != null && bgImage.sprite != null)
                    {
                        if (gameGenerator != null)
                        {
                            // Actualizar el grid en GameGenerator antes de generar
                            gameGenerator.rows = currentRows;
                            gameGenerator.columns = currentColumns;
                            gameGenerator.GenerateGame();
                            StartCoroutine(ActivateMemoryAfterGeneration());
                        }
                    }
                    else
                    {
                        SelectRandomPuzzle();
                        StartCoroutine(GenerateAndActivateMemory());
                    }
                }

                needsRegeneration = false;
            }
            else
            {
                // Los cubos existen y coinciden con el grid
                memoryModeActive = true;
                memoryModePaused = false; // Asegurarse de que no está pausado al activar
                StartMemoryMode();

                // NUEVO: Reanudar el contador de tiempo
                if (gameGenerator != null)
                {
                    gameGenerator.ResumeTimer();
                }

                UpdateDebugInfo($"Modo memoria ACTIVADO con {cubes.Length} cubos (grid {currentRows}x{currentColumns}) - Timer reanudado");
            }
        }

        UpdateDisplays();
    }
    /// <summary>
    /// Pausa el modo memoria manteniendo el estado pero deteniendo los efectos visuales
    /// </summary>
    public void PauseMemoryMode()
    {
        if (memoryModeActive && !memoryModePaused)
        {
            memoryModePaused = true;

            // Pausar todos los controladores de cubos
            foreach (var kvp in cubeControllers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.PauseEffects();
                }
            }

            UpdateDebugInfo("Modo memoria PAUSADO - efectos visuales detenidos");
        }
    }

    /// <summary>
    /// Reanuda el modo memoria después de una pausa
    /// </summary>
    public void UnpauseMemoryMode()
    {
        if (memoryModeActive && memoryModePaused)
        {
            memoryModePaused = false;

            // Reanudar todos los controladores de cubos
            foreach (var kvp in cubeControllers)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ResumeEffects();
                }
            }

            UpdateDebugInfo("Modo memoria REANUDADO - efectos visuales activos");
        }
    }

    // Reemplazar estos métodos en MemoryModeSystem.cs:

    /// <summary>
    /// Desactiva temporalmente el modo memoria para regeneración pero mantiene el estado para reactivarlo
    /// </summary>
    public void TemporarilyDisableForRegeneration()
    {
        if (memoryModeActive)
        {
            // Detener el modo memoria pero mantener memoryModeActive = true
            StopMemoryMode();
            // NO cambiar memoryModeActive aquí, lo mantenemos true para saber que debe reactivarse
            // memoryModeActive = true; // Mantenerlo activo
            memoryModePaused = false; // Limpiar el estado de pausa
            UpdateDebugInfo("Modo memoria temporalmente desactivado para regeneración (mantiene estado activo)");
        }
    }

    /// <summary>
    /// Reactiva el modo memoria después de regenerar el puzzle
    /// </summary>
    public void ReactivateAfterRegeneration()
    {
        if (memoryModeEnabled && memoryModeActive)
        {
            memoryModePaused = false; // Asegurarse de que no está pausado

            // Reactivar el modo memoria con los nuevos cubos
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
            if (cubes.Length > 0)
            {
                StartMemoryMode();

                // Asegurar que el timer esté corriendo
                if (gameGenerator != null)
                {
                    gameGenerator.ResumeTimer();
                }

                UpdateDebugInfo($"Modo memoria reactivado con {cubes.Length} cubos después de regeneración");
                UpdateDisplays();
            }
            else
            {
                UpdateDebugInfo("ERROR: No hay cubos para reactivar el modo memoria");
            }
        }
    }
    /// <summary>
    /// Reinicia el modo memoria con los cubos actuales (usado después de regenerar el puzzle)
    /// </summary>
    public void RestartMemoryModeWithCurrentCubes()
    {
        if (memoryModeActive && memoryModeEnabled)
        {
            // Limpiar controladores anteriores completamente
            StopAllCubeControllers();
            cubeControllers.Clear();
            originalCubeMaterials.Clear();

            // Despausar si estaba pausado
            memoryModePaused = false;

            // Buscar los nuevos cubos
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

            if (cubes.Length > 0)
            {
                // Iniciar con los nuevos cubos
                StartMemoryMode();

                // Asegurar que el timer esté corriendo
                if (gameGenerator != null)
                {
                    gameGenerator.ResumeTimer();
                }

                UpdateDebugInfo($"Modo memoria REINICIADO con {cubes.Length} nuevos cubos");
                UpdateDisplays();
            }
            else
            {
                UpdateDebugInfo("ERROR: No se encontraron cubos para reiniciar el modo memoria");
            }
        }
    }
    /// <summary>
    /// Mantiene el modo memoria activo cuando se continúa el juego
    /// </summary>
    public void MaintainMemoryModeState()
    {
        if (memoryModeActive && !memoryModePaused)
        {
            // Actualizar todos los controladores existentes con los tiempos actuales
            UpdateAllCubeControllers();
            UpdateDebugInfo("Estado del modo memoria mantenido");
        }
    }
    void RegenerateForNewGrid()
    {
        // Limpiar cubos existentes
        if (gameGenerator != null)
        {
            // Primero detener cualquier modo memoria activo
            StopMemoryMode();

            // Verificar si hay un puzzle seleccionado
            GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
            if (curvedBackground != null)
            {
                Image bgImage = curvedBackground.GetComponent<Image>();
                if (bgImage != null && bgImage.sprite != null)
                {
                    // Hay un puzzle seleccionado, regenerar el juego
                    gameGenerator.GenerateGame();
                    StartCoroutine(ActivateMemoryAfterGeneration());
                }
                else
                {
                    // No hay puzzle seleccionado, seleccionar uno aleatorio
                    SelectRandomPuzzle();
                    StartCoroutine(GenerateAndActivateMemory());
                }
            }
        }
    }

    void SelectRandomPuzzle()
    {
        // Primero, establecer un tamaño de grid aleatorio
        GridCreator gridCreator = FindObjectOfType<GridCreator>();
        if (gridCreator != null)
        {
            // Generar tamaño aleatorio usando los valores configurables
            // Asegurar que nunca exceda 4x4 para que quepa en el espacio
            int safeMaxSize = Mathf.Min(maxGridSize, 4);
            int randomRows = Random.Range(minGridSize, safeMaxSize + 1);
            int randomColumns = Random.Range(minGridSize, safeMaxSize + 1);

            // Actualizar los sliders si existen
            if (gridCreator.rowsSlider != null)
            {
                gridCreator.rowsSlider.value = randomRows;
            }
            if (gridCreator.columnsSlider != null)
            {
                gridCreator.columnsSlider.value = randomColumns;
            }

            // Cambiar el tamaño del grid
            gridCreator.changeSize(randomRows, randomColumns);

            // Actualizar nuestros valores locales
            currentRows = randomRows;
            currentColumns = randomColumns;

            UpdateDebugInfo($"Grid aleatorio establecido: {randomRows}x{randomColumns}");
        }

        // Buscar todos los toggles de puzzles
        Toggle[] puzzleToggles = FindObjectsOfType<Toggle>();
        List<Toggle> validToggles = new List<Toggle>();

        foreach (Toggle toggle in puzzleToggles)
        {
            // Verificar si es un toggle de puzzle (tiene una imagen)
            Transform contentTransform = toggle.transform.Find("Content");
            if (contentTransform != null)
            {
                Transform backgroundTransform = contentTransform.Find("Background");
                if (backgroundTransform != null)
                {
                    Image img = backgroundTransform.GetComponent<Image>();
                    if (img != null && img.sprite != null)
                    {
                        validToggles.Add(toggle);
                    }
                }
            }
        }

        if (validToggles.Count > 0)
        {
            // Seleccionar uno aleatorio
            Toggle randomToggle = validToggles[Random.Range(0, validToggles.Count)];
            randomToggle.isOn = true;
            UpdateDebugInfo($"Puzzle aleatorio seleccionado: {randomToggle.name}");
        }
        else
        {
            UpdateDebugInfo("ERROR: No se encontraron puzzles disponibles");
        }
    }

    IEnumerator GenerateAndActivateMemory()
    {
        yield return new WaitForSeconds(0.2f); // Esperar a que se seleccione el puzzle

        if (gameGenerator != null)
        {
            gameGenerator.GenerateGame();
            yield return new WaitForSeconds(0.5f);

            GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
            if (cubes.Length > 0)
            {
                memoryModeActive = true;
                StartMemoryMode();
                UpdateDebugInfo($"Puzzle aleatorio generado y modo memoria activado con {cubes.Length} cubos");
            }
        }

        UpdateDisplays();
    }

    IEnumerator ActivateMemoryAfterGeneration()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
        if (cubes.Length > 0)
        {
            memoryModeActive = true;
            StartMemoryMode();
            UpdateDebugInfo($"Juego generado y modo memoria activado con {cubes.Length} cubos");
        }
        else
        {
            UpdateDebugInfo("ERROR: No se pudieron generar los cubos");
        }

        UpdateDisplays();
    }

    public void UpdateDebugInfo(string message)
    {
        if (gameGenerator != null)
        {
            gameGenerator.UpdateDebugInfo("[MEMORIA] " + message);
        }
        else
        {
            Debug.Log($"[MEMORIA] {message}");
        }
    }

    public void ForceDisableMemoryMode()
    {
        if (memoryModeEnabled || memoryModeActive)
        {
            memoryModeEnabled = false;
            memoryModeActive = false;
            memoryModePaused = false;
            needsRegeneration = false;

            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(false);

            StopMemoryMode();
            UpdateDisplays();

            Debug.Log("Modo Memoria desactivado por activación de pistas");
        }
    }

    public bool IsMemoryModeEnabled()
    {
        return memoryModeEnabled;
    }

    void StartMemoryMode()
    {
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        if (cubes.Length == 0)
        {
            Debug.LogWarning("No se encontraron cubos para el modo memoria");
            return;
        }

        // Limpiar cualquier controlador existente antes de crear nuevos
        foreach (var kvp in cubeControllers)
        {
            if (kvp.Value != null && kvp.Key != null)
            {
                // Verificar si el cubo todavía existe
                bool cubeStillExists = false;
                foreach (GameObject cube in cubes)
                {
                    if (cube == kvp.Key)
                    {
                        cubeStillExists = true;
                        break;
                    }
                }

                // Si el cubo ya no existe, destruir el controlador
                if (!cubeStillExists)
                {
                    Destroy(kvp.Value);
                }
            }
        }

        // Limpiar el diccionario
        cubeControllers.Clear();
        originalCubeMaterials.Clear();

        // Crear nuevos controladores para todos los cubos
        foreach (GameObject cube in cubes)
        {
            SaveOriginalMaterials(cube);

            // Remover cualquier controlador existente
            CubeMemoryController existingController = cube.GetComponent<CubeMemoryController>();
            if (existingController != null)
            {
                Destroy(existingController);
            }

            // Crear nuevo controlador
            CubeMemoryController controller = cube.AddComponent<CubeMemoryController>();

            float randomOffset = Random.Range(0f, visibleTime + hiddenTime);
            controller.Initialize(visibleTime, hiddenTime, randomOffset, grayMaterial);

            cubeControllers[cube] = controller;
        }

        if (gameGenerator != null)
        {
            gameGenerator.UpdateDebugInfo($"Modo Memoria iniciado con {cubes.Length} cubos");
        }
    }

    void StopMemoryMode()
    {
        StopAllCubeControllers();
        RestoreAllOriginalMaterials();
        cubeControllers.Clear();

        if (gameGenerator != null)
        {
            gameGenerator.UpdateDebugInfo("Modo Memoria detenido");
        }
    }

    void StopAllCubeControllers()
    {
        foreach (var kvp in cubeControllers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.StopMemoryMode();
                Destroy(kvp.Value);
            }
        }
    }

    void UpdateAllCubeControllers()
    {
        foreach (var kvp in cubeControllers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateTimes(visibleTime, hiddenTime);
            }
        }
    }

    void SaveOriginalMaterials(GameObject cube)
    {
        if (!originalCubeMaterials.ContainsKey(cube))
        {
            originalCubeMaterials[cube] = new Dictionary<string, Material>();
            Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                originalCubeMaterials[cube][renderer.name] = renderer.material;
            }
        }
    }

    void RestoreAllOriginalMaterials()
    {
        foreach (var cubeEntry in originalCubeMaterials)
        {
            GameObject cube = cubeEntry.Key;
            if (cube != null)
            {
                Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (cubeEntry.Value.ContainsKey(renderer.name))
                    {
                        renderer.material = cubeEntry.Value[renderer.name];
                    }
                }
            }
        }
        originalCubeMaterials.Clear();
    }

    public void OnPuzzleChanged()
    {
        ForceDisableMemoryMode();
    }

    void OnDestroy()
    {
        StopMemoryMode();
    }
}
public class CubeMemoryController : MonoBehaviour
{
    private float visibleTime;
    private float hiddenTime;
    private bool isVisible;
    private bool isPaused = false;
    private Material grayMaterial;
    private Dictionary<string, Material> originalMaterials = new Dictionary<string, Material>();
    private Coroutine memoryCoroutine;

    // Variables para el haz de luz
    private GameObject lightBeamObject;
    private LineRenderer lightBeam;
    private Vector3 targetMagnetPosition;
    private bool hasLightBeam = false;
    private Material lightBeamMaterial;
    private Color beamColor;

    public void PauseEffects()
    {
        isPaused = true;

        // Restaurar materiales originales temporalmente
        RestoreOriginalMaterials();

        // Ocultar el haz de luz
        if (lightBeamObject != null)
        {
            lightBeamObject.SetActive(false);
        }
    }

    public void ResumeEffects()
    {
        isPaused = false;

        // Si el cubo debería estar gris, aplicarlo de nuevo Y mostrar el haz
        if (!isVisible)
        {
            ApplyGrayMaterial();
            ShowLightBeam();
        }
        else
        {
            HideLightBeam();
        }
    }

    public void Initialize(float visible, float hidden, float startOffset, Material gray)
    {
        visibleTime = visible;
        hiddenTime = hidden;
        grayMaterial = gray;

        SaveOriginalMaterials();

        // Configurar el haz de luz
        SetupLightBeam();

        float cycleTime = visible + hidden;
        float offsetInCycle = startOffset % cycleTime;
        isVisible = offsetInCycle < visible;

        // Aplicar el estado inicial correctamente
        if (!isVisible)
        {
            ApplyGrayMaterial();
            ShowLightBeam();  // Mostrar el haz si empieza en estado gris
        }
        else
        {
            RestoreOriginalMaterials();  // IMPORTANTE: Restaurar materiales originales
            HideLightBeam();  // Ocultar el haz si empieza en estado visible
        }

        float remainingTime = isVisible ?
            (visible - offsetInCycle) :
            (cycleTime - offsetInCycle);

        memoryCoroutine = StartCoroutine(MemoryCycle(remainingTime));
    }

    void SetupLightBeam()
    {
        // Obtener la posición del imán correspondiente
        string[] splitName = gameObject.name.Split('_');
        if (splitName.Length >= 3)
        {
            int row, col;
            if (int.TryParse(splitName[1], out row) && int.TryParse(splitName[2], out col))
            {
                // Buscar el GameGenerator para obtener la posición del imán
                GameGenerator gameGen = FindObjectOfType<GameGenerator>();
                if (gameGen != null)
                {
                    targetMagnetPosition = gameGen.GetMagnetPosition(row, col);

                    // Crear un GameObject separado para el haz (así no se ve afectado por los materiales del cubo)
                    lightBeamObject = new GameObject($"LightBeam_{gameObject.name}");
                    lightBeamObject.transform.SetParent(transform.parent);

                    // Crear el LineRenderer
                    lightBeam = lightBeamObject.AddComponent<LineRenderer>();

                    // Generar un color único para este cubo basado en su posición
                    beamColor = GenerateUniqueColor(row, col, gameGen.rows, gameGen.columns);

                    // Configurar el material del haz
                    CreateLightBeamMaterial();
                    lightBeam.material = lightBeamMaterial;

                    // Configurar propiedades del LineRenderer
                    lightBeam.startWidth = 0.01f;
                    lightBeam.endWidth = 0.005f;

                    // Usar más puntos para crear una curva suave
                    int curveSegments = 20;
                    lightBeam.positionCount = curveSegments;

                    // Configurar el gradiente de color con el color único
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(beamColor, 0.0f),
                            new GradientColorKey(beamColor * 0.7f, 1.0f)
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1f, 0.0f),
                            new GradientAlphaKey(0.6f, 1.0f)
                        }
                    );
                    lightBeam.colorGradient = gradient;

                    // Usar worldspace
                    lightBeam.useWorldSpace = true;

                    // Suavizar el haz
                    lightBeam.numCapVertices = 5;
                    lightBeam.numCornerVertices = 5;

                    // Establecer la curva inicial del haz
                    UpdateBeamCurve();

                    hasLightBeam = true;

                    // IMPORTANTE: Inicialmente ocultar el haz
                    lightBeamObject.SetActive(false);

                    Debug.Log($"Haz de luz configurado para {gameObject.name} con color RGB({beamColor.r:F2}, {beamColor.g:F2}, {beamColor.b:F2})");
                }
            }
        }
    }

    Color GenerateUniqueColor(int row, int col, int totalRows, int totalCols)
    {
        // Calcular un índice único para este cubo
        float normalizedIndex = (row * totalCols + col) / (float)(totalRows * totalCols);

        // Usar HSV para generar colores bien distribuidos
        float hue = normalizedIndex;
        float saturation = 0.8f;
        float value = 0.9f;

        return Color.HSVToRGB(hue, saturation, value);
    }

    void UpdateBeamCurve()
    {
        if (!hasLightBeam || lightBeam == null) return;

        Vector3 cubeCenter = transform.position;
        int segments = lightBeam.positionCount;

        // Calcular la dirección y distancia
        Vector3 direction = (targetMagnetPosition - cubeCenter).normalized;
        float distance = Vector3.Distance(cubeCenter, targetMagnetPosition);

        // Vector perpendicular para las ondas
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (Mathf.Abs(direction.y) > 0.9f)
        {
            perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        }

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            // Interpolación lineal entre el cubo y el imán
            Vector3 point = Vector3.Lerp(cubeCenter, targetMagnetPosition, t);

            // Añadir ondas laterales estáticas
            if (i > 0 && i < segments - 1)
            {
                float waveAmplitude = 0.015f * (1f - t * 0.5f);
                float wave = Mathf.Sin(t * Mathf.PI * 3) * waveAmplitude;
                point += perpendicular * wave;
            }

            lightBeam.SetPosition(i, point);
        }
    }

    void CreateLightBeamMaterial()
    {
        lightBeamMaterial = new Material(Shader.Find("Sprites/Default"));
        lightBeamMaterial.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.8f);

        // Si es posible usar un shader con emisión
        if (Shader.Find("Standard") != null)
        {
            lightBeamMaterial = new Material(Shader.Find("Standard"));
            lightBeamMaterial.SetFloat("_Mode", 3);
            lightBeamMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lightBeamMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lightBeamMaterial.SetInt("_ZWrite", 0);
            lightBeamMaterial.DisableKeyword("_ALPHATEST_ON");
            lightBeamMaterial.EnableKeyword("_ALPHABLEND_ON");
            lightBeamMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lightBeamMaterial.renderQueue = 3000;

            lightBeamMaterial.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.8f);
            lightBeamMaterial.EnableKeyword("_EMISSION");
            lightBeamMaterial.SetColor("_EmissionColor", beamColor * 0.5f);
        }
    }

    // Nuevo método para mostrar el haz de luz
    void ShowLightBeam()
    {
        if (lightBeamObject != null && !isPaused)
        {
            lightBeamObject.SetActive(true);
        }
    }

    // Nuevo método para ocultar el haz de luz
    void HideLightBeam()
    {
        if (lightBeamObject != null)
        {
            lightBeamObject.SetActive(false);
        }
    }

    void Update()
    {
        // Solo actualizar el haz si está activo y visible
        if (hasLightBeam && lightBeam != null && lightBeamObject != null && lightBeamObject.activeSelf)
        {
            // Solo actualizar si el cubo se ha movido significativamente
            Vector3 currentCubePos = transform.position;
            if (Vector3.Distance(currentCubePos, lightBeam.GetPosition(0)) > 0.01f)
            {
                UpdateBeamCurve();
            }
        }
    }

    public void UpdateTimes(float visible, float hidden)
    {
        visibleTime = visible;
        hiddenTime = hidden;
    }

    void SaveOriginalMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            originalMaterials[renderer.name] = renderer.material;
        }
    }

    IEnumerator MemoryCycle(float initialWait)
    {
        yield return new WaitForSeconds(initialWait);
        isVisible = !isVisible;

        // Actualizar el estado del haz después del cambio inicial
        if (isVisible)
        {
            HideLightBeam();
        }
        else
        {
            ShowLightBeam();
        }

        while (true)
        {
            // Si está pausado, mantener el estado actual sin cambios
            if (isPaused)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (isVisible)
            {
                RestoreOriginalMaterials();
                HideLightBeam();  // Ocultar el haz cuando el cubo es visible
                yield return new WaitForSeconds(visibleTime);
                isVisible = false;
            }
            else
            {
                ApplyGrayMaterial();
                ShowLightBeam();  // Mostrar el haz cuando el cubo está gris
                yield return new WaitForSeconds(hiddenTime);
                isVisible = true;
            }
        }
    }

    void ApplyGrayMaterial()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.name != "Face1" && grayMaterial != null)
            {
                renderer.material = grayMaterial;
            }
        }
    }

    void RestoreOriginalMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (originalMaterials.ContainsKey(renderer.name))
            {
                renderer.material = originalMaterials[renderer.name];
            }
        }
    }

    public void StopMemoryMode()
    {
        if (memoryCoroutine != null)
        {
            StopCoroutine(memoryCoroutine);
            memoryCoroutine = null;
        }

        // Limpiar el haz de luz
        if (lightBeamObject != null)
        {
            Destroy(lightBeamObject);
            lightBeamObject = null;
            lightBeam = null;
        }

        if (lightBeamMaterial != null)
        {
            Destroy(lightBeamMaterial);
            lightBeamMaterial = null;
        }

        // IMPORTANTE: Siempre restaurar los materiales originales al detener
        RestoreOriginalMaterials();
        
        // Limpiar las referencias
        originalMaterials.Clear();
    }

    void OnDestroy()
    {
        StopMemoryMode();
    }
}