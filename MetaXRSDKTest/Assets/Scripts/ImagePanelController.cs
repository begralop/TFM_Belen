using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelController : MonoBehaviour
{
    public GameGenerator gameGenerator; // Referencia al script GameGenerator
    // Start is called before the first frame update
    void Start()
    {
        // Encuentra automáticamente el GameGenerator si no está asignado
        if (gameGenerator == null)
        {
            gameGenerator = FindObjectOfType<GameGenerator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SelectImage(bool active)
    {
        if (active) { 
            Image toggleButtonImage = this.gameObject.GetComponentInChildren<Image>();
            GameObject curvedBackground = GameObject.FindGameObjectWithTag("curvedBackground");
            curvedBackground.GetComponent<Image>().sprite = toggleButtonImage.sprite;

            // Actualiza la imagen seleccionada en el GameGenerator
            gameGenerator.OnImageSelected(toggleButtonImage);
        }
    }
}
