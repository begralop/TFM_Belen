using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class ScoreDisplayManager : MonoBehaviour
{
    [Header("Componentes de UI")]
    public Image puzzleThumbnail;
    public TextMeshProUGUI puzzleTitleText;
    public TextMeshProUGUI scoresText;

    public void DisplayScoresForPuzzle(Sprite puzzleSprite)
    {
        if (puzzleSprite == null) return;

        // 1. Actualizar la UI con la info del puzle
        puzzleThumbnail.sprite = puzzleSprite;
        puzzleTitleText.text = puzzleSprite.name;

        // 2. Obtener el usuario y las puntuaciones
        string currentUser = UserManager.GetCurrentUser();
        List<float> scores = UserManager.GetScores(currentUser, puzzleSprite.name);

        // 3. Construir y mostrar el texto de las puntuaciones
        if (scores.Count > 0)
        {
            scores.Sort(); // Ordenar tiempos de mejor a peor
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>Tus Mejores Tiempos:</b>");

            for (int i = 0; i < Mathf.Min(scores.Count, 5); i++) // Mostrar hasta 5 mejores tiempos
            {
                int minutes = Mathf.FloorToInt(scores[i] / 60f);
                int seconds = Mathf.FloorToInt(scores[i] % 60f);
                sb.AppendLine($"{i + 1}. {minutes:00}:{seconds:00}");
            }
            scoresText.text = sb.ToString();
        }
        else
        {
            scoresText.text = "Aún no has completado este puzle.";
        }
    }
}