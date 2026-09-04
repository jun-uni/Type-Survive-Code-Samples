using System.Text;
using TMPro;
using UnityEngine;

public sealed class WordDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TextMeshProUGUI wordText;

    [Header("색상")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color typingColor = Color.yellow;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color typoColor = Color.red;

    [Header("애니메이션")]
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private float animationDuration = 0.2f;

    private Camera mainCamera;
    private string currentWord;
    private string[] koreanJamos;
    private int[] koreanSyllableJamoCounts;
    private int typedCount;
    private bool isKoreanWord;

    private void Awake()
    {
        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();

        mainCamera = Camera.main;
        if (worldCanvas != null)
        {
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // 월드 캔버스의 수평 빌보드 회전
        Vector3 direction = mainCamera.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-direction);
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    public void SetWord(string word)
    {
        currentWord = word ?? string.Empty;
        isKoreanWord = ContainsHangulSyllable(currentWord);
        koreanJamos = isKoreanWord ? KoreanTool.SplitKoreanCharacters(currentWord) : null;
        koreanSyllableJamoCounts = isKoreanWord ? BuildSyllableJamoCounts(currentWord) : null;
        typedCount = 0;
        RefreshText();
    }

    public void UpdateProgress(int progress, bool korean, string[] jamos)
    {
        typedCount = Mathf.Max(0, progress);
        isKoreanWord = korean;
        koreanJamos = jamos;
        RefreshText();
    }

    public void ShowCompletionEffect()
    {
        if (wordText == null)
            return;

        LeanTween.cancel(gameObject);
        wordText.text = currentWord;
        wordText.color = completedColor;

        Vector3 originalScale = transform.localScale;
        LeanTween.scale(gameObject, originalScale * bounceScale, animationDuration * 0.5f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
                LeanTween.scale(gameObject, originalScale, animationDuration * 0.5f)
                    .setEase(LeanTweenType.easeInBack));
    }

    public void ShowTypoEffect()
    {
        if (wordText == null)
            return;

        LeanTween.cancel(gameObject);
        wordText.text = currentWord;
        wordText.color = typoColor;

        Vector3 originalScale = transform.localScale;
        LeanTween.scale(gameObject, originalScale * 1.1f, animationDuration * 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
                LeanTween.scale(gameObject, originalScale, animationDuration * 0.5f)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(RefreshText));
    }

    private void RefreshText()
    {
        if (wordText == null || string.IsNullOrEmpty(currentWord))
            return;

        int highlightedCharacters = isKoreanWord
            ? CountCompletedKoreanSyllables()
            : Mathf.Min(typedCount, currentWord.Length);

        string typedColor = ColorUtility.ToHtmlStringRGB(typingColor);
        string remainingColor = ColorUtility.ToHtmlStringRGB(defaultColor);
        StringBuilder builder = new();

        for (int i = 0; i < currentWord.Length; i++)
        {
            string color = i < highlightedCharacters ? typedColor : remainingColor;
            builder.Append("<color=#").Append(color).Append('>')
                .Append(currentWord[i]).Append("</color>");
        }

        wordText.color = Color.white;
        wordText.text = builder.ToString();
    }

    private int CountCompletedKoreanSyllables()
    {
        if (koreanJamos == null || koreanSyllableJamoCounts == null || typedCount <= 0)
            return 0;

        int completedSyllables = 0;
        int consumedJamos = 0;

        foreach (int requiredJamos in koreanSyllableJamoCounts)
        {
            if (requiredJamos == 0 || consumedJamos + requiredJamos > typedCount)
                break;

            consumedJamos += requiredJamos;
            completedSyllables++;
        }

        return completedSyllables;
    }

    private static int[] BuildSyllableJamoCounts(string word)
    {
        int[] counts = new int[word.Length];

        // 표시 갱신 시 재분해하지 않도록 음절별 자모 수 캐싱
        for (int i = 0; i < word.Length; i++)
            counts[i] = KoreanTool.SplitKoreanCharacters(word[i].ToString()).Length;

        return counts;
    }

    private static bool ContainsHangulSyllable(string value)
    {
        foreach (char character in value)
        {
            if (character is >= '\uAC00' and <= '\uD7A3')
                return true;
        }

        return false;
    }
}
