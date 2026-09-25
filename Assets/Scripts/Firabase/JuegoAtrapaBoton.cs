using System.Collections;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Juego original: "Atrapa el Botón".
// Un botón aparece en posiciones aleatorias dentro de un área; cada vez que
// el jugador le hace clic, suma un punto y el botón se mueve a otra posición.
// Al terminar el tiempo, se guarda el puntaje más alto en Firebase Realtime
// Database, dentro del perfil del usuario logueado (usuarios/{uid}/score).
public class JuegoAtrapaBoton : MonoBehaviour
{
    [Header("UI del juego")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private RectTransform areaJuego;
    [SerializeField] private RectTransform botonObjetivo;
    [SerializeField] private Button botonObjetivoButton;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultadoText;

    [Header("Configuración")]
    [SerializeField] private float duracionSegundos = 15f;

    private const string DATABASE_URL = "https://actividadfirebaseunity-default-rtdb.firebaseio.com";

    private int puntaje = 0;
    private float tiempoRestante;
    private bool jugando = false;
    private DatabaseReference dbRoot;

    public void StartGameButtonClick()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (botonObjetivoButton != null) botonObjetivoButton.interactable = true;

        puntaje = 0;
        tiempoRestante = duracionSegundos;
        jugando = true;

        ActualizarTextos();
        if (resultadoText != null) resultadoText.text = "";

        MoverBotonAPosicionAleatoria();
        StartCoroutine(Cronometro());
    }

    // Este método se conecta al OnClick() del botón objetivo en el Inspector.
    public void OnBotonObjetivoClick()
    {
        if (!jugando) return;
        puntaje++;
        ActualizarTextos();
        MoverBotonAPosicionAleatoria();
    }

    IEnumerator Cronometro()
    {
        while (tiempoRestante > 0f && jugando)
        {
            tiempoRestante -= Time.deltaTime;
            if (timerText != null) timerText.text = "Tiempo: " + Mathf.CeilToInt(Mathf.Max(tiempoRestante, 0f));
            yield return null;
        }

        if (jugando) TerminarJuego();
    }

    void TerminarJuego()
    {
        jugando = false;
        if (botonObjetivoButton != null) botonObjetivoButton.interactable = false;
        if (resultadoText != null) resultadoText.text = "¡Se acabó el tiempo! Puntaje: " + puntaje;
        StartCoroutine(GuardarPuntaje());
    }

    void MoverBotonAPosicionAleatoria()
    {
        if (areaJuego == null || botonObjetivo == null) return;

        float mitadAncho = areaJuego.rect.width / 2f;
        float mitadAlto = areaJuego.rect.height / 2f;

        float botonMitadAncho = botonObjetivo.rect.width / 2f;
        float botonMitadAlto = botonObjetivo.rect.height / 2f;

        float x = Random.Range(-mitadAncho + botonMitadAncho, mitadAncho - botonMitadAncho);
        float y = Random.Range(-mitadAlto + botonMitadAlto, mitadAlto - botonMitadAlto);

        botonObjetivo.anchoredPosition = new Vector2(x, y);
    }

    void ActualizarTextos()
    {
        if (scoreText != null) scoreText.text = "Puntaje: " + puntaje;
    }

    IEnumerator GuardarPuntaje()
    {
        string uid = FirebaseAuthManager.UsuarioActualId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.Log("No hay usuario logueado, no se guarda el puntaje.");
            yield break;
        }

        if (dbRoot == null) dbRoot = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, DATABASE_URL).RootReference;

        var leerTask = dbRoot.Child("usuarios").Child(uid).Child("score").GetValueAsync();
        yield return new WaitUntil(() => leerTask.IsCompleted);

        int puntajeGuardado = 0;
        if (leerTask.Result != null && leerTask.Result.Exists && leerTask.Result.Value != null)
        {
            int.TryParse(leerTask.Result.Value.ToString(), out puntajeGuardado);
        }

        // Solo sobreescribimos si el nuevo puntaje es mejor (record personal).
        if (puntaje > puntajeGuardado)
        {
            var guardarTask = dbRoot.Child("usuarios").Child(uid).Child("score").SetValueAsync(puntaje);
            yield return new WaitUntil(() => guardarTask.IsCompleted);

            if (resultadoText != null)
            {
                resultadoText.text += "\n¡Nuevo récord guardado!";
            }
        }
    }

    public void CloseGameButtonClick()
    {
        jugando = false;
        if (botonObjetivoButton != null) botonObjetivoButton.interactable = true;
        if (gamePanel != null) gamePanel.SetActive(false);
    }
}