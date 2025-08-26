using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryModeSystem : MonoBehaviour
{
    [Header("Bot�n Principal")]
    [Tooltip("Bot�n para activar/desactivar modo memoria")]
    public Button memoryModeMainButton;
    [Tooltip("Texto del bot�n principal")]
    public TextMeshProUGUI memoryModeMainButtonText;

    [Header("Panel de Control (aparece al activar)")]
    [Tooltip("Panel con controles de tiempo que aparece al activar el modo")]
    public GameObject memoryControlPanel;

    [Header("Indicador Visual en Mesa")]
    [Tooltip("GameObject c�rculo que aparece en la mesa - tocarlo activa el modo")]
    public GameObject memoryCircle;

    [Header("Control de Tiempo Visible")]
    [Tooltip("Texto que muestra el tiempo visible")]
    public TextMeshProUGUI visibleTimeText;
    [Tooltip("Bot�n para aumentar tiempo visible")]
    public Button visiblePlusButton;
    [Tooltip("Bot�n para disminuir tiempo visible")]
    public Button visibleMinusButton;

    [Header("Control de Tiempo Oculto")]
    [Tooltip("Texto que muestra el tiempo oculto")]
    public TextMeshProUGUI hiddenTimeText;
    [Tooltip("Bot�n para aumentar tiempo oculto")]
    public Button hiddenPlusButton;
    [Tooltip("Bot�n para disminuir tiempo oculto")]
    public Button hiddenMinusButton;

    [Header("Configuraci�n")]
    [Tooltip("Tiempo m�nimo en segundos")]
    public float minTime = 0.5f;
    [Tooltip("Tiempo m�ximo en segundos")]
    public float maxTime = 10f;
    [Tooltip("Incremento/decremento por click")]
    public float timeStep = 0.5f;

    [Header("Material Gris para Ocultar")]
    public Material grayMaterial;

    // Estado interno
    private bool memoryModeActive = false;
    private bool memoryModeEnabled = false; // Si el bot�n principal est� activado
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

        // Crear material gris por defecto si no est� asignado
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

        // Ocultar panel de control y c�rculo inicialmente
        if (memoryControlPanel != null)
            memoryControlPanel.SetActive(false);

        if (memoryCircle != null)
        {
            memoryCircle.SetActive(false);

            // Asegurarse de que tenga el detector
            MemoryCircleDetector detector = memoryCircle.GetComponent<MemoryCircleDetector>();
            if (detector == null)
            {
                detector = memoryCircle.AddComponent<MemoryCircleDetector>();
            }
        }
    }

    void SetupButtons()
    {
        // Bot�n principal
        if (memoryModeMainButton != null)
            memoryModeMainButton.onClick.AddListener(ToggleMemoryMode);

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

        // Si el modo est� activo, actualizar los controladores
        if (memoryModeActive)
        {
            UpdateAllCubeControllers();
        }
    }

    void AdjustHiddenTime(float delta)
    {
        hiddenTime = Mathf.Clamp(hiddenTime + delta, minTime, maxTime);
        UpdateDisplays();

        // Si el modo est� activo, actualizar los controladores
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
            memoryModeMainButtonText.text = memoryModeActive ? "Desactivar Memoria" : "Activar Memoria";

        // Cambiar color del bot�n principal seg�n estado
        UpdateButtonColor();
    }

    void UpdateButtonColor()
    {
        if (memoryModeMainButton != null)
        {
            Image buttonImage = memoryModeMainButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (memoryModeActive)
                {
                    // Color cuando est� activo (similar al de pistas activas)
                    buttonImage.color = HexToColor("B03000"); // Rojo oscuro
                }
                else
                {
                    // Color cuando est� inactivo (similar al de pistas inactivas)
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
            // IMPORTANTE: Si las pistas est�n activas, desactivarlas
            if (hintSystem != null && hintSystem.AreHintsEnabled())
            {
                Debug.Log("Desactivando pistas para habilitar modo memoria");
                hintSystem.ForceDisableHints();
            }

            // Mostrar panel de control y c�rculo (pero NO activar el modo a�n)
            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(true);

            if (memoryCircle != null)
            {
                memoryCircle.SetActive(true);
            }

            UpdateDebugInfo("Modo memoria habilitado - toca el c�rculo para activar");
        }
        else
        {
            // Desactivar todo
            memoryModeEnabled = false;
            memoryModeActive = false;

            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(false);

            if (memoryCircle != null)
                memoryCircle.SetActive(false);

            StopMemoryMode();
        }

        UpdateDisplays();
        Debug.Log($"Modo Memoria Habilitado: {memoryModeEnabled}");
    }

    /// <summary>
    /// M�todo llamado cuando se toca el c�rculo para activar el modo memoria
    /// </summary>
    public void ActivateMemoryModeFromCircle()
    {
        if (memoryModeEnabled && !memoryModeActive)
        {
            memoryModeActive = true;
            StartMemoryMode();
            UpdateDebugInfo("Modo memoria ACTIVADO por toque del c�rculo");

            // Opcionalmente, hacer que el c�rculo cambie de color o tenga feedback visual
            if (memoryCircle != null)
            {
                Renderer circleRenderer = memoryCircle.GetComponent<Renderer>();
                if (circleRenderer != null)
                {
                    // Cambiar a verde o color de activado
                    circleRenderer.material.color = Color.green;
                }
            }
        }
    }

    /// <summary>
    /// Verifica si se puede activar el modo desde el c�rculo
    /// </summary>
    public bool CanActivateFromCircle()
    {
        return memoryModeEnabled && !memoryModeActive;
    }

    /// <summary>
    /// M�todo p�blico para actualizar debug desde el detector del c�rculo
    /// </summary>
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

    // M�todo p�blico para que HintSystem pueda desactivar el modo memoria
    public void ForceDisableMemoryMode()
    {
        if (memoryModeEnabled || memoryModeActive)
        {
            memoryModeEnabled = false;
            memoryModeActive = false;

            if (memoryControlPanel != null)
                memoryControlPanel.SetActive(false);

            if (memoryCircle != null)
                memoryCircle.SetActive(false);

            StopMemoryMode();
            UpdateDisplays();

            Debug.Log("Modo Memoria desactivado por activaci�n de pistas");
        }
    }

    public bool IsMemoryModeEnabled()
    {
        return memoryModeEnabled;
    }

    void StartMemoryMode()
    {
        // Limpiar controladores anteriores
        StopAllCubeControllers();

        // Buscar todos los cubos
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        if (cubes.Length == 0)
        {
            Debug.LogWarning("No se encontraron cubos para el modo memoria");
            return;
        }

        foreach (GameObject cube in cubes)
        {
            // Guardar materiales originales
            SaveOriginalMaterials(cube);

            // Crear controlador para cada cubo
            CubeMemoryController controller = cube.GetComponent<CubeMemoryController>();
            if (controller == null)
            {
                controller = cube.AddComponent<CubeMemoryController>();
            }

            // Configurar el controlador con tiempos aleatorios desincronizados
            float randomOffset = Random.Range(0f, visibleTime + hiddenTime);
            controller.Initialize(visibleTime, hiddenTime, randomOffset, grayMaterial);

            cubeControllers[cube] = controller;
        }

        // Notificar al GameGenerator si es necesario
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

    // Llamar cuando se cambie de puzzle o se reinicie
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

        // Guardar materiales originales
        SaveOriginalMaterials();

        // Determinar estado inicial basado en offset
        float cycleTime = visible + hidden;
        float offsetInCycle = startOffset % cycleTime;
        isVisible = offsetInCycle < visible;

        // Aplicar estado inicial
        if (!isVisible)
        {
            ApplyGrayMaterial();
        }

        // Iniciar ciclo con el tiempo restante del estado actual
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
        // Esperar el tiempo inicial basado en el offset
        yield return new WaitForSeconds(initialWait);

        // Cambiar al siguiente estado
        isVisible = !isVisible;

        // Ciclo continuo
        while (true)
        {
            if (isVisible)
            {
                // Mostrar contenido original
                RestoreOriginalMaterials();
                yield return new WaitForSeconds(visibleTime);
                isVisible = false;
            }
            else
            {
                // Ocultar con material gris
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
            if (grayMaterial != null)
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

        // Restaurar materiales originales al detener
        RestoreOriginalMaterials();
    }

    void OnDestroy()
    {
        StopMemoryMode();
    }
}