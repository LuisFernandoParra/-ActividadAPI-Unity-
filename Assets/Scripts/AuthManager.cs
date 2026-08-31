using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    private string Url = "https://sid-restapi.onrender.com";
    string token = "";
    string username = "";

    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text profileUsernameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_InputField scoreInputField;
    [SerializeField] private TMP_Text scoreStatusText;
    [SerializeField] private TMP_Text leaderboardText;

    void Start()
    {
        ShowLogin();

        token = PlayerPrefs.GetString("token", "");
        username = PlayerPrefs.GetString("username", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
           StartCoroutine(GetProfile());
        }
    }
    public void RegisterButtonClick()
    {
        StartCoroutine(RegisterUser());
    }
    public void LoginButtonClick()
    {
        StartCoroutine(Login());
    }

    public void LogoutButtonClick()
    {
        token = "";
        username = "";
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.DeleteKey("username");
        ShowLogin();
    }

    public void UpdateScoreButtonClick()
    {
        StartCoroutine(UpdateScore());
    }

    public void ShowLeaderboardButtonClick()
    {
        StartCoroutine(GetUsersList());
    }

    public void BackToProfileButtonClick()
    {
        ShowProfile(username);
    }

    IEnumerator GetProfile()
    {
        UnityWebRequest www = UnityWebRequest.Get(Url + "/api/usuarios/" + username);
        www.SetRequestHeader("x-token",token);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(www.downloadHandler.text);
            LogoutButtonClick();
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            UserResponse userData = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);
            Debug.Log("User profile: " + userData.usuario.username);
            ShowProfile(userData.usuario.username);
        }
    }
    IEnumerator Login()
    {

        AuthData authData = new AuthData();

        authData.username = GameObject.Find("UsernameField").GetComponent<TMP_InputField>().text;
        authData.password = GameObject.Find("PasswordField").GetComponent<TMP_InputField>().text;

        string jsonData = JsonUtility.ToJson(authData);

        Debug.Log("Sending JSON data: " + jsonData);
        UnityWebRequest www = UnityWebRequest.Post(Url + "/api/auth/login", jsonData, "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(www.downloadHandler.text);
            SetStatus("Usuario o contraseña incorrectos.");
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);

            token = userResponse.token;
            username = userResponse.usuario.username;

            PlayerPrefs.SetString("token", token);
            PlayerPrefs.SetString("username", username);

            ShowProfile(username);
        }
    }
    IEnumerator RegisterUser()
    {

        AuthData authData = new AuthData();

        authData.username = GameObject.Find("UsernameField").GetComponent<TMP_InputField>().text;
        authData.password = GameObject.Find("PasswordField").GetComponent<TMP_InputField>().text;

        string jsonData = JsonUtility.ToJson(authData);

        Debug.Log("Sending JSON data: " + jsonData);
        UnityWebRequest www = UnityWebRequest.Post(Url + "/api/usuarios", jsonData,"application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(www.downloadHandler.text);
            SetStatus("No se pudo registrar el usuario.");
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);
            Debug.Log("User registered: " + userResponse.usuario.username);

            StartCoroutine(Login());

        }
    }

    IEnumerator UpdateScore()
    {
        int scoreValue = 0;
        int.TryParse(scoreInputField.text, out scoreValue);

        UpdateScoreRequest updateData = new UpdateScoreRequest();
        updateData.username = username;
        updateData.data = new ScoreData();
        updateData.data.score = scoreValue;

        string jsonData = JsonUtility.ToJson(updateData);
        Debug.Log("Sending PATCH data: " + jsonData);

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(Url + "/api/usuarios", "PATCH");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("x-token", token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(www.downloadHandler.text);
            if (scoreStatusText != null) scoreStatusText.text = "No se pudo actualizar el score.";
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            if (scoreStatusText != null) scoreStatusText.text = "Score actualizado!";
        }
    }

    IEnumerator GetUsersList()
    {
        UnityWebRequest www = UnityWebRequest.Get(Url + "/api/usuarios");
        www.SetRequestHeader("x-token", token);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(www.downloadHandler.text);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            UsersListResponse listResponse = JsonUtility.FromJson<UsersListResponse>(www.downloadHandler.text);

            System.Array.Sort(listResponse.usuarios, (a, b) =>
            {
                int scoreA = (a.data != null) ? a.data.score : 0;
                int scoreB = (b.data != null) ? b.data.score : 0;
                return scoreB.CompareTo(scoreA);
            });

            string tableText = "";
            int position = 1;
            foreach (UserData u in listResponse.usuarios)
            {
                int score = (u.data != null) ? u.data.score : 0;
                tableText += position + ". " + u.username + " - " + score + "\n";
                position++;
            }

            if (leaderboardText != null) leaderboardText.text = tableText;

            ShowLeaderboard();
        }
    }

    private void ShowLogin()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    private void ShowProfile(string displayName)
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (profileUsernameText != null) profileUsernameText.text = displayName;
    }

    private void ShowLeaderboard()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
[System.Serializable]

public class AuthData
{
    public string username;
    public string password;

}
[System.Serializable]
public class  UserResponse
{
    public UserData usuario;
    public string token;
}
[System.Serializable]

public class UserData
{
    public string _id;
    public string username;
    public string password;
    public bool estado;
    public ScoreData data;
}

[System.Serializable]
public class ScoreData
{
    public int score;
}

[System.Serializable]
public class UpdateScoreRequest
{
    public string username;
    public ScoreData data;
}

[System.Serializable]
public class UsersListResponse
{
    public UserData[] usuarios;
}