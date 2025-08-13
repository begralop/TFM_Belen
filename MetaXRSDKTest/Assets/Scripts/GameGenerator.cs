using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.SceneManagement;
public class GameGenerator : MonoBehaviour
{
    // ELIMINADO: Ya no usamos esta clave directamente
    // private const string PlayerNameKey = "PlayerName";
    [Header("Gesti�n de Sesi�n")]
    [Tooltip("El bot�n que el usuario pulsar� para cerrar sesi�n.")]
    public Button logoutButton;
    [Tooltip("El nombre exacto de tu escena de Login (ej: 'LoginScene').")]
    public string loginSceneName;

    [Header("Contador de Tiempo")]
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;

    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    // --- VARIABLES DE UI UNIFICADAS ---
    [Header("Panel de Resultado")]
    [Tooltip("El panel que aparecer� al final del puzle.")]
    public GameObject resultPanel;
    [Tooltip("El texto para el mensaje principal (�xito o advertencia).")]
    public TextMeshProUGUI resultMessageText;
    [Tooltip("El boton para continuar o cerrar el panel.")]
    public Button continueButton;
    [Tooltip("El boton para reiniciar la partida.")]
    public Button restartButton;



    [Header("Configuraci�n del Puzle")]
    public GameObject cubePrefab;
    public GameObject magnetPrefab;
    public GameObject tableCenterObject;
    public List<Material> otherPuzzleMaterials;  // Lista de materiales gen�ricos para las otras caras
    public Image selectedImage; // Esta ser� la imagen seleccionada cuando el usuario haga clic
    public Transform imagesPanel;

    private GameObject selectPuzzle;
    private List<Vector3> magnetPositions = new List<Vector3>(); // Lista de posiciones de los imanes
    private GridCreator gridCreator;


    public Button playButton; // Bot�n de Play
    public TextMeshProUGUI welcomeText;


    private int placedCubesCount = 0;

    private bool puzzleCompleted = false; // Variable para controlar si el puzzle est� completado

    public float cubeSize = 0.1f; // Tama�o del cubo, ajustar seg�n sea necesario
    public float magnetHeightOffset = 0.005f; // Altura adicional para que solo la parte roja del im�n sea visible

    public int rows;
    public int columns;

    private Vector3 initialSuccessPanelPosition; // Posici�n inicial del panel de �xito
    private Vector3 initialWarningPanelPosition; // Posici�n inicial del panel de advertencia

    void Start()
    {
        // Aseg�rate de que el panel est� desactivado al inicio
        resultPanel.SetActive(false);

        // selectPuzzle.SetActive(false); // Esta l�nea daba error si selectPuzzle no estaba asignado

        // puzzlePanel.SetActive(false); // Esta l�nea daba error si puzzlePanel no estaba asignado

        // Desactivar inicialmente los imanes y cubos
        ClearCurrentMagnets();
        ClearCurrentCubes();

        // --- L�GICA ACTUALIZADA PARA MOSTRAR EL NOMBRE ---
        if (welcomeText != null)
        {
            // 1. Obtenemos el nombre del usuario actual desde nuestro UserManager
            string playerName = UserManager.GetCurrentUser();

            // 2. Actualizamos el texto en la pantalla
            welcomeText.text = $"Elige un puzzle, {playerName}";
        }
        else
        {
            Debug.LogWarning("La referencia a 'welcomeText' no esta asignada en el Inspector.");
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
    }

    public void Logout()
    {
        Debug.Log("Cerrando sesion...");

        // 1. Limpiamos el usuario actual para que la pr�xima vez no se inicie sesi�n autom�ticamente.
        UserManager.SetCurrentUser(null);

        // 2. Comprobamos que el nombre de la escena de login est� definido.
        if (string.IsNullOrEmpty(loginSceneName))
        {
            Debug.LogError("El nombre de la escena de login (loginSceneName) no esta especificado en el Inspector.");
            return;
        }

        // 3. Cargamos la escena de login.
        SceneManager.LoadScene(loginSceneName);
    }

    private void ShowResult(bool isSuccess)
    {
        resultPanel.SetActive(true);

        // 1. Limpiamos CUALQUIER listener que pudieran tener de antes.
        continueButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();


        restartButton.onClick.AddListener(RestartGame);

        if (isSuccess)
        {
            // Puzle completado con �xito
            resultMessageText.text = "�Bien hecho! Has completado el puzle. ¿Quieres jugar de nuevo?";
            // Al pulsar 'Continuar', solo cerramos el panel
            continueButton.onClick.AddListener(CloseResultPanel);
            // Guardamos la puntuaci�n: usuario actual, nombre del sprite del puzle y tiempo transcurrido.
         //   UserManager.AddScore(UserManager.GetCurrentUser(), selectedImage.sprite.name, elapsedTime);

        }
        else
        {
            // Puzle incorrecto
            resultMessageText.text = "No has completado el puzle correctamente. ¿Quieres seguir intentándolo?";
            // Al pulsar 'Continuar', cerramos el panel Y reanudamos el tiempo
            continueButton.onClick.AddListener(ContinueGameAfterWarning);
        }
    }

    public void CloseResultPanel()
    {
        resultPanel.SetActive(false);
    }

    public void ContinueGameAfterWarning()
    {
        resultPanel.SetActive(false);
        isTimerRunning = true; // Reanuda el contador

        // --- NUEVA L�GICA PARA MOVER TODAS LAS PIEZAS ---

        // 1. Buscamos todos los cubos en la escena.
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("cube");

        // 2. Obtenemos el centro de la mesa como referencia.
        Vector3 puzzleCenter = tableCenterObject.transform.position;

        // 3. Recorremos cada cubo para aplicarle un desplazamiento.
        foreach (GameObject cube in cubes)
        {
            // Calculamos el vector que va desde el centro del puzle hacia el cubo.
            Vector3 directionFromCenter = (cube.transform.position - puzzleCenter).normalized;

            // Le quitamos la componente vertical para que el empuje sea solo hacia los lados.
            directionFromCenter.y = 0;

            // Definimos el desplazamiento: un empuj�n hacia fuera y un peque�o salto.
            float outwardPush = 0.15f; // 15 cm hacia fuera
            float upwardPush = 0.05f;  // 5 cm hacia arriba
            Vector3 displacement = directionFromCenter * outwardPush + Vector3.up * upwardPush;

            // Aplicamos el desplazamiento al cubo.
            cube.transform.position += displacement;
        }
    }
    public void RestartGame()
    {
        resultPanel.SetActive(false);
        // L�gica para reiniciar el juego
        placedCubesCount = 0; // Esto puede dar error si no existe la clase
        elapsedTime = 0f;
        timerText.text = "00:00";
        isTimerRunning = true;
        puzzleCompleted = false;

        ClearCurrentMagnets();
        ClearCurrentCubes();
        GenerateGame();
    }

    public void OnCubePlaced()
    {
        placedCubesCount++;
        if (placedCubesCount >= (rows * columns))
        {
            StartDelayedCheck();
        }
    }

    public void OnCubeRemoved()
    {
        placedCubesCount--;
    }

    public void CheckPuzzleCompletion()
    {
        // 1. Detenemos el tiempo
        isTimerRunning = false;

        // 2. Comprobamos si el puzle esta bien resuelto
        bool puzzleEsCorrecto = IsPuzzleComplete();

        // 3. Llamamos a nuestro nuevo m�todo unificado para mostrar el resultado
        ShowResult(puzzleEsCorrecto);
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

            // Verificación de alineación y orientación
            if (Vector3.Distance(cube.transform.position, targetPosition) <= 0.1f &&
                Quaternion.Angle(cube.transform.rotation, Quaternion.identity) <= 15.0f)
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

    // Añade este método público para que pueda ser llamado desde otros scripts
    public void StartDelayedCheck()
    {
        StartCoroutine(CheckPuzzleAfterDelay());
    }

    // Esta es la corutina que crea el retardo
    private IEnumerator CheckPuzzleAfterDelay()
    {
        // Espera un breve momento para que la física y el snapping se completen
        yield return new WaitForSeconds(0.2f);

        // Ahora sí, ejecuta la lógica de verificación
        CheckPuzzleCompletion();
    }

    void PositionPanel(GameObject panel)
    {
        // Asegurarse de que el centro de la mesa est� asignado
        if (tableCenterObject == null)
        {
            Debug.LogError("No se ha asignado un objeto de referencia para el centro de la mesa.");
            return;
        }

        // Calcular el centro de la cuadr�cula de imanes
        Vector3 tableCenter = tableCenterObject.transform.position;
        float puzzleWidth = gridCreator.columns * cubeSize;
        float puzzleHeight = gridCreator.rows * cubeSize;

        // Obtener la posici�n central de la cuadr�cula
        Vector3 puzzleCenter = new Vector3(
            tableCenter.x,
            tableCenter.y + magnetHeightOffset,  // Altura de los imanes
            tableCenter.z
        );

        // A�adir un offset vertical para que el panel aparezca justo encima de los imanes
        float panelHeightOffset = 0.2f;  // Ajustar la altura del panel por encima de los imanes
        Vector3 panelPosition = new Vector3(
            puzzleCenter.x,
            puzzleCenter.y + panelHeightOffset, // Elevar el panel por encima de los imanes
            puzzleCenter.z
        );

        // Posicionar el panel
        panel.transform.position = panelPosition;

        // Asegurarse de que el panel est� mirando hacia la c�mara
        panel.transform.LookAt(Camera.main.transform);

        // Ajustar la rotaci�n en el eje Y si es necesario para que el panel est� completamente de frente
        panel.transform.rotation = Quaternion.Euler(0, panel.transform.rotation.eulerAngles.y, 0);

        // Escalar el panel si es necesario
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
                            cube.AddComponent<CubeInteraction>(); // Puede dar error si la clase no existe
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
                    Debug.LogError("No se pudo cargar el prefab de im�n.");
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
            Debug.LogError("La posici�n del im�n est� fuera de los l�mites.");
            return Vector3.zero;
        }
    }

    public void OnImageSelected(Image image)
    {
        // --- INICIO: LÓGICA AÑADIDA PARA REINICIAR EL TEMPORIZADOR ---
        // Cuando el usuario elige una imagen nueva, reseteamos el temporizador.
        elapsedTime = 0f;
        isTimerRunning = false;

        // Actualizamos el texto para que muestre "00:00"
        if (timerText != null)
        {
            timerText.text = "00:00";
        }
        // --- FIN: LÓGICA AÑADIDA ---

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
                        Debug.Log("Imagen a�adida a otherPuzzleMaterials: " + img.sprite.name);
                    }
                    else
                    {
                        Debug.LogWarning("El Image en " + backgroundTransform.name + " no tiene un sprite asignado o es el sprite seleccionado.");
                    }
                }
                else
                {
                    Debug.LogWarning("No se encontr� 'Background' en " + child.name);
                }
            }
            else
            {
                Debug.LogWarning("No se encontr� 'Content' en " + child.name);
            }
        }
        Debug.Log("Total de materiales generados: " + otherPuzzleMaterials.Count);
    }
}
