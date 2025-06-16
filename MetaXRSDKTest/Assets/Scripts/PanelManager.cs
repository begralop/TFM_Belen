using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{

    public GameObject successPanel; // Arrastra tu panel de éxito aquí en el Inspector
    public GameObject warningPanel; // Arrastra tu panel de advertencia aquí en el Inspector

    public void ClosePanel()
    {
        if (successPanel.activeSelf)
        {
            successPanel.SetActive(false);
        }
        else if (warningPanel.activeSelf)
        {
            warningPanel.SetActive(false);
        }
    }
}
