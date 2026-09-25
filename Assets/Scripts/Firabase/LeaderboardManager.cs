using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text leaderboardText;

    private const string DATABASE_URL = "https://actividadfirebaseunity-default-rtdb.firebaseio.com";

    private DatabaseReference dbRoot;

    public void ShowLeaderboardButtonClick()
    {
        StartCoroutine(CargarLeaderboard());
    }

    public void CloseLeaderboardButtonClick()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    IEnumerator CargarLeaderboard()
    {
        if (dbRoot == null) dbRoot = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, DATABASE_URL).RootReference;

        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        if (leaderboardText != null) leaderboardText.text = "Cargando...";

        var query = dbRoot.Child("usuarios").GetValueAsync();
        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null || query.Result == null || !query.Result.Exists)
        {
            if (leaderboardText != null) leaderboardText.text = "No se pudo cargar la tabla de puntajes.";
            yield break;
        }

        List<(string username, int score)> filas = new List<(string, int)>();

        foreach (DataSnapshot hijo in query.Result.Children)
        {
            string username = hijo.Child("username").Value != null ? hijo.Child("username").Value.ToString() : "Sin nombre";
            int score = 0;
            if (hijo.Child("score").Value != null)
            {
                int.TryParse(hijo.Child("score").Value.ToString(), out score);
            }
            filas.Add((username, score));
        }

        // Orden descendente por puntaje (el más alto primero)
        filas.Sort((a, b) => b.score.CompareTo(a.score));

        string texto = "";
        int posicion = 1;
        foreach (var fila in filas)
        {
            texto += posicion + ". " + fila.username + " - " + fila.score + "\n";
            posicion++;
        }

        if (leaderboardText != null)
        {
            leaderboardText.text = filas.Count > 0 ? texto : "Todavía no hay puntajes guardados.";
        }
    }
}