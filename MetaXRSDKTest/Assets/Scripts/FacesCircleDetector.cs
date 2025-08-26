using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacesCircleDetector : MonoBehaviour
{
    private HintSystem hintSystem;
    private GameObject currentCube;
    private bool cubeIsInside = false;
    private bool effectActivated = false; // Para evitar m�ltiples activaciones

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
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Cubo entr�: {currentCube.name}");
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
            // Detener la corrutina de verificaci�n
            StopAllCoroutines();

            // Si el efecto estaba activo, restaurarlo al salir
            if (effectActivated && hintSystem != null)
            {
                hintSystem.RestoreGrayFaces();
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Cubo sali� - caras restauradas");
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
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Estado del cubo {currentCube.name}: Agarrado={isGrabbed}");
            }

            // Si el cubo NO est� siendo agarrado, activar el efecto
            if (!isGrabbed)
            {
                // Si las pistas est�n activadas, mostrar las caras grises
                if (hintSystem.AreHintsEnabled())
                {
                    hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] ACTIVANDO caras grises para {currentCube.name}");
                    hintSystem.ShowGrayFacesForCube(currentCube);

                    // Marcar como activado para evitar m�ltiples activaciones
                    effectActivated = true;
                }
                else
                {
                    hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Pistas desactivadas - no se muestran caras grises");
                }
            }
        }
        else
        {
            if (hintSystem != null)
            {
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] ERROR: No se encontr� CubeInteraction en {currentCube.name}");
            }
        }
    }

    // ELIMINADO: No necesitamos corrutina ya que el efecto es permanente mientras est� dentro

    /// <summary>
    /// Busca un GameObject con tag "cube" en la jerarqu�a
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