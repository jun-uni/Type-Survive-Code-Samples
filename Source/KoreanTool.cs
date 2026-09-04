using System;
using System.Collections.Generic;

public static class KoreanTool
{
    private const int HangulBase = 0xAC00;
    private const int HangulEnd = 0xD7A3;
    private const int MedialsPerInitial = 21;
    private const int FinalsPerMedial = 28;
    private const int SyllablesPerInitial = MedialsPerInitial * FinalsPerMedial;

    private static readonly char[] Initials =
    {
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
        'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    private static readonly char[] Medials =
    {
        'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ',
        'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
    };

    private static readonly char[] Finals =
    {
        '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ',
        'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ',
        'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    private static readonly IReadOnlyDictionary<char, string> CompoundMedials =
        new Dictionary<char, string>
        {
            ['ㅘ'] = "ㅗㅏ",
            ['ㅙ'] = "ㅗㅐ",
            ['ㅚ'] = "ㅗㅣ",
            ['ㅝ'] = "ㅜㅓ",
            ['ㅞ'] = "ㅜㅔ",
            ['ㅟ'] = "ㅜㅣ",
            ['ㅢ'] = "ㅡㅣ"
        };

    private static readonly IReadOnlyDictionary<char, string> CompoundFinals =
        new Dictionary<char, string>
        {
            ['ㄳ'] = "ㄱㅅ",
            ['ㄵ'] = "ㄴㅈ",
            ['ㄶ'] = "ㄴㅎ",
            ['ㄺ'] = "ㄹㄱ",
            ['ㄻ'] = "ㄹㅁ",
            ['ㄼ'] = "ㄹㅂ",
            ['ㄽ'] = "ㄹㅅ",
            ['ㄾ'] = "ㄹㅌ",
            ['ㄿ'] = "ㄹㅍ",
            ['ㅀ'] = "ㄹㅎ",
            ['ㅄ'] = "ㅂㅅ"
        };

    private static readonly IReadOnlyDictionary<char, char> DubeolsikKeys =
        new Dictionary<char, char>
        {
            ['Q'] = 'ㅂ', ['W'] = 'ㅈ', ['E'] = 'ㄷ', ['R'] = 'ㄱ', ['T'] = 'ㅅ',
            ['Y'] = 'ㅛ', ['U'] = 'ㅕ', ['I'] = 'ㅑ', ['O'] = 'ㅐ', ['P'] = 'ㅔ',
            ['A'] = 'ㅁ', ['S'] = 'ㄴ', ['D'] = 'ㅇ', ['F'] = 'ㄹ', ['G'] = 'ㅎ',
            ['H'] = 'ㅗ', ['J'] = 'ㅓ', ['K'] = 'ㅏ', ['L'] = 'ㅣ', ['Z'] = 'ㅋ',
            ['X'] = 'ㅌ', ['C'] = 'ㅊ', ['V'] = 'ㅍ', ['B'] = 'ㅠ', ['N'] = 'ㅜ',
            ['M'] = 'ㅡ'
        };

    private static readonly IReadOnlyDictionary<char, char> ShiftedDubeolsikKeys =
        new Dictionary<char, char>
        {
            ['Q'] = 'ㅃ',
            ['W'] = 'ㅉ',
            ['E'] = 'ㄸ',
            ['R'] = 'ㄲ',
            ['T'] = 'ㅆ',
            ['O'] = 'ㅒ',
            ['P'] = 'ㅖ'
        };

    public static string[] SplitKoreanCharacters(string word)
    {
        if (string.IsNullOrEmpty(word))
            return Array.Empty<string>();

        List<string> jamos = new();

        foreach (char syllable in word)
        {
            if (!IsHangulSyllable(syllable))
                continue;

            // 완성형 음절의 초성·중성·종성 인덱스 계산
            int offset = syllable - HangulBase;
            int initialIndex = offset / SyllablesPerInitial;
            int medialIndex = offset % SyllablesPerInitial / FinalsPerMedial;
            int finalIndex = offset % FinalsPerMedial;

            jamos.Add(Initials[initialIndex].ToString());
            AddExpandedJamo(jamos, Medials[medialIndex], CompoundMedials);

            if (finalIndex > 0)
                AddExpandedJamo(jamos, Finals[finalIndex], CompoundFinals);
        }

        return jamos.ToArray();
    }

    public static string EnglishLetterToKoreanLetter(char key, bool isShiftPressed)
    {
        char normalizedKey = char.ToUpperInvariant(key);

        if (isShiftPressed && ShiftedDubeolsikKeys.TryGetValue(normalizedKey, out char shiftedJamo))
            return shiftedJamo.ToString();

        return DubeolsikKeys.TryGetValue(normalizedKey, out char jamo)
            ? jamo.ToString()
            : string.Empty;
    }

    private static bool IsHangulSyllable(char value)
    {
        return value >= HangulBase && value <= HangulEnd;
    }

    private static void AddExpandedJamo(
        ICollection<string> destination,
        char jamo,
        IReadOnlyDictionary<char, string> compounds)
    {
        // 복합 자모를 실제 두벌식 입력 순서로 확장
        if (compounds.TryGetValue(jamo, out string expanded))
        {
            foreach (char component in expanded)
                destination.Add(component.ToString());
            return;
        }

        destination.Add(jamo.ToString());
    }
}
