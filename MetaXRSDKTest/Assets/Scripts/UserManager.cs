using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json; // Asegúrate de tener esta librería en tu proyecto
using System.IO;       // Necesario para leer y escribir archivos

// La clase para guardar los datos sigue igual
public class UserData
{
    public List<string> Usernames = new List<string>();
    public string CurrentUser;
}

public static class UserManager
{
    private const string FILE_NAME = "user_profiles.json"; // El nombre de nuestro archivo de guardado
    private static UserData localUserData = new UserData();

    // Método para obtener la ruta completa y segura del archivo
    private static string GetFilePath()
    {
        // Application.persistentDataPath es una carpeta especial que sobrevive a las actualizaciones
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }

    // Carga los datos desde el archivo local
    public static void LoadData()
    {
        string filePath = GetFilePath();
        Debug.Log($"Cargando datos desde la ruta: {filePath}");

        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            localUserData = JsonConvert.DeserializeObject<UserData>(jsonData);
            Debug.Log($"Datos cargados. Se encontraron {localUserData.Usernames.Count} usuarios.");
        }
        else
        {
            Debug.Log("No se encontró archivo de datos. Se creará uno nuevo al guardar.");
            localUserData = new UserData();
        }
    }

    // Guarda los datos en el archivo local
    private static void SaveData()
    {
        string jsonData = JsonConvert.SerializeObject(localUserData, Formatting.Indented);
        string filePath = GetFilePath();
        File.WriteAllText(filePath, jsonData);
        Debug.Log($"Datos guardados correctamente en: {filePath}");
    }

    // --- MÉTODOS PÚBLICOS (Ahora usan SaveData) ---

    public static void SaveUser(string username)
    {
        if (localUserData.Usernames.Contains(username))
        {
            Debug.Log($"El usuario '{username}' ya existe.");
            return;
        }
        localUserData.Usernames.Add(username);
        SaveData(); // Guarda los cambios en el archivo
    }

    public static List<string> GetUsers()
    {
        return localUserData.Usernames;
    }

    public static void SetCurrentUser(string username)
    {
        localUserData.CurrentUser = username;
        SaveData(); // Guarda los cambios en el archivo
    }

    public static string GetCurrentUser()
    {
        return string.IsNullOrEmpty(localUserData.CurrentUser) ? "Invitado" : localUserData.CurrentUser;
    }
}
