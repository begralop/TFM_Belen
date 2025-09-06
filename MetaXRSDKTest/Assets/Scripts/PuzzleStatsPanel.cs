using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Gestiona un panel de estadísticas simplificado para un puzzle específico.
/// Muestra el tiempo máximo, mínimo, promedio y el desglose de partidas por modo de juego.
/// </summary>
public class PuzzleStatsPanel : MonoBehaviour
{
    [Header("=== Panel Principal ===")]
    public GameObject statsPanel;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI puzzleNameText;
    public Button closeButton;

    [Header("=== Estadísticas Simplificadas ===")]
    [Tooltip("Texto para mostrar el mejor tiempo.")]
    public TextMeshProUGUI bestTimeText;
    [Tooltip("Texto para mostrar el peor tiempo.")]
    public TextMeshProUGUI worstTimeText;
    [Tooltip("Texto para mostrar el tiempo promedio.")]
    public TextMeshProUGUI averageTimeText;
    [Tooltip("Texto para las veces completado en modo memoria.")]
    public TextMeshProUGUI memoryCompletionsText;
    [Tooltip("Texto para las veces completado en modo normal.")]
    public TextMeshProUGUI normalCompletionsText;
    [Tooltip("Objeto que se muestra cuando no hay datos.")]
    public GameObject noDataMessageObject;

    [Header("=== Configuración Visual ===")]
    public float animationDuration = 0.3f;

    private List<ScoreEntry> allScores;
    private Coroutine animationCoroutine;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el panel con las estadísticas para un puzzle determinado.
    /// </summary>
    public void ShowStatsForPuzzle(string puzzleId, Sprite puzzleSprite)
    {
        string currentUser = UserManager.GetCurrentUser();
        allScores = UserManager.GetScoreEntries(currentUser, puzzleId);

        if (statsPanel == null) return;

        statsPanel.SetActive(true);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimatePanel(true));

        UpdateSimplifiedStats(puzzleId);
    }

    /// <summary>
    /// Calcula y muestra las estadísticas clave en la UI.
    /// </summary>
    private void UpdateSimplifiedStats(string puzzleId)
    {
        if (puzzleNameText != null)
        {
            puzzleNameText.text = $"Estadísticas de {FormatPuzzleName(puzzleId)}";
        }

        bool hasData = allScores != null && allScores.Any();

        if (noDataMessageObject != null)
        {
            noDataMessageObject.SetActive(!hasData);
        }

        // Ocultar textos de estadísticas si no hay datos
        bestTimeText.gameObject.SetActive(hasData);
        worstTimeText.gameObject.SetActive(hasData);
        averageTimeText.gameObject.SetActive(hasData);
        memoryCompletionsText.gameObject.SetActive(hasData);
        normalCompletionsText.gameObject.SetActive(hasData);

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

        // --- Actualización de la UI ---
        if (bestTimeText != null)
            bestTimeText.text = $"Mejor tiempo:\t{FormatTime(bestTime)}";

        if (worstTimeText != null)
            worstTimeText.text = $"Peor tiempo:\t{FormatTime(worstTime)}";

        if (averageTimeText != null)
            averageTimeText.text = $"Tiempo promedio:\t{FormatTime(avgTime)}";

        if (memoryCompletionsText != null)
            memoryCompletionsText.text = $"Finalizado (Memoria):\t{memoryCompletions} veces";

        if (normalCompletionsText != null)
            normalCompletionsText.text = $"Finalizado (Normal):\t{normalCompletions} veces";
    }

    /// <summary>
    /// Oculta el panel de estadísticas con una animación.
    /// </summary>
    public void ClosePanel()
    {
        if (statsPanel != null && statsPanel.activeSelf)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimatePanel(false));
        }
    }

    /// <summary>
    /// Corrutina para la animación de entrada y salida del panel.
    /// </summary>
    private IEnumerator AnimatePanel(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = startAlpha;
        }

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = endAlpha;
        }

        if (!fadeIn && statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    // === MÉTODOS AUXILIARES ===

    private string FormatPuzzleName(string puzzleId)
    {
        return puzzleId.Replace("_", " ").Replace("-", " ");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}