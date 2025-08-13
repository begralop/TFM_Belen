using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeInteraction : MonoBehaviour
{
    private GameGenerator gameGenerator;
   

    private bool isPlaced = false; // Variable para asegurarse de que solo se cuente una vez

    void Start()
    {
        // Buscar y asignar el componente GameGenerator
        gameGenerator = FindObjectOfType<GameGenerator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isPlaced && other.CompareTag("refCube"))
        {
            isPlaced = true;
            if (gameGenerator != null)
            {
                gameGenerator.OnCubePlaced();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isPlaced && other.CompareTag("refCube"))
        {
            isPlaced = false;
            if (gameGenerator != null)
            {
                gameGenerator.OnCubeRemoved();
            }
        }
    }
}
