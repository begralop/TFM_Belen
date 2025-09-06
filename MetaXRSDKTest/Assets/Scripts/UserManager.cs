using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Linq;

// ESTRUCTURA DE DATOS MEJORADA CON ESTADÍSTICAS
[System.Serializable]
public class ScoreEntry
{
    public float time;
    public int attempts;
    public string date;
    public int cubes;

    // NUEVOS CAMPOS para estadísticas detalladas
    public bool hintsUsed;
    public bool memoryModeUsed;
    public int gridRows;
    public int gridColumns;
    public string puzzleName;
    public float memoryVisibleTime;
    public float memoryHiddenTime;
    public int hintsActivatedCount;

    // Constructor completo
    public ScoreEntry(float time, int attempts, string date, int cubes,
                     bool hints = false, bool memory = false,
                     int rows = 3, int cols = 3, string puzzle = "",
                     float memVisible = 0, float memHidden = 0, int hintsCount = 0)
    {
        this.time = time;
        this.attempts = attempts;
        this.date = date;
        this.cubes = cubes;
        this.hintsUsed = hints;
        this.memoryModeUsed = memory;
        this.gridRows = rows;
        this.gridColumns = cols;
        this.puzzleName = puzzle;
        this.memoryVisibleTime = memVisible;
        this.memoryHiddenTime = memHidden;
        this.hintsActivatedCount = hintsCount;
    }

    // Constructor de compatibilidad para datos existentes
    public ScoreEntry(float time, int attempts, string date, int cubes)
        : this(time, attempts, date, cubes, false, false, 3, 3, "", 0, 0, 0) { }

    // Constructor más simple
    public ScoreEntry(float time, int attempts, string date)
        : this(time, attempts, date, 0, false, false, 3, 3, "", 0, 0, 0) { }
}

// Clase para estadísticas globales del usuario
[System.Serializable]
public class UserStatistics
{
    public int totalPuzzlesCompleted;
    public float totalPlayTime;
    public int perfectCompletions;
    public int memoryModeCompletions;
    public int hintsUsedCount;
    public Dictionary<string, int> puzzleCompletionCount = new Dictionary<string, int>();
    public float bestOverallTime;
    public string mostPlayedPuzzle;
    public string firstPlayDate;
    public string lastPlayDate;
    public int currentStreak;
    public int bestStreak;

    public UserStatistics()
    {
        puzzleCompletionCount = new Dictionary<string, int>();
        firstPlayDate = "";
        lastPlayDate = "";
        mostPlayedPuzzle = "";
    }

    public void UpdateStats(ScoreEntry newEntry)
    {
        totalPuzzlesCompleted++;
        totalPlayTime += newEntry.time;

        if (newEntry.attempts == 1 && !newEntry.hintsUsed)
            perfectCompletions++;

        if (newEntry.memoryModeUsed)
            memoryModeCompletions++;

        if (newEntry.hintsUsed)
            hintsUsedCount++;

        if (bestOverallTime == 0 || newEntry.time < bestOverallTime)
            bestOverallTime = newEntry.time;

        // Actualizar contador de puzzles
        if (!string.IsNullOrEmpty(newEntry.puzzleName))
        {
            if (!puzzleCompletionCount.ContainsKey(newEntry.puzzleName))
                puzzleCompletionCount[newEntry.puzzleName] = 0;
            puzzleCompletionCount[newEntry.puzzleName]++;

            // Actualizar puzzle más jugado
            mostPlayedPuzzle = puzzleCompletionCount
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault().Key;
        }

        lastPlayDate = newEntry.date;
        if (string.IsNullOrEmpty(firstPlayDate))
            firstPlayDate = newEntry.date;
    }
}

public class UserData
{
    public List<string> Usernames = new List<string>();
    public string CurrentUser;
    public Dictionary<string, Dictionary<string, List<ScoreEntry>>> UserScores = new Dictionary<string, Dictionary<string, List<ScoreEntry>>>();

    // NUEVO: Estadísticas globales por usuario
    public Dictionary<string, UserStatistics> UserStats = new Dictionary<string, UserStatistics>();
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

                // Asegurarse de que los diccionarios nunca sean nulos
                if (localUserData.UserScores == null)
                {
                    localUserData.UserScores = new Dictionary<string, Dictionary<string, List<ScoreEntry>>>();
                }

                if (localUserData.UserStats == null)
                {
                    localUserData.UserStats = new Dictionary<string, UserStatistics>();
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

    // MÉTODO PRINCIPAL ACTUALIZADO para añadir puntuaciones con estadísticas completas
    public static void AddScoreEntry(string username, string puzzleId, ScoreEntry entry)
    {
        // Asegurar que el usuario existe en el diccionario de puntuaciones
        if (!localUserData.UserScores.ContainsKey(username))
        {
            localUserData.UserScores[username] = new Dictionary<string, List<ScoreEntry>>();
        }

        // Asegurar que el puzzle existe para ese usuario
        if (!localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            localUserData.UserScores[username][puzzleId] = new List<ScoreEntry>();
        }

        // Añadir la puntuación
        localUserData.UserScores[username][puzzleId].Add(entry);

        // NUEVO: Actualizar estadísticas globales
        if (!localUserData.UserStats.ContainsKey(username))
        {
            localUserData.UserStats[username] = new UserStatistics();
        }
        localUserData.UserStats[username].UpdateStats(entry);

        SaveData();

        Debug.Log($"Puntuación guardada: Usuario={username}, Puzle={puzzleId}, " +
                 $"Tiempo={entry.time:F2}, Intentos={entry.attempts}, Cubos={entry.cubes}, " +
                 $"Pistas={entry.hintsUsed}, Memoria={entry.memoryModeUsed}, " +
                 $"Grid={entry.gridRows}x{entry.gridColumns}, Fecha={entry.date}");
    }

    // MÉTODOS DE COMPATIBILIDAD con diferentes sobrecargas
    public static void AddScore(string username, string puzzleId, float time, int attempts, int cubes,
                                bool hints = false, bool memory = false, int rows = 3, int cols = 3)
    {
        string currentDate = System.DateTime.Now.ToString("dd/MM/yyyy");
        ScoreEntry newScore = new ScoreEntry(time, attempts, currentDate, cubes,
                                            hints, memory, rows, cols, puzzleId);
        AddScoreEntry(username, puzzleId, newScore);
    }

    public static void AddScore(string username, string puzzleId, float time, int attempts, int cubes)
    {
        AddScore(username, puzzleId, time, attempts, cubes, false, false, 3, 3);
    }

    public static void AddScore(string username, string puzzleId, float time, int attempts)
    {
        AddScore(username, puzzleId, time, attempts, 0);
    }

    public static void AddScore(string username, string puzzleId, float time)
    {
        AddScore(username, puzzleId, time, 1);
    }

    // Obtener todas las entradas de puntuación
    public static List<ScoreEntry> GetScoreEntries(string username, string puzzleId)
    {
        if (localUserData.UserScores.ContainsKey(username) &&
            localUserData.UserScores[username].ContainsKey(puzzleId))
        {
            return localUserData.UserScores[username][puzzleId];
        }
        return new List<ScoreEntry>();
    }

    // Compatibilidad: obtener solo los tiempos
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

    // NUEVO: Obtener estadísticas globales del usuario
    public static UserStatistics GetUserStatistics(string username)
    {
        if (localUserData.UserStats.ContainsKey(username))
        {
            return localUserData.UserStats[username];
        }
        return new UserStatistics();
    }

    // MÉTODOS EXISTENTES sin cambios
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

            if (localUserData.UserStats.ContainsKey(username))
            {
                localUserData.UserStats.Remove(username);
            }

            if (localUserData.CurrentUser == username)
            {
                localUserData.CurrentUser = null;
            }

            SaveData();
        }
    }
}