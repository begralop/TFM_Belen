using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class CubeInteraction : MonoBehaviour
{
    private GameGenerator gameGenerator;
    private HintSystem hintSystem;
    private bool isPlaced = false;

    // NUEVO: Usar PhysicsGrabbable en lugar de XRGrabInteractable
    private PhysicsGrabbable physicsGrabbable;
    private IPointable pointable;
    private bool isGrabbed = false;

    void Awake()
    {
        gameGenerator = FindObjectOfType<GameGenerator>();
        hintSystem = FindObjectOfType<HintSystem>();
    }

    public bool IsGrabbed()
    {
        return isGrabbed;
    }

    void Start()
    {
        if (gameGenerator == null)
        {
            gameGenerator = FindObjectOfType<GameGenerator>();
        }

        if (gameGenerator != null)
        {
            UpdateGameDebugInfo($"CubeInteraction START para {gameObject.name}");
        }

        if (hintSystem == null)
        {
            hintSystem = FindObjectOfType<HintSystem>();
        }

        if (hintSystem == null)
        {
            UpdateGameDebugInfo($"ERROR: No HintSystem para {gameObject.name}");
        }
        else
        {
            UpdateGameDebugInfo($"HintSystem OK para {gameObject.name}");
        }

        // NUEVO: Configurar PhysicsGrabbable
        SetupPhysicsGrabbable();
    }

    void SetupPhysicsGrabbable()
    {
        UpdateGameDebugInfo($"Configurando PhysicsGrabbable para {gameObject.name}");

        // Buscar o agregar PhysicsGrabbable
        physicsGrabbable = GetComponent<PhysicsGrabbable>();
        if (physicsGrabbable == null)
        {
            UpdateGameDebugInfo($"PhysicsGrabbable no encontrado, agregando...");
            physicsGrabbable = gameObject.AddComponent<PhysicsGrabbable>();
        }

        // Buscar o agregar Grabbable (requerido por PhysicsGrabbable)
        var grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
        {
            UpdateGameDebugInfo($"Grabbable no encontrado, agregando...");
            grabbable = gameObject.AddComponent<Grabbable>();
        }

        // Asegurarse de que tenga Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            UpdateGameDebugInfo($"Rigidbody no encontrado, agregando...");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Configurar el PhysicsGrabbable con los componentes necesarios
        if (physicsGrabbable != null && grabbable != null && rb != null)
        {
            physicsGrabbable.InjectAllPhysicsGrabbable(grabbable, rb);
            UpdateGameDebugInfo($"PhysicsGrabbable configurado correctamente");
        }

        // Obtener la interfaz IPointable
        pointable = grabbable as IPointable;
        if (pointable != null)
        {
            UpdateGameDebugInfo($"IPointable obtenido, suscribiendo a eventos...");
        }
        else
        {
            UpdateGameDebugInfo($"ERROR: No se pudo obtener IPointable");
        }
    }

    void OnEnable()
    {
        if (pointable != null)
        {
            pointable.WhenPointerEventRaised += HandlePointerEvent;
            UpdateGameDebugInfo($"Eventos de puntero suscritos para {gameObject.name}");
        }
    }

    void OnDisable()
    {
        if (pointable != null)
        {
            pointable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    /// <summary>
    /// Maneja los eventos del sistema de punteros de Oculus
    /// </summary>
    private void HandlePointerEvent(PointerEvent evt)
    {
        UpdateGameDebugInfo($"[POINTER EVENT] {gameObject.name} - Tipo: {evt.Type}");

        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnCubeGrabbed();
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                OnCubeReleased();
                break;

            case PointerEventType.Hover:
                UpdateGameDebugInfo($"[HOVER] {gameObject.name}");
                break;

            case PointerEventType.Unhover:
                UpdateGameDebugInfo($"[UNHOVER] {gameObject.name}");
                break;
        }
    }

    /// <summary>
    /// Se llama cuando se agarra el cubo
    /// </summary>
    void OnCubeGrabbed()
    {
        if (isGrabbed) return; // Evitar duplicados
        isGrabbed = true;
    }

    /// <summary>
    /// Se llama cuando se suelta el cubo
    /// </summary>
    void OnCubeReleased()
    {
        if (!isGrabbed) return; // Evitar duplicados
        isGrabbed = false;
    }

    // Métodos originales para el sistema de colocación
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

    /// <summary>
    /// Actualiza el panel de debug del GameGenerator
    /// </summary>
    void UpdateGameDebugInfo(string message)
    {
        if (gameGenerator != null)
        {
            gameGenerator.UpdateDebugInfo($"[CUBE] {message}");
        }
        else
        {
            Debug.Log($"[CubeInteraction] {message}");
        }
    }

    /// <summary>
    /// Método público para reconectar eventos (llamado desde GameGenerator)
    /// </summary>
    public void ReconnectEvents()
    {
        UpdateGameDebugInfo($"ReconnectEvents llamado para {gameObject.name}");
        SetupPhysicsGrabbable();

        // Re-suscribir a eventos si es necesario
        if (pointable != null)
        {
            pointable.WhenPointerEventRaised -= HandlePointerEvent;
            pointable.WhenPointerEventRaised += HandlePointerEvent;
            UpdateGameDebugInfo($"Eventos reconectados");
        }
    }
}