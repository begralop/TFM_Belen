using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CubeInteraction : MonoBehaviour
{
    private GameGenerator gameGenerator;
    private HintSystem hintSystem; // Referencia al sistema de pistas
    private bool isPlaced = false; // Variable para asegurarse de que solo se cuente una vez
    private XRGrabInteractable grabInteractable; // Referencia al componente de agarre

    void Start()
    {
        // Buscar y asignar el componente GameGenerator
        gameGenerator = FindObjectOfType<GameGenerator>();

        // Buscar y asignar el sistema de pistas
        hintSystem = FindObjectOfType<HintSystem>();
        if (hintSystem == null)
        {
            Debug.LogWarning($"No se encontró HintSystem para el cubo {gameObject.name}");
        }

        // Obtener el componente XRGrabInteractable
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Suscribirse a los eventos de agarre
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            Debug.Log($"Eventos de agarre configurados para {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No se encontró XRGrabInteractable en {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse de los eventos para evitar errores
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    /// <summary>
    /// Se llama cuando se agarra el cubo
    /// </summary>
    void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"CUBO AGARRADO: {gameObject.name}");

        // Activar pista si el sistema está habilitado
        if (hintSystem != null)
        {
            if (hintSystem.AreHintsEnabled())
            {
                Debug.Log($"Pistas activadas - Mostrando pista para {gameObject.name}");
                hintSystem.ShowHintForPickedCube(gameObject);
            }
            else
            {
                Debug.Log("Pistas desactivadas - No se muestra pista");
            }
        }
        else
        {
            Debug.LogError("HintSystem es null - No se puede mostrar pista");
        }
    }

    /// <summary>
    /// Se llama cuando se suelta el cubo
    /// </summary>
    void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log($"Cubo {gameObject.name} fue soltado");
        // Aquí podrías agregar lógica adicional si es necesario
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