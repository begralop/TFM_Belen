using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacesCircleDetector : MonoBehaviour
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
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Cubo entr�: {currentCube.name}");
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
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Cubo sali�");
            }
        }
    }
    
    void CheckIfCubeWasDropped()
    {
        if (currentCube == null || hintSystem == null || !cubeIsInside) return;
        
        // Verificar si el cubo no est� siendo agarrado
        var cubeInteraction = currentCube.GetComponent<CubeInteraction>();
        if (cubeInteraction != null && !cubeInteraction.IsGrabbed())
        {
            // Si las pistas est�n activadas, mostrar las caras grises
            if (hintSystem.AreHintsEnabled())
            {
                hintSystem.UpdateDebugInfo($"[C�RCULO CARAS] Activando caras grises para {currentCube.name}");
                hintSystem.ShowGrayFacesForCube(currentCube);
                
                // Quitar las caras grises despu�s de 5 segundos
                StartCoroutine(RestoreFacesAfterDelay(5f));
                
                // Reset para evitar m�ltiples activaciones
                cubeIsInside = false;
            }
        }
    }
    
    IEnumerator RestoreFacesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintSystem != null)
        {
            hintSystem.RestoreGrayFaces();
            hintSystem.UpdateDebugInfo("[C�RCULO CARAS] Caras restauradas");
        }
    }
}