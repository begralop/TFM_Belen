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

    [Header("Componentes de UI")]
    public TextMeshProUGUI allTimesText;

    [Header("Configuraci�n")]
    [Tooltip("N�mero m�ximo de tiempos a mostrar")]
    public int maxTimesToShow = 5;

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
    /// Actualiza la visualizaci�n de puntuaciones
    /// </summary>
    private void UpdateScoreDisplay()
    {
        string currentUser = UserManager.GetCurrentUser();
        List<float> scores = UserManager.GetScores(currentUser, currentPuzzleId);

        if (scores == null || scores.Count == 0)
        {
            if (allTimesText != null)
            {
                allTimesText.text = "Sin completar";
            }
        }
        else
        {
            // Ordenar tiempos de menor a mayor
            scores.Sort();

            // Mostrar lista de mejores tiempos
            if (allTimesText != null)
            {
                StringBuilder sb = new StringBuilder();

                int timesToShow = Mathf.Min(scores.Count, maxTimesToShow);
                for (int i = 0; i < timesToShow; i++)
                {
                    string time = FormatTime(scores[i]);
                    // L�NEA CORREGIDA - Agregar cada tiempo a la lista
                    sb.AppendLine($"{i + 1}. {time}");
                }

                // Si hay m�s tiempos que los mostrados
                if (scores.Count > maxTimesToShow)
                {
                    sb.AppendLine($"... y {scores.Count - maxTimesToShow} tiempo(s) m�s");
                }

                // Mostrar tiempo promedio
               // float averageTime = scores.Average();
             //   sb.AppendLine($"\n<b>Tiempo promedio:</b> {FormatTime(averageTime)}");

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
}