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

    [Header("Componentes de UI")]
    public Image puzzleThumbnail;
    public TextMeshProUGUI puzzleTitleText;
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI allTimesText;
    public TextMeshProUGUI attemptsCountText;

    [Header("Configuración")]
    [Tooltip("Número máximo de tiempos a mostrar")]
    public int maxTimesToShow = 5;

    private string currentPuzzleId;
    private Sprite currentPuzzleSprite;

    void Start()
    {
        // Asegurarse de que el panel esté oculto al inicio
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra las puntuaciones para un puzzle específico
    /// </summary>
    public void ShowScoresForPuzzle(Sprite puzzleSprite)
    {
        if (puzzleSprite == null)
        {
            Debug.LogWarning("No se proporcionó un sprite de puzzle");
            return;
        }

        currentPuzzleSprite = puzzleSprite;
        currentPuzzleId = puzzleSprite.name;

        // Activar el panel
        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        // Actualizar la miniatura y título
        if (puzzleThumbnail != null)
        {
            puzzleThumbnail.sprite = puzzleSprite;
        }

        if (puzzleTitleText != null)
        {
            puzzleTitleText.text = FormatPuzzleName(currentPuzzleId);
        }

        // Obtener y mostrar las puntuaciones
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Actualiza la visualización de puntuaciones
    /// </summary>
    private void UpdateScoreDisplay()
    {
        string currentUser = UserManager.GetCurrentUser();
        List<float> scores = UserManager.GetScores(currentUser, currentPuzzleId);

        if (scores == null || scores.Count == 0)
        {
            // No hay puntuaciones registradas
            if (bestTimeText != null)
            {
                bestTimeText.text = "--:--";
            }

            if (allTimesText != null)
            {
                allTimesText.text = "Aún no has completado este puzzle";
            }

            if (attemptsCountText != null)
            {
                attemptsCountText.text = "0";
            }
        }
        else
        {
            // Ordenar tiempos de menor a mayor
            scores.Sort();

            // Mostrar mejor tiempo
            if (bestTimeText != null)
            {
                string bestTime = FormatTime(scores[0]);
                bestTimeText.text = bestTime;
            }

            // Mostrar número de intentos
            if (attemptsCountText != null)
            {
                attemptsCountText.text = $"{scores.Count} intentos";
            }

            // Mostrar lista de mejores tiempos
            if (allTimesText != null)
            {
                StringBuilder sb = new StringBuilder();

                int timesToShow = Mathf.Min(scores.Count, maxTimesToShow);
                for (int i = 0; i < timesToShow; i++)
                {
                    string time = FormatTime(scores[i]);
                    // LÍNEA CORREGIDA - Agregar cada tiempo a la lista
                    sb.AppendLine($"{i + 1}. {time}");
                }

                // Si hay más tiempos que los mostrados
                if (scores.Count > maxTimesToShow)
                {
                    sb.AppendLine($"... y {scores.Count - maxTimesToShow} tiempo(s) más");
                }

                // Mostrar tiempo promedio
                float averageTime = scores.Average();
                sb.AppendLine($"\n<b>Tiempo promedio:</b> {FormatTime(averageTime)}");

                allTimesText.text = sb.ToString();
            }
        }
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
    /// Formatea el nombre del puzzle para mostrarlo
    /// </summary>
    private string FormatPuzzleName(string rawName)
    {
        // Remover guiones bajos y capitalizar
        string formatted = rawName.Replace("_", " ");

        // Capitalizar primera letra de cada palabra
        string[] words = formatted.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }

        return string.Join(" ", words);
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
    /// Método para refrescar las puntuaciones (útil si se completa un puzzle mientras el panel está abierto)
    /// </summary>
    public void RefreshScores()
    {
        if (!string.IsNullOrEmpty(currentPuzzleId))
        {
            UpdateScoreDisplay();
        }
    }
}