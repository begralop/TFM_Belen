using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;
using Oculus.Interaction;

public class GameGenerator : MonoBehaviour
{
    [Header("Gestión de Sesión")]
    [Tooltip("El botón que el usuario pulsará para cerrar sesión.")]
    public Button logoutButton;
    [Tooltip("El nombre exacto de tu escena de Login (ej: 'LoginScene').")]
    public string loginSceneName;

    [Header("Sistema de Pistas")]
    [Tooltip("Referencia al sistema de pistas")]
    public HintSystem hintSystem;

    [Header("Círculos de Pistas")]
    [Tooltip("GameObject del círculo para mostrar imán verde")]
    public GameObject magnetCircle;

    [Tooltip("GameObject del círculo para mostrar caras grises")]
    public GameObject facesCircle;

    [Header("Contador de Tiempo")]
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;

    private float elapsedTime = 0f;
    private bool isTimerRunning = false;
    private bool timeAlreadySaved = false;

    [Header("Sistema de Intentos")]
    private int currentAttempts = 0;
    private bool resultAlreadyShown = false;

    [Header("Panel de Debug")]
    [Tooltip("Panel de Canvas para mostrar información de debug")]
    public GameObject debugPanel;
    [Tooltip("Texto para mostrar información de debug")]
    public TextMeshProUGUI debugText;
    [Tooltip("Activar/Desactivar modo debug")]
    public bool debugMode = true;

    private StringBuilder debugInfo = new StringBuilder();
    private Dictionary<string, Vector3> cubePositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> cubeRotations = new Dictionary<string, Quaternion>();

    [Header("Panel de Resultado")]
    [Tooltip("El panel que aparecerá al final del puzle.")]
    public GameObject resultPanel;
    [Tooltip("El texto para el mensaje principal (éxito o advertencia).")]
    public TextMeshProUGUI resultMessageText;
    [Tooltip("El boton para continuar o cerrar el panel.")]
    public Button continueButton;
    [Tooltip("El boton para reiniciar la partida.")]
    public Button restartButton;

    [Header("Sistema de Puntuaciones")]
    [Tooltip("Referencia al sistema de visualización de puntuaciones")]
    public PuzzleScoreDisplay scoreDisplay;
    private int currentPuzzleIndex = -1;
    private List<GameObject> availablePuzzles = new List<GameObject>();

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
        resultPanel.SetActive(false);

        if (scoreDisplay == null)
        {
            scoreDisplay = FindObjectOfType<PuzzleScoreDisplay>();
        }

        if (debugPanel != null && debugMode)
        {
            debugPanel.SetActive(true);
            UpdateDebugInfo("Sistema de debug iniciado...");
        }

        ClearCurrentMagnets();
        ClearCurrentCubes();

        if (hintSystem == null)
        {
            hintSystem = FindObjectOfType<HintSystem>();
        }

        // Configurar círculos de pistas si existen
        SetupHintCircles();

        if (welcomeText != null)
        {
            string playerName = UserManager.GetCurrentUser();
            welcomeText.text = $"Elige un puzzle, {playerName}";
        }
    }

    void SetupHintCircles()
    {
        // Configurar círculo de imán verde
        if (magnetCircle != null)
        {
            MagnetCircleDetector detector = magnetCircle.GetComponent<MagnetCircleDetector>();
            if (detector == null)
            {
                detector = magnetCircle.AddComponent<MagnetCircleDetector>();
            }
            detector.SetHintSystem(hintSystem);

            // IMPORTANTE: Asegurarse de que inicialmente esté oculto
            magnetCircle.SetActive(false);
            UpdateDebugInfo("Círculo de imán configurado y oculto inicialmente");
        }

        // Configurar círculo de caras grises
        if (facesCircle != null)
        {
            FacesCircleDetector detector = facesCircle.GetComponent<FacesCircleDetector>();
            if (detector == null)
            {
                detector = facesCircle.AddComponent<FacesCircleDetector>();
            }
            detector.SetHintSystem(hintSystem);

            // IMPORTANTE: Asegurarse de que inicialmente esté oculto
            facesCircle.SetActive(false);
            UpdateDebugInfo("Círculo de caras configurado y oculto inicialmente");
        }

        // Actualizar visibilidad basada en el estado actual de las pistas
        UpdateCirclesVisibility();
    }

    public void UpdateCirclesVisibility()
    {
        bool hintsEnabled = hintSystem != null && hintSystem.AreHintsEnabled();

        if (magnetCircle != null)
            magnetCircle.SetActive(hintsEnabled);

        if (facesCircle != null)
            facesCircle.SetActive(hintsEnabled);

        UpdateDebugInfo($"Círculos actualizados - Pistas {(hintsEnabled ? "ACTIVADAS" : "DESACTIVADAS")}");
    }

    public void UpdateDebugInfo(string message)
    {
        debugInfo.AppendLine($"{message}");

        if (debugText != null)
        {
            debugText.text = debugInfo.ToString();

            string[] lines = debugInfo.ToString().Split('\n');
            if (lines.Length > 50)
            {
                debugInfo.Clear();
                for (int i = lines.Length - 50; i < lines.Length; i++)
                {
                    if (i >= 0 && !string.IsNullOrEmpty(lines[i]))
                        debugInfo.AppendLine(lines[i]);
                }
                debugText.text = debugInfo.ToString();
            }
        }
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

        // NO actualizar círculos constantemente en Update
        // Solo cuando sea necesario (al cambiar estado de pistas)
    }

    public void Logout()
    {
        Debug.Log("Cerrando sesion...");
        UserManager.SetCurrentUser(null);

        if (string.IsNullOrEmpty(loginSceneName))
        {
            Debug.LogError("El nombre de la escena de login no esta especificado.");
            return;
        }

        SceneManager.LoadScene(loginSceneName);
    }

    private void ShowResult(bool isSuccess)
    {
        if (resultAlreadyShown)
        {
            UpdateDebugInfo("ShowResult ya fue llamado, evitando duplicado");
            return;
        }

        resultAlreadyShown = true;
        currentAttempts++;
        UpdateDebugInfo($"RESULTADO: Intento {currentAttempts} - {(isSuccess ? "ÉXITO" : "FALLO")}");

        resultPanel.SetActive(true);

        continueButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);

        if (isSuccess)
        {
            isTimerRunning = false;

            if (!timeAlreadySaved)
            {
                string puzzleId = GetCurrentPuzzleId();
                string currentUser = UserManager.GetCurrentUser();

                if (!string.IsNullOrEmpty(puzzleId) && currentUser != "Invitado")
                {
                    int totalCubes = rows * columns;
                    UserManager.AddScore(currentUser, puzzleId, elapsedTime, currentAttempts, totalCubes);
                    Debug.Log($"Puntuación guardada");
                    timeAlreadySaved = true;

                    if (scoreDisplay != null && selectedImage != null && selectedImage.sprite != null)
                    {
                        StartCoroutine(RefreshScoreDisplayAfterDelay());
                    }
                }
            }

            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            bool hasNextPuzzle = (currentPuzzleIndex >= 0 && currentPuzzleIndex < availablePuzzles.Count - 1);
            string attemptText = currentAttempts == 1 ? "1 intento" : $"{currentAttempts} intentos";

            if (hasNextPuzzle)
            {
                resultMessageText.text = $"¡Bien hecho! Has completado el puzle en {minutes:00}:{seconds:00} con {attemptText}.\n¿Quieres continuar con el siguiente puzzle?";
                var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (continueButtonText != null)
                    continueButtonText.text = "Siguiente";
                continueButton.onClick.AddListener(LoadNextPuzzle);
            }
            else
            {
                resultMessageText.text = $"¡Felicidades! Has completado el puzle en {minutes:00}:{seconds:00} con {attemptText}.\n¡Has completado todos los puzzles disponibles!";
                var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (continueButtonText != null)
                    continueButtonText.text = "Cerrar";
                continueButton.onClick.AddListener(CloseResultPanel);
            }
        }
        else
        {
            string attemptText = currentAttempts == 1 ? "1 intento" : $"{currentAttempts} intentos";
            resultMessageText.text = $"No has completado el puzle correctamente. Llevas {attemptText}. ¿Quieres seguir intentándolo?";
            var continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueButtonText != null)
                continueButtonText.text = "Reintentar";
            continueButton.onClick.AddListener(ContinueGameAfterWarning);
        }
    }

    private string GetCurrentPuzzleId()
    {
        if (selectedImage != null && selectedImage.sprite != null)
        {
            return selectedImage.sprite.name;
        }

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
        resultAlreadyShown = false;
        UpdateDebugInfo("Panel de resultado cerrado");
    }

    public void ContinueGameAfterWarning()
    {
        resultPanel.SetActive(false);
        resultAlreadyShown = false;
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
        resultAlreadyShown = false;
        placedCubesCount = 0;
        elapsedTime = 0f;
        timerText.text = "00:00";
        isTimerRunning = true;
        puzzleCompleted = false;
        timeAlreadySaved = false;
        currentAttempts = 0;

        // IMPORTANTE: Limpiar efectos de pistas antes de regenerar
        if (hintSystem != null)
        {
            hintSystem.OnPuzzleChanged();
        }

        ClearCurrentMagnets();
        ClearCurrentCubes();

        // Asegurarse de que tenemos materiales de otros puzzles antes de generar
        if (otherPuzzleMaterials.Count == 0)
        {
            GenerateMaterialsFromPanelImages();
            UpdateDebugInfo($"Materiales de otros puzzles generados: {otherPuzzleMaterials.Count}");
        }

        GenerateGame();

        if (scoreDisplay != null && selectedImage != null && selectedImage.sprite != null)
        {
            if (scoreDisplay.scorePanel != null && scoreDisplay.scorePanel.activeSelf)
            {
                StartCoroutine(RefreshScoreDisplayAfterRestart());
            }
        }

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
        UpdateDebugInfo("INICIANDO VERIFICACIÓN DEL PUZZLE");
        isTimerRunning = false;
        bool puzzleEsCorrecto = IsPuzzleComplete();
        ShowResult(puzzleEsCorrecto);
    }

    private bool IsPuzzleComplete()
    {
        debugInfo.Clear();
        debugInfo.AppendLine("=======================================");
        debugInfo.AppendLine("VERIFICANDO PUZZLE");
        debugInfo.AppendLine("=======================================");

        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        if (cubes.Length == 0)
        {
            debugInfo.AppendLine("No se encontraron cubos.");
            return false;
        }

        bool todosCorrectos = true;

        foreach (GameObject cube in cubes)
        {
            string[] splitName = cube.name.Split('_');
            int row, col;

            if (splitName.Length < 3 || !int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
            {
                debugInfo.AppendLine($"ERROR: Nombre inválido: {cube.name}");
                todosCorrectos = false;
                continue;
            }

            Vector3 targetPosition = GetMagnetPosition(row, col);
            float distancia = Vector3.Distance(cube.transform.position, targetPosition);
            Quaternion targetRotation = Quaternion.identity;
            float angulo = Quaternion.Angle(cube.transform.rotation, targetRotation);

            debugInfo.AppendLine($"Cubo {cube.name}: Dist={distancia:F3} Ang={angulo:F1}");

            if (distancia <= 0.1f && angulo <= 10.0f)
            {
                debugInfo.AppendLine($"  -> CORRECTO");
                cube.transform.rotation = targetRotation;
            }
            else
            {
                debugInfo.AppendLine($"  -> INCORRECTO");
                todosCorrectos = false;
            }
        }

        debugInfo.AppendLine("=======================================");
        debugInfo.AppendLine(todosCorrectos ? "ÉXITO" : "FALLO");
        return todosCorrectos;
    }

    public void StartDelayedCheck()
    {
        StartCoroutine(CheckPuzzleAfterDelay());
    }

    private IEnumerator CheckPuzzleAfterDelay()
    {
        UpdateDebugInfo("Esperando estabilización...");
        yield return new WaitForSeconds(0.2f);
        CheckPuzzleCompletion();
    }

    void GenerateGame()
    {
        placedCubesCount = 0;
        currentAttempts = 0;
        resultAlreadyShown = false;

        // IMPORTANTE: Limpiar cualquier efecto de pistas antes de generar
        if (hintSystem != null)
        {
            hintSystem.OnPuzzleChanged();
        }

        UpdateDebugInfo($"Generando juego...");

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
                            // Verificar que tenemos materiales de otros puzzles
                            if (otherPuzzleMaterials == null || otherPuzzleMaterials.Count == 0)
                            {
                                UpdateDebugInfo("ERROR CRÍTICO: No hay materiales de otros puzzles!");
                                UpdateDebugInfo("Intentando regenerar materiales...");
                                GenerateMaterialsFromPanelImages();
                            }

                            foreach (Renderer renderer in cubeRenderers)
                            {
                                if (renderer.name == "Face1")
                                {
                                    // Face1 tiene el fragmento correcto del puzzle
                                    renderer.material = materials[r * gridCreator.columns + c];
                                }
                                else
                                {
                                    // Las otras caras deben tener materiales aleatorios de otros puzzles
                                    if (otherPuzzleMaterials != null && otherPuzzleMaterials.Count > 0)
                                    {
                                        int randomIndex = Random.Range(0, otherPuzzleMaterials.Count);
                                        renderer.material = otherPuzzleMaterials[randomIndex];
                                    }
                                    else
                                    {
                                        // Si aún no hay materiales, usar el material del propio puzzle como fallback
                                        UpdateDebugInfo($"FALLBACK: Usando material del puzzle actual para {renderer.name}");
                                        int randomPieceIndex = Random.Range(0, materials.Length);
                                        renderer.material = materials[randomPieceIndex];
                                    }
                                }
                            }

                            cube.name = $"Cube_{r}_{c}";

                            var rb = cube.GetComponent<Rigidbody>();
                            if (rb == null)
                            {
                                rb = cube.AddComponent<Rigidbody>();
                                rb.useGravity = true;
                                rb.isKinematic = false;
                            }

                            var grabbable = cube.AddComponent<Grabbable>();
                            var physicsGrabbable = cube.AddComponent<PhysicsGrabbable>();
                            physicsGrabbable.InjectAllPhysicsGrabbable(grabbable, rb);

                            var cubeInteraction = cube.AddComponent<CubeInteraction>();
                        }
                        else
                        {
                            Debug.LogWarning("El cubo no tiene renderers");
                            Destroy(cube);
                        }

                        var magnet = Instantiate(magnetPrefab, magnetPositions[magnetIndex], Quaternion.identity);
                        magnet.tag = "refCube";
                        magnetIndex++;
                        StartTimer();
                    }
                }

                UpdateDebugInfo($"Juego generado: {rows}x{columns}");
            }
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
            Debug.LogError("No se encontró el centro de la mesa.");
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
            }
        }

        UpdateDebugInfo($"Generados {magnetPositions.Count} imanes");
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

    public Vector3 GetMagnetPosition(int row, int col)
    {
        if (row * gridCreator.columns + col < magnetPositions.Count)
        {
            return magnetPositions[row * gridCreator.columns + col];
        }
        else
        {
            Debug.LogError("Posición de imán fuera de límites.");
            return Vector3.zero;
        }
    }

    public void OnImageSelected(Image image)
    {
        if (resultPanel != null && resultPanel.activeSelf)
        {
            resultPanel.SetActive(false);
        }

        // Limpiar cualquier efecto de pistas antes de generar el nuevo puzzle
        if (hintSystem != null)
        {
            hintSystem.OnPuzzleChanged();
        }

        elapsedTime = 0f;
        isTimerRunning = false;
        timeAlreadySaved = false;
        currentAttempts = 0;
        placedCubesCount = 0;
        puzzleCompleted = false;
        resultAlreadyShown = false;

        if (timerText != null)
        {
            timerText.text = "00:00";
        }

        selectedImage = image;

        // IMPORTANTE: Primero obtener los valores de rows y columns
        var grid = GameObject.FindGameObjectWithTag("Grid");
        if (grid != null)
        {
            gridCreator = grid.GetComponent<GridCreator>();
            if (gridCreator != null)
            {
                rows = gridCreator.rows;
                columns = gridCreator.columns;
                UpdateDebugInfo($"Grid encontrado: {rows}x{columns}");
            }
        }

        // DESPUÉS generar los materiales con los valores correctos
        otherPuzzleMaterials.Clear();
        GenerateMaterialsFromPanelImages();

        UpdateCurrentPuzzleIndex();
        UpdateDebugInfo($"Puzzle seleccionado: {image.sprite.name}");
        UpdateDebugInfo($"Materiales de otros puzzles disponibles: {otherPuzzleMaterials.Count}");

        // Actualizar visibilidad de los círculos después de seleccionar puzzle
        UpdateCirclesVisibility();
    }

    private void LoadNextPuzzle()
    {
        if (currentPuzzleIndex >= 0 && currentPuzzleIndex < availablePuzzles.Count - 1)
        {
            resultPanel.SetActive(false);
            timeAlreadySaved = false;
            currentAttempts = 0;

            ClearCurrentMagnets();
            ClearCurrentCubes();

            currentPuzzleIndex++;
            GameObject nextPuzzle = availablePuzzles[currentPuzzleIndex];

            var toggle = nextPuzzle.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null)
            {
                foreach (var puzzle in availablePuzzles)
                {
                    var t = puzzle.GetComponent<UnityEngine.UI.Toggle>();
                    if (t != null && t != toggle)
                    {
                        t.isOn = false;
                    }
                }

                toggle.isOn = true;
                StartCoroutine(GenerateNextPuzzleAfterDelay());
            }
        }
        else
        {
            CloseResultPanel();
        }
    }

    private IEnumerator RefreshScoreDisplayAfterRestart()
    {
        yield return new WaitForSeconds(0.1f);
        if (scoreDisplay != null && selectedImage != null && selectedImage.sprite != null)
        {
            scoreDisplay.RefreshScores();
        }
    }

    private IEnumerator RefreshScoreDisplayAfterDelay()
    {
        yield return null;
        if (scoreDisplay != null && selectedImage != null && selectedImage.sprite != null)
        {
            scoreDisplay.ShowScoresForPuzzle(selectedImage.sprite);
        }
    }

    private IEnumerator GenerateNextPuzzleAfterDelay()
    {
        yield return null;
        GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
        if (curvedBackground != null)
        {
            Image bgImage = curvedBackground.GetComponent<Image>();
            if (bgImage != null && bgImage.sprite != null)
            {
                if (playButton != null)
                {
                    playButton.onClick.Invoke();
                }
                else
                {
                    GenerateGame();
                }
            }
        }
    }

    private void CollectAvailablePuzzles()
    {
        availablePuzzles.Clear();
        ImagePanelController[] controllers = FindObjectsOfType<ImagePanelController>();

        foreach (var controller in controllers)
        {
            if (controller.gameObject.GetComponent<Toggle>() != null)
            {
                availablePuzzles.Add(controller.gameObject);
            }
        }

        availablePuzzles.Sort((a, b) => a.name.CompareTo(b.name));
        Debug.Log($"Se encontraron {availablePuzzles.Count} puzzles");
    }

    private void UpdateCurrentPuzzleIndex()
    {
        for (int i = 0; i < availablePuzzles.Count; i++)
        {
            var toggle = availablePuzzles[i].GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null && toggle.isOn)
            {
                currentPuzzleIndex = i;
                return;
            }
        }
        currentPuzzleIndex = -1;
    }

    void GenerateMaterialsFromPanelImages()
    {
        // IMPORTANTE: Obtener rows y columns del GridCreator si aún no están establecidos
        if (rows == 0 || columns == 0)
        {
            var grid = GameObject.FindGameObjectWithTag("Grid");
            if (grid != null)
            {
                gridCreator = grid.GetComponent<GridCreator>();
                if (gridCreator != null)
                {
                    rows = gridCreator.rows;
                    columns = gridCreator.columns;
                    UpdateDebugInfo($"Rows y columns obtenidos del GridCreator: {rows}x{columns}");
                }
            }
        }

        // Si aún no tenemos valores válidos, usar valores por defecto
        if (rows == 0 || columns == 0)
        {
            rows = 3;  // Valor por defecto
            columns = 3;  // Valor por defecto
            UpdateDebugInfo($"ADVERTENCIA: Usando valores por defecto: {rows}x{columns}");
        }

        // Limpiar lista anterior
        otherPuzzleMaterials.Clear();

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
                    }
                }
            }
        }

        UpdateDebugInfo($"Total materiales de otros puzzles generados: {otherPuzzleMaterials.Count}");
    }
}