using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnknownGame.GameFlow;
using UnknownGame.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public sealed class FinalScoreSubmission : MonoBehaviour
{
    private const int NicknameLimit = 10;
    private const string PlayerIdKey = "Scores.PlayerId";

    [SerializeField] private string endpoint = "https://unknown-game-api.onrender.com/api/scores";
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private UnityEngine.UI.Button submitButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private TMP_Text backLabel;
    [SerializeField] private TMP_Text statusText;

    [Serializable]
    private sealed class Score
    {
        public string nickname;
        public int deaths;
        public string level;
        public float timeSeconds;
        [NonSerialized] public string submittedKey;
    }

    [Serializable]
    private sealed class RunEnrollment
    {
        public string runId;
        public string playerId;
        public string proof;
    }

    [Serializable]
    private sealed class RunCredentials
    {
        public string token;

        public static string ReadToken(string response)
        {
            try
            {
                return JsonUtility.FromJson<RunCredentials>(response)?.token;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    private Action returnToMenu;
    private readonly List<Score> scores = new List<Score>();
    private Uri scoresUri;
    private string runId;
    private bool sending;

    public bool TryShow(Action returnToMenu)
    {
        var savedRun = GameSession.Current;
        if (savedRun == null || savedRun.levelScene != gameObject.scene.name)
            return false;

        if (!Guid.TryParseExact(savedRun.runId, "N", out var id) ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out scoresUri) ||
            (scoresUri.Scheme != Uri.UriSchemeHttps && !(Application.isEditor && scoresUri.IsLoopback && scoresUri.Scheme == Uri.UriSchemeHttp)))
            return false;

        runId = id.ToString("N");
        scores.Clear();
        foreach (var level in savedRun.levelScores?.results ?? Array.Empty<LevelScore>())
        {
            string title = LevelScore.NameForScene(level?.scene);
            if (title == null || level.deaths < 0 || !float.IsFinite(level.timeSeconds) || level.timeSeconds < 0f)
                continue;

            string previousKey = $"FinalScore.Attempted.{savedRun.runId}.{level.scene}";
            string submittedKey = $"FinalScore.Submitted.{runId}.{level.scene}";
            if (!PlayerPrefs.HasKey(previousKey) && !PlayerPrefs.HasKey(submittedKey))
                scores.Add(new Score { deaths = level.deaths, timeSeconds = level.timeSeconds, level = title, submittedKey = submittedKey });
        }
        if (scores.Count == 0)
            return false;

        this.returnToMenu = returnToMenu;
        nicknameInput.characterLimit = NicknameLimit;
        gameObject.SetActive(true);
        nicknameInput.ActivateInputField();
        return true;
    }

    private void OnEnable()
    {
        submitButton.onClick.AddListener(Submit);
        nicknameInput.onSubmit.AddListener(SubmitOnEnter);
        backButton.onClick.AddListener(ReturnToMenu);
    }

    private void OnDisable()
    {
        submitButton.onClick.RemoveListener(Submit);
        nicknameInput.onSubmit.RemoveListener(SubmitOnEnter);
        backButton.onClick.RemoveListener(ReturnToMenu);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ReturnToMenu();
    }

    private void ReturnToMenu() => returnToMenu?.Invoke();

    private void SubmitOnEnter(string _) => Submit();

    private void Submit()
    {
        if (sending || !submitButton.isActiveAndEnabled || !submitButton.interactable)
            return;

        string nickname = nicknameInput.text.Trim();
        if (nickname.Length < 1 || nickname.Length > NicknameLimit)
        {
            statusText.text = $"Escribe un apodo de 1 a {NicknameLimit} caracteres.";
            nicknameInput.ActivateInputField();
            return;
        }

        sending = true;
        nicknameInput.interactable = false;
        submitButton.interactable = false;
        backLabel.text = "Volver";
        statusText.text = "Enviando resultados…";
        StartCoroutine(SendScores(nickname));
    }

    private IEnumerator SendScores(string nickname)
    {
        var enrollment = GetRunEnrollment();
        var runsUri = new Uri(scoresUri.AbsoluteUri.TrimEnd('/'));
        runsUri = new Uri(runsUri, "runs");
        string token = null;
        bool renewed = false;
        int confirmed = 0;
        foreach (var score in scores)
        {
            if (PlayerPrefs.HasKey(score.submittedKey))
            {
                confirmed++;
                continue;
            }

            score.nickname = nickname;
            while (true)
            {
                if (token == null)
                {
                    using var sessionRequest = PostJson(runsUri, enrollment);
                    yield return sessionRequest.SendWebRequest();
                    if (sessionRequest.result == UnityWebRequest.Result.Success)
                        token = RunCredentials.ReadToken(sessionRequest.downloadHandler.text);

                    if (string.IsNullOrEmpty(token) || token.Length != 43 || token.Contains('\r') || token.Contains('\n'))
                    {
                        ShowSendError(sessionRequest.responseCode, confirmed);
                        yield break;
                    }
                }

                using var request = PostJson(scoresUri, score);
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && (request.responseCode == 200 || request.responseCode == 201))
                {
                    PlayerPrefs.SetInt(score.submittedKey, 1);
                    PlayerPrefs.Save();
                    confirmed++;
                    break;
                }

                if (request.responseCode == 401 && !renewed)
                {
                    token = null;
                    renewed = true;
                    continue;
                }

                ShowSendError(request.responseCode, confirmed);
                yield break;
            }
        }

        sending = false;
        statusText.text = $"Resultados enviados: {confirmed} de {scores.Count}.";
        PlayerPrefs.DeleteKey(RunProofKey);
        PlayerPrefs.Save();
    }

    private string RunProofKey => $"Scores.RunProof.{runId}";

    private RunEnrollment GetRunEnrollment()
    {
        if (!Guid.TryParseExact(PlayerPrefs.GetString(PlayerIdKey, string.Empty), "N", out var playerId))
        {
            playerId = Guid.NewGuid();
            PlayerPrefs.SetString(PlayerIdKey, playerId.ToString("N"));
        }

        string proof = PlayerPrefs.GetString(RunProofKey, string.Empty);
        if (proof.Length != 64)
        {
            proof = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(RunProofKey, proof);
        }
        PlayerPrefs.Save();
        return new RunEnrollment { runId = runId, playerId = playerId.ToString("N"), proof = proof };
    }

    private static UnityWebRequest PostJson(Uri uri, object data)
    {
        var request = UnityWebRequest.Post(uri, JsonUtility.ToJson(data), "application/json");
        request.redirectLimit = 0;
        request.timeout = 25;
        return request;
    }

    private void ShowSendError(long statusCode, int confirmed)
    {
        sending = false;
        nicknameInput.interactable = confirmed == 0;
        submitButton.interactable = true;
        string message = statusCode switch
        {
            400 => "El servidor rechazó los datos.",
            429 => "Demasiados envíos. Espera un minuto e inténtalo de nuevo.",
            _ => "No se pudo confirmar el envío. Inténtalo de nuevo.",
        };
        statusText.text = confirmed == 0 ? message : $"{confirmed} de {scores.Count} enviados. {message}";
    }
}
