using System.Collections;
using UnityEngine;

public sealed class WordTarget : MonoBehaviour
{
    [SerializeField] private WordDisplay wordDisplay;

    private string[] koreanJamos;
    private bool isKoreanWord;
    private bool isCompleted;

    public string Word { get; private set; }
    public int CurrentProgress { get; private set; }

    private void Awake()
    {
        if (wordDisplay == null)
            wordDisplay = GetComponent<WordDisplay>();
    }

    private void Start()
    {
        TypingManager.Instance?.RegisterTarget(this);
        AssignRandomWord();
    }

    private void OnEnable()
    {
        TypingManager.Instance?.RegisterTarget(this);
    }

    private void OnDisable()
    {
        TypingManager.Instance?.UnregisterTarget(this);
    }

    public void AssignRandomWord()
    {
        if (WordDatabase.Instance == null)
            return;

        // 현재 게임 언어에 맞는 단어 선택
        bool useKorean = TypingManager.Instance != null && TypingManager.Instance.IsKoreanMode();
        string nextWord = useKorean
            ? WordDatabase.Instance.GetRandomKoreanWord()
            : WordDatabase.Instance.GetRandomWord();

        SetWord(nextWord, useKorean);
    }

    public bool CanAcceptNextChar(char input)
    {
        if (isKoreanWord || isCompleted || string.IsNullOrEmpty(Word) || CurrentProgress >= Word.Length)
            return false;

        return char.ToLowerInvariant(Word[CurrentProgress]) == char.ToLowerInvariant(input);
    }

    public bool CanAcceptNextJamo(string input)
    {
        return isKoreanWord &&
               !isCompleted &&
               koreanJamos != null &&
               CurrentProgress < koreanJamos.Length &&
               koreanJamos[CurrentProgress] == input;
    }

    public void AcceptCharacter(char input)
    {
        if (CanAcceptNextChar(input))
            Advance();
    }

    public void AcceptJamo(string input)
    {
        if (CanAcceptNextJamo(input))
            Advance();
    }

    public void TriggerIndividualTypo()
    {
        CurrentProgress = 0;
        wordDisplay?.UpdateProgress(0, isKoreanWord, koreanJamos);
    }

    public void HandleBackspace()
    {
        if (CurrentProgress <= 0)
            return;

        CurrentProgress--;
        UpdateDisplay();
    }

    public bool IsWordCompleted()
    {
        if (isCompleted || string.IsNullOrEmpty(Word))
            return false;

        int targetLength = isKoreanWord ? koreanJamos?.Length ?? 0 : Word.Length;
        return CurrentProgress >= targetLength;
    }

    public void OnWordCompleted()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        wordDisplay?.ShowCompletionEffect();
        // 입력 판정과 공격 실행 사이의 대상 전달
        WordCompletionEvents.TriggerWordCompleted(transform);
        TypingManager.Instance?.UnregisterTarget(this);

        StartCoroutine(AssignNewWordAfterDelay(0.5f));
    }

    public void ShowTypoEffect()
    {
        wordDisplay?.ShowTypoEffect();
    }

    private void SetWord(string word, bool korean)
    {
        Word = word;
        isKoreanWord = korean;
        koreanJamos = korean ? KoreanTool.SplitKoreanCharacters(word) : null;
        CurrentProgress = 0;
        isCompleted = false;

        wordDisplay?.SetWord(word);
        UpdateDisplay();
    }

    private void Advance()
    {
        CurrentProgress++;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        wordDisplay?.UpdateProgress(CurrentProgress, isKoreanWord, koreanJamos);
    }

    private IEnumerator AssignNewWordAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isActiveAndEnabled)
            yield break;

        AssignRandomWord();
        TypingManager.Instance?.RegisterTarget(this);
    }
}
