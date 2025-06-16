using UnityEngine;

public class UIPositioning : MonoBehaviour
{
    public Transform userHead;
    public float distanceFromUser = 1.5f; // Distancia del panel al usuario
    //public Vector3 panelOffset = new Vector3(0, 0, 0); // Ajusta esta variable para centrar el panel en la vista del usuario

    void Start()
    {
        // Reposicionar el panel al inicio
       // PositionPanel();
    }

    public void PositionPanel()
    {
        // Colocar el panel a cierta distancia frente al usuario
       // transform.position = userHead.position + userHead.forward * distanceFromUser;
        // Ajustar la posición con el offset si es necesario
       // transform.position += panelOffset;
        // Asegurarse de que el panel mire al usuario
      //  transform.LookAt(userHead);
      //  transform.Rotate(0, 180, 0); // Girar 180 grados para que el panel esté orientado correctamente

       // Debug.Log("Panel Position: " + transform.position + ", Rotation: " + transform.rotation.eulerAngles);
    }

    void Update()
    {
        // Opcional: Actualizar la posición del panel cada frame (si el usuario se mueve)
        // PositionPanel();
    }
}