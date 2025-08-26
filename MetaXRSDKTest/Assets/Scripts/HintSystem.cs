using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class HintSystem : MonoBehaviour
{
    [Header("Configuraci�n de Pistas")]
    [Tooltip("Bot�n para activar/desactivar el sistema de pistas")]
    public Button hintsButton;

    [Tooltip("Texto del bot�n")]
    public TextMeshProUGUI buttonText;

    [Header("Panel de Debug")]
    [Tooltip("Panel de Canvas para mostrar informaci�n de debug")]
    public GameObject debugPanel;

    [Tooltip("Texto para mostrar informaci�n de debug")]
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

    // Estado interno del sistema de pistas - STATIC para persistir entre cambios
    private static bool hintsEnabled = false;
    private StringBuilder debugInfo = new StringBuilder();

    // Almacenar el im�n verde actual y las caras grises actuales
    private GameObject currentGreenMagnet = null;
    private List<GameObject> currentGrayFaceCubes = new List<GameObject>();

    void Start()
    {
        // Buscar GameGenerator autom�ticamente
        gameGenerator = FindObjectOfType<GameGenerator>();

        // Configurar el bot�n
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
            Debug.LogError("HintSystem: hintsButton no est� asignado en el Inspector");
        }

        // Crear materiales por defecto si no est�n asignados
        CreateDefaultMaterials();

        // Activar panel de debug si est� en modo debug
        if (debugPanel != null && debugMode)
        {
            debugPanel.SetActive(true);
            UpdateDebugInfo("Sistema de pistas iniciado...");
        }

        // Actualizar el texto del bot�n con el estado persistente
        UpdateButtonText();
        UpdateDebugInfo($"Estado de pistas recuperado: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");

        // Actualizar el color del bot�n
        UpdateButtonColor();
    }

    void CreateDefaultMaterials()
    {
        if (greenMagnetMaterial == null)
        {
            greenMagnetMaterial = new Material(Shader.Find("Standard"));
            greenMagnetMaterial.color = new Color(0f, 1f, 0f, 1f); // Verde puro
            greenMagnetMaterial.SetFloat("_Metallic", 0.3f);
            greenMagnetMaterial.SetFloat("_Glossiness", 0.8f);

            // Agregar emisi�n para que brille
            greenMagnetMaterial.EnableKeyword("_EMISSION");
            greenMagnetMaterial.SetColor("_EmissionColor", Color.green * 0.5f);
        }

        if (grayFaceMaterial == null)
        {
            grayFaceMaterial = new Material(Shader.Find("Standard"));
            grayFaceMaterial.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Gris oscuro
            grayFaceMaterial.SetFloat("_Metallic", 0f);
            grayFaceMaterial.SetFloat("_Glossiness", 0.2f);
        }
    }

    public void UpdateDebugInfo(string message)
    {
        if (!debugMode) return;

        debugInfo.AppendLine($"[PISTAS] {message}");

        // Limitar el tama�o del debug log
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
    /// Alternar el estado de las pistas al hacer clic en el bot�n
    /// </summary>
    void ToggleHints()
    {
        hintsEnabled = !hintsEnabled;
        UpdateButtonText();

        if (!hintsEnabled)
        {
            // Si se desactivan las pistas, restaurar todo y ocultar c�rculos
            RestoreAllMaterials();
        }

        UpdateDebugInfo($"Sistema: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");

        // Notificar al GameGenerator para actualizar los c�rculos
        if (gameGenerator != null)
        {
            gameGenerator.UpdateCirclesVisibility();
        }
    }

    void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = hintsEnabled ? "Desactivar pistas" : "Activar pistas";
        }
        UpdateButtonColor();
    }

    /// <summary>
    /// Actualiza el color del bot�n seg�n el estado de las pistas
    /// </summary>
    void UpdateButtonColor()
    {
        if (hintsButton != null)
        {
            Image buttonImage = hintsButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (hintsEnabled)
                {
                    // Color cuando est� activo (texto "Desactivar pistas")
                    buttonImage.color = HexToColor("0040B0"); // Azul oscuro
                }
                else
                {
                    // Color cuando est� inactivo (texto "Activar pistas")
                    buttonImage.color = HexToColor("85A5DB"); // Azul claro
                }

                UpdateDebugInfo($"Color del bot�n actualizado: {(hintsEnabled ? "Azul oscuro" : "Azul claro")}");
            }
        }
    }

    /// <summary>
    /// Convierte un c�digo hexadecimal a Color
    /// </summary>
    Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    /// <summary>
    /// Verifica si las pistas est�n habilitadas
    /// </summary>
    public bool AreHintsEnabled()
    {
        return hintsEnabled;
    }

    /// <summary>
    /// Muestra el im�n verde correspondiente a un cubo espec�fico
    /// Solo se llama desde MagnetCircleDetector cuando se suelta un cubo
    /// </summary>
    public void ShowGreenMagnetForCube(int row, int col)
    {
        UpdateDebugInfo($"ShowGreenMagnetForCube llamado para posici�n ({row},{col})");

        if (!AreHintsEnabled())
        {
            UpdateDebugInfo("Pistas deshabilitadas - no se muestra im�n verde");
            return;
        }

        // Destruir cualquier im�n verde existente
        DestroyCurrentGreenMagnet();

        // Crear nuevo im�n verde superpuesto
        CreateGreenMagnetOverlay(row, col);
    }

    /// <summary>
    /// Crea un im�n verde superpuesto en la posici�n indicada
    /// </summary>
    private void CreateGreenMagnetOverlay(int row, int col)
    {
        Vector3 magnetPosition = GetMagnetPosition(row, col);

        if (magnetPosition == Vector3.zero)
        {
            UpdateDebugInfo("ERROR: Posici�n del im�n inv�lida");
            return;
        }

        // Crear un cubo que servir� como im�n verde
        currentGreenMagnet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentGreenMagnet.name = $"GreenMagnetOverlay_{row}_{col}";

        // Posicionar ligeramente encima del im�n original
        currentGreenMagnet.transform.position = magnetPosition + Vector3.up * 0.01f;
        currentGreenMagnet.transform.localScale = new Vector3(0.096f, 0.012f, 0.096f);

        // Desactivar el collider
        Collider greenCollider = currentGreenMagnet.GetComponent<Collider>();
        if (greenCollider != null)
        {
            greenCollider.enabled = false;
        }

        // Aplicar material verde brillante
        Renderer greenRenderer = currentGreenMagnet.GetComponent<Renderer>();
        if (greenRenderer != null)
        {
            Material greenMaterial = new Material(Shader.Find("Standard"));
            greenMaterial.color = new Color(0f, 1f, 0f, 0.9f);
            greenMaterial.SetFloat("_Metallic", 0.3f);
            greenMaterial.SetFloat("_Glossiness", 0.8f);

            // Agregar emisi�n para que brille
            greenMaterial.EnableKeyword("_EMISSION");
            greenMaterial.SetColor("_EmissionColor", Color.green * 0.6f);

            // Configurar transparencia
            greenMaterial.SetFloat("_Mode", 3);
            greenMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            greenMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            greenMaterial.SetInt("_ZWrite", 0);
            greenMaterial.DisableKeyword("_ALPHATEST_ON");
            greenMaterial.EnableKeyword("_ALPHABLEND_ON");
            greenMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            greenMaterial.renderQueue = 3000;

            greenRenderer.material = greenMaterial;
        }

        // Agregar animaci�n de pulso
        StartCoroutine(PulseGreenMagnet(currentGreenMagnet));

        UpdateDebugInfo($"Im�n verde creado en posici�n ({row},{col})");
    }

    /// <summary>
    /// Muestra las caras grises para un cubo espec�fico
    /// Solo se llama desde FacesCircleDetector cuando se suelta un cubo
    /// </summary>
    public void ShowGrayFacesForCube(GameObject cube)
    {
        if (!AreHintsEnabled() || cube == null)
        {
            UpdateDebugInfo("No se pueden mostrar caras grises - pistas deshabilitadas o cubo nulo");
            return;
        }

        UpdateDebugInfo($"Mostrando caras grises para {cube.name}");

        // Guardar materiales originales si no se han guardado
        if (!originalCubeMaterials.ContainsKey(cube))
        {
            originalCubeMaterials[cube] = new Dictionary<string, Material>();
            Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                originalCubeMaterials[cube][renderer.name] = renderer.material;
            }
        }

        // Aplicar material gris a todas las caras excepto Face1
        Renderer[] cubeRenderers = cube.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in cubeRenderers)
        {
            if (renderer.name != "Face1")
            {
                renderer.material = grayFaceMaterial;
            }
        }

        // Agregar a la lista de cubos con caras grises
        if (!currentGrayFaceCubes.Contains(cube))
        {
            currentGrayFaceCubes.Add(cube);
        }

        UpdateDebugInfo($"Caras grises aplicadas a {cube.name}");
    }

    /// <summary>
    /// Destruye el im�n verde actual si existe
    /// </summary>
    private void DestroyCurrentGreenMagnet()
    {
        if (currentGreenMagnet != null)
        {
            StopAllCoroutines(); // Detener la animaci�n de pulso
            Destroy(currentGreenMagnet);
            currentGreenMagnet = null;
            UpdateDebugInfo("Im�n verde destruido");
        }
    }

    /// <summary>
    /// Animaci�n de pulso para el im�n verde
    /// </summary>
    IEnumerator PulseGreenMagnet(GameObject magnet)
    {
        if (magnet == null) yield break;

        Vector3 originalScale = magnet.transform.localScale;
        float pulseTime = 0f;

        while (magnet != null)
        {
            float scale = 1f + Mathf.Sin(pulseTime * Mathf.PI * 2f) * 0.15f;
            magnet.transform.localScale = new Vector3(
                originalScale.x * scale,
                originalScale.y,
                originalScale.z * scale
            );

            pulseTime += Time.deltaTime * 2f;
            yield return null;
        }
    }

    /// <summary>
    /// Restaura los colores de los imanes (elimina el im�n verde)
    /// </summary>
    public void RestoreMagnetColors()
    {
        DestroyCurrentGreenMagnet();
        UpdateDebugInfo("Colores de imanes restaurados");
    }

    /// <summary>
    /// Restaura las caras grises de los cubos
    /// </summary>
    public void RestoreGrayFaces()
    {
        foreach (GameObject cube in currentGrayFaceCubes)
        {
            if (cube != null && originalCubeMaterials.ContainsKey(cube))
            {
                Renderer[] renderers = cube.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (originalCubeMaterials[cube].ContainsKey(renderer.name))
                    {
                        renderer.material = originalCubeMaterials[cube][renderer.name];
                    }
                }
                originalCubeMaterials.Remove(cube);
            }
        }

        currentGrayFaceCubes.Clear();
        UpdateDebugInfo("Caras grises restauradas");
    }

    /// <summary>
    /// Restaura todos los materiales a su estado original
    /// </summary>
    void RestoreAllMaterials()
    {
        RestoreMagnetColors();
        RestoreGrayFaces();
        UpdateDebugInfo("Todos los materiales restaurados");
    }

    /// <summary>
    /// Calcula la posici�n de un im�n basado en fila y columna
    /// </summary>
    Vector3 GetMagnetPosition(int row, int col)
    {
        if (gameGenerator == null)
        {
            gameGenerator = FindObjectOfType<GameGenerator>();
            if (gameGenerator == null)
            {
                UpdateDebugInfo("ERROR: No se pudo encontrar GameGenerator");
                return Vector3.zero;
            }
        }

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

                Vector3 position = new Vector3(
                    tableCenter.x - puzzleWidth / 2 + col * cubeSize * 0.8f,
                    tableCenter.y + magnetHeightOffset,
                    tableCenter.z - puzzleHeight / 2 + row * cubeSize * 0.8f
                );

                return position;
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// M�todo p�blico para limpiar el sistema cuando se cambia de puzzle
    /// </summary>
    public void OnPuzzleChanged()
    {
        // Limpiar todos los materiales pero mantener el estado de activaci�n
        RestoreAllMaterials();
        UpdateDebugInfo($"Puzzle cambiado - Estado mantenido: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");
    }
}