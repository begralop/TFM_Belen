using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeInteraction : MonoBehaviour
{
    private GameGenerator gameGenerator;
    public static int cubesPlacedCorrectly = 0; // Contador de cubos colocados correctamente
    private static int totalCubes; // Número total de cubos a colocar

    private bool isPlaced = false; // Variable para asegurarse de que solo se cuente una vez

    void Start()
    {
        // Buscar y asignar el componente GameGenerator
        gameGenerator = FindObjectOfType<GameGenerator>();

        if (gameGenerator != null)
        {
            totalCubes = gameGenerator.rows * gameGenerator.columns;
            cubesPlacedCorrectly = 0;
        }
        else
        {
            Debug.LogError("No se encontró el componente GameGenerator.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isPlaced && other.CompareTag("refCube"))
        {
            Debug.Log($"Cubo {gameObject.name} colocado correctamente en {other.gameObject.name}");
            // Incrementar el contador de cubos colocados correctamente
            cubesPlacedCorrectly++;
            isPlaced = true; // Marcar este cubo como colocado

            // Solo verificar la completitud del puzzle si todos los cubos han sido colocados
            if (cubesPlacedCorrectly == totalCubes && gameGenerator != null)
            {
                Debug.Log("Puzzle completado. Verificando...");
                gameGenerator.CheckPuzzleCompletion();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isPlaced && other.CompareTag("refCube"))
        {
            // Decrementar el contador si el cubo es removido
            cubesPlacedCorrectly--;
            isPlaced = false;
        }
    }
}
