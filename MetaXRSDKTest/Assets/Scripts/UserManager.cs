using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

public class UserData
{
    public List<string> Usernames = new List<string>();
    public string CurrentUser;
}

public static class UserManager
{
    private const string FILE_NAME = "user_profiles.json";
    private static UserData localUserData;

    // --- CAMBIO CLAVE: Constructor Estático ---
    // Este código se ejecuta AUTOMÁTICAMENTE una sola vez, la primera vez que cualquier
    // script intenta usar la clase UserManager. Esto garantiza que los datos SIEMPRE se cargan.
    static UserManager()
    {
        LoadData();
    }

    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }

    // Ahora este método es privado. Nadie más necesita llamarlo.
    private static void LoadData()
    {
        string filePath = GetFilePath();
        Debug.Log($"Cargando datos desde: {filePath}");

        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            localUserData = JsonConvert.DeserializeObject<UserData>(jsonData);
            Debug.Log($"Datos cargados. Se encontraron {localUserData.Usernames.Count} usuarios.");
        }
        else
        {
            Debug.Log("No se encontró archivo de datos. Creando uno nuevo.");
            localUserData = new UserData();
        }
    }

    private static void SaveData()
    {
        string jsonData = JsonConvert.SerializeObject(localUserData, Formatting.Indented);
        File.WriteAllText(GetFilePath(), jsonData);
        Debug.Log("Datos guardados en el archivo local.");
    }

    // --- MÉTODOS PÚBLICOS (Sin cambios, pero ahora más fiables) ---

    public static void SaveUser(string username)
    {
        if (localUserData.Usernames.Contains(username))
        {
            Debug.Log($"El usuario '{username}' ya existe.");
            return;
        }
        localUserData.Usernames.Add(username);
        SaveData();
    }

    public static void DeleteUser(string username)
    {
        if (localUserData.Usernames.Contains(username))
        {
            localUserData.Usernames.Remove(username);
            if (localUserData.CurrentUser == username)
            {
                localUserData.CurrentUser = null;
            }
            SaveData();
            Debug.Log($"Usuario '{username}' eliminado.");
        }
    }

    public static List<string> GetUsers()
    {
        return localUserData.Usernames;
    }

    public static void SetCurrentUser(string username)
    {
        localUserData.CurrentUser = username;
        SaveData();
    }

    public static string GetCurrentUser()
    {
        return string.IsNullOrEmpty(localUserData.CurrentUser) ? "Invitado" : localUserData.CurrentUser;
    }
}
