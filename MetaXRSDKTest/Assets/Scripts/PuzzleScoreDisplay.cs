using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class PuzzleScoreDisplay : MonoBehaviour
{
    [Header("Panel de Puntuaciones")]
    [Tooltip("Panel que contendrá la información de puntuaciones")]
    public GameObject scorePanel;

    [Header("Contenedores internos")]
    [Tooltip("GameObject contenedor de los textos de puntuaciones")]
    public GameObject scoresContainer; // Este contendrá timesText, attemptsText, cubesText, datesText

    [Header("Componentes de UI - Cuatro TextMesh separados")]
    [Tooltip("TextMesh para mostrar los tiempos (ej: '1. 01:30')")]
    public TextMeshProUGUI timesText;

    [Tooltip("TextMesh para mostrar los intentos (ej: '2 intentos')")]
    public TextMeshProUGUI attemptsText;

    [Tooltip("TextMesh para mostrar el número de cubos (ej: '12 cubos')")]
    public TextMeshProUGUI cubesText;

    [Tooltip("TextMesh para mostrar las fechas (ej: '27/08/2025')")]
    public TextMeshProUGUI datesText;


    // --- AÑADIDO: Campos para el sistema de estadísticas ---
    [Header("=== Sistema de Estadísticas ===")]
    [Tooltip("Botón para alternar entre puntuaciones y estadísticas")]
    public Button viewStatsButton;
    [Tooltip("Texto del botón que cambiará")]
    public TextMeshProUGUI statsButtonText;
    [Tooltip("Referencia al panel de estadísticas que se mostrará")]
    public PuzzleStatsPanel statsPanel;


    [Header("Configuración")]
    [Tooltip("Número máximo de registros a mostrar")]
    public int maxRecordsToShow = 5;

    private string currentPuzzleId;
    private Sprite currentPuzzleSprite;
    private bool isShowingStats = false; // Estado para saber qué panel se muestra

    void Awake()
    {
        // Validar que todos los componentes estén asignados
        if (timesText == null) Debug.LogWarning("PuzzleScoreDisplay: timesText no está asignado.");
        if (attemptsText == null) Debug.LogWarning("PuzzleScoreDisplay: attemptsText no está asignado.");
        if (cubesText == null) Debug.LogWarning("PuzzleScoreDisplay: cubesText no está asignado.");
        if (datesText == null) Debug.LogWarning("PuzzleScoreDisplay: datesText no está asignado.");
        if (viewStatsButton == null) Debug.LogWarning("PuzzleScoreDisplay: viewStatsButton no está asignado.");
        if (statsButtonText == null) Debug.LogWarning("PuzzleScoreDisplay: statsButtonText no está asignado.");
        if (statsPanel == null) Debug.LogWarning("PuzzleScoreDisplay: statsPanel no está asignado.");
    }

    void Start()
    {
        // Asegurarse de que el panel esté oculto al inicio
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
        // --- AÑADIDO: Configurar el botón al iniciar ---
        SetupStatsButton();
    }

   
    /// <summary>
    /// Actualiza la visualización de puntuaciones con tiempo, intentos, cubos y fecha separados
    /// </summary>
    private void UpdateScoreDisplay()
    {
        string currentUser = UserManager.GetCurrentUser();
        List<ScoreEntry> scoreEntries = UserManager.GetScoreEntries(currentUser, currentPuzzleId);

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
                timesBuilder.AppendLine($"{i + 1}. {FormatTime(entry.time)}");
                attemptsBuilder.AppendLine(entry.attempts == 1 ? "1" : $"{entry.attempts}");
                cubesBuilder.AppendLine(entry.cubes == 0 ? "N/A" : $"{entry.cubes}");
                datesBuilder.AppendLine(entry.date);
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

    // --- AÑADIDO: Sección completa para la lógica del botón ---
    #region LogicaBotonEstadisticas

    /// <summary>
    /// Configura el listener del botón de estadísticas.
    /// </summary>
    void SetupStatsButton()
    {
        if (viewStatsButton != null)
        {
            viewStatsButton.onClick.RemoveAllListeners();
            viewStatsButton.onClick.AddListener(ToggleView);
            viewStatsButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Alterna la vista entre el panel de puntuaciones y el de estadísticas.
    /// </summary>
    void ToggleView()
    {
        isShowingStats = !isShowingStats;

        if (isShowingStats)
        {
            // Ocultar contenedor de puntuaciones (no el panel principal)
            if (scoresContainer != null)
            {
                scoresContainer.SetActive(false);
            }
            else
            {
                // Si no hay contenedor, ocultar los textos individuales
                if (timesText != null) timesText.gameObject.SetActive(false);
                if (attemptsText != null) attemptsText.gameObject.SetActive(false);
                if (cubesText != null) cubesText.gameObject.SetActive(false);
                if (datesText != null) datesText.gameObject.SetActive(false);
            }

            // Mostrar el panel de estadísticas
            if (statsPanel != null)
            {
                statsPanel.ShowStatsForPuzzle(currentPuzzleId, currentPuzzleSprite);
            }

            if (statsButtonText != null)
            {
                statsButtonText.text = "Ver Puntuaciones";
            }
        }
        else
        {
            // Mostrar contenedor de puntuaciones
            if (scoresContainer != null)
            {
                scoresContainer.SetActive(true);
            }
            else
            {
                // Si no hay contenedor, mostrar los textos individuales
                if (timesText != null) timesText.gameObject.SetActive(true);
                if (attemptsText != null) attemptsText.gameObject.SetActive(true);
                if (cubesText != null) cubesText.gameObject.SetActive(true);
                if (datesText != null) datesText.gameObject.SetActive(true);
            }

            // Ocultar el panel de estadísticas
            if (statsPanel != null)
            {
                statsPanel.ClosePanel();
            }

            // Actualizar los datos de puntuaciones
            UpdateScoreDisplay();

            if (statsButtonText != null)
            {
                statsButtonText.text = "Ver Estadísticas";
            }
        }
    }

    // Y actualiza ShowScoresForPuzzle:

    public void ShowScoresForPuzzle(Sprite puzzleSprite)
    {
        if (puzzleSprite == null)
        {
            Debug.LogWarning("No se proporcionó un sprite de puzzle");
            return;
        }

        currentPuzzleSprite = puzzleSprite;
        currentPuzzleId = puzzleSprite.name;

        // Resetear la vista al estado inicial (puntuaciones)
        isShowingStats = false;

        // Mostrar el panel principal ya que hay un puzzle seleccionado
        if (scorePanel != null) scorePanel.SetActive(true);

        // Asegurarse de que el contenedor de puntuaciones esté visible
        if (scoresContainer != null)
        {
            scoresContainer.SetActive(true);
        }
        else
        {
            // Si no hay contenedor, mostrar los textos individuales
            if (timesText != null) timesText.gameObject.SetActive(true);
            if (attemptsText != null) attemptsText.gameObject.SetActive(true);
            if (cubesText != null) cubesText.gameObject.SetActive(true);
            if (datesText != null) datesText.gameObject.SetActive(true);
        }

        // Asegurarse de que el panel de stats esté cerrado
        if (statsPanel != null) statsPanel.ClosePanel();

        // Obtener y mostrar las puntuaciones y actualizar el botón
        UpdateScoreDisplay();
        UpdateStatsButtonText();
    }
    /// <summary>
    /// Actualiza la visibilidad y el texto del botón de estadísticas.
    /// </summary>
    void UpdateStatsButtonText()
    {
        if (viewStatsButton == null) return;

        string currentUser = UserManager.GetCurrentUser();
        var scoreEntries = UserManager.GetScoreEntries(currentUser, currentPuzzleId);
        bool hasData = scoreEntries != null && scoreEntries.Count > 0;

        viewStatsButton.gameObject.SetActive(hasData);

        if (hasData && statsButtonText != null)
        {
            statsButtonText.text = "Ver Estadísticas";
        }
    }

    #endregion

    /// <summary>
    /// Muestra mensaje cuando no hay puntuaciones
    /// </summary>
    private void ShowNoScoresMessage()
    {
        if (timesText != null) timesText.text = "Sin registros";
        if (attemptsText != null) attemptsText.text = "-";
        if (cubesText != null) cubesText.text = "-";
        if (datesText != null) datesText.text = "--/--/--";
    }

    /// <summary>
    /// Formatea el tiempo en minutos:segundos
    /// </summary>
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Oculta el panel de puntuaciones y el de estadísticas.
    /// </summary>
    public void HideScorePanel()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
        // --- AÑADIDO: Ocultar también el panel de estadísticas ---
        if (statsPanel != null)
        {
            statsPanel.ClosePanel();
        }
    }

    /// <summary>
    /// Método para refrescar las puntuaciones
    /// </summary>
    public void RefreshScores()
    {
        if (!string.IsNullOrEmpty(currentPuzzleId))
        {
            UpdateScoreDisplay();
        }
    }
}