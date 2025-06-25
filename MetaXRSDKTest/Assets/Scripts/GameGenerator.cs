using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class GameGenerator : MonoBehaviour
{
    public GameObject cubePrefab;
    public GameObject magnetPrefab;
    public GameObject tableCenterObject;
    public List<Material> otherPuzzleMaterials;  // Lista de materiales gen�ricos para las otras caras
    public Image selectedImage; // Esta ser� la imagen seleccionada cuando el usuario haga clic
    public Transform imagesPanel;

    private List<Vector3> magnetPositions = new List<Vector3>(); // Lista de posiciones de los imanes
    private GridCreator gridCreator;

    public GameObject warningPanel;
    public GameObject successPanel;
    public TextMeshProUGUI successMessageText; // Para el panel de �xito
    public TextMeshProUGUI warningMessageText; // Para el panel de advertencia   
    public Button playButton; // Bot�n de Play
    public Button continueButtonSuccess;
    public Button restartButtonSuccess;
    public Button continueButtonWarning;
    public Button restartButtonWarning;


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
        warningPanel.SetActive(false);
        successPanel.SetActive(false);

        restartButtonSuccess.onClick.AddListener(RestartGame);
        restartButtonWarning.onClick.AddListener(RestartGame);

        continueButtonSuccess.onClick.AddListener(CloseMessagePanel);
        continueButtonWarning.onClick.AddListener(CloseMessagePanel);


        // Desactivar inicialmente los imanes y cubos
        ClearCurrentMagnets();
        ClearCurrentCubes();
    }

    void Update()
    {
        if (successPanel.activeSelf)
        {
           // PositionPanel(successPanel);
        }

        if (warningPanel.activeSelf)
        {
           // PositionPanel(warningPanel);
        }
    }

    public void RestartGame()
    {
        // L�gica para reiniciar el juego
        CubeInteraction.cubesPlacedCorrectly = 0;
        warningPanel.SetActive(false);
        successPanel.SetActive(false);
        puzzleCompleted = false;
        ClearCurrentMagnets();
        ClearCurrentCubes();
        GenerateGame();
    }

    public void CloseMessagePanel()
    {

            warningPanel.SetActive(false);
 
            successPanel.SetActive(false);
    }

    public void CheckPuzzleCompletion()
    {
        if (IsPuzzleComplete())
        {
            puzzleCompleted = true;
            ShowMessageSuccess("�Bien hecho! Has completado el puzzle. �Quieres jugar de nuevo?", Color.white, true);
        }
        else
        {
            ShowMessageWarning("�Int�ntalo de nuevo!  No has completado el puzzle correctamente. �Quieres seguir intent�ndolo?\"", Color.white, true);
            puzzleCompleted = true;
            //ShowMessageSuccess("�Bien hecho! Has completado el puzzle. �Quieres jugar de nuevo?", Color.white, true);
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
                Debug.LogError($"No se pudieron parsear los �ndices de la cuadr�cula desde el nombre del cubo: {cube.name}");
                return false;
            }

            Vector3 targetPosition = GetMagnetPosition(row, col);

            // Verificaci�n de alineaci�n y orientaci�n
            if (Vector3.Distance(cube.transform.position, targetPosition) <= 0.08f &&
                Quaternion.Angle(cube.transform.rotation, Quaternion.identity) <= 10.0f)
            {
                cube.transform.position = targetPosition;
                cube.transform.rotation = Quaternion.identity;
                Debug.Log($"Cubo {cube.name} alineado correctamente.");
            }
            else
            {
                Debug.Log($"Cubo {cube.name} no est� alineado correctamente.");
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
      //  PositionPanel(successPanel);
    }

    private void ShowMessageWarning(string message, Color color, bool showRestartButtonWarning)
    {
        warningMessageText.text = message;
        warningMessageText.color = color;
        warningPanel.SetActive(true);
        restartButtonWarning.gameObject.SetActive(showRestartButtonWarning);
        continueButtonWarning.gameObject.SetActive(showRestartButtonWarning);
     //   PositionPanel(warningPanel);
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
                ClearCurrentMagnets(); // Limpiar los imanes actuales
                ClearCurrentCubes(); // Limpiar las piezas del puzzle actuales
                GenerateMagnetPositions(gridCreator.rows, gridCreator.columns); // Generar posiciones de los imanes

                int magnetIndex = 0; // Inicializar �ndice de imanes

                for (int r = 0; r < gridCreator.rows; r++)
                {
                    for (int c = 0; c < gridCreator.columns; c++)
                    {
                        // Generar cubo en posici�n aleatoria
                        Vector3 cubePosition = new Vector3(Random.Range(-0.5f, 0.5f), 1.2f, Random.Range(0.2f, 0.6f));
                        Quaternion cubeRotation = Quaternion.Euler(Random.Range(0f, 90f), Random.Range(0f, 90f), Random.Range(0f, 90f));
                        GameObject cube = Instantiate(cubePrefab, cubePosition, cubeRotation);

                        // Asignar el tag "cube" al cubo
                        cube.tag = "cube";

                        // Obtener los renderers de las caras del cubo
                        Renderer[] cubeRenderers = cube.GetComponentsInChildren<Renderer>();

                        if (cubeRenderers != null && cubeRenderers.Length > 0)
                        {
                            foreach (Renderer renderer in cubeRenderers)
                            {
                                // Asignar el material de la imagen dividida a una cara espec�fica del cubo
                                if (renderer.name == "Face1")  // Asumimos que la cara con nombre "Face1" es donde queremos la pieza del puzzle
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

                            // Configurar el nombre del objeto
                            cube.name = $"Cube_{r}_{c}";

                            // A�adir el componente XRGrabInteractable y el script para detecci�n de eventos de soltar cubo
                            var interactable = cube.AddComponent<XRGrabInteractable>();
                            cube.AddComponent<CubeInteraction>();
                        }
                        else
                        {
                            Debug.LogWarning("El objeto clonado no tiene componentes Renderer en las caras del cubo.");
                            Destroy(cube);
                        }

                        // Instanciar im�n en la posici�n predefinida
                        var magnet = Instantiate(magnetPrefab, magnetPositions[magnetIndex], Quaternion.identity);
                        magnet.tag = "refCube"; // Aseg�rate de que el im�n tenga el tag correcto

                        magnetIndex++; // Mover al siguiente im�n
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
        // Limpiar los imanes actuales antes de generar nuevas posiciones
        ClearCurrentMagnets();

        if (tableCenterObject == null)
        {
            Debug.LogError("No se ha asignado un objeto de referencia para el centro de la mesa.");
            return;
        }

        Vector3 tableCenter = tableCenterObject.transform.position; // Obtener la posici�n del centro de la mesa

        // Calcular el tama�o del puzzle en relaci�n a las filas y columnas
        float puzzleWidth = gridColumns * cubeSize;
        float puzzleHeight = gridRows * cubeSize;

        magnetPositions.Clear();

        // Generar posiciones de imanes
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                // Calcular posici�n en relaci�n con el centro de la mesa
                Vector3 magnetPosition = new Vector3(
                    tableCenter.x - puzzleWidth / 2 + c * cubeSize * 0.8f,
                    tableCenter.y + magnetHeightOffset, // Ajustar altura para que solo la parte roja del im�n sea visible
                    tableCenter.z - puzzleHeight / 2 + r * cubeSize * 0.8f
                );

                magnetPositions.Add(magnetPosition);

                // Crear el nuevo im�n en la posici�n calculada
                if (magnetPrefab != null)
                {
                    GameObject newMagnet = Instantiate(magnetPrefab, magnetPosition, Quaternion.identity);
                    newMagnet.tag = "refCube"; // Aseg�rate de que el nuevo im�n tenga el tag correcto

                    // Ajustar la escala del im�n para que solo se vea la parte roja
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
        selectedImage = image; // Establece la imagen seleccionada
        otherPuzzleMaterials.Clear(); // Limpia la lista para regenerar los materiales
        GenerateMaterialsFromPanelImages(); // Regenera la lista de materiales excluyendo la seleccionada
    }

    void GenerateMaterialsFromPanelImages()
    {
        foreach (Transform child in imagesPanel)
        {
            // Busca el transform "Content" dentro de cada Toggle
            Transform contentTransform = child.Find("Content");

            if (contentTransform != null)
            {
                // Busca el transform "Background" dentro de "Content"
                Transform backgroundTransform = contentTransform.Find("Background");

                if (backgroundTransform != null)
                {
                    // Busca el componente Image dentro de "Background"
                    var img = backgroundTransform.GetComponent<Image>();

                    if (img != null && img.sprite != null && img.sprite != selectedImage.sprite)
                    {
                        // Convertir el Sprite a Texture2D
                        Texture2D texture = SpriteToTexture2D(img.sprite);

                        // Dividir la imagen en partes seg�n las filas y columnas
                        Material[] materials = DivideImageIntoMaterials(texture, rows, columns);

                        // A�adir todos los materiales generados a la lista otherPuzzleMaterials
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
