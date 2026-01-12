using UnityEngine;

public class HeatFeedback : MonoBehaviour
{
    [Header("Termómetro UI")]
    public Transform ejeTermometro;
    public float minRotationZ = 15f;    
    public float maxRotationZ = 165f;  
    [Range(1f, 20f)] public float rotationSpeed = 8f;

    [Header("Color del Personaje")]
    public Material playerMaterial;
    public Color overheatColor = new Color(1f, 0.2f, 0.2f, 1f);
    [Range(1f, 20f)] public float colorSpeed = 5f;

    [Header("🔥 Fuego UI (Visual)")]
    [Tooltip("GameObject (UI) con la animación de fuego alrededor del medidor")]
    public GameObject fireVfxRoot;

    [Tooltip("Opcional: si el fuego es UI, ponle un CanvasGroup para poder hacer fade suave")]
    public CanvasGroup fireVfxCanvasGroup;

    [Tooltip("Heat a partir del cual se activa el fuego")]
    public int fireThreshold = 50;

    [Tooltip("Velocidad del fade visual del fuego (si hay CanvasGroup)")]
    [Range(1f, 30f)] public float fireVisualFadeSpeed = 10f;

    [Header("🔥 Fuego (Audio)")]
    [Tooltip("AudioSource con loop de fuego (loop = true)")]
    public AudioSource fireLoopSource;

    [Tooltip("Volumen máximo del loop de fuego (cuando heat = maxHeat)")]
    [Range(0f, 1f)] public float fireMaxVolume = 0.8f;

    [Tooltip("Suavizado del volumen del fuego")]
    [Range(1f, 30f)] public float fireVolumeSpeed = 10f;

    // Shader props
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Standard

    private Color _originalColor;
    private bool _hasInitializedColor;

    private float _targetRotationZ;
    private float _currentRotationZ;

    private bool _fireActive;
    private float _fireTargetAlpha = 0f;

    private GameManager _gm;
    private int _lastHeat = int.MinValue;
    private int _lastMaxHeat = int.MinValue;
    private float _cachedHeatPercentage;
    private float _lastHeatPct = -1f;

    private void Awake()
    {
        if (ejeTermometro != null)
            _currentRotationZ = ejeTermometro.localEulerAngles.z;

        if (playerMaterial != null)
        {
            if (playerMaterial.HasProperty(BaseColorId))
                _originalColor = playerMaterial.GetColor(BaseColorId);
            else if (playerMaterial.HasProperty(ColorId))
                _originalColor = playerMaterial.GetColor(ColorId);
            else
                _originalColor = Color.white;

            _hasInitializedColor = true;
        }

        // Estado inicial fuego
        SetFireVisual(false, instant: true);
        SetFireAudioActive(false, instant: true);
    }

    private void Start()
    {
        TryBindFireLoop();

        var gm = GameManager.Instance;
        int heat = gm != null ? gm.heat : 0;

        bool active = heat >= fireThreshold;

        SetFireVisual(active, instant: true);
        SetFireAudioActive(active, instant: true);

        _lastHeat = int.MinValue;     // fuerza primer tick correcto
        _lastMaxHeat = int.MinValue;
    }


    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int currentHeat = gm.heat;
        int maxHeat = gm.maxHeat;
        if (maxHeat <= 0) return;

        bool heatChanged = (currentHeat != _lastHeat) || (maxHeat != _lastMaxHeat);

        // Calculamos porcentaje solo si cambió
        if (heatChanged)
        {
            _lastHeat = currentHeat;
            _lastMaxHeat = maxHeat;

            _lastHeatPct = Mathf.Clamp01((float)currentHeat / maxHeat);

            UpdateThermometer(_lastHeatPct);
            UpdatePlayerColor(_lastHeatPct);
        }

        // ---- FIRE: actualizar si cambió el heat O si aún hay transición ----
        bool shouldBeActive = currentHeat >= fireThreshold;

        bool audioNeedsTick = false;
        if (fireLoopSource != null)
        {
            // Target volumen según heat
            float targetVol = 0f;
            if (shouldBeActive)
            {
                float t = Mathf.InverseLerp(fireThreshold, maxHeat, currentHeat);
                targetVol = t * fireMaxVolume;
            }

            // Si aún no llegamos al target, hay transición
            audioNeedsTick = Mathf.Abs(fireLoopSource.volume - targetVol) > 0.001f
                             || (!shouldBeActive && fireLoopSource.isPlaying && fireLoopSource.volume > 0.0001f);
        }

        bool visualNeedsTick = false;
        if (fireVfxCanvasGroup != null)
        {
            float targetAlpha = shouldBeActive ? 1f : 0f;
            visualNeedsTick = Mathf.Abs(fireVfxCanvasGroup.alpha - targetAlpha) > 0.001f;
        }
        else
        {
            // Si NO usas CanvasGroup, solo hace falta tick cuando cambie el estado
            visualNeedsTick = false;
        }

            UpdateFireVisualAndAudio(currentHeat, maxHeat);
    }


    private void UpdateThermometer(float heatPercentage)
    {
        if (ejeTermometro == null) return;

        _targetRotationZ = Mathf.Lerp(minRotationZ, maxRotationZ, heatPercentage);
        _currentRotationZ = Mathf.LerpAngle(_currentRotationZ, _targetRotationZ, Time.deltaTime * rotationSpeed);

        var r = ejeTermometro.localEulerAngles;
        ejeTermometro.localEulerAngles = new Vector3(r.x, r.y, _currentRotationZ);
    }

    private void UpdatePlayerColor(float heatPercentage)
    {
        if (playerMaterial == null) return;

        Color targetColor = Color.Lerp(Color.white, overheatColor, heatPercentage);

        Color currentColor = Color.white;
        if (playerMaterial.HasProperty(BaseColorId))
            currentColor = playerMaterial.GetColor(BaseColorId);
        else if (playerMaterial.HasProperty(ColorId))
            currentColor = playerMaterial.GetColor(ColorId);

        Color smoothColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorSpeed);

        if (playerMaterial.HasProperty(BaseColorId))
            playerMaterial.SetColor(BaseColorId, smoothColor);
        if (playerMaterial.HasProperty(ColorId))
            playerMaterial.SetColor(ColorId, smoothColor);
    }

    // ---------------- 🔥 FIRE LOGIC ----------------

    private void UpdateFireVisualAndAudio(int currentHeat, int maxHeat)
    {
        if (maxHeat <= 0) return;

        float dt = Time.deltaTime;

        bool shouldBeActive = currentHeat >= fireThreshold;

        // 1) Estado: solo cuando cambia el "shouldBeActive"
        if (shouldBeActive != _fireActive)
        {
            _fireActive = shouldBeActive;

            // Visual: si se enciende, activamos el root ya
            if (fireVfxRoot != null && shouldBeActive && !fireVfxRoot.activeSelf)
                fireVfxRoot.SetActive(true);

            // Target alpha (si usas CanvasGroup)
            if (fireVfxCanvasGroup != null)
                _fireTargetAlpha = shouldBeActive ? 1f : 0f;

            // Audio: si se enciende, arrancamos el loop ya
            if (fireLoopSource != null && shouldBeActive && !fireLoopSource.isPlaying)
                fireLoopSource.Play();
        }

        // 2) Target volumen (si está apagado -> target 0)
        float targetVol = 0f;
        if (shouldBeActive)
        {
            float t = Mathf.InverseLerp(fireThreshold, maxHeat, currentHeat); // 0..1
            targetVol = t * fireMaxVolume;
        }

        // 3) Suavizado de volumen + stop limpio
        if (fireLoopSource != null)
        {
            // MoveTowards suele ser más estable que Lerp para ir a 0 sin quedarse "flotando"
            float maxDelta = fireVolumeSpeed * dt;
            fireLoopSource.volume = Mathf.MoveTowards(fireLoopSource.volume, targetVol, maxDelta);

            // Si está apagado y ya llegó a 0, paramos el loop
            if (!shouldBeActive && fireLoopSource.isPlaying && fireLoopSource.volume <= 0.0001f)
                fireLoopSource.Stop();
        }

        // 4) Visual (robusto): NO depende de _fireActive para encender
        if (fireVfxRoot != null)
        {
            if (shouldBeActive && !fireVfxRoot.activeSelf)
                fireVfxRoot.SetActive(true);
        }

        if (fireVfxCanvasGroup != null)
        {
            _fireTargetAlpha = shouldBeActive ? 1f : 0f;

            float maxDelta = fireVisualFadeSpeed * dt;
            fireVfxCanvasGroup.alpha = Mathf.MoveTowards(fireVfxCanvasGroup.alpha, _fireTargetAlpha, maxDelta);

            if (!shouldBeActive && fireVfxRoot != null && fireVfxRoot.activeSelf && fireVfxCanvasGroup.alpha <= 0.0001f)
                fireVfxRoot.SetActive(false);
        }
        else
        {
            // Si no hay CanvasGroup, on/off directo
            if (fireVfxRoot != null)
                fireVfxRoot.SetActive(shouldBeActive);
        }
    }


    private void SetFireVisual(bool active, bool instant)
    {
        _fireActive = active;
        _fireTargetAlpha = active ? 1f : 0f;

        if (fireVfxRoot == null) return;

        // IMPORTANTE: si activamos, encender root SIEMPRE
        if (active && !fireVfxRoot.activeSelf)
            fireVfxRoot.SetActive(true);

        // Si no hay CanvasGroup, simplemente on/off
        if (fireVfxCanvasGroup == null)
        {
            if (!active) fireVfxRoot.SetActive(false);
            return;
        }

        if (instant)
        {
            fireVfxCanvasGroup.alpha = _fireTargetAlpha;

            if (!active && fireVfxCanvasGroup.alpha <= 0.001f)
                fireVfxRoot.SetActive(false);
        }
    }

    private void SetFireAudioActive(bool active, bool instant)
    {
        if (fireLoopSource == null) return;

        if (instant)
        {
            fireLoopSource.volume = active ? fireMaxVolume : 0f;
            if (active && !fireLoopSource.isPlaying) fireLoopSource.Play();
            if (!active && fireLoopSource.isPlaying) fireLoopSource.Stop();
            return;
        }

        // Si no es instant, dejamos que Update haga el smoothing.
        if (active && !fireLoopSource.isPlaying) fireLoopSource.Play();
    }

    private void TryBindFireLoop()
    {
        if (fireLoopSource != null) return;
        if (AudioManager.Instance == null) return;

        fireLoopSource = AudioManager.Instance.FireLoopSource;
    }

    // ---------------- RESET ----------------

    public void ResetFeedback()
    {
        _currentRotationZ = 0f;
        _targetRotationZ = 0f;

        if (ejeTermometro != null)
        {
            Vector3 currentRotation = ejeTermometro.localEulerAngles;
            ejeTermometro.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, 0f);
        }

        if (playerMaterial != null && _hasInitializedColor)
        {
            if (playerMaterial.HasProperty(BaseColorId))
                playerMaterial.SetColor(BaseColorId, _originalColor);
            if (playerMaterial.HasProperty(ColorId))
                playerMaterial.SetColor(ColorId, _originalColor);
        }

        // Apagar fuego visual y audio
        SetFireVisual(false, instant: true);
        SetFireAudioActive(false, instant: true);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameReset += OnGameReset;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameReset -= OnGameReset;
    }

    private void OnGameReset()
    {
        ResetFeedback();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxRotationZ = Mathf.Clamp(maxRotationZ, 0f, 360f);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        colorSpeed = Mathf.Max(0.1f, colorSpeed);

        fireThreshold = Mathf.Max(0, fireThreshold);
        fireVisualFadeSpeed = Mathf.Max(0.1f, fireVisualFadeSpeed);
        fireVolumeSpeed = Mathf.Max(0.1f, fireVolumeSpeed);
    }
#endif
}
