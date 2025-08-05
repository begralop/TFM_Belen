using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.SceneManagement;
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


    public GameObject cubePrefab;
    public GameObject magnetPrefab;
    public GameObject tableCenterObject;
    public List<Material> otherPuzzleMaterials;  // Lista de materiales genéricos para las otras caras
    public Image selectedImage; // Esta será la imagen seleccionada cuando el usuario haga clic
    public Transform imagesPanel;
    private GameObject selectPuzzle;
    private List<Vector3> magnetPositions = new List<Vector3>(); // Lista de posiciones de los imanes
    private GridCreator gridCreator;

    public GameObject warningPanel;
    public GameObject successPanel;
    public TextMeshProUGUI successMessageText; // Para el panel de éxito
    public TextMeshProUGUI warningMessageText; // Para el panel de advertencia    
    public Button playButton; // Botón de Play
    public Button continueButtonSuccess;
    public Button restartButtonSuccess;
    public Button continueButtonWarning;
    public Button restartButtonWarning;
    public TextMeshProUGUI welcomeText;

    private bool puzzleCompleted = false; // Variable para controlar si el puzzle está completado

    public float cubeSize = 0.1f; // Tamaño del cubo, ajustar según sea necesario
    public float magnetHeightOffset = 0.005f; // Altura adicional para que solo la parte roja del imán sea visible

    public int rows;
    public int columns;

    private Vector3 initialSuccessPanelPosition; // Posición inicial del panel de éxito
    private Vector3 initialWarningPanelPosition; // Posición inicial del panel de advertencia

    void Start()
    {
        // Asegúrate de que el panel esté desactivado al inicio
        warningPanel.SetActive(false);
        successPanel.SetActive(false);
        // selectPuzzle.SetActive(false); // Esta línea daba error si selectPuzzle no estaba asignado
        restartButtonSuccess.onClick.AddListener(RestartGame);
        restartButtonWarning.onClick.AddListener(RestartGame);
        // puzzlePanel.SetActive(false); // Esta línea daba error si puzzlePanel no estaba asignado
        continueButtonSuccess.onClick.AddListener(CloseMessagePanel);
        continueButtonWarning.onClick.AddListener(CloseMessagePanel);
        // Desactivar inicialmente los imanes y cubos
        ClearCurrentMagnets();
        ClearCurrentCubes();
        logoutButton.onClick.AddListener(Logout);


        // --- LÓGICA ACTUALIZADA PARA MOSTRAR EL NOMBRE ---
        if (welcomeText != null)
        {
            // 1. Obtenemos el nombre del usuario actual desde nuestro UserManager
            string playerName = UserManager.GetCurrentUser();

            // 2. Actualizamos el texto en la pantalla
            welcomeText.text = $"Elige un puzzle, {playerName}";
        }
        else
        {
            Debug.LogWarning("La referencia a 'welcomeText' no está asignada en el Inspector.");
        }
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

        // Ya existen estas líneas:
        if (successPanel.activeSelf)
        {
            // PositionPanel(successPanel);
        }

        if (warningPanel.activeSelf)
        {
            // PositionPanel(warningPanel);
        }
    }

   public void StartTimer()
    {
        elapsedTime = 0f;
        isTimerRunning = true;

    }

    public void RestartGame()
    {
        // Lógica para reiniciar el juego
        // CubeInteraction.cubesPlacedCorrectly = 0; // Esto puede dar error si no existe la clase
        warningPanel.SetActive(false);
        successPanel.SetActive(false);
        puzzleCompleted = false;
        ClearCurrentMagnets();
        ClearCurrentCubes();
        GenerateGame();
    }
    public void Logout()
    {
        Debug.Log("Cerrando sesión...");

        // 1. Limpiamos el usuario actual para que la próxima vez no se inicie sesión automáticamente.
        UserManager.SetCurrentUser(null);

        // 2. Comprobamos que el nombre de la escena de login está definido.
        if (string.IsNullOrEmpty(loginSceneName))
        {
            Debug.LogError("El nombre de la escena de login (loginSceneName) no está especificado en el Inspector.");
            return;
        }

        // 3. Cargamos la escena de login.
        SceneManager.LoadScene(loginSceneName);
    }
    public void CloseMessagePanel()
    {
        warningPanel.SetActive(false);
        successPanel.SetActive(false);
    }

    public void CheckPuzzleCompletion()
    {
        isTimerRunning = false; //  Detenemos el contador
        if (IsPuzzleComplete())
        {
            puzzleCompleted = true;
            ShowMessageSuccess("¡Bien hecho! Has completado el puzzle. ¿Quieres jugar de nuevo?", Color.white, true);
        }
        else
        {
            ShowMessageWarning("¡Inténtalo de nuevo!  No has completado el puzzle correctamente. ¿Quieres seguir intentándolo?\"", Color.white, true);
            puzzleCompleted = false;
        }
    }

    private bool IsPuzzleComplete()
    {
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        foreach (GameObject cube in cubes)
        {
            string[] splitName = cube.name.Split('_');
            if (splitName.Length < 3)
            {
                Debug.LogError($"El nombre del cubo no tiene el formato esperado: {cube.name}");
                return false;
            }

            int row, col;
            if (!int.TryParse(splitName[1], out row) || !int.TryParse(splitName[2], out col))
            {
                Debug.LogError($"No se pudieron parsear los índices de la cuadrícula desde el nombre del cubo: {cube.name}");
                return false;
            }

            Vector3 targetPosition = GetMagnetPosition(row, col);

            if (Vector3.Distance(cube.transform.position, targetPosition) <= 0.08f &&
                Quaternion.Angle(cube.transform.rotation, Quaternion.identity) <= 10.0f)
            {
                cube.transform.position = targetPosition;
                cube.transform.rotation = Quaternion.identity;
                Debug.Log($"Cubo {cube.name} alineado correctamente.");
            }
            else
            {
                Debug.Log($"Cubo {cube.name} no está alineado correctamente.");
                return false;
            }
        }
        return true;
    }

    private void ShowMessageSuccess(string message, Color color, bool showRestartButtonSuccess)
    {
        successMessageText.text = message;
        successMessageText.color = color;
        successPanel.SetActive(true);
        restartButtonSuccess.gameObject.SetActive(showRestartButtonSuccess);
        continueButtonSuccess.gameObject.SetActive(showRestartButtonSuccess);
    }

    private void ShowMessageWarning(string message, Color color, bool showRestartButtonWarning)
    {
        warningMessageText.text = message;
        warningMessageText.color = color;
        warningPanel.SetActive(true);
        restartButtonWarning.gameObject.SetActive(showRestartButtonWarning);
        continueButtonWarning.gameObject.SetActive(showRestartButtonWarning);
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
                            // cube.AddComponent<CubeInteraction>(); // Puede dar error si la clase no existe
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
        Debug.Log($"Se generaron {magnetPositions.Count} posiciones de imanes.");
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
        selectedImage = image;
        otherPuzzleMaterials.Clear();
        GenerateMaterialsFromPanelImages();
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
                    else
                    {
                        Debug.LogWarning("El Image en " + backgroundTransform.name + " no tiene un sprite asignado o es el sprite seleccionado.");
                    }
                }
                else
                {
                    Debug.LogWarning("No se encontró 'Background' en " + child.name);
                }
            }
            else
            {
                Debug.LogWarning("No se encontró 'Content' en " + child.name);
            }
        }
        Debug.Log("Total de materiales generados: " + otherPuzzleMaterials.Count);
    }
}
