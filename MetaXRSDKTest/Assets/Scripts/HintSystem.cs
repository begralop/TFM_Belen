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
    private Dictionary<GameObject, List<GameObject>> visualIndicators = new Dictionary<GameObject, List<GameObject>>();

    // Estado interno del sistema de pistas - STATIC para persistir entre cambios
    private static bool hintsEnabled = false; // STATIC para mantener el estado
    private StringBuilder debugInfo = new StringBuilder();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    // NUEVO: Almacenar el im�n verde actual
    private GameObject currentGreenMagnet = null;

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

        // IMPORTANTE: Actualizar el texto del bot�n con el estado persistente
        UpdateButtonText();
        UpdateDebugInfo($"Estado de pistas recuperado: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");

        // NUEVO: Asegurar que el color del bot�n est� correcto al inicio
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

    void UpdateDebugInfo(string message)
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

        // NUEVO: Actualizar el color del bot�n
        UpdateButtonColor();
    }

    /// <summary>
    /// Actualiza el color del bot�n seg�n el estado de las pistas
    /// </summary>
    void UpdateButtonColor()
    {
        if (hintsButton != null)
        {
            // Obtenemos el bloque de colores actual del bot�n
            ColorBlock colors = hintsButton.colors;

            if (hintsEnabled)
            {
                // Estado ACTIVO (texto "Desactivar pistas")
                // El color normal ser� azul oscuro
                colors.normalColor = HexToColor("0040B0");
                // Opcional: puedes hacer el color seleccionado igual para que se quede marcado
                colors.selectedColor = HexToColor("0040B0");
            }
            else
            {
                // Estado INACTIVO (texto "Activar pistas")
                // El color normal ser� azul claro
                colors.normalColor = HexToColor("85A5DB");
                // Opcional: puedes hacer el color seleccionado igual
                colors.selectedColor = HexToColor("85A5DB");
            }

            // Asignamos el nuevo bloque de colores al bot�n
            hintsButton.colors = colors;

            UpdateDebugInfo($"Color del bot�n actualizado: {(hintsEnabled ? "Bloque Azul oscuro" : "Bloque Azul claro")}");
        }
    }

    /// <summary>
    /// Convierte un c�digo hexadecimal a Color
    /// </summary>
    Color HexToColor(string hex)
    {
        // Remover # si existe
        hex = hex.Replace("#", "");

        // Convertir hexadecimal a RGB
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
    /// Muestra SOLO el im�n verde correspondiente a un cubo espec�fico cuando se agarra
    /// </summary>
    public void ShowGreenMagnetForCube(int row, int col)
    {
        UpdateDebugInfo($"=== ShowGreenMagnetForCube LLAMADO ===");
        UpdateDebugInfo($"Par�metros: row={row}, col={col}");

        bool enabled = AreHintsEnabled();
        UpdateDebugInfo($"Pistas habilitadas: {enabled}");

        if (!enabled)
        {
            UpdateDebugInfo("SALIENDO: Pistas deshabilitadas");
            return;
        }

        UpdateDebugInfo("Pistas ACTIVADAS, procediendo...");

        // Primero eliminar cualquier im�n verde existente
        UpdateDebugInfo("Destruyendo im�n verde anterior (si existe)...");
        DestroyCurrentGreenMagnet();

        // Crear nuevo im�n verde superpuesto
        UpdateDebugInfo($"Creando nuevo im�n verde en posici�n ({row},{col})...");
        CreateGreenMagnetOverlay(row, col);

        UpdateDebugInfo($"ShowGreenMagnetForCube COMPLETADO");
    }

    /// <summary>
    /// Crea un im�n verde superpuesto en la posici�n indicada
    /// </summary>
    private void CreateGreenMagnetOverlay(int row, int col)
    {
        UpdateDebugInfo($"=== CreateGreenMagnetOverlay INICIADO ===");
        UpdateDebugInfo($"Creando overlay para posici�n ({row},{col})");

        Vector3 magnetPosition = GetMagnetPosition(row, col);
        UpdateDebugInfo($"Posici�n calculada del im�n: {magnetPosition}");

        // Verificar si la posici�n es v�lida
        if (magnetPosition == Vector3.zero)
        {
            UpdateDebugInfo("ERROR: La posici�n del im�n es Vector3.zero - puede ser inv�lida");
        }

        // Crear un cubo que servir� como im�n verde
        UpdateDebugInfo("Creando GameObject primitivo (Cube)...");
        currentGreenMagnet = GameObject.CreatePrimitive(PrimitiveType.Cube);

        if (currentGreenMagnet == null)
        {
            UpdateDebugInfo("ERROR: No se pudo crear el GameObject del im�n verde");
            return;
        }

        currentGreenMagnet.name = $"GreenMagnetOverlay_{row}_{col}";
        UpdateDebugInfo($"GameObject creado con nombre: {currentGreenMagnet.name}");

        // Posicionar ligeramente encima del im�n original
        currentGreenMagnet.transform.position = magnetPosition + Vector3.up * 0.01f;
        UpdateDebugInfo($"Posici�n establecida: {currentGreenMagnet.transform.position}");

        // Hacer el im�n verde un poco m�s grande que el original (20% m�s grande)
        currentGreenMagnet.transform.localScale = new Vector3(0.096f, 0.012f, 0.096f);
        UpdateDebugInfo($"Escala establecida: {currentGreenMagnet.transform.localScale}");

        // Desactivar el collider para que no interfiera con la f�sica
        Collider greenCollider = currentGreenMagnet.GetComponent<Collider>();
        if (greenCollider != null)
        {
            greenCollider.enabled = false;
            UpdateDebugInfo("Collider desactivado");
        }
        else
        {
            UpdateDebugInfo("ADVERTENCIA: No se encontr� collider");
        }

        // Aplicar material verde brillante con emisi�n
        Renderer greenRenderer = currentGreenMagnet.GetComponent<Renderer>();
        if (greenRenderer != null)
        {
            UpdateDebugInfo("Configurando material verde...");

            Material greenMaterial = new Material(Shader.Find("Standard"));
            if (greenMaterial == null)
            {
                UpdateDebugInfo("ERROR: No se pudo crear el material con shader Standard");
                return;
            }

            greenMaterial.color = new Color(0f, 1f, 0f, 0.9f);
            greenMaterial.SetFloat("_Metallic", 0.3f);
            greenMaterial.SetFloat("_Glossiness", 0.8f);

            // Agregar emisi�n para que brille
            greenMaterial.EnableKeyword("_EMISSION");
            greenMaterial.SetColor("_EmissionColor", Color.green * 0.6f);

            // Configurar el material para transparencia
            greenMaterial.SetFloat("_Mode", 3);
            greenMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            greenMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            greenMaterial.SetInt("_ZWrite", 0);
            greenMaterial.DisableKeyword("_ALPHATEST_ON");
            greenMaterial.EnableKeyword("_ALPHABLEND_ON");
            greenMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            greenMaterial.renderQueue = 3000;

            greenRenderer.material = greenMaterial;
            UpdateDebugInfo("Material verde aplicado exitosamente");
        }
        else
        {
            UpdateDebugInfo("ERROR: No se encontr� Renderer en el im�n verde");
        }

        // Opcional: Agregar una animaci�n de pulso
        if (currentGreenMagnet != null)
        {
            UpdateDebugInfo("Iniciando animaci�n de pulso...");
            activeCoroutines[currentGreenMagnet] = StartCoroutine(PulseGreenMagnet(currentGreenMagnet));
        }

        UpdateDebugInfo($"=== CreateGreenMagnetOverlay COMPLETADO ===");
        UpdateDebugInfo($"Im�n verde creado: {currentGreenMagnet != null}");
    }

    /// <summary>
    /// Destruye el im�n verde actual si existe
    /// </summary>
    private void DestroyCurrentGreenMagnet()
    {
        if (currentGreenMagnet != null)
        {
            // Detener animaci�n si existe
            if (activeCoroutines.ContainsKey(currentGreenMagnet))
            {
                StopCoroutine(activeCoroutines[currentGreenMagnet]);
                activeCoroutines.Remove(currentGreenMagnet);
            }

            Destroy(currentGreenMagnet);
            currentGreenMagnet = null;
            UpdateDebugInfo("Im�n verde overlay destruido");
        }
    }

    /// <summary>
    /// Animaci�n de pulso para el im�n verde superpuesto
    /// </summary>
    IEnumerator PulseGreenMagnet(GameObject magnet)
    {
        if (magnet == null) yield break;

        Vector3 originalScale = magnet.transform.localScale;
        float pulseTime = 0f;

        while (magnet != null)
        {
            float scale = 1f + Mathf.Sin(pulseTime * Mathf.PI * 2f) * 0.15f; // Pulso m�s notable
            magnet.transform.localScale = new Vector3(
                originalScale.x * scale,
                originalScale.y,
                originalScale.z * scale
            );

            pulseTime += Time.deltaTime * 2f; // Velocidad del pulso
            yield return null;
        }
    }

    /// <summary>
    /// Restaura SOLO los colores de los imanes (no las caras grises de los cubos)
    /// </summary>
    public void RestoreMagnetColors()
    {
        // NUEVO: Simplemente destruir el im�n verde superpuesto
        DestroyCurrentGreenMagnet();

        // Limpiar cualquier corrutina activa
        foreach (var coroutine in activeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeCoroutines.Clear();

        UpdateDebugInfo("Im�n verde removido");
    }

    /// <summary>
    /// Resalta un �nico im�n sin afectar otros elementos
    /// </summary>
    private void HighlightSingleMagnet(int row, int col)
    {
        GameObject targetMagnet = FindMagnetAtPosition(row, col);

        if (targetMagnet != null)
        {
            // Intentar obtener el renderer del im�n o de sus hijos
            Renderer magnetRenderer = targetMagnet.GetComponent<Renderer>();

            // Si no tiene renderer en el objeto principal, buscar en los hijos
            if (magnetRenderer == null)
            {
                magnetRenderer = targetMagnet.GetComponentInChildren<Renderer>();
            }

            // Si a�n no hay renderer, buscar todos los renderers en los hijos
            if (magnetRenderer == null)
            {
                Renderer[] childRenderers = targetMagnet.GetComponentsInChildren<Renderer>();
                if (childRenderers.Length > 0)
                {
                    // Aplicar el material verde a TODOS los renderers encontrados
                    foreach (Renderer renderer in childRenderers)
                    {
                        // Guardar material original si no se ha guardado ya
                        if (!originalMagnetMaterials.ContainsKey(targetMagnet))
                        {
                            originalMagnetMaterials[targetMagnet] = renderer.material;
                        }

                        // Aplicar material verde con emisi�n para que sea m�s visible
                        Material greenMaterial = new Material(Shader.Find("Standard"));
                        greenMaterial.color = new Color(0f, 1f, 0f, 1f); // Verde brillante
                        greenMaterial.SetFloat("_Metallic", 0.3f);
                        greenMaterial.SetFloat("_Glossiness", 0.8f);

                        // Agregar emisi�n para que brille
                        greenMaterial.EnableKeyword("_EMISSION");
                        greenMaterial.SetColor("_EmissionColor", Color.green * 0.5f);

                        renderer.material = greenMaterial;
                    }
                }
                else
                {
                    // Crear indicador visual si no hay renderer
                    CreateVisualIndicator(targetMagnet, row, col);
                }
            }
            else
            {
                // Si encontramos un renderer �nico, aplicar el material
                if (!originalMagnetMaterials.ContainsKey(targetMagnet))
                {
                    originalMagnetMaterials[targetMagnet] = magnetRenderer.material;
                }

                // Crear material verde con emisi�n
                Material greenMaterial = new Material(Shader.Find("Standard"));
                greenMaterial.color = new Color(0f, 1f, 0f, 1f);
                greenMaterial.SetFloat("_Metallic", 0.3f);
                greenMaterial.SetFloat("_Glossiness", 0.8f);
                greenMaterial.EnableKeyword("_EMISSION");
                greenMaterial.SetColor("_EmissionColor", Color.green * 0.5f);

                magnetRenderer.material = greenMaterial;
            }
        }
    }

    /// <summary>
    /// M�todo principal para mostrar pistas cuando se detecta un error en verificaci�n
    /// SOLO muestra caras grises, NO imanes verdes
    /// </summary>
    public void ShowHintsForIncorrectCubes()
    {
        if (!AreHintsEnabled())
        {
            UpdateDebugInfo("Pistas desactivadas - no se muestran");
            return;
        }

        UpdateDebugInfo("=== MOSTRANDO PISTAS (SOLO CARAS GRISES) ===");

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

                // SOLO mostrar caras grises para cubos con rotaci�n incorrecta
                if (state == CubeState.CorrectPosition_WrongFace || state == CubeState.BothWrong)
                {
                    GrayOutIncorrectFaces(cube);
                }
            }
        }

        UpdateDebugInfo($"Total cubos incorrectos: {incorrectCount} (mostrando solo caras grises)");

        // Restaurar SOLO las caras grises despu�s de 3 segundos
        StartCoroutine(RestoreGrayFacesAfterDelay(3f));
    }

    /// <summary>
    /// Muestra pistas para una lista espec�fica de cubos
    /// SOLO muestra caras grises, NO imanes verdes
    /// </summary>
    public void ShowHintsForSpecificCubes(List<GameObject> cubes)
    {
        if (!AreHintsEnabled())
        {
            UpdateDebugInfo("Pistas desactivadas - no se muestran");
            return;
        }

        UpdateDebugInfo("=== MOSTRANDO PISTAS PARA CUBOS ESPEC�FICOS (SOLO CARAS GRISES) ===");

        int processedCount = 0;

        foreach (GameObject cube in cubes)
        {
            if (cube == null) continue;

            string[] splitName = cube.name.Split('_');
            if (splitName.Length < 3) continue;

            int row, col;
            if (!int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
                continue;

            CubeState state = AnalyzeCubeState(cube, row, col);

            if (state != CubeState.Correct)
            {
                processedCount++;
                UpdateDebugInfo($"Cubo {cube.name}: {state}");

                // SOLO mostrar caras grises para cubos con rotaci�n incorrecta
                if (state == CubeState.CorrectPosition_WrongFace || state == CubeState.BothWrong)
                {
                    GrayOutIncorrectFaces(cube);
                }
            }
        }

        UpdateDebugInfo($"Pistas mostradas para {processedCount} cubos (solo caras grises)");

        // Restaurar SOLO las caras grises despu�s de 10 segundos
        StartCoroutine(RestoreGrayFacesAfterDelay(10f));
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
    /// Analiza el estado de un cubo compar�ndolo con su posici�n y rotaci�n objetivo
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
    /// Crea un indicador visual cuando no hay renderer
    /// </summary>
    void CreateVisualIndicator(GameObject magnet, int row, int col)
    {
        // Crear un cubo verde como indicador visual
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = $"HintIndicator_{row}_{col}";
        indicator.transform.position = magnet.transform.position + Vector3.up * 0.05f;
        indicator.transform.localScale = new Vector3(0.08f, 0.01f, 0.08f);

        // Aplicar material verde brillante
        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        Material greenMaterial = new Material(Shader.Find("Standard"));
        greenMaterial.color = Color.green;
        greenMaterial.EnableKeyword("_EMISSION");
        greenMaterial.SetColor("_EmissionColor", Color.green * 0.8f);
        indicatorRenderer.material = greenMaterial;

        // Guardar referencia para poder eliminarlo despu�s
        if (!visualIndicators.ContainsKey(magnet))
        {
            visualIndicators[magnet] = new List<GameObject>();
        }
        visualIndicators[magnet].Add(indicator);

        UpdateDebugInfo($"Indicador visual creado para im�n ({row},{col})");
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
    /// Encuentra el im�n en una posici�n espec�fica
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
    /// Calcula la posici�n de un im�n basado en fila y columna
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
    /// Restaura SOLO las caras grises despu�s de un retraso
    /// </summary>
    IEnumerator RestoreGrayFacesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestoreGrayFaces();
        UpdateDebugInfo("Caras grises restauradas autom�ticamente");
    }

    /// <summary>
    /// Restaura SOLO las caras grises de los cubos
    /// </summary>
    void RestoreGrayFaces()
    {
        // Restaurar solo materiales de cubos
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

        originalCubeMaterials.Clear();
        UpdateDebugInfo("Caras grises restauradas");
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
        RestoreGrayFaces();

        // NUEVO: Destruir el im�n verde superpuesto si existe
        DestroyCurrentGreenMagnet();

        UpdateDebugInfo("Todos los materiales restaurados");
    }

    /// <summary>
    /// M�todo p�blico para limpiar el sistema cuando se cambia de puzzle
    /// </summary>
    public void OnPuzzleChanged()
    {
        // Limpiar solo los materiales pero mantener el estado de activaci�n
        RestoreAllMaterials();
        UpdateDebugInfo($"Puzzle cambiado - Estado de pistas mantenido: {(hintsEnabled ? "ACTIVADO" : "DESACTIVADO")}");
    }
}