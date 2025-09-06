using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

public class PuzzleScoreDisplay : MonoBehaviour
{
    [Header("Panel de Puntuaciones")]
    [Tooltip("Panel que contendrá la información de puntuaciones")]
    public GameObject scorePanel;

    [Header("Componentes de UI - Cuatro TextMesh separados")]
    public TextMeshProUGUI timesText;
    public TextMeshProUGUI attemptsText;
    public TextMeshProUGUI cubesText;
    public TextMeshProUGUI datesText;

    [Header("=== Sistema de Estadísticas ===")]
    [Tooltip("Botón para alternar entre puntuaciones y estadísticas")]
    public Button viewStatsButton;
    [Tooltip("Texto del botón que cambiará")]
    public TextMeshProUGUI statsButtonText;
    [Tooltip("Panel de estadísticas detalladas")]
    public PuzzleStatsPanel statsPanel;

    [Header("=== Indicadores de Modo ===")]
    public Transform modeIconsContainer;
    public GameObject hintsIconPrefab;
    public GameObject memoryIconPrefab;

    [Header("=== Indicadores Visuales ===")]
    public Color hintsColor = new Color(0.8f, 0.8f, 0.2f);
    public Color memoryColor = new Color(0.2f, 0.8f, 0.2f);
    public Color normalColor = Color.white;

    [Header("Configuración")]
    public int maxRecordsToShow = 5;

    private string currentPuzzleId;
    private Sprite currentPuzzleSprite;
    private List<GameObject> createdIcons = new List<GameObject>();
    private bool isShowingStats = false; // Estado para saber qué panel se muestra

    void Start()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
        SetupStatsButton();
    }

    void SetupStatsButton()
    {
        if (viewStatsButton != null)
        {
            viewStatsButton.onClick.RemoveAllListeners();
            // El listener ahora llama a la nueva función de alternar
            viewStatsButton.onClick.AddListener(ToggleView);
            viewStatsButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Alterna la vista entre el panel de puntuaciones y el de estadísticas.
    /// </summary>
    void ToggleView()
    {
        isShowingStats = !isShowingStats; // Invierte el estado actual

        if (isShowingStats)
        {
            // --- MOSTRAR ESTADÍSTICAS ---
            scorePanel.SetActive(false);
            statsPanel.ShowStatsForPuzzle(currentPuzzleId, currentPuzzleSprite);
            if (statsButtonText != null)
            {
                statsButtonText.text = "Ver Puntuaciones";
            }
        }
        else
        {
            // --- MOSTRAR PUNTUACIONES ---
            scorePanel.SetActive(true);
            statsPanel.ClosePanel(); // Cierra el panel de estadísticas
            UpdateStatsButtonText(); // Restaura el texto original del botón
        }
    }

    /// <summary>
    /// Muestra las puntuaciones para un puzzle, reseteando la vista al estado inicial.
    /// </summary>
    public void ShowScoresForPuzzle(Sprite puzzleSprite)
    {
        if (puzzleSprite == null) return;

        currentPuzzleSprite = puzzleSprite;
        currentPuzzleId = puzzleSprite.name;

        // --- Resetear al estado inicial (vista de puntuaciones) ---
        isShowingStats = false;
        scorePanel.SetActive(true);
        statsPanel.ClosePanel(); // Asegurarse de que el panel de stats esté cerrado

        UpdateScoreDisplay();
        UpdateStatsButtonText(); // Actualiza el botón (visibilidad y texto)
    }

    /// <summary>
    /// Actualiza la visibilidad y el texto del botón de estadísticas.
    /// </summary>
    void UpdateStatsButtonText()
    {
        if (viewStatsButton == null) return;

        string currentUser = UserManager.GetCurrentUser();
        List<ScoreEntry> scoreEntries = UserManager.GetScoreEntries(currentUser, currentPuzzleId);

        bool hasData = scoreEntries != null && scoreEntries.Count > 0;
        viewStatsButton.gameObject.SetActive(hasData);

        if (hasData && statsButtonText != null)
        {
            // Texto cuando se muestran las puntuaciones
            statsButtonText.text = "Ver Estadísticas";
        }
    }

    /// <summary>
    /// Oculta ambos paneles.
    /// </summary>
    public void HideScorePanel()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
        if (statsPanel != null)
        {
            statsPanel.ClosePanel();
        }
        ClearModeIcons();
    }

    /// <summary>
    /// Refresca las puntuaciones y resetea la vista al panel de puntuaciones.
    /// </summary>
    public void RefreshScores()
    {
        if (!string.IsNullOrEmpty(currentPuzzleId))
        {
            // Llamar a ShowScoresForPuzzle resetea la vista correctamente
            ShowScoresForPuzzle(currentPuzzleSprite);
        }
    }

    // --- El resto de métodos (UpdateScoreDisplay, FormatTime, etc.) permanecen igual ---
    // (Asegúrate de mantener el resto de tus funciones que no he incluido aquí para brevedad)
    #region "Métodos sin cambios"
    private void UpdateScoreDisplay()
    {
        string currentUser = UserManager.GetCurrentUser();
        List<ScoreEntry> scoreEntries = UserManager.GetScoreEntries(currentUser, currentPuzzleId);

        ClearModeIcons();

        if (scoreEntries == null || scoreEntries.Count == 0)
        {
            ShowNoScoresMessage();
        }
        else
        {
            scoreEntries = scoreEntries.OrderBy(entry => entry.time).ToList();

            StringBuilder timesBuilder = new StringBuilder();
            StringBuilder attemptsBuilder = new StringBuilder();
            StringBuilder cubesBuilder = new StringBuilder();
            StringBuilder datesBuilder = new StringBuilder();

            int recordsToShow = Mathf.Min(scoreEntries.Count, maxRecordsToShow);

            for (int i = 0; i < recordsToShow; i++)
            {
                ScoreEntry entry = scoreEntries[i];
                string timeFormatted = FormatTime(entry.time);
                string coloredTime = timeFormatted;
                if (entry.memoryModeUsed)
                {
                    coloredTime = $"<color=#{ColorUtility.ToHtmlStringRGB(memoryColor)}>{timeFormatted}</color>";
                }
                else if (entry.hintsUsed)
                {
                    coloredTime = $"<color=#{ColorUtility.ToHtmlStringRGB(hintsColor)}>{timeFormatted}</color>";
                }
                timesBuilder.AppendLine($"{i + 1}. {coloredTime}");
                string attemptText = entry.attempts == 1 ? "1" : $"{entry.attempts}";
                if (entry.attempts == 1)
                {
                    attemptText = $"<color=#FFD700>{attemptText}</color>";
                }
                attemptsBuilder.AppendLine(attemptText);
                string cubeText = (entry.gridRows > 0 && entry.gridColumns > 0) ? $"{entry.gridRows}x{entry.gridColumns}" : (entry.cubes > 0 ? $"{entry.cubes}" : "3x3");
                cubesBuilder.AppendLine(cubeText);
                datesBuilder.AppendLine(entry.date);
                CreateModeIcons(entry, i);
            }

            if (scoreEntries.Count > maxRecordsToShow)
            {
                int remainingRecords = scoreEntries.Count - maxRecordsToShow;
                timesBuilder.AppendLine($"... y {remainingRecords} más");
                attemptsBuilder.AppendLine("...");
                cubesBuilder.AppendLine("...");
                datesBuilder.AppendLine("...");
            }

            if (timesText != null) timesText.text = timesBuilder.ToString();
            if (attemptsText != null) attemptsText.text = attemptsBuilder.ToString();
            if (cubesText != null) cubesText.text = cubesBuilder.ToString();
            if (datesText != null) datesText.text = datesBuilder.ToString();
        }
    }
    void CreateModeIcons(ScoreEntry entry, int index)
    {
        if (modeIconsContainer == null) return;
        float yOffset = index * -30f;
        float xOffset = 0;
        if (entry.hintsUsed && hintsIconPrefab != null)
        {
            GameObject icon = Instantiate(hintsIconPrefab, modeIconsContainer);
            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null) { rect.anchoredPosition = new Vector2(xOffset, yOffset); xOffset += 25; }
            createdIcons.Add(icon);
        }
        if (entry.memoryModeUsed && memoryIconPrefab != null)
        {
            GameObject icon = Instantiate(memoryIconPrefab, modeIconsContainer);
            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null) { rect.anchoredPosition = new Vector2(xOffset, yOffset); }
            createdIcons.Add(icon);
        }
    }
    void ClearModeIcons()
    {
        foreach (GameObject icon in createdIcons) { if (icon != null) Destroy(icon); }
        createdIcons.Clear();
        if (modeIconsContainer != null) { foreach (Transform child in modeIconsContainer) { Destroy(child.gameObject); } }
    }
    private void ShowNoScoresMessage()
    {
        if (timesText != null) timesText.text = "<color=#808080>Sin registros</color>";
        if (attemptsText != null) attemptsText.text = "-";
        if (cubesText != null) cubesText.text = "-";
        if (datesText != null) datesText.text = "--/--/--";
        if (viewStatsButton != null) viewStatsButton.gameObject.SetActive(false);
    }
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion
}