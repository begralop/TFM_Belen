using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System;

// --- ESTRUCTURA DE DATOS MEJORADA ---
[System.Serializable]
public class ScoreEntry
{
    public float time;
    public int attempts;
    public string date;
    public int cubes; // NUEVO: Número de cubos del puzzle

    public ScoreEntry(float time, int attempts, string date, int cubes)
    {
        this.time = time;
        this.attempts = attempts;
        this.date = date;
        this.cubes = cubes;
    }

    // Constructor de compatibilidad para datos existentes
    public ScoreEntry(float time, int attempts, string date)
    {
        this.time = time;
        this.attempts = attempts;
        this.date = date;
        this.cubes = 0; // Valor por defecto para registros antiguos
    }
}

public class UserData
{
    // Mantenemos la lista de usuarios y el usuario actual
    public List<string> Usernames = new List<string>();
    public string CurrentUser;

    // MODIFICAMOS LA ESTRUCTURA PARA GUARDAR PUNTUACIONES CON INTENTOS Y FECHA
    // Diccionario<usuario, Diccionario<id_del_puzle, Lista_de_ScoreEntry>>
    public Dictionary<string, Dictionary<string, List<ScoreEntry>>> UserScores = new Dictionary<string, Dictionary<string, List<ScoreEntry>>>();
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
            try
            {
                string jsonData = File.ReadAllText(filePath);
                localUserData = JsonConvert.DeserializeObject<UserData>(jsonData);

                // Asegurarse de que el diccionario de puntuaciones nunca sea nulo
                if (localUserData.UserScores == null)
                {
                    localUserData.UserScores = new Dictionary<string, Dictionary<string, List<ScoreEntry>>>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al cargar datos de usuario: {e.Message}. Creando nuevos datos.");
                localUserData = new UserData();
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

    // --- NUEVOS MÉTODOS PARA GESTIONAR PUNTUACIONES CON INTENTOS ---

    /// <summary>
    /// Añade una nueva puntuación (tiempo, intentos, fecha y número de cubos) para un usuario y un puzle específicos.
    /// </summary>
    public static void AddScore(string username, string puzzleId, float time, int attempts, int cubes)
    {
        // Si el usuario no existe en el diccionario de puntuaciones, lo creamos
        if (!localUserData.UserScores.ContainsKey(username))
        {
            localUserData.UserScores[username] = new Dictionary<string, List<ScoreEntry>>();
        }

        // Si el puzle no existe para ese usuario, creamos la lista de puntuaciones
        if (!localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            localUserData.UserScores[username][puzzleId] = new List<ScoreEntry>();
        }

        // Crear la fecha actual en formato dd/MM/yyyy
        string currentDate = System.DateTime.Now.ToString("dd/MM/yyyy");

        // Añadimos la nueva puntuación y guardamos los datos
        ScoreEntry newScore = new ScoreEntry(time, attempts, currentDate, cubes);
        localUserData.UserScores[username][puzzleId].Add(newScore);
        SaveData();
        Debug.Log($"Puntuación guardada: Usuario={username}, Puzle={puzzleId}, Tiempo={time}, Intentos={attempts}, Cubos={cubes}, Fecha={currentDate}");
    }

    /// <summary>
    /// Versión de compatibilidad del método anterior (sin cubos)
    /// </summary>
    public static void AddScore(string username, string puzzleId, float time, int attempts)
    {
        AddScore(username, puzzleId, time, attempts, 0); // Por defecto 0 cubos para compatibilidad
    }

    /// <summary>
    /// Versión de compatibilidad del método anterior (sin intentos)
    /// </summary>
    public static void AddScore(string username, string puzzleId, float time)
    {
        AddScore(username, puzzleId, time, 1); // Por defecto 1 intento
    }

    /// <summary>
    /// Devuelve la lista de puntuaciones para un usuario y un puzle. Si no hay, devuelve una lista vacía.
    /// </summary>
    public static List<ScoreEntry> GetScoreEntries(string username, string puzzleId)
    {
        if (localUserData.UserScores.ContainsKey(username) &&
            localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            return localUserData.UserScores[username][puzzleId];
        }

        // Si no se encuentran puntuaciones, devolver una lista vacía para evitar errores
        return new List<ScoreEntry>();
    }

    /// <summary>
    /// Devuelve solo los tiempos para compatibilidad con código existente
    /// </summary>
    public static List<float> GetScores(string username, string puzzleId)
    {
        List<ScoreEntry> entries = GetScoreEntries(username, puzzleId);
        List<float> times = new List<float>();

        foreach (ScoreEntry entry in entries)
        {
            times.Add(entry.time);
        }

        return times;
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