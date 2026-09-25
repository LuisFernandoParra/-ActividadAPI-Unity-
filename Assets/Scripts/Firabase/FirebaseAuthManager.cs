using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("Nombre completo (se muestra en la interfaz)")]
    [SerializeField] private TMP_Text nombreCompletoText;
    private const string NOMBRE_COMPLETO_PROYECTO = "Luis Fernando Parra";

    [Header("Paneles")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject profilePanel;

    [Header("Campos - Login")]
    [SerializeField] private TMP_InputField loginEmailField;
    [SerializeField] private TMP_InputField loginPasswordField;

    [Header("Campos - Registro")]
    [SerializeField] private TMP_InputField regEmailField;
    [SerializeField] private TMP_InputField regPasswordField;
    [SerializeField] private TMP_InputField regUsernameField;
    [SerializeField] private TMP_InputField regExtraField;

    [Header("Textos de estado")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text profileUsernameText;

    private const string DATABASE_URL = "https://actividadfirebaseunity-default-rtdb.firebaseio.com";

    // Otros scripts (LeaderboardManager, JuegoAtrapaBoton) leen estos dos
    // valores estáticos para saber quién es el usuario logueado ahora mismo.
    public static string UsuarioActualId = "";
    public static string UsuarioActualUsername = "";

    private FirebaseAuth auth;
    private DatabaseReference dbRoot;
    private bool firebaseListo = false;

    void Start()
    {
        if (nombreCompletoText != null) nombreCompletoText.text = NOMBRE_COMPLETO_PROYECTO;

        ShowLogin();

        Debug.Log("Iniciando chequeo de Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("Chequeo de Firebase terminó. Status: " + task.Result);

            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                try
                {
                    dbRoot = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, DATABASE_URL).RootReference;
                }
                catch (System.Exception e)
                {
                    SetStatus("Error conectando a la base de datos: " + e.Message);
                    return;
                }

                firebaseListo = true;
                Debug.Log("Firebase listo para usarse.");

                if (auth.CurrentUser != null)
                {
                    StartCoroutine(CargarPerfilYMostrar(auth.CurrentUser));
                }
            }
            else
            {
                SetStatus("No se pudo inicializar Firebase: " + status);
            }
        });
    }

    // ---------- Botones ----------
    public void RegisterButtonClick() { StartCoroutine(RegistrarUsuario()); }
    public void LoginButtonClick() { StartCoroutine(IniciarSesion()); }
    public void ForgotPasswordButtonClick() { StartCoroutine(RecuperarContrasena()); }

    public void LogoutButtonClick()
    {
        if (auth != null) auth.SignOut();
        UsuarioActualId = "";
        UsuarioActualUsername = "";
        ShowLogin();
    }

    public void GoToRegisterButtonClick() { ShowRegister(); }
    public void GoToLoginButtonClick() { ShowLogin(); }

    // ---------- Registro ----------
    IEnumerator RegistrarUsuario()
    {
        if (!firebaseListo) { SetStatus("Firebase todavía se está iniciando, espera un segundo e intenta de nuevo."); yield break; }

        string email = regEmailField.text;
        string password = regPasswordField.text;
        string username = regUsernameField.text;
        string extra = regExtraField != null ? regExtraField.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            SetStatus("Completa email, contraseña y nombre de usuario.");
            yield break;
        }

        var registroTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registroTask.IsCompleted);

        if (registroTask.Exception != null)
        {
            SetStatus("No se pudo registrar: " + registroTask.Exception.InnerException.Message);
            yield break;
        }

        FirebaseUser nuevoUsuario = registroTask.Result.User;

        // Guardamos username + dato extra + score inicial en Realtime Database,
        // en el nodo usuarios/{uid}
        UsuarioData datos = new UsuarioData { username = username, extra = extra, score = 0 };
        string json = JsonUtility.ToJson(datos);

        var guardarTask = dbRoot.Child("usuarios").Child(nuevoUsuario.UserId).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => guardarTask.IsCompleted);

        if (guardarTask.Exception != null)
        {
            SetStatus("Usuario creado, pero no se pudo guardar el perfil: " + guardarTask.Exception.InnerException.Message);
            yield break;
        }

        UsuarioActualId = nuevoUsuario.UserId;
        UsuarioActualUsername = username;

        SetStatus("¡Registro exitoso!");
        MostrarPerfil(username);
    }

    // ---------- Login ----------
    IEnumerator IniciarSesion()
    {
        if (!firebaseListo) { SetStatus("Firebase todavía se está iniciando, espera un segundo e intenta de nuevo."); yield break; }

        string email = loginEmailField.text;
        string password = loginPasswordField.text;

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            SetStatus("Usuario o contraseña incorrectos.");
            yield break;
        }

        yield return StartCoroutine(CargarPerfilYMostrar(loginTask.Result.User));
    }

    IEnumerator CargarPerfilYMostrar(FirebaseUser usuario)
    {
        var leerTask = dbRoot.Child("usuarios").Child(usuario.UserId).GetValueAsync();
        yield return new WaitUntil(() => leerTask.IsCompleted);

        string usernameMostrado = usuario.Email;

        if (leerTask.Result != null && leerTask.Result.Exists)
        {
            UsuarioData datos = JsonUtility.FromJson<UsuarioData>(leerTask.Result.GetRawJsonValue());
            if (!string.IsNullOrEmpty(datos.username)) usernameMostrado = datos.username;
        }

        UsuarioActualId = usuario.UserId;
        UsuarioActualUsername = usernameMostrado;

        MostrarPerfil(usernameMostrado);
    }

    // ---------- Recuperar contraseña ----------
    IEnumerator RecuperarContrasena()
    {
        if (!firebaseListo) { SetStatus("Firebase todavía se está iniciando, espera un segundo e intenta de nuevo."); yield break; }

        string email = loginEmailField.text;
        if (string.IsNullOrEmpty(email))
        {
            SetStatus("Escribe tu email en el campo de arriba y luego dale a recuperar contraseña.");
            yield break;
        }

        var resetTask = auth.SendPasswordResetEmailAsync(email);
        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.Exception != null)
        {
            SetStatus("No se pudo enviar el correo de recuperación.");
        }
        else
        {
            SetStatus("Te enviamos un correo para recuperar tu contraseña.");
        }
    }

    // ---------- Cambiar de panel ----------
    void ShowLogin()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
    }

    void ShowRegister()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (profilePanel != null) profilePanel.SetActive(false);
    }

    void MostrarPerfil(string username)
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(true);
        if (profileUsernameText != null) profileUsernameText.text = username;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}

[System.Serializable]
public class UsuarioData
{
    public string username;
    public string extra;
    public int score;
}