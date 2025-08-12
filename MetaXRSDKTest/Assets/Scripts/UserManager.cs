using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

// --- ESTRUCTURA DE DATOS MEJORADA ---
public class UserData
{
    // Mantenemos la lista de usuarios y el usuario actual
    public List<string> Usernames = new List<string>();
    public string CurrentUser;

    // AÑADIMOS LA ESTRUCTURA PARA GUARDAR PUNTUACIONES
    // Diccionario<usuario, Diccionario<id_del_puzle, Lista_de_tiempos>>
    public Dictionary<string, Dictionary<string, List<float>>> UserScores = new Dictionary<string, Dictionary<string, List<float>>>();
}

public static class UserManager
{
    private const string FILE_NAME = "user_profiles.json";
    private static UserData localUserData;

    static UserManager()
    {
        LoadData();
    }

    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }

    private static void LoadData()
    {
        string filePath = GetFilePath();
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            localUserData = JsonConvert.DeserializeObject<UserData>(jsonData);
            // Asegurarse de que el diccionario de puntuaciones nunca sea nulo
            if (localUserData.UserScores == null)
            {
                localUserData.UserScores = new Dictionary<string, Dictionary<string, List<float>>>();
            }
        }
        else
        {
            localUserData = new UserData();
        }
    }

    private static void SaveData()
    {
        string jsonData = JsonConvert.SerializeObject(localUserData, Formatting.Indented);
        File.WriteAllText(GetFilePath(), jsonData);
    }

    // --- NUEVOS MÉTODOS PARA GESTIONAR PUNTUACIONES ---

    /// <summary>
    /// Añade una nueva puntuación (tiempo) para un usuario y un puzle específicos.
    /// </summary>
    public static void AddScore(string username, string puzzleId, float time)
    {
        // Si el usuario no existe en el diccionario de puntuaciones, lo creamos
        if (!localUserData.UserScores.ContainsKey(username))
        {
            localUserData.UserScores[username] = new Dictionary<string, List<float>>();
        }

        // Si el puzle no existe para ese usuario, creamos la lista de tiempos
        if (!localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            localUserData.UserScores[username][puzzleId] = new List<float>();
        }

        // Añadimos el nuevo tiempo y guardamos los datos
        localUserData.UserScores[username][puzzleId].Add(time);
        SaveData();
        Debug.Log($"Puntuación guardada: Usuario={username}, Puzle={puzzleId}, Tiempo={time}");
    }

    /// <summary>
    /// Devuelve la lista de tiempos para un usuario y un puzle. Si no hay, devuelve una lista vacía.
    /// </summary>
    public static List<float> GetScores(string username, string puzzleId)
    {
        if (localUserData.UserScores.ContainsKey(username) &&
            localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            return localUserData.UserScores[username][puzzleId];
        }

        // Si no se encuentran puntuaciones, devolver una lista vacía para evitar errores
        return new List<float>();
    }

    // --- MÉTODOS EXISTENTES (sin cambios) ---

    public static void SaveUser(string username)
    {
        if (!localUserData.Usernames.Contains(username))
        {
            localUserData.Usernames.Add(username);
            SaveData();
        }
    }

    public static List<string> GetUsers() => localUserData.Usernames;

    public static void SetCurrentUser(string username)
    {
        localUserData.CurrentUser = username;
        SaveData();
    }

    public static string GetCurrentUser() => string.IsNullOrEmpty(localUserData.CurrentUser) ? "Invitado" : localUserData.CurrentUser;

    public static void DeleteUser(string username)
    {
        if (localUserData.Usernames.Contains(username))
        {
            localUserData.Usernames.Remove(username);
            if (localUserData.UserScores.ContainsKey(username))
            {
                localUserData.UserScores.Remove(username);
            }
            if (localUserData.CurrentUser == username)
            {
                localUserData.CurrentUser = null;
            }
            SaveData();
        }
    }
}