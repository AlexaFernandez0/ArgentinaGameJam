using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;         // loops (menu/gameplay)
    public AudioSource jingleSource;        // one-time music (level finish / lose)
    public AudioSource sfxSourceOneShot;    // SFX one-shots
    public AudioSource sfxSourceLoop;       // SFX loop

    [Header("Music Clips (loop)")]
    public AudioClip mainMenuLoop;
    public AudioClip gameplayLoop;

    [Header("Jingles (one-shot)")]
    public AudioClip levelFinishedJingle;
    public AudioClip gameLostJingle;

    [Header("SFX (one-shot)")]
    public AudioClip clickButton;
    public AudioClip footstepClip;
    public AudioClip turnEndClip;
    public AudioClip invalidActionClip;
    public AudioClip pushEnemyClip;

    [Header("SFX Settings(one-shot)")]
    [SerializeField, Range(0.8f, 1.2f)] private float PitchMin = 0.95f;
    [SerializeField, Range(0.8f, 1.2f)] private float PitchMax = 1.05f;

    [Header("SFX (loop)")]
    public AudioClip fireLoopClip;

    public AudioSource FireLoopSource => sfxSourceLoop;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float jingleVolume = 0.9f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    [Header("Behaviour")]
    public bool fadeBetweenLoops = true;
    public float fadeTime = 0.35f;
    public float duckMusicDuringJingle = 0.35f; // 0 = mute music, 1 = keep music same

    private Coroutine _fadeRoutine;
    private Coroutine _duckRoutine;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyVolumes();

        // Asegurar que el AudioSource tiene el clip asignado
        if (sfxSourceLoop != null && sfxSourceLoop.clip == null && fireLoopClip != null)
            sfxSourceLoop.clip = fireLoopClip;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        TrySubscribeGameEvents();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        UnsubscribeGameEvents();
    }

    private void Start()
    {
        // Arranque inicial según la escena actual
        PlayLoopForScene(SceneManager.GetActiveScene().name);
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        StopJinglesAndRestoreMusic(); // <- clave
        PlayLoopForScene(newScene.name);
        TrySubscribeGameEvents();
    }

    // ---------------- MUSIC ----------------

    private void PlayLoopForScene(string sceneName)
    {
        // Ajusta estos nombres a tus escenas reales:
        // Ej: "MainMenu" y "Game"
        if (sceneName.Contains("Menu"))
            PlayMusicLoop(mainMenuLoop);
        else
            PlayMusicLoop(gameplayLoop);
    }

    public void PlayMusicLoop(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

        if (!fadeBetweenLoops || !musicSource.isPlaying)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume; // ojo: aquí no restaura ducking si había
            musicSource.Play();
            return;
        }

        _fadeRoutine = StartCoroutine(FadeToNewLoop(clip));
    }


    private System.Collections.IEnumerator FadeToNewLoop(AudioClip newClip)
    {
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeTime);
            yield return null;
        }

        musicSource.volume = musicVolume;
        _fadeRoutine = null;
    }


    // ---------------- JINGLES ----------------

    public void PlayLevelFinishedJingle()
    {
        PlayJingle(levelFinishedJingle);
    }

    public void PlayGameLostJingle()
    {
        PlayJingle(gameLostJingle);
    }

    private void PlayJingle(AudioClip clip)
    {
        if (jingleSource == null || clip == null) return;

        // Cancelar "restore" anterior
        if (_duckRoutine != null) StopCoroutine(_duckRoutine);

        // Duck music (sin golpe raro)
        if (musicSource != null)
            musicSource.volume = musicVolume * duckMusicDuringJingle;

        jingleSource.volume = jingleVolume;
        jingleSource.Stop();              // opcional: evita stacking raro si spameas
        jingleSource.PlayOneShot(clip);

        _duckRoutine = StartCoroutine(RestoreAfterSeconds(clip.length));
    }

    private System.Collections.IEnumerator RestoreAfterSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreMusicVolume();
        _duckRoutine = null;
    }


    private void RestoreMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void StopJinglesAndRestoreMusic()
    {
        CancelInvoke(nameof(RestoreMusicVolume));

        if (jingleSource != null)
            jingleSource.Stop();

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    // ---------------- SFX ----------------

    public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSourceOneShot == null || clip == null) return;
        sfxSourceOneShot.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    public void PlaySFXPitchVariability(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSourceOneShot == null || footstepClip == null) return;

        sfxSourceOneShot.pitch = UnityEngine.Random.Range(PitchMin, PitchMax);
        sfxSourceOneShot.PlayOneShot(clip, sfxSourceOneShot.volume);
    }

    public void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (jingleSource != null) jingleSource.volume = jingleVolume;
        if (sfxSourceOneShot != null) sfxSourceOneShot.volume = sfxVolume;
    }

    // ---------------- EVENTS HOOK ----------------
    // Aquí conectamos con tus eventos ya existentes

    private void TrySubscribeGameEvents()
    {
        UnsubscribeGameEvents();

        // Lose viene del GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameLost += OnGameLost;
            GameManager.Instance.GameReset += OnGameReset;
        }
            

        // LevelFinished / GameWon los estás disparando desde LevelTransitionManager
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.LevelFinished += OnLevelFinished;
            LevelTransitionManager.Instance.GameWon += OnGameWon;
        }
    }

    private void UnsubscribeGameEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameLost -= OnGameLost;
            GameManager.Instance.GameReset -= OnGameReset;
        }
            

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.LevelFinished -= OnLevelFinished;
            LevelTransitionManager.Instance.GameWon -= OnGameWon;
        }
    }

    private void OnLevelFinished(string msg)
    {
        PlayLevelFinishedJingle();
    }

    private void OnGameWon(string msg)
    {
        // Si quieres un jingle distinto de victoria final, lo añades luego.
        // Por ahora puedes reutilizar levelFinishedJingle o nada.
        PlayLevelFinishedJingle();
    }

    private void OnGameLost(string msg)
    {
        PlayGameLostJingle();
    }

    private void OnGameReset()
    {
        StopJinglesAndRestoreMusic();
    }
}

