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

    // Estado interno
    private bool memoryModeActive = false;
    private bool memoryModeEnabled = false;
    private float visibleTime = 2f;
    private float hiddenTime = 3f;

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
                    buttonImage.color = HexToColor("008A06"); // Rojo oscuro
                }
                else
                {
                    buttonImage.color = HexToColor("58DB5E"); // Azul claro
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
                    // Color rojo cuando el modo está activo (el botón dirá "Detener")
                    buttonImage.color = HexToColor("0040B0"); // Un rojo para indicar "detener"
                }
                else
                {
                    // Color azul/verde cuando está inactivo (el botón dirá "Iniciar")
                    buttonImage.color = HexToColor("85A5DB"); // Un azul para indicar "iniciar"
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

    public void ActivateMemoryDirect()
    {
        if (!memoryModeEnabled)
        {
            Debug.LogWarning("Primero debes habilitar el modo memoria");
            return;
        }

        // Toggle entre activar y desactivar
        if (memoryModeActive)
        {
            memoryModeActive = false;
            StopMemoryMode();
            UpdateDebugInfo("Modo memoria DETENIDO");
        }
        else
        {
            // Verificar si hay cubos, si no, generar el juego primero
            GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

            if (cubes.Length == 0)
            {
                UpdateDebugInfo("No hay cubos - generando juego");

                // Verificar si hay un puzzle seleccionado
                GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
                if (curvedBackground != null)
                {
                    Image bgImage = curvedBackground.GetComponent<Image>();
                    if (bgImage != null && bgImage.sprite != null)
                    {
                        // Hay un puzzle seleccionado, generar el juego
                        if (gameGenerator != null)
                        {
                            gameGenerator.GenerateGame();
                            StartCoroutine(ActivateMemoryAfterGeneration());
                        }
                    }
                    else
                    {
                        // No hay puzzle seleccionado, seleccionar uno aleatorio
                        SelectRandomPuzzle();
                        StartCoroutine(GenerateAndActivateMemory());
                    }
                }
                else
                {
                    UpdateDebugInfo("ERROR: No se encontró curvedBackground");
                }
            }
            else
            {
                memoryModeActive = true;
                StartMemoryMode();
                UpdateDebugInfo("Modo memoria ACTIVADO");
            }
        }

        UpdateDisplays();
    }

    void SelectRandomPuzzle()
    {
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
            gameGenerator.UpdateDebugInfo(message);
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
        StopAllCubeControllers();

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        if (cubes.Length == 0)
        {
            Debug.LogWarning("No se encontraron cubos para el modo memoria");
            return;
        }

        foreach (GameObject cube in cubes)
        {
            SaveOriginalMaterials(cube);

            CubeMemoryController controller = cube.GetComponent<CubeMemoryController>();
            if (controller == null)
            {
                controller = cube.AddComponent<CubeMemoryController>();
            }

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
        if (memoryModeActive)
        {
            ForceDisableMemoryMode();
        }
    }

    void OnDestroy()
    {
        StopMemoryMode();
    }
}

/// <summary>
/// Controlador individual para cada cubo en modo memoria
/// </summary>
public class CubeMemoryController : MonoBehaviour
{
    private float visibleTime;
    private float hiddenTime;
    private bool isVisible;
    private Material grayMaterial;
    private Dictionary<string, Material> originalMaterials = new Dictionary<string, Material>();
    private Coroutine memoryCoroutine;

    public void Initialize(float visible, float hidden, float startOffset, Material gray)
    {
        visibleTime = visible;
        hiddenTime = hidden;
        grayMaterial = gray;

        SaveOriginalMaterials();

        float cycleTime = visible + hidden;
        float offsetInCycle = startOffset % cycleTime;
        isVisible = offsetInCycle < visible;

        if (!isVisible)
        {
            ApplyGrayMaterial();
        }

        float remainingTime = isVisible ?
            (visible - offsetInCycle) :
            (cycleTime - offsetInCycle);

        memoryCoroutine = StartCoroutine(MemoryCycle(remainingTime));
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

        while (true)
        {
            if (isVisible)
            {
                RestoreOriginalMaterials();
                yield return new WaitForSeconds(visibleTime);
                isVisible = false;
            }
            else
            {
                ApplyGrayMaterial();
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
            // Solo aplicar gris a las caras incorrectas (no a Face1)
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
        RestoreOriginalMaterials();
    }

    void OnDestroy()
    {
        StopMemoryMode();
    }
}