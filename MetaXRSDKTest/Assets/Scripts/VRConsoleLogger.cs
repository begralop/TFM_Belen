using UnityEngine;
using TMPro;
using System.Text;

public class VRConsoleLogger : MonoBehaviour
{
    [Header("Componente de UI")]
    [Tooltip("Arrastra aqu� el objeto de texto TextMeshPro que creaste.")]
    public TextMeshProUGUI consoleText;

    private StringBuilder logStringBuilder = new StringBuilder();
    private int maxLines = 85; // N�mero m�ximo de l�neas a mostrar

    void OnEnable()
    {
        // Suscribirse al evento de recepci�n de logs de Unity
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        // Darse de baja para evitar errores
        Application.logMessageReceived -= HandleLog;
    }

    // Este m�todo se ejecuta cada vez que se llama a Debug.Log, Debug.LogWarning, etc.
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // A�adir color seg�n el tipo de mensaje
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                logStringBuilder.AppendLine($"<color=red>{logString}</color>");
                break;
            case LogType.Warning:
                logStringBuilder.AppendLine($"<color=yellow>{logString}</color>");
                break;
            default:
                logStringBuilder.AppendLine(logString);
                break;
        }

        // Limitar el n�mero de l�neas para no sobrecargar
        var lines = logStringBuilder.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            logStringBuilder.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    logStringBuilder.AppendLine(lines[i]);
                }
            }
        }

        // Actualizar el texto en la UI
        if (consoleText != null)
        {
            consoleText.text = logStringBuilder.ToString();
        }
    }
}