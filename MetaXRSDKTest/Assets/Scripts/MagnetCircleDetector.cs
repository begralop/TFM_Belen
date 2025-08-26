using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetCircleDetector : MonoBehaviour
{
    private HintSystem hintSystem;
    private GameObject currentCube;
    private bool cubeIsInside = false;

    public void SetHintSystem(HintSystem system)
    {
        hintSystem = system;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("cube"))
        {
            currentCube = other.gameObject;
            cubeIsInside = true;

            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[C�RCULO IM�N] Cubo entr�: {currentCube.name}");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("cube") && currentCube == other.gameObject)
        {
            // Verificar si el cubo fue soltado
            CheckIfCubeWasDropped();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("cube") && other.gameObject == currentCube)
        {
            cubeIsInside = false;
            currentCube = null;

            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[C�RCULO IM�N] Cubo sali�");
            }
        }
    }

    void CheckIfCubeWasDropped()
    {
        if (currentCube == null || hintSystem == null || !cubeIsInside) return;
        var cubeInteraction = currentCube.GetComponent<CubeInteraction>();
        // Verificar si el cubo no est� siendo agarrado

        if (cubeInteraction != null && !cubeInteraction.IsGrabbed())
        {
            // Si las pistas est�n activadas, mostrar el im�n verde
            if (hintSystem.AreHintsEnabled())
            {
                string[] parts = currentCube.name.Split('_');
                if (parts.Length >= 3)
                {
                    int row, col;
                    if (int.TryParse(parts[1], out row) && int.TryParse(parts[2], out col))
                    {
                        hintSystem.UpdateDebugInfo($"[C�RCULO IM�N] Activando im�n verde para {currentCube.name}");
                        hintSystem.ShowGreenMagnetForCube(row, col);

                        // Quitar el im�n verde despu�s de 5 segundos
                        StartCoroutine(RestoreMagnetAfterDelay(5f));

                        // Reset para evitar m�ltiples activaciones
                        cubeIsInside = false;
                    }
                }
            }
        }
    }

    IEnumerator RestoreMagnetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintSystem != null)
        {
            hintSystem.RestoreMagnetColors();
            hintSystem.UpdateDebugInfo("[C�RCULO IM�N] Im�n restaurado");
        }
    }
}