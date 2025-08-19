using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class PuzzleScoreDisplay : MonoBehaviour
{
    [Header("Panel de Puntuaciones")]
    [Tooltip("Panel que contendr� la informaci�n de puntuaciones")]
    public GameObject scorePanel;

    [Header("Componentes de UI - Tres TextMesh separados")]
    [Tooltip("TextMesh para mostrar los tiempos (ej: '1. 01:30')")]
    public TextMeshProUGUI timesText;

    [Tooltip("TextMesh para mostrar los intentos (ej: '2 intentos')")]
    public TextMeshProUGUI attemptsText;

    [Tooltip("TextMesh para mostrar las fechas (ej: '27/08/2025')")]
    public TextMeshProUGUI datesText;

    [Header("Configuraci�n")]
    [Tooltip("N�mero m�ximo de registros a mostrar")]
    public int maxRecordsToShow = 5;

    private string currentPuzzleId;
    private Sprite currentPuzzleSprite;

    void Start()
    {
        // Asegurarse de que el panel est� oculto al inicio
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra las puntuaciones para un puzzle espec�fico
    /// </summary>
    public void ShowScoresForPuzzle(Sprite puzzleSprite)
    {
        if (puzzleSprite == null)
        {
            Debug.LogWarning("No se proporcion� un sprite de puzzle");
            return;
        }

        currentPuzzleSprite = puzzleSprite;
        currentPuzzleId = puzzleSprite.name;

        // Activar el panel
        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        // Obtener y mostrar las puntuaciones
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Actualiza la visualizaci�n de puntuaciones con tiempo, intentos y fecha separados
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
            // Ordenar por tiempo (mejor tiempo primero)
            scoreEntries = scoreEntries.OrderBy(entry => entry.time).ToList();

            // Preparar los strings para cada columna
            StringBuilder timesBuilder = new StringBuilder();
            StringBuilder attemptsBuilder = new StringBuilder();
            StringBuilder datesBuilder = new StringBuilder();

            int recordsToShow = Mathf.Min(scoreEntries.Count, maxRecordsToShow);

            for (int i = 0; i < recordsToShow; i++)
            {
                ScoreEntry entry = scoreEntries[i];

                // Columna de tiempos
                string timeFormatted = FormatTime(entry.time);
                timesBuilder.AppendLine($"{i + 1}. {timeFormatted}");

                // Columna de intentos
                string attemptText = entry.attempts == 1 ? "1 intento" : $"{entry.attempts} intentos";
                attemptsBuilder.AppendLine(attemptText);

                // Columna de fechas
                datesBuilder.AppendLine(entry.date);
            }

            // Si hay m�s registros que los mostrados
            if (scoreEntries.Count > maxRecordsToShow)
            {
                int remainingRecords = scoreEntries.Count - maxRecordsToShow;
                timesBuilder.AppendLine($"... y {remainingRecords} m�s");
                attemptsBuilder.AppendLine("...");
                datesBuilder.AppendLine("...");
            }

            // Asignar el texto a cada TextMeshProUGUI
            if (timesText != null)
                timesText.text = timesBuilder.ToString();

            if (attemptsText != null)
                attemptsText.text = attemptsBuilder.ToString();

            if (datesText != null)
                datesText.text = datesBuilder.ToString();
        }
    }

    /// <summary>
    /// Muestra mensaje cuando no hay puntuaciones
    /// </summary>
    private void ShowNoScoresMessage()
    {
        if (timesText != null)
            timesText.text = "--:--";

        if (attemptsText != null)
            attemptsText.text = "Sin intentar";

        if (datesText != null)
            datesText.text = "--/--/--";
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
    /// Oculta el panel de puntuaciones
    /// </summary>
    public void HideScorePanel()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
    }

    /// <summary>
    /// M�todo para refrescar las puntuaciones (�til si se completa un puzzle mientras el panel est� abierto)
    /// </summary>
    public void RefreshScores()
    {
        if (!string.IsNullOrEmpty(currentPuzzleId))
        {
            UpdateScoreDisplay();
        }
    }

    /// <summary>
    /// NUEVO: M�todo para validar que todos los TextMeshPro est�n asignados
    /// </summary>
    void Awake()
    {
        if (timesText == null)
            Debug.LogWarning("PuzzleScoreDisplay: timesText no est� asignado en el Inspector");

        if (attemptsText == null)
            Debug.LogWarning("PuzzleScoreDisplay: attemptsText no est� asignado en el Inspector");

        if (datesText == null)
            Debug.LogWarning("PuzzleScoreDisplay: datesText no est� asignado en el Inspector");
    }
}