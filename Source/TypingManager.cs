using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TypingManager : MonoBehaviour
{
    private readonly struct TypoSnapshot
    {
        public TypoSnapshot(WordTarget target, int progress)
        {
            Target = target;
            Progress = progress;
        }

        public WordTarget Target { get; }
        public int Progress { get; }
    }

    private static readonly KeyCode[] DubeolsikKeyCodes =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
        KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
        KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Z,
        KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };

    public static TypingManager Instance { get; private set; }

    [Header("입력 설정")]
    [SerializeField] private bool allowBackspace = true;

    [Header("전역 오타 설정")]
    [SerializeField] private float typoEffectDuration = 0.5f;

    private readonly List<WordTarget> activeTargets = new();
    private bool isKoreanMode;
    private bool isGlobalTypo;
    private float typoTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void Start()
    {
        UpdateLanguageMode();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        HandleInput();
        UpdateTypoState();
    }

    private void HandleInput()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        if (allowBackspace && Input.inputString.Contains("\b"))
            HandleBackspace();

        if (isKoreanMode)
        {
            HandleKoreanKeyInput();
            return;
        }

        foreach (char input in Input.inputString)
        {
            if (input != '\b' && IsEnglishLetter(input))
                ProcessEnglishInput(char.ToLowerInvariant(input));
        }
    }

    private void HandleKoreanKeyInput()
    {
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        foreach (KeyCode keyCode in DubeolsikKeyCodes)
        {
            if (!Input.GetKeyDown(keyCode))
                continue;

            string jamo = KoreanTool.EnglishLetterToKoreanLetter(KeyCodeToCharacter(keyCode), isShiftPressed);
            if (!string.IsNullOrEmpty(jamo))
                ProcessKoreanInput(jamo);
            break;
        }
    }

    private void ProcessEnglishInput(char input)
    {
        ProcessTargets(
            target => target.CanAcceptNextChar(input),
            target => target.AcceptCharacter(input));
    }

    private void ProcessKoreanInput(string input)
    {
        ProcessTargets(
            target => target.CanAcceptNextJamo(input),
            target => target.AcceptJamo(input));
    }

    private void ProcessTargets(Func<WordTarget, bool> canAccept, Action<WordTarget> accept)
    {
        // 입력 도중 등록 상태가 변해도 같은 타겟 집합으로 판정
        WordTarget[] targets = activeTargets.ToArray();
        List<TypoSnapshot> typoTargets = new();
        List<WordTarget> acceptingTargets = new();

        foreach (WordTarget target in targets)
        {
            if (target == null)
                continue;

            int previousProgress = target.CurrentProgress;

            if (canAccept(target))
            {
                accept(target);
                acceptingTargets.Add(target);
                continue;
            }

            if (previousProgress <= 0)
                continue;

            typoTargets.Add(new TypoSnapshot(target, previousProgress));
            target.TriggerIndividualTypo();
        }

        if (ShouldTriggerGlobalTypo(typoTargets, acceptingTargets))
        {
            TriggerGlobalTypo(typoTargets);
            return;
        }

        foreach (WordTarget target in targets)
        {
            if (target.IsWordCompleted())
                target.OnWordCompleted();
        }
    }

    private static bool ShouldTriggerGlobalTypo(
        IReadOnlyList<TypoSnapshot> typoTargets,
        IReadOnlyList<WordTarget> acceptingTargets)
    {
        bool hasSignificantTypo = false;
        int maxTypoProgress = 0;

        foreach (TypoSnapshot typo in typoTargets)
        {
            hasSignificantTypo |= typo.Progress >= 2;
            maxTypoProgress = Mathf.Max(maxTypoProgress, typo.Progress);
        }

        if (!hasSignificantTypo)
            return false;

        // 더 많이 진행된 유효 타겟이 있으면 입력 모호성으로 처리
        foreach (WordTarget target in acceptingTargets)
        {
            if (target.CurrentProgress > maxTypoProgress)
                return false;
        }

        return true;
    }

    private void TriggerGlobalTypo(IEnumerable<TypoSnapshot> typoTargets)
    {
        isGlobalTypo = true;
        typoTimer = typoEffectDuration;

        if (GameManager.Instance != null)
            GameManager.Instance.AddGlobalTypo();

        foreach (TypoSnapshot typo in typoTargets)
            typo.Target.ShowTypoEffect();
    }

    private void HandleBackspace()
    {
        foreach (WordTarget target in activeTargets.ToArray())
        {
            if (target != null)
                target.HandleBackspace();
        }
    }

    private void UpdateTypoState()
    {
        if (!isGlobalTypo)
            return;

        typoTimer -= Time.deltaTime;
        if (typoTimer <= 0f)
            isGlobalTypo = false;
    }

    private void OnLanguageChanged(SystemLanguage _)
    {
        UpdateLanguageMode();
    }

    private void UpdateLanguageMode()
    {
        isKoreanMode = LocalizationManager.IsInitialized &&
                       LocalizationManager.GameLanguage == SystemLanguage.Korean;
    }

    public void RegisterTarget(WordTarget target)
    {
        if (target != null && !activeTargets.Contains(target))
            activeTargets.Add(target);
    }

    public void UnregisterTarget(WordTarget target)
    {
        activeTargets.Remove(target);
    }

    public bool IsKoreanMode()
    {
        return isKoreanMode;
    }

    public bool IsGlobalTypo()
    {
        return isGlobalTypo;
    }

    private static bool IsEnglishLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static char KeyCodeToCharacter(KeyCode keyCode)
    {
        return keyCode.ToString()[0];
    }
}
