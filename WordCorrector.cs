using System.Text;

namespace PuntoSwitch;

internal sealed class WordCorrector
{
    private readonly HashSet<string> _ru;
    private readonly HashSet<string> _en;

    public WordCorrector(string dataDirectory)
    {
        _ru = LoadWords(Path.Combine(dataDirectory, "ru.txt"), RussianDefaults);
        _en = LoadWords(Path.Combine(dataDirectory, "en.txt"), EnglishDefaults);
    }

    public string? TryCorrect(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2) return null;
        var lower = word.ToLowerInvariant();
        if (_ru.Contains(lower) || _en.Contains(lower)) return null;

        var toRussian = ConvertLayout(word, toRussian: true);
        var toEnglish = ConvertLayout(word, toRussian: false);
        bool ruCandidate = _ru.Contains(toRussian.ToLowerInvariant());
        bool enCandidate = _en.Contains(toEnglish.ToLowerInvariant());

        if (ruCandidate && !enCandidate) return MatchCase(word, toRussian);
        if (enCandidate && !ruCandidate) return MatchCase(word, toEnglish);
        return null;
    }

    private static string ConvertLayout(string input, bool toRussian)
    {
        const string en = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?";
        const string ru = "ёйцукенгшщзхъ\фывапролджэячсмитьбю.ЁЙЦУКЕНГШЩЗХЪ/ФЫВАПРОЛДЖЭЯЧСМИТЬБЮ,";
        var from = toRussian ? en : ru;
        var to = toRussian ? ru : en;
        var result = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            int index = from.IndexOf(c);
            result.Append(index >= 0 && index < to.Length ? to[index] : c);
        }
        return result.ToString();
    }

    private static string MatchCase(string original, string replacement)
    {
        if (original.ToUpperInvariant() == original) return replacement.ToUpperInvariant();
        if (char.IsUpper(original[0])) return char.ToUpperInvariant(replacement[0]) + replacement[1..];
        return replacement;
    }

    private static HashSet<string> LoadWords(string path, string defaults)
    {
        var words = new HashSet<string>(defaults.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        if (File.Exists(path))
            foreach (var line in File.ReadLines(path))
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                    words.Add(line.Trim());
        return words;
    }

    private const string RussianDefaults = "и в не на я быть он с что а это как по к для но они мы вы из у за от до так его ее уже если да нет бы все был была были есть будет можно нужно где когда кто почему потому привет здравствуйте спасибо пожалуйста сегодня завтра вчера хорошо отлично работа программа текст слово язык русский английский переключить раскладка компьютер клавиатура";
    private const string EnglishDefaults = "the be to of and a in that have I it for not on with he as you do at this but his by from they we say her she or an will my one all would there their what so up out if about who get which go me hello world thanks please today tomorrow yesterday good great work program text word language russian english switch layout computer keyboard";
}
