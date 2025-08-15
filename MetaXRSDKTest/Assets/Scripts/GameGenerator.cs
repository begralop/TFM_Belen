using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;

public class GameGenerator : MonoBehaviour
{
    [Header("Gestión de Sesión")]
    [Tooltip("El botón que el usuario pulsará para cerrar sesión.")]
    public Button logoutButton;
    [Tooltip("El nombre exacto de tu escena de Login (ej: 'LoginScene').")]
    public string loginSceneName;

    [Header("Contador de Tiempo")]
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;

    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    // --- PANEL DE DEBUG ---
    [Header("Panel de Debug")]
    [Tooltip("Panel de Canvas para mostrar información de debug")]
    public GameObject debugPanel;
    [Tooltip("Texto para mostrar información de debug")]
    public TextMeshProUGUI debugText;
    [Tooltip("Activar/Desactivar modo debug")]
    public bool debugMode = true;

    // Variables para almacenar información de debug
    private StringBuilder debugInfo = new StringBuilder();
    private Dictionary<string, Vector3> cubePositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> cubeRotations = new Dictionary<string, Quaternion>();

    // --- VARIABLES DE UI UNIFICADAS ---
    [Header("Panel de Resultado")]
    [Tooltip("El panel que aparecerá al final del puzle.")]
    public GameObject resultPanel;
    [Tooltip("El texto para el mensaje principal (éxito o advertencia).")]
    public TextMeshProUGUI resultMessageText;
    [Tooltip("El boton para continuar o cerrar el panel.")]
    public Button continueButton;
    [Tooltip("El boton para reiniciar la partida.")]
    public Button restartButton;

    [Header("Configuración del Puzle")]
    public GameObject cubePrefab;
    public GameObject magnetPrefab;
    public GameObject tableCenterObject;
    public List<Material> otherPuzzleMaterials;
    public Image selectedImage;
    public Transform imagesPanel;

    private GameObject selectPuzzle;
    private List<Vector3> magnetPositions = new List<Vector3>();
    private GridCreator gridCreator;

    public Button playButton;
    public TextMeshProUGUI welcomeText;

    private int placedCubesCount = 0;
    private bool puzzleCompleted = false;

    public float cubeSize = 0.1f;
    public float magnetHeightOffset = 0.005f;

    public int rows;
    public int columns;

    private Vector3 initialSuccessPanelPosition;
    private Vector3 initialWarningPanelPosition;

    void Start()
    {
        // Asegúrate de que el panel esté desactivado al inicio
        resultPanel.SetActive(false);

        // Activar panel de debug si está en modo debug
        if (debugPanel != null && debugMode)
        {
            debugPanel.SetActive(true);
            UpdateDebugInfo("Sistema de debug iniciado...");
        }

        ClearCurrentMagnets();
        ClearCurrentCubes();

        if (welcomeText != null)
        {
            string playerName = UserManager.GetCurrentUser();
            welcomeText.text = $"Elige un puzzle, {playerName}";
        }
        else
        {
            Debug.LogWarning("La referencia a 'welcomeText' no esta asignada en el Inspector.");
        }
    }

    void UpdateDebugInfo(string message)
    {
        debugInfo.AppendLine($"Mensaje: {message}");


        debugText.text = debugInfo.ToString();
    }

    void UpdateDebugRealTime()
    {
        if (!debugMode || debugText == null) return;

        debugInfo.Clear();
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
        GameObject[] magnets = GameObject.FindGameObjectsWithTag("refCube");

        debugInfo.AppendLine("=== ESTADO ACTUAL DEL PUZZLE ===");
        debugInfo.AppendLine($"Tiempo: {elapsedTime:F1}s | Timer: {(isTimerRunning ? "ON" : "OFF")}");
        debugInfo.AppendLine($"Grid: {rows}x{columns} = {rows * columns} piezas");
        debugInfo.AppendLine($"Cubos: {cubes.Length}/{rows * columns} | Imanes: {magnets.Length}");
        debugInfo.AppendLine("=====================================");

        if (cubes.Length > 0)
        {
            int cubosEnPosicion = 0;
            int cubosConFace1Arriba = 0;
            int cubosCompletamenteCorrectos = 0;

            foreach (GameObject cube in cubes)
            {
                debugInfo.AppendLine($"\n>>> {cube.name} <<<");

                string[] splitName = cube.name.Split('_');
                if (splitName.Length >= 3)
                {
                    int row, col;
                    if (int.TryParse(splitName[1], out row) && int.TryParse(splitName[2], out col))
                    {
                        // POSICIÓN
                        Vector3 targetPosition = GetMagnetPosition(row, col);
                        Vector3 currentPos = cube.transform.position;
                        float distancia = Vector3.Distance(currentPos, targetPosition);
                        bool enPosicion = distancia <= 0.1f;

                        debugInfo.AppendLine($"POSICIÓN:");
                        debugInfo.AppendLine($"  Actual: ({currentPos.x:F2}, {currentPos.y:F2}, {currentPos.z:F2})");
                        debugInfo.AppendLine($"  Target: ({targetPosition.x:F2}, {targetPosition.y:F2}, {targetPosition.z:F2})");
                        debugInfo.AppendLine($"  Distancia: {distancia:F3} {(enPosicion ? "✓ EN POSICIÓN" : "✗ LEJOS")}");

                        // ROTACIÓN
                        Vector3 euler = cube.transform.rotation.eulerAngles;
                        debugInfo.AppendLine($"ROTACIÓN ACTUAL:");
                        debugInfo.AppendLine($"  Euler: ({euler.x:F0}°, {euler.y:F0}°, {euler.z:F0}°)");

                        // ANÁLISIS DE TODAS LAS CARAS
                        debugInfo.AppendLine($"ANÁLISIS DE CARAS:");
                        string caraSuperior = DeterminarCaraSuperior(cube);
                        debugInfo.AppendLine($"  Cara mirando ARRIBA: {caraSuperior}");

                        // ANÁLISIS ESPECÍFICO DE FACE1
                        Transform face1 = cube.transform.Find("Face1");
                        if (face1 != null)
                        {
                            debugInfo.AppendLine($"FACE1 (la del puzzle):");
                            Debug.DrawRay(face1.position, face1.transform.up * 0.2f, Color.green);

                            // Dibuja una línea AZUL en la dirección "adelante" de la cara del puzle
                            Debug.DrawRay(face1.position, face1.transform.forward * 0.2f, Color.blue);
                            // --- FIN DEL CÓDIGO AÑADIDO ---

                            // Verificar todos los vectores de Face1
                            float dotForward = Vector3.Dot(face1.transform.forward, Vector3.up);
                            float dotBack = Vector3.Dot(-face1.transform.forward, Vector3.up);
                            float dotUp = Vector3.Dot(face1.transform.up, Vector3.up);
                            float dotDown = Vector3.Dot(-face1.transform.up, Vector3.up);
                            float dotRight = Vector3.Dot(face1.transform.right, Vector3.up);
                            float dotLeft = Vector3.Dot(-face1.transform.right, Vector3.up);

                            debugInfo.AppendLine($"  Forward: {dotForward:F2} | Back: {dotBack:F2}");
                            debugInfo.AppendLine($"  Up: {dotUp:F2} | Down: {dotDown:F2}");
                            debugInfo.AppendLine($"  Right: {dotRight:F2} | Left: {dotLeft:F2}");

                            float maxDot = Mathf.Max(dotForward, dotBack, dotUp, dotDown, dotRight, dotLeft);
                            string direccionFace1 = "";

                            if (maxDot == dotForward) direccionFace1 = "FORWARD";
                            else if (maxDot == dotBack) direccionFace1 = "BACK";
                            else if (maxDot == dotUp) direccionFace1 = "UP";
                            else if (maxDot == dotDown) direccionFace1 = "DOWN";
                            else if (maxDot == dotRight) direccionFace1 = "RIGHT";
                            else if (maxDot == dotLeft) direccionFace1 = "LEFT";

                            debugInfo.AppendLine($"  Face1 apunta hacia: {direccionFace1} (dot={maxDot:F2})");

                            bool face1Arriba = maxDot > 0.85f;

                            if (face1Arriba)
                            {
                                debugInfo.AppendLine($"  ✓✓✓ FACE1 ESTÁ ARRIBA ✓✓✓");
                                cubosConFace1Arriba++;
                            }
                            else
                            {
                                debugInfo.AppendLine($"  ✗ Face1 NO está arriba");
                                debugInfo.AppendLine($"  NECESITAS ROTAR hasta dot > 0.85");

                                // Sugerir rotación
                                if (maxDot < 0.2f)
                                {
                                    debugInfo.AppendLine($"  SUGERENCIA: Gira 90° el cubo");
                                }
                                else if (maxDot < 0.5f)
                                {
                                    debugInfo.AppendLine($"  SUGERENCIA: Gira ~45-60° más");
                                }
                                else
                                {
                                    debugInfo.AppendLine($"  SUGERENCIA: Gira ~30° más");
                                }
                            }

                            // ESTADO FINAL DEL CUBO
                            if (enPosicion && face1Arriba)
                            {
                                debugInfo.AppendLine($"★★★ CUBO COMPLETO ★★★");
                                cubosCompletamenteCorrectos++;
                            }
                            else if (enPosicion)
                            {
                                debugInfo.AppendLine($"⚠ En posición pero MAL ORIENTADO");
                            }
                            else if (face1Arriba)
                            {
                                debugInfo.AppendLine($"⚠ Bien orientado pero FUERA DE POSICIÓN");
                            }
                        }

                        if (enPosicion) cubosEnPosicion++;
                    }
                }
                debugInfo.AppendLine("-----------------------------------");
            }

            // RESUMEN FINAL
            debugInfo.AppendLine($"\n=== RESUMEN GLOBAL ===");
            debugInfo.AppendLine($"En posición: {cubosEnPosicion}/{cubes.Length}");
            debugInfo.AppendLine($"Face1 arriba: {cubosConFace1Arriba}/{cubes.Length}");
            debugInfo.AppendLine($"COMPLETOS: {cubosCompletamenteCorrectos}/{cubes.Length}");

            if (cubosCompletamenteCorrectos == rows * columns)
            {
                debugInfo.AppendLine($"\n¡¡¡ PUZZLE LISTO PARA VALIDAR !!!");
            }
            else
            {
                int faltanPosicion = rows * columns - cubosEnPosicion;
                int faltanOrientacion = cubosEnPosicion - cubosCompletamenteCorrectos;

                if (faltanPosicion > 0)
                {
                    debugInfo.AppendLine($"\nFaltan {faltanPosicion} cubos por posicionar");
                }
                if (faltanOrientacion > 0)
                {
                    debugInfo.AppendLine($"Faltan {faltanOrientacion} cubos por rotar correctamente");
                }
            }
        }
        else
        {
            debugInfo.AppendLine("\nNo hay cubos en la escena.");
            debugInfo.AppendLine("Selecciona una imagen para empezar.");
        }

        debugText.text = debugInfo.ToString();
    }

    private string DeterminarCaraSuperior(GameObject cube)
    {
        string[] faceNames = { "Face1", "Face2", "Face3", "Face4", "Face5", "Face6" };
        string caraSuperior = "NINGUNA";
        float maxDotProduct = -1f;

        foreach (string faceName in faceNames)
        {
            Transform face = cube.transform.Find(faceName);
            if (face != null)
            {
                // Probar todos los vectores de cada cara
                float[] dots = new float[] {
                    Vector3.Dot(face.transform.forward, Vector3.up),
                    Vector3.Dot(-face.transform.forward, Vector3.up),
                    Vector3.Dot(face.transform.up, Vector3.up),
                    Vector3.Dot(-face.transform.up, Vector3.up),
                    Vector3.Dot(face.transform.right, Vector3.up),
                    Vector3.Dot(-face.transform.right, Vector3.up)
                };

                float maxDotForFace = Mathf.Max(dots);
                if (maxDotForFace > maxDotProduct)
                {
                    maxDotProduct = maxDotForFace;
                    caraSuperior = faceName;
                }
            }
        }

        if (maxDotProduct > 0.85f)
        {
            return $"{caraSuperior} (dot={maxDotProduct:F2})";
        }
        else
        {
            return $"{caraSuperior} (dot={maxDotProduct:F2} - NO está plana)";
        }
    }

    private bool CheckFace1QuickCheck(GameObject cube)
    {
        Transform face1 = cube.transform.Find("Face1");
        if (face1 == null) return false;

        float maxDot = GetMaxDotProductFace1(face1);
        return maxDot > 0.85f;
    }

    private float GetMaxDotProductFace1(Transform face1)
    {
        float dotForward = Vector3.Dot(face1.transform.forward, Vector3.up);
        float dotBackward = Vector3.Dot(-face1.transform.forward, Vector3.up);
        float dotUp = Vector3.Dot(face1.transform.up, Vector3.up);
        float dotDown = Vector3.Dot(-face1.transform.up, Vector3.up);
        float dotRight = Vector3.Dot(face1.transform.right, Vector3.up);
        float dotLeft = Vector3.Dot(-face1.transform.right, Vector3.up);

        return Mathf.Max(dotForward, dotBackward, dotUp, dotDown, dotRight, dotLeft);
    }

    void StartTimer()
    {
        elapsedTime = 0f;
        isTimerRunning = true;

        if (timerPanel != null)
            timerPanel.SetActive(true);


    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

    }

    public void Logout()
    {
        Debug.Log("Cerrando sesion...");
        UserManager.SetCurrentUser(null);

        if (string.IsNullOrEmpty(loginSceneName))
        {
            Debug.LogError("El nombre de la escena de login (loginSceneName) no esta especificado en el Inspector.");
            return;
        }

        SceneManager.LoadScene(loginSceneName);
    }

    private void ShowResult(bool isSuccess)
    {
        resultPanel.SetActive(true);

        UpdateDebugInfo($"Mostrando resultado: {(isSuccess ? "ÉXITO" : "FALLO")}");

        continueButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);

        if (isSuccess)
        {
            resultMessageText.text = "¡Bien hecho! Has completado el puzle. ¿Quieres jugar de nuevo?";
            continueButton.onClick.AddListener(CloseResultPanel);
        }
        else
        {
            resultMessageText.text = "No has completado el puzle correctamente. ¿Quieres seguir intentándolo?";
            continueButton.onClick.AddListener(ContinueGameAfterWarning);
        }
    }

    public void CloseResultPanel()
    {
        resultPanel.SetActive(false);
        UpdateDebugInfo("Panel de resultado cerrado");
    }

    public void ContinueGameAfterWarning()
    {
        resultPanel.SetActive(false);
        isTimerRunning = true;

        UpdateDebugInfo("Continuando juego - Empujando piezas");

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
        Vector3 puzzleCenter = tableCenterObject.transform.position;

        foreach (GameObject cube in cubes)
        {
            Vector3 directionFromCenter = (cube.transform.position - puzzleCenter).normalized;
            directionFromCenter.y = 0;

            float outwardPush = 0.15f;
            float upwardPush = 0.05f;
            Vector3 displacement = directionFromCenter * outwardPush + Vector3.up * upwardPush;

            cube.transform.position += displacement;
        }
    }

    public void RestartGame()
    {
        resultPanel.SetActive(false);
        placedCubesCount = 0;
        elapsedTime = 0f;
        timerText.text = "00:00";
        isTimerRunning = true;
        puzzleCompleted = false;

        ClearCurrentMagnets();
        ClearCurrentCubes();
        GenerateGame();

        UpdateDebugInfo("Juego reiniciado");
    }

    public void OnCubePlaced()
    {
        placedCubesCount++;
        UpdateDebugInfo($"Cubo colocado. Total: {placedCubesCount}/{rows * columns}");

        if (placedCubesCount >= (rows * columns))
        {
            UpdateDebugInfo("Todos los cubos colocados. Iniciando verificación...");
            StartDelayedCheck();
        }
    }

    public void OnCubeRemoved()
    {
        placedCubesCount--;
        UpdateDebugInfo($"Cubo removido. Total: {placedCubesCount}/{rows * columns}");
    }

    public void CheckPuzzleCompletion()
    {
        UpdateDebugInfo("INICIANDO VERIFICACIÓN COMPLETA DEL PUZZLE");

        isTimerRunning = false;
        bool puzzleEsCorrecto = IsPuzzleComplete();
        ShowResult(puzzleEsCorrecto);
    }

    private bool IsPuzzleComplete()
    {
        // Limpiamos el panel de debug para empezar un nuevo informe
        debugInfo.Clear();

        debugInfo.AppendLine("=======================================");
        debugInfo.AppendLine("INICIANDO VERIFICACIÓN (LÓGICA SIMPLE)");
        debugInfo.AppendLine("=======================================");

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        if (cubes.Length == 0)
        {
            debugInfo.AppendLine("ADVERTENCIA: No se encontraron cubos.");
            return false;
        }

        bool todosCorrectos = true;

        foreach (GameObject cube in cubes)
        {
            string[] splitName = cube.name.Split('_');
            int row, col;

            if (splitName.Length < 3 || !int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
            {
                debugInfo.AppendLine($"ERROR: Nombre de cubo inválido: {cube.name}");
                todosCorrectos = false;
                continue; // Pasamos al siguiente cubo en lugar de salir
            }

            debugInfo.AppendLine($"\n--- Verificando Cubo: {cube.name} ---");

            // --- CÁLCULOS ---
            Vector3 targetPosition = GetMagnetPosition(row, col);
            float distancia = Vector3.Distance(cube.transform.position, targetPosition);

            Quaternion targetRotation = Quaternion.identity;
            float angulo = Quaternion.Angle(cube.transform.rotation, targetRotation);

            // --- INFORMACIÓN DETALLADA ---
            debugInfo.AppendLine($"Distancia: {distancia:F3} (Req: <= 0.05)");
            debugInfo.AppendLine($"Ángulo: {angulo:F1} (Req: <= 5.0)");

            // --- VERIFICACIÓN ---
            if (distancia <= 0.1f && angulo <= 10.0f)
            {
                debugInfo.AppendLine($"===> RESULTADO: CORRECTO");
                // --- CORRECCIÓN APLICADA AQUÍ ---

                // 1. MODIFICAMOS LA ALTURA DEL OBJETIVO
                // Esto pondrá la pieza al doble de la altura del imán respecto al origen del mundo (Y=0).
                targetPosition.y = targetPosition.y * 2;

                // 2. DESCOMENTAMOS LA LÍNEA PARA APLICAR LA POSICIÓN FINAL
                // Esta es la línea clave que faltaba. Ahora el cubo se ajustará a su sitio.
               // cube.transform.position = targetPosition;

                // La rotación ya estaba correcta.
                cube.transform.rotation = targetRotation;
            }
            else
            {
                debugInfo.AppendLine($"===> RESULTADO: INCORRECTO");
                todosCorrectos = false; // Marcamos que al menos uno ha fallado
            }
        }

        debugInfo.AppendLine("\n=======================================");
        if (todosCorrectos)
        {
            debugInfo.AppendLine("¡ÉXITO! Todas las piezas están correctas.");
        }
        else
        {
            debugInfo.AppendLine("FALLO: Al menos una pieza es incorrecta.");
        }
        debugInfo.AppendLine("=======================================");

        // Actualizamos el panel de texto con toda la información recopilada

        return todosCorrectos;
    }

    private bool VerificarSiFace1EstArriba(GameObject cube)
    {
        Transform face1 = cube.transform.Find("Face1");
        if (face1 == null)
        {
            debugInfo.AppendLine($"    ERROR: No se encuentra Face1 en {cube.name}");
            return false;
        }

        // --- Reemplaza el bloque de cálculo dentro de VerificarSiFace1EstArriba ---

        // Con el prefab rotado 90 grados en X, la normal de la cara ahora es el vector LOCAL "up" del objeto.
        float dot = Vector3.Dot(face1.transform.up, Vector3.up);

        // Encontrar el máximo dot product (ahora solo hay uno)
        float maxDot = dot;

        // Debug
        if (debugMode)
        {
            debugInfo.AppendLine($"    Face1: La cara visible (transform.up) apunta hacia arriba ({dot:F3})");
        }

        bool estaArriba = maxDot > 0.85f;

        if (debugMode)
        {
            debugInfo.AppendLine($"    Face1 está arriba: {(estaArriba ? "SÍ" : "NO")} (max dot: {maxDot:F3})");
        }

        return estaArriba;

        // --- Fin del reemplazo ---
    }

    private void VerificarCaraSuperior(GameObject cube, int row, int col)
    {
        debugInfo.AppendLine($"  === Verificación Cara Superior ===");

        // Buscar todas las caras del cubo
        string[] faceNames = { "Face1", "Face2", "Face3", "Face4", "Face5", "Face6" };
        string caraSuperior = "";
        float maxDotProduct = -1f;

        foreach (string faceName in faceNames)
        {
            Transform face = cube.transform.Find(faceName);
            if (face != null)
            {
                // Probar diferentes vectores de la cara para encontrar cuál apunta hacia arriba
                Vector3[] vectors = {
                    face.transform.up,
                    -face.transform.up,
                    face.transform.forward,
                    -face.transform.forward,
                    face.transform.right,
                    -face.transform.right
                };

                foreach (Vector3 vector in vectors)
                {
                    float dot = Vector3.Dot(vector, Vector3.up);
                    if (dot > maxDotProduct)
                    {
                        maxDotProduct = dot;
                        caraSuperior = faceName;
                    }
                }
            }
        }

        debugInfo.AppendLine($"    Cara mirando hacia arriba: {caraSuperior}");
        debugInfo.AppendLine($"    Dot product máximo: {maxDotProduct:F3}");

        // Verificar específicamente Face1
        Transform face1 = cube.transform.Find("Face1");
        if (face1 != null)
        {
            // Verificar cada posible orientación de Face1
            Vector3 face1Normal = face1.transform.forward; // Normal de la cara
            float dotForward = Vector3.Dot(face1.transform.forward, Vector3.up);
            float dotBack = Vector3.Dot(-face1.transform.forward, Vector3.up);
            float dotUp = Vector3.Dot(face1.transform.up, Vector3.up);
            float dotDown = Vector3.Dot(-face1.transform.up, Vector3.up);
            float dotRight = Vector3.Dot(face1.transform.right, Vector3.up);
            float dotLeft = Vector3.Dot(-face1.transform.right, Vector3.up);

            debugInfo.AppendLine($"    Face1 orientaciones:");
            debugInfo.AppendLine($"      Forward: {dotForward:F3}");
            debugInfo.AppendLine($"      Back: {dotBack:F3}");
            debugInfo.AppendLine($"      Up: {dotUp:F3}");
            debugInfo.AppendLine($"      Down: {dotDown:F3}");
            debugInfo.AppendLine($"      Right: {dotRight:F3}");
            debugInfo.AppendLine($"      Left: {dotLeft:F3}");

            float maxFace1Dot = Mathf.Max(dotForward, dotBack, dotUp, dotDown, dotRight, dotLeft);

            if (caraSuperior == "Face1" && maxFace1Dot > 0.9f)
            {
                debugInfo.AppendLine($"    ✓ Face1 está mirando hacia ARRIBA");
                debugInfo.AppendLine($"    Esta es la cara con la parte del puzzle [{row},{col}]");
            }
            else if (maxFace1Dot > 0.9f)
            {
                debugInfo.AppendLine($"    ⚠ Face1 PODRÍA estar arriba con rotación");
                debugInfo.AppendLine($"    Necesita girar el cubo para que Face1 esté visible");
            }
            else
            {
                debugInfo.AppendLine($"    ✗ Face1 NO está mirando hacia arriba");
                debugInfo.AppendLine($"    Cara superior actual: {caraSuperior}");
                debugInfo.AppendLine($"    La imagen del puzzle NO está visible correctamente");
            }
        }
        else
        {
            debugInfo.AppendLine($"    ERROR: No se encuentra Face1 en el cubo");
        }
    }

    private void UpdateDebugText()
    {
        if (debugText != null && debugMode)
        {
            debugText.text = debugInfo.ToString();
        }
    }

    public void StartDelayedCheck()
    {
        StartCoroutine(CheckPuzzleAfterDelay());
    }

    private IEnumerator CheckPuzzleAfterDelay()
    {
        UpdateDebugInfo("Esperando 0.2s para que la física se estabilice...");
        yield return new WaitForSeconds(0.2f);
        CheckPuzzleCompletion();
    }


    void PositionPanel(GameObject panel)
    {
        if (tableCenterObject == null)
        {
            Debug.LogError("No se ha asignado un objeto de referencia para el centro de la mesa.");
            return;
        }

        Vector3 tableCenter = tableCenterObject.transform.position;
        float puzzleWidth = gridCreator.columns * cubeSize;
        float puzzleHeight = gridCreator.rows * cubeSize;

        Vector3 puzzleCenter = new Vector3(
            tableCenter.x,
            tableCenter.y + magnetHeightOffset,
            tableCenter.z
        );

        float panelHeightOffset = 0.2f;
        Vector3 panelPosition = new Vector3(
            puzzleCenter.x,
            puzzleCenter.y + panelHeightOffset,
            puzzleCenter.z
        );

        panel.transform.position = panelPosition;
        panel.transform.LookAt(Camera.main.transform);
        panel.transform.rotation = Quaternion.Euler(0, panel.transform.rotation.eulerAngles.y, 0);
        panel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    void GenerateGame()
    {
        UpdateDebugInfo("Generando nuevo juego...");

        var grid = GameObject.FindGameObjectWithTag("Grid");
        var curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");

        if (grid != null && curvedBackground != null)
        {
            gridCreator = grid.GetComponent<GridCreator>();
            rows = gridCreator.rows;
            columns = gridCreator.columns;
            var curvedBackgroundImage = curvedBackground.GetComponent<Image>();
            Sprite curvedBackgroundSprite = curvedBackgroundImage.sprite;
            Texture2D curvedBackgroundTexture2D = SpriteToTexture2D(curvedBackgroundSprite);

            Material[] materials = DivideImageIntoMaterials(curvedBackgroundTexture2D, gridCreator.rows, gridCreator.columns);

            if (materials != null && materials.Length > 0)
            {
                ClearCurrentMagnets();
                ClearCurrentCubes();
                GenerateMagnetPositions(gridCreator.rows, gridCreator.columns);

                int magnetIndex = 0;

                for (int r = 0; r < gridCreator.rows; r++)
                {
                    for (int c = 0; c < gridCreator.columns; c++)
                    {
                        Vector3 cubePosition = new Vector3(Random.Range(-0.5f, 0.5f), 1.2f, Random.Range(0.2f, 0.6f));
                        Quaternion cubeRotation = Quaternion.Euler(Random.Range(0f, 90f), Random.Range(0f, 90f), Random.Range(0f, 90f));
                        GameObject cube = Instantiate(cubePrefab, cubePosition, cubeRotation);
                        cube.tag = "cube";
                        Renderer[] cubeRenderers = cube.GetComponentsInChildren<Renderer>();

                        if (cubeRenderers != null && cubeRenderers.Length > 0)
                        {
                            foreach (Renderer renderer in cubeRenderers)
                            {
                                if (renderer.name == "Face1")
                                {
                                    renderer.material = materials[r * gridCreator.columns + c];
                                }
                                else
                                {
                                    if (otherPuzzleMaterials != null && otherPuzzleMaterials.Count > 0)
                                    {
                                        int randomIndex = Random.Range(0, otherPuzzleMaterials.Count);
                                        renderer.material = otherPuzzleMaterials[randomIndex];
                                    }
                                    else
                                    {
                                        GenerateMaterialsFromPanelImages();
                                        int randomIndex = Random.Range(0, otherPuzzleMaterials.Count);
                                        renderer.material = otherPuzzleMaterials[randomIndex];
                                    }
                                }
                            }
                            cube.name = $"Cube_{r}_{c}";
                            var interactable = cube.AddComponent<XRGrabInteractable>();
                            cube.AddComponent<CubeInteraction>();
                        }
                        else
                        {
                            Debug.LogWarning("El objeto clonado no tiene componentes Renderer en las caras del cubo.");
                            Destroy(cube);
                        }
                        var magnet = Instantiate(magnetPrefab, magnetPositions[magnetIndex], Quaternion.identity);
                        magnet.tag = "refCube";
                        magnetIndex++;
                        StartTimer();
                    }
                }

                UpdateDebugInfo($"Juego generado: {rows}x{columns} = {rows * columns} cubos");
            }
            else
            {
                Debug.LogError("No se pudieron generar los materiales");
            }
        }
        else
        {
            Debug.LogError("No se encontraron los objetos con los tags 'Grid' o 'curvedBackground'.");
        }
    }

    private Material[] DivideImageIntoMaterials(Texture2D image, int rows, int columns)
    {
        Material[] materials = new Material[rows * columns];
        int partWidth = image.width / columns;
        int partHeight = image.height / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Texture2D partTexture = new Texture2D(partWidth, partHeight);
                partTexture.SetPixels(image.GetPixels(c * partWidth, (rows - 1 - r) * partHeight, partWidth, partHeight));
                partTexture.Apply();

                Material material = new Material(Shader.Find("Unlit/Texture"));
                material.mainTexture = partTexture;
                materials[r * columns + c] = material;
            }
        }
        return materials;
    }

    private Texture2D SpriteToTexture2D(Sprite sprite)
    {
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        texture.SetPixels(sprite.texture.GetPixels(
            (int)sprite.rect.x,
            (int)sprite.rect.y,
            (int)sprite.rect.width,
            (int)sprite.rect.height
        ));
        texture.Apply();
        return texture;
    }

    void GenerateMagnetPositions(int gridRows, int gridColumns)
    {
        ClearCurrentMagnets();

        if (tableCenterObject == null)
        {
            Debug.LogError("No se ha asignado un objeto de referencia para el centro de la mesa.");
            return;
        }

        Vector3 tableCenter = tableCenterObject.transform.position;
        float puzzleWidth = gridColumns * cubeSize;
        float puzzleHeight = gridRows * cubeSize;
        magnetPositions.Clear();

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                Vector3 magnetPosition = new Vector3(
                    tableCenter.x - puzzleWidth / 2 + c * cubeSize * 0.8f,
                    tableCenter.y + magnetHeightOffset,
                    tableCenter.z - puzzleHeight / 2 + r * cubeSize * 0.8f
                );
                magnetPositions.Add(magnetPosition);
                if (magnetPrefab != null)
                {
                    GameObject newMagnet = Instantiate(magnetPrefab, magnetPosition, Quaternion.identity);
                    newMagnet.tag = "refCube";
                    newMagnet.transform.localScale = new Vector3(newMagnet.transform.localScale.x, 0.01f, newMagnet.transform.localScale.z);
                }
                else
                {
                    Debug.LogError("No se pudo cargar el prefab de imán.");
                }
            }
        }

        UpdateDebugInfo($"Se generaron {magnetPositions.Count} posiciones de imanes");
    }

    void ClearCurrentMagnets()
    {
        GameObject[] magnets = GameObject.FindGameObjectsWithTag("refCube");
        foreach (GameObject magnet in magnets)
        {
            Destroy(magnet);
        }
    }

    void ClearCurrentCubes()
    {
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");
        foreach (GameObject cube in cubes)
        {
            Destroy(cube);
        }
    }

    List<Vector3> GetCurrentMagnetPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        GameObject[] magnets = GameObject.FindGameObjectsWithTag("refCube");
        foreach (GameObject magnet in magnets)
        {
            positions.Add(magnet.transform.position);
        }
        return positions;
    }

    Vector3 GetMagnetPosition(int row, int col)
    {
        if (row * gridCreator.columns + col < magnetPositions.Count)
        {
            return magnetPositions[row * gridCreator.columns + col];
        }
        else
        {
            Debug.LogError("La posición del imán está fuera de los límites.");
            return Vector3.zero;
        }
    }

    public void OnImageSelected(Image image)
    {
        elapsedTime = 0f;
        isTimerRunning = false;

        if (timerText != null)
        {
            timerText.text = "00:00";
        }

        selectedImage = image;
        otherPuzzleMaterials.Clear();
        GenerateMaterialsFromPanelImages();

        UpdateDebugInfo($"Imagen seleccionada: {image.sprite.name}");
    }

    void GenerateMaterialsFromPanelImages()
    {
        foreach (Transform child in imagesPanel)
        {
            Transform contentTransform = child.Find("Content");
            if (contentTransform != null)
            {
                Transform backgroundTransform = contentTransform.Find("Background");
                if (backgroundTransform != null)
                {
                    var img = backgroundTransform.GetComponent<Image>();
                    if (img != null && img.sprite != null && img.sprite != selectedImage.sprite)
                    {
                        Texture2D texture = SpriteToTexture2D(img.sprite);
                        Material[] materials = DivideImageIntoMaterials(texture, rows, columns);
                        otherPuzzleMaterials.AddRange(materials);
                        Debug.Log("Imagen añadida a otherPuzzleMaterials: " + img.sprite.name);
                    }
                }
            }
        }

        UpdateDebugInfo($"Total de materiales generados: {otherPuzzleMaterials.Count}");
    }
}
