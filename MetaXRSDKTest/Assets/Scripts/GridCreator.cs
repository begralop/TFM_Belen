using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCreator : MonoBehaviour
{
    public int rows;
    public int columns;
    public GameObject gridCellPrefab;
    private GameObject[,] gameGrid;
    private const int PANEL_WIDTH = 800;
    private const int PANEL_HEIGHT = 500;
    private const int IMAGE_WIDTH = 725;
    private const int IMAGE_HEIGHT = 725;
    public UnityEngine.UI.Slider columnsSlider, rowsSlider;

    private void createGrid()
    {
        var cell_x_size = PANEL_WIDTH / columns;
        var cell_y_size = PANEL_HEIGHT / rows;
        var x_scale = (float)cell_x_size / IMAGE_WIDTH;
        var y_scale = (float)cell_y_size / IMAGE_HEIGHT;
        var x_offset = (PANEL_WIDTH - cell_x_size) / 2;
        var y_offset = (PANEL_HEIGHT - cell_y_size) / 2;

        gameGrid = new GameObject[columns, rows];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                gameGrid[x, y] = Instantiate(gridCellPrefab, new Vector3(-x_offset + x * cell_x_size, -y_offset + y * cell_y_size), Quaternion.identity);
                gameGrid[x, y].transform.parent = transform;
                gameGrid[x, y].transform.localPosition = new Vector3(-x_offset + x * cell_x_size, -y_offset + y * cell_y_size);
                gameGrid[x, y].transform.localScale = new Vector3(x_scale, y_scale, 1f);
            }
    }

    public void OnColumnsSliderValueChange()
    {
        changeSize(rows, (int)columnsSlider.value);
    }

    public void OnRowsSliderValueChange()
    {
        changeSize((int)rowsSlider.value, columns);
    }

    void Start()
    {
        columnsSlider.onValueChanged.AddListener(delegate { OnColumnsSliderValueChange(); });
        rowsSlider.onValueChanged.AddListener(delegate { OnRowsSliderValueChange(); });
        createGrid();
    }

    void Update()
    {

    }

    public void changeSize(int newRows, int newColumns)
    {
        // Destruir el grid anterior
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                Destroy(gameGrid[x, y]);

        rows = newRows;
        columns = newColumns;
        createGrid();

        // NUEVA FUNCIONALIDAD: Notificar al sistema de memoria sobre el cambio
        MemoryModeSystem memorySystem = FindObjectOfType<MemoryModeSystem>();
        if (memorySystem != null && memorySystem.IsMemoryModeEnabled())
        {
            memorySystem.OnGridSizeChanged(newRows, newColumns);
        }

        // También notificar al GameGenerator si existe
        GameGenerator gameGen = FindObjectOfType<GameGenerator>();
        if (gameGen != null)
        {
            gameGen.OnGridSizeChanged(newRows, newColumns);
        }
    }
}