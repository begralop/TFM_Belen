using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryCircleDetector : MonoBehaviour
{
    private MemoryModeSystem memorySystem;
    private bool handInside = false;
    private float activationTimer = 0f;
    private float activationDelay = 0.5f; // Medio segundo para activar

    void Start()
    {
        memorySystem = FindObjectOfType<MemoryModeSystem>();
        if (memorySystem == null)
        {
            Debug.LogError("MemoryCircleDetector: No se encontró MemoryModeSystem");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Detectar si es una mano
        if (IsHand(other))
        {
            handInside = true;
            activationTimer = 0f;

            if (memorySystem != null)
            {
                memorySystem.UpdateDebugInfo($"[CÍRCULO MEMORIA] Mano detectada");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (handInside && IsHand(other))
        {
            activationTimer += Time.deltaTime;

            // Si la mano ha estado dentro el tiempo suficiente
            if (activationTimer >= activationDelay)
            {
                if (memorySystem != null && memorySystem.CanActivateFromCircle())
                {
                    memorySystem.ActivateMemoryModeFromCircle();
                    handInside = false; // Reset para evitar activaciones múltiples
                    activationTimer = 0f;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsHand(other))
        {
            handInside = false;
            activationTimer = 0f;

            if (memorySystem != null)
            {
                memorySystem.UpdateDebugInfo($"[CÍRCULO MEMORIA] Mano salió");
            }
        }
    }

    bool IsHand(Collider other)
    {
        // Detectar si el collider pertenece a una mano
        // Oculus SDK usa estos tags/nombres típicamente
        return other.name.Contains("Hand") ||
               other.name.Contains("hand") ||
               other.name.Contains("Palm") ||
               other.tag == "Hand" ||
               other.tag == "Player" ||
               other.GetComponentInParent<OVRHand>() != null ||
               other.GetComponentInParent<OVRGrabber>() != null;
    }
}