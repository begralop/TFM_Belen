using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestiona un panel de estadísticas con campos de texto individuales para cada dato.
/// </summary>
public class PuzzleStatsPanel : MonoBehaviour
{
    [Header("Panel Principal")]
    public GameObject statsPanel;

    [Header("Campos de Estadísticas Individuales")]
    [Tooltip("El TextMeshPro para el MEJOR tiempo")]
    public TextMeshProUGUI bestTimeText;

    [Tooltip("El TextMeshPro para el PEOR tiempo")]
    public TextMeshProUGUI worstTimeText;

    [Tooltip("El TextMeshPro para el tiempo PROMEDIO")]
    public TextMeshProUGUI averageTimeText;

    [Tooltip("El TextMeshPro para los intentos en MODO NORMAL")]
    public TextMeshProUGUI normalCompletionsText;

    [Tooltip("El TextMeshPro para los intentos en MODO MEMORIA")]
    public TextMeshProUGUI memoryCompletionsText;

    [Tooltip("Objeto que se muestra cuando no hay datos.")]
    public GameObject noDataMessageObject;

    private List<ScoreEntry> allScores;

    void Start()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el panel y actualiza los valores de las estadísticas.
    /// </summary>
    public void ShowStatsForPuzzle(string puzzleId, Sprite puzzleSprite)
    {
        if (statsPanel == null) return;

        string currentUser = UserManager.GetCurrentUser();
        allScores = UserManager.GetScoreEntries(currentUser, puzzleId);

        statsPanel.SetActive(true);
        UpdateStatsDisplay();
    }

    /// <summary>
    /// Calcula y muestra los valores en los campos de texto individuales.
    /// </summary>
    private void UpdateStatsDisplay()
    {
        bool hasData = allScores != null && allScores.Any();

        if (noDataMessageObject != null)
        {
            noDataMessageObject.SetActive(!hasData);
        }

        // Activa o desactiva todos los campos de texto según si hay datos
        bestTimeText.gameObject.SetActive(hasData);
        worstTimeText.gameObject.SetActive(hasData);
        averageTimeText.gameObject.SetActive(hasData);
        normalCompletionsText.gameObject.SetActive(hasData);
        memoryCompletionsText.gameObject.SetActive(hasData);

        if (!hasData)
        {
            return;
        }

        // --- Cálculos ---
        float bestTime = allScores.Min(s => s.time);
        float worstTime = allScores.Max(s => s.time);
        float avgTime = allScores.Average(s => s.time);
        int memoryCompletions = allScores.Count(s => s.memoryModeUsed);
        int normalCompletions = allScores.Count(s => !s.memoryModeUsed);

        // --- Actualización de la UI (campo por campo) ---
        if (bestTimeText != null)
            bestTimeText.text = FormatTime(bestTime);

        if (worstTimeText != null)
            worstTimeText.text = FormatTime(worstTime);

        if (averageTimeText != null)
            averageTimeText.text = FormatTime(avgTime);

        if (normalCompletionsText != null)
            normalCompletionsText.text = $"{normalCompletions} veces";

        if (memoryCompletionsText != null)
            memoryCompletionsText.text = $"{memoryCompletions} veces";
    }

    /// <summary>
    /// Oculta el panel de estadísticas. Es llamado por PuzzleScoreDisplay.
    /// </summary>
    public void ClosePanel()
    {
        if (statsPanel != null && statsPanel.activeSelf)
        {
            statsPanel.SetActive(false);
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}