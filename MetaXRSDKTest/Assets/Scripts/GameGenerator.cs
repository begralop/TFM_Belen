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
    // Añade una variable para controlar si ya se guardó el tiempo
    private bool timeAlreadySaved = false;

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


    [Header("Sistema de Progresión de Puzzles")]
    private int currentPuzzleIndex = -1; // Índice del puzzle actual
    private List<GameObject> availablePuzzles = new List<GameObject>(); // Lista de todos los toggles de puzzles

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
        CollectAvailablePuzzles();
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
    // Modifica el método ShowResult para cambiar el comportamiento del botón continuar:
    private void ShowResult(bool isSuccess)
    {
        resultPanel.SetActive(true);

        UpdateDebugInfo($"Mostrando resultado: {(isSuccess ? "ÉXITO" : "FALLO")}");

        continueButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);

        if (isSuccess)
        {
            // Detener el timer y guardar la puntuación
            isTimerRunning = false;

            // IMPORTANTE: Solo guardar si no se ha guardado ya
            if (!timeAlreadySaved)
            {
                // Obtener el ID del puzzle actual
                string puzzleId = GetCurrentPuzzleId();

                // Guardar el tiempo para el usuario actual
                string currentUser = UserManager.GetCurrentUser();
                if (!string.IsNullOrEmpty(puzzleId) && currentUser != "Invitado")
                {
                    UserManager.AddScore(currentUser, puzzleId, elapsedTime);
                    Debug.Log($"Tiempo guardado: Usuario={currentUser}, Puzzle={puzzleId}, Tiempo={elapsedTime:F2}s");
                    timeAlreadySaved = true; // Marcar que ya se guardó
                }
            }
            else
            {
                Debug.Log("El tiempo ya fue guardado anteriormente, evitando duplicado");
            }

            // Mostrar el mensaje de éxito con el tiempo
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);

            // MODIFICADO: Cambiar el texto del botón y su función según si hay más puzzles
            bool hasNextPuzzle = (currentPuzzleIndex >= 0 && currentPuzzleIndex < availablePuzzles.Count - 1);

            if (hasNextPuzzle)
            {
                resultMessageText.text = $"¡Bien hecho! Has completado el puzle en {minutes:00}:{seconds:00}.\n¿Quieres continuar con el siguiente puzzle?";

                // Cambiar el texto del botón
                var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (continueButtonText != null)
                {
                    continueButtonText.text = "Siguiente";
                }

                continueButton.onClick.AddListener(LoadNextPuzzle);
            }
            else
            {
                resultMessageText.text = $"¡Felicidades! Has completado el puzle en {minutes:00}:{seconds:00}.\n¡Has completado todos los puzzles disponibles!";

                // Cambiar el texto del botón
                var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (continueButtonText != null)
                {
                    continueButtonText.text = "Cerrar";
                }

                continueButton.onClick.AddListener(CloseResultPanel);
            }
        }
        else
        {
            resultMessageText.text = "No has completado el puzle correctamente. ¿Quieres seguir intentándolo?";

            // Asegurarse de que el texto del botón sea "Continuar"
            var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueButtonText != null)
            {
                continueButtonText.text = "Reintentar";
            }

            continueButton.onClick.AddListener(ContinueGameAfterWarning);
        }
    }


    // NUEVO: Método auxiliar para obtener el ID del puzzle actual
    private string GetCurrentPuzzleId()
    {
        if (selectedImage != null && selectedImage.sprite != null)
        {
            return selectedImage.sprite.name;
        }

        // Si no hay imagen seleccionada, intentar obtenerla del curvedBackground
        GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
        if (curvedBackground != null)
        {
            Image bgImage = curvedBackground.GetComponent<Image>();
            if (bgImage != null && bgImage.sprite != null)
            {
                return bgImage.sprite.name;
            }
        }

        return null;
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
        timeAlreadySaved = false;

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
        placedCubesCount = 0;
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
    // Modifica el método OnImageSelected para resetear la bandera cuando se selecciona un nuevo puzzle:
    public void OnImageSelected(Image image)
    {
        elapsedTime = 0f;
        isTimerRunning = false;
        timeAlreadySaved = false; // IMPORTANTE: Resetear cuando se selecciona un nuevo puzzle

        if (timerText != null)
        {
            timerText.text = "00:00";
        }

        selectedImage = image;
        otherPuzzleMaterials.Clear();
        GenerateMaterialsFromPanelImages();

        // Actualizar el índice del puzzle actual
        UpdateCurrentPuzzleIndex();

        UpdateDebugInfo($"Imagen seleccionada: {image.sprite.name} (Índice: {currentPuzzleIndex})");
    }

    private void LoadNextPuzzle()
    {
        if (currentPuzzleIndex >= 0 && currentPuzzleIndex < availablePuzzles.Count - 1)
        {
            // Cerrar el panel de resultado
            resultPanel.SetActive(false);

            // Resetear la bandera para el siguiente puzzle
            timeAlreadySaved = false;

            // Limpiar el puzzle actual
            ClearCurrentMagnets();
            ClearCurrentCubes();

            // Incrementar al siguiente puzzle
            currentPuzzleIndex++;

            // Activar el siguiente puzzle
            GameObject nextPuzzle = availablePuzzles[currentPuzzleIndex];

            UpdateDebugInfo($"Intentando cargar puzzle: {nextPuzzle.name} (índice {currentPuzzleIndex})");

            var toggle = nextPuzzle.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null)
            {
                // Desactivar todos los otros toggles primero
                foreach (var puzzle in availablePuzzles)
                {
                    var t = puzzle.GetComponent<UnityEngine.UI.Toggle>();
                    if (t != null && t != toggle)
                    {
                        t.isOn = false;
                    }
                }

                // Activar el siguiente toggle
                toggle.isOn = true;

                // IMPORTANTE: Forzar la actualización del puzzle
                // Esperar un frame y luego generar el juego
                StartCoroutine(GenerateNextPuzzleAfterDelay());
            }
            else
            {
                Debug.LogError($"No se encontró Toggle en {nextPuzzle.name}");
            }
        }
        else
        {
            // No hay más puzzles, solo cerrar el panel
            CloseResultPanel();
            UpdateDebugInfo("No hay más puzzles disponibles");
        }
    }

    // NUEVO: Corrutina para generar el puzzle después de un pequeño retraso
    private IEnumerator GenerateNextPuzzleAfterDelay()
    {
        // Esperar un frame para que el toggle se active completamente
        yield return null;

        // Verificar que se haya seleccionado correctamente la imagen
        GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
        if (curvedBackground != null)
        {
            Image bgImage = curvedBackground.GetComponent<Image>();
            if (bgImage != null && bgImage.sprite != null)
            {
                UpdateDebugInfo($"Imagen del siguiente puzzle cargada: {bgImage.sprite.name}");

                // Si el playButton existe y necesita ser presionado
                if (playButton != null)
                {
                    // Simular el click del botón play
                    playButton.onClick.Invoke();
                }
                else
                {
                    // Si no hay playButton, generar directamente
                    GenerateGame();
                }
            }
            else
            {
                Debug.LogError("No se pudo obtener el sprite del curvedBackground");
                UpdateDebugInfo("ERROR: No se pudo cargar la imagen del puzzle");
            }
        }
        else
        {
            Debug.LogError("No se encontró el curvedBackground");
        }
    }

    // También mejora el método CollectAvailablePuzzles para ser más robusto:
    private void CollectAvailablePuzzles()
    {
        // Limpiar la lista anterior
        availablePuzzles.Clear();

        // Método 1: Buscar por ImagePanelController
        ImagePanelController[] controllers = FindObjectsOfType<ImagePanelController>();

        foreach (var controller in controllers)
        {
            // Verificar que el GameObject tenga un Toggle
            if (controller.gameObject.GetComponent<Toggle>() != null)
            {
                availablePuzzles.Add(controller.gameObject);
            }
        }

        // Si no encontró ninguno, intentar buscar directamente los Toggles con el patrón de nombre
        if (availablePuzzles.Count == 0)
        {
            Debug.LogWarning("No se encontraron puzzles por ImagePanelController, buscando por nombre...");

            // Buscar todos los GameObjects que contengan "RoundedBoxToggle" en el nombre
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("RoundedBoxToggle") && obj.GetComponent<Toggle>() != null)
                {
                    availablePuzzles.Add(obj);
                }
            }
        }

        // Ordenar por nombre para mantener un orden consistente
        availablePuzzles.Sort((a, b) => a.name.CompareTo(b.name));

        Debug.Log($"Se encontraron {availablePuzzles.Count} puzzles disponibles:");
        for (int i = 0; i < availablePuzzles.Count; i++)
        {
            Debug.Log($"  {i}: {availablePuzzles[i].name}");
        }

        if (availablePuzzles.Count == 0)
        {
            Debug.LogError("¡ADVERTENCIA! No se encontraron puzzles disponibles en la escena");
        }
    }

    // NUEVO: Método para actualizar el índice del puzzle actual
    private void UpdateCurrentPuzzleIndex()
    {
        // Buscar cuál toggle está activo
        for (int i = 0; i < availablePuzzles.Count; i++)
        {
            var toggle = availablePuzzles[i].GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null && toggle.isOn)
            {
                currentPuzzleIndex = i;
                Debug.Log($"Puzzle actual índice: {currentPuzzleIndex} de {availablePuzzles.Count}");
                return;
            }
        }

        currentPuzzleIndex = -1;
    }

    // OPCIONAL: Método para saltar a un puzzle específico (útil para debugging)
    public void JumpToPuzzle(int index)
    {
        if (index >= 0 && index < availablePuzzles.Count)
        {
            currentPuzzleIndex = index;
            GameObject targetPuzzle = availablePuzzles[index];
            var toggle = targetPuzzle.GetComponent<UnityEngine.UI.Toggle>();

            if (toggle != null)
            {
                // Desactivar todos los otros toggles
                foreach (var puzzle in availablePuzzles)
                {
                    var t = puzzle.GetComponent<UnityEngine.UI.Toggle>();
                    if (t != null && t != toggle)
                    {
                        t.isOn = false;
                    }
                }

                // Activar el toggle objetivo
                toggle.isOn = true;
            }
        }
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
