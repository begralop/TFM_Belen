using UnityEngine;
using System.Collections.Generic;

public static class UserManager
{
    private const string UserCountKey = "UserCount";
    private const string UserPrefixKey = "User_";
    private const string CurrentUserKey = "CurrentUser";

    public static void SaveUser(string username)
    {
        int userCount = PlayerPrefs.GetInt(UserCountKey, 0);
        PlayerPrefs.SetString(UserPrefixKey + userCount, username);
        PlayerPrefs.SetInt(UserCountKey, userCount + 1);
        PlayerPrefs.Save();
    }

    public static List<string> GetUsers()
    {
        List<string> users = new List<string>();
        int userCount = PlayerPrefs.GetInt(UserCountKey, 0);
        for (int i = 0; i < userCount; i++)
        {
            users.Add(PlayerPrefs.GetString(UserPrefixKey + i));
        }
        return users;
    }

    public static void SetCurrentUser(string username)
    {
        PlayerPrefs.SetString(CurrentUserKey, username);
        PlayerPrefs.Save();
    }

    public static string GetCurrentUser()
    {
        return PlayerPrefs.GetString(CurrentUserKey, "Invitado");
    }
}