using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacesCircleDetector : MonoBehaviour
{
    private HintSystem hintSystem;
    private GameObject currentCube;
    private bool cubeIsInside = false;
    private bool effectActivated = false; // Para evitar múltiples activaciones

    public void SetHintSystem(HintSystem system)
    {
        hintSystem = system;
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si es un cubo o si tiene un cubo como padre
        GameObject cube = FindCubeInHierarchy(other.gameObject);

        if (cube != null)
        {
            currentCube = cube;
            cubeIsInside = true;
            effectActivated = false; // Reset para nueva entrada

            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] Cubo entró: {currentCube.name}");
            }

            // Comenzar a verificar inmediatamente
            StartCoroutine(CheckCubeStatus());
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Ya no necesitamos esto, lo manejamos con la corrutina
    }

    void OnTriggerExit(Collider other)
    {
        GameObject cube = FindCubeInHierarchy(other.gameObject);

        if (cube != null && cube == currentCube)
        {
            // Detener la corrutina de verificación
            StopAllCoroutines();

            // Si el efecto estaba activo, restaurarlo al salir
            if (effectActivated && hintSystem != null)
            {
                hintSystem.RestoreGrayFaces();
                hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] Cubo salió - caras restauradas");
            }

            cubeIsInside = false;
            effectActivated = false;
            currentCube = null;
        }
    }

    IEnumerator CheckCubeStatus()
    {
        while (cubeIsInside && currentCube != null)
        {
            if (!effectActivated)
            {
                CheckIfCubeWasDropped();
            }

            // Verificar cada pocos frames
            yield return new WaitForSeconds(0.1f);
        }
    }

    void CheckIfCubeWasDropped()
    {
        if (currentCube == null || hintSystem == null || !cubeIsInside || effectActivated) return;

        // Buscar CubeInteraction en el cubo o sus componentes
        var cubeInteraction = currentCube.GetComponent<CubeInteraction>();

        if (cubeInteraction == null)
        {
            cubeInteraction = currentCube.GetComponentInChildren<CubeInteraction>();
        }

        if (cubeInteraction != null)
        {
            bool isGrabbed = cubeInteraction.IsGrabbed();

            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] Estado del cubo {currentCube.name}: Agarrado={isGrabbed}");
            }

            // Si el cubo NO está siendo agarrado, activar el efecto
            if (!isGrabbed)
            {
                // Si las pistas están activadas, mostrar las caras grises
                if (hintSystem.AreHintsEnabled())
                {
                    hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] ACTIVANDO caras grises para {currentCube.name}");
                    hintSystem.ShowGrayFacesForCube(currentCube);

                    // Marcar como activado para evitar múltiples activaciones
                    effectActivated = true;
                }
                else
                {
                    hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] Pistas desactivadas - no se muestran caras grises");
                }
            }
        }
        else
        {
            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[CÍRCULO CARAS] ERROR: No se encontró CubeInteraction en {currentCube.name}");
            }
        }
    }

    // ELIMINADO: No necesitamos corrutina ya que el efecto es permanente mientras esté dentro

    /// <summary>
    /// Busca un GameObject con tag "cube" en la jerarquía
    /// </summary>
    GameObject FindCubeInHierarchy(GameObject obj)
    {
        // Primero verificar el objeto mismo
        if (obj.CompareTag("cube"))
            return obj;

        // Luego buscar en los padres
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag("cube"))
                return parent.gameObject;
            parent = parent.parent;
        }

        return null;
    }
}