using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // Necesario para eventos

public class InputFieldEvents : MonoBehaviour
{
    // Arrastra aqu� desde el editor el GameObject que quieres activar/desactivar
    [SerializeField]
    private GameObject keyboardContainer;

    // Esta funci�n se llamar� cuando el InputField sea seleccionado
    public void OnSelect()
    {
        if (keyboardContainer != null)
        {
            keyboardContainer.SetActive(true);
        }
    }

    // Esta funci�n se llamar� cuando el InputField sea deseleccionado
    public void OnDeselect()
    {
        if (keyboardContainer != null)
        {
            keyboardContainer.SetActive(false);
        }
    }
}