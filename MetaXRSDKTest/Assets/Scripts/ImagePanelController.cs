using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelController : MonoBehaviour
{
    public GameGenerator gameGenerator; // Referencia al script GameGenerator
    [Header("Sistema de Puntuaciones")]
    [Tooltip("Referencia al componente que muestra las puntuaciones")]
    public PuzzleScoreDisplay scoreDisplay;

    [Header("Configuración")]
    [Tooltip("Mostrar puntuaciones automáticamente al seleccionar")]
    public bool autoShowScores = true;

    [Tooltip("Retraso antes de mostrar puntuaciones (segundos)")]
    public float scoreDisplayDelay = 0.5f;

    private Coroutine scoreDisplayCoroutine;

    void Start()
    {
        // Encuentra automáticamente el GameGenerator si no está asignado
        if (gameGenerator == null)
        {
            gameGenerator = FindObjectOfType<GameGenerator>();
        }

        // Encuentra automáticamente el PuzzleScoreDisplay si no está asignado
        if (scoreDisplay == null)
        {
            scoreDisplay = FindObjectOfType<PuzzleScoreDisplay>();
        }
    }

    void Update()
    {

    }

    public void SelectImage(bool active)
    {
        if (active)
        {
            Image toggleButtonImage = this.gameObject.GetComponentInChildren<Image>();
            GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");

            if (curvedBackground != null)
            {
                curvedBackground.GetComponent<Image>().sprite = toggleButtonImage.sprite;
            }

            // Actualiza la imagen seleccionada en el GameGenerator
            if (gameGenerator != null)
            {
                gameGenerator.OnImageSelected(toggleButtonImage);
            }

            // NUEVO: Mostrar las puntuaciones del puzzle seleccionado
            if (autoShowScores && scoreDisplay != null && toggleButtonImage.sprite != null)
            {
                // Cancelar cualquier corrutina anterior
                if (scoreDisplayCoroutine != null)
                {
                    StopCoroutine(scoreDisplayCoroutine);
                }

                // Iniciar nueva corrutina para mostrar puntuaciones
                scoreDisplayCoroutine = StartCoroutine(ShowScoresWithDelay(toggleButtonImage.sprite));
            }
        }
        else
        {
            // NUEVO: Ocultar puntuaciones cuando se deselecciona
            if (scoreDisplay != null)
            {
                // Cancelar corrutina si está en proceso
                if (scoreDisplayCoroutine != null)
                {
                    StopCoroutine(scoreDisplayCoroutine);
                    scoreDisplayCoroutine = null;
                }

                scoreDisplay.HideScorePanel();
            }
        }
    }

    /// <summary>
    /// Muestra las puntuaciones con un pequeño retraso
    /// </summary>
    private IEnumerator ShowScoresWithDelay(Sprite puzzleSprite)
    {
        yield return new WaitForSeconds(scoreDisplayDelay);

        if (scoreDisplay != null)
        {
            scoreDisplay.ShowScoresForPuzzle(puzzleSprite);
        }

        scoreDisplayCoroutine = null;
    }

    /// <summary>
    /// Método público para mostrar puntuaciones manualmente
    /// </summary>
    public void ShowScoresForCurrentPuzzle()
    {
        Image toggleButtonImage = this.gameObject.GetComponentInChildren<Image>();

        if (scoreDisplay != null && toggleButtonImage != null && toggleButtonImage.sprite != null)
        {
            scoreDisplay.ShowScoresForPuzzle(toggleButtonImage.sprite);
        }
    }
}
