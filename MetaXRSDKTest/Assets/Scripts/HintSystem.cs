using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class HintSystem : MonoBehaviour
{
    [Header("Configuración de Pistas")]
    [Tooltip("Botón para activar/desactivar el sistema de pistas")]
    public Button hintsButton;

    [Tooltip("Texto del botón")]
    public TextMeshProUGUI buttonText;

    [Header("Panel de Debug")]
    [Tooltip("Panel de Canvas para mostrar información de debug")]
    public GameObject debugPanel;

    [Tooltip("Texto para mostrar información de debug")]
    public TextMeshProUGUI debugText;

    [Tooltip("Activar/Desactivar modo debug")]
    public bool debugMode = true;

    [Header("Materiales para Pistas")]
    [Tooltip("Material verde para resaltar imanes")]
    public Material greenMagnetMaterial;

    [Tooltip("Material gris para ocultar caras incorrectas")]
    public Material grayFaceMaterial;

    [Header("Referencias")]
    private GameGenerator gameGenerator;
    private Dictionary<GameObject, Material> originalMagnetMaterials = new Dictionary<GameObject, Material>();
    private Dictionary<GameObject, Dictionary<string, Material>> originalCubeMaterials = new Dictionary<GameObject, Dictionary<string, Material>>();

    // Estado interno del sistema de pistas
    private bool hintsEnabled = false;
    private StringBuilder debugInfo = new StringBuilder();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    void Start()
    {
        // Buscar GameGenerator automáticamente
        gameGenerator = FindObjectOfType<GameGenerator>();

        // Configurar el botón
        if (hintsButton != null)
        {
            hintsButton.onClick.AddListener(ToggleHints);

            if (buttonText == null)
            {
                buttonText = hintsButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        else
        {
            Debug.LogError("HintSystem: hintsButton no está asignado en el Inspector");
        }

        // Crear materiales por defecto si no están asignados
        CreateDefaultMaterials();

        // Activar panel de debug si está en modo debug
        if (debugPanel != null && debugMode)
        {
            debugPanel.SetActive(true);
            UpdateDebugInfo("Sistema de pistas iniciado...");
        }

        // Establecer estado inicial del texto
        UpdateButtonText();
    }

    void CreateDefaultMaterials()
    {
        if (greenMagnetMaterial == null)
        {
            greenMagnetMaterial = new Material(Shader.Find("Standard"));
            greenMagnetMaterial.color = Color.green;
            greenMagnetMaterial.SetFloat("_Metallic", 0.5f);
            greenMagnetMaterial.SetFloat("_Glossiness", 0.5f);
        }

        if (grayFaceMaterial == null)
        {
            grayFaceMaterial = new Material(Shader.Find("Standard"));
            grayFaceMaterial.color = Color.gray;
        }
    }

    void UpdateDebugInfo(string message)
    {
        if (!debugMode) return;

        debugInfo.AppendLine($"[PISTAS] {message}");

        // Limitar el tamaño del debug log
        string[] lines = debugInfo.ToString().Split('\n');
        if (lines.Length > 20)
        {
            debugInfo.Clear();
            for (int i = lines.Length - 20; i < lines.Length; i++)
            {
                if (i >= 0 && !string.IsNullOrEmpty(lines[i]))
                    debugInfo.AppendLine(lines[i]);
            }
        }

        if (debugText != null)
            debugText.text = debugInfo.ToString();
    }

    /// <summary>
    /// Alternar el estado de las pistas al hacer clic en el botón
    /// </summary>
    void ToggleHints()
    {
        hintsEnabled = !hintsEnabled;
        UpdateButtonText();

        if (!hintsEnabled)
        {
            // Si se desactiva, restaurar materiales inmediatamente
            RestoreAllMaterials();
        }

        UpdateDebugInfo($"Sistema: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");
    }

    void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = hintsEnabled ? "Desactivar pistas" : "Activar pistas";
        }
    }

    /// <summary>
    /// Verifica si las pistas están habilitadas
    /// </summary>
    public bool AreHintsEnabled()
    {
        return hintsEnabled;
    }

    /// <summary>
    /// Método para mostrar pistas cuando se recoge un cubo específico
    /// </summary>
    public void ShowHintForPickedCube(GameObject pickedCube)
    {
        if (!AreHintsEnabled())
        {
            return;
        }

        string[] splitName = pickedCube.name.Split('_');
        if (splitName.Length < 3) return;

        int row, col;
        if (!int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
            return;

        CubeState state = AnalyzeCubeState(pickedCube, row, col);

        UpdateDebugInfo($"Cubo recogido: {pickedCube.name} - Estado: {state}");

        // Solo mostrar pista si la cara está correcta pero la posición no
        if (state == CubeState.WrongPosition_CorrectFace || state == CubeState.BothWrong)
        {
            HighlightCorrectMagnet(row, col);
            UpdateDebugInfo($"Mostrando imán verde para {pickedCube.name}");

            // Restaurar después de 5 segundos
            StartCoroutine(RestoreAllAfterDelay(5f));
        }
    }

    /// <summary>
    /// Método principal para mostrar pistas cuando se detecta un error en verificación
    /// </summary>
    public void ShowHintsForIncorrectCubes()
    {
        if (!AreHintsEnabled())
        {
            UpdateDebugInfo("Pistas desactivadas - no se muestran");
            return;
        }

        UpdateDebugInfo("=== MOSTRANDO PISTAS ===");

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
        int incorrectCount = 0;

        foreach (GameObject cube in cubes)
        {
            string[] splitName = cube.name.Split('_');
            if (splitName.Length < 3) continue;

            int row, col;
            if (!int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
                continue;

            CubeState state = AnalyzeCubeState(cube, row, col);

            if (state != CubeState.Correct)
            {
                incorrectCount++;
                UpdateDebugInfo($"Cubo {cube.name}: {state}");

                switch (state)
                {
                    case CubeState.WrongPosition_CorrectFace:
                        HighlightCorrectMagnet(row, col);
                        break;

                    case CubeState.CorrectPosition_WrongFace:
                        GrayOutIncorrectFaces(cube);
                        break;

                    case CubeState.BothWrong:
                        HighlightCorrectMagnet(row, col);
                        GrayOutIncorrectFaces(cube);
                        break;
                }
            }
        }

        UpdateDebugInfo($"Total cubos incorrectos: {incorrectCount}");

        // Restaurar después de 3 segundos
        StartCoroutine(RestoreAllAfterDelay(3f));
    }

    /// <summary>
    /// Estados posibles de un cubo
    /// </summary>
    enum CubeState
    {
        Correct,
        WrongPosition_CorrectFace,
        CorrectPosition_WrongFace,
        BothWrong
    }

    /// <summary>
    /// Analiza el estado de un cubo comparándolo con su posición y rotación objetivo
    /// </summary>
    CubeState AnalyzeCubeState(GameObject cube, int targetRow, int targetCol)
    {
        Vector3 targetPosition = GetMagnetPosition(targetRow, targetCol);
        float distance = Vector3.Distance(cube.transform.position, targetPosition);

        Quaternion targetRotation = Quaternion.identity;
        float angle = Quaternion.Angle(cube.transform.rotation, targetRotation);

        bool positionCorrect = distance <= 0.1f;
        bool rotationCorrect = angle <= 10.0f;

        if (positionCorrect && rotationCorrect)
            return CubeState.Correct;
        else if (!positionCorrect && rotationCorrect)
            return CubeState.WrongPosition_CorrectFace;
        else if (positionCorrect && !rotationCorrect)
            return CubeState.CorrectPosition_WrongFace;
        else
            return CubeState.BothWrong;
    }

    /// <summary>
    /// Resalta el imán correcto en verde
    /// </summary>
    void HighlightCorrectMagnet(int row, int col)
    {
        GameObject targetMagnet = FindMagnetAtPosition(row, col);

        if (targetMagnet != null)
        {
            Renderer magnetRenderer = targetMagnet.GetComponent<Renderer>();
            if (magnetRenderer != null)
            {
                // Guardar material original si no se ha guardado ya
                if (!originalMagnetMaterials.ContainsKey(targetMagnet))
                {
                    originalMagnetMaterials[targetMagnet] = magnetRenderer.material;
                }

                // Aplicar material verde
                magnetRenderer.material = greenMagnetMaterial;

                // Detener cualquier animación anterior en este imán
                if (activeCoroutines.ContainsKey(targetMagnet))
                {
                    StopCoroutine(activeCoroutines[targetMagnet]);
                }

                // Iniciar nueva animación de pulso
                activeCoroutines[targetMagnet] = StartCoroutine(PulseMagnet(targetMagnet));

                UpdateDebugInfo($"Imán ({row},{col}) resaltado en VERDE");
            }
        }
    }

    /// <summary>
    /// Pone en gris las caras incorrectas del cubo
    /// </summary>
    void GrayOutIncorrectFaces(GameObject cube)
    {
        Renderer[] cubeRenderers = cube.GetComponentsInChildren<Renderer>();

        // Guardar materiales originales si no se han guardado ya
        if (!originalCubeMaterials.ContainsKey(cube))
        {
            originalCubeMaterials[cube] = new Dictionary<string, Material>();
            foreach (Renderer renderer in cubeRenderers)
            {
                originalCubeMaterials[cube][renderer.name] = renderer.material;
            }
        }

        // Aplicar material gris a todas las caras excepto Face1 (la correcta)
        foreach (Renderer renderer in cubeRenderers)
        {
            if (renderer.name != "Face1")
            {
                renderer.material = grayFaceMaterial;
            }
        }

        UpdateDebugInfo($"Cubo {cube.name} - caras incorrectas en GRIS");
    }

    /// <summary>
    /// Encuentra el imán en una posición específica
    /// </summary>
    GameObject FindMagnetAtPosition(int row, int col)
    {
        Vector3 expectedPosition = GetMagnetPosition(row, col);
        GameObject[] magnets = GameObject.FindGameObjectsWithTag("refCube");

        foreach (GameObject magnet in magnets)
        {
            if (Vector3.Distance(magnet.transform.position, expectedPosition) < 0.01f)
            {
                return magnet;
            }
        }

        return null;
    }

    /// <summary>
    /// Calcula la posición de un imán basado en fila y columna
    /// </summary>
    Vector3 GetMagnetPosition(int row, int col)
    {
        if (gameGenerator != null)
        {
            GameObject tableCenterObject = gameGenerator.tableCenterObject;
            float cubeSize = gameGenerator.cubeSize;
            float magnetHeightOffset = gameGenerator.magnetHeightOffset;

            if (tableCenterObject != null)
            {
                Vector3 tableCenter = tableCenterObject.transform.position;
                float puzzleWidth = gameGenerator.columns * cubeSize;
                float puzzleHeight = gameGenerator.rows * cubeSize;

                return new Vector3(
                    tableCenter.x - puzzleWidth / 2 + col * cubeSize * 0.8f,
                    tableCenter.y + magnetHeightOffset,
                    tableCenter.z - puzzleHeight / 2 + row * cubeSize * 0.8f
                );
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Animación de pulso para los imanes resaltados
    /// </summary>
    IEnumerator PulseMagnet(GameObject magnet)
    {
        float pulseTime = 0f;
        float pulseDuration = 3f;
        Vector3 originalScale = magnet.transform.localScale;

        while (pulseTime < pulseDuration)
        {
            float scale = 1f + Mathf.Sin(pulseTime * Mathf.PI * 2f) * 0.1f;
            magnet.transform.localScale = new Vector3(
                originalScale.x * scale,
                originalScale.y,
                originalScale.z * scale
            );

            pulseTime += Time.deltaTime;
            yield return null;
        }

        // Restaurar escala original
        magnet.transform.localScale = originalScale;

        // Remover de corrutinas activas
        if (activeCoroutines.ContainsKey(magnet))
        {
            activeCoroutines.Remove(magnet);
        }
    }

    /// <summary>
    /// Restaura todos los materiales después de un retraso
    /// </summary>
    IEnumerator RestoreAllAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestoreAllMaterials();
        UpdateDebugInfo("Materiales restaurados automáticamente");
    }

    /// <summary>
    /// Restaura todos los materiales a su estado original
    /// </summary>
    void RestoreAllMaterials()
    {
        // Detener todas las corrutinas activas
        foreach (var coroutine in activeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeCoroutines.Clear();

        // Restaurar materiales de cubos
        foreach (var cubeEntry in originalCubeMaterials)
        {
            if (cubeEntry.Key != null)
            {
                Renderer[] renderers = cubeEntry.Key.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (cubeEntry.Value.ContainsKey(renderer.name))
                    {
                        renderer.material = cubeEntry.Value[renderer.name];
                    }
                }
            }
        }

        // Restaurar materiales de imanes y escalas
        foreach (var magnetEntry in originalMagnetMaterials)
        {
            if (magnetEntry.Key != null)
            {
                Renderer renderer = magnetEntry.Key.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = magnetEntry.Value;
                }

                // Restaurar escala original también
                magnetEntry.Key.transform.localScale = Vector3.one;
            }
        }

        originalCubeMaterials.Clear();
        originalMagnetMaterials.Clear();
    }
}