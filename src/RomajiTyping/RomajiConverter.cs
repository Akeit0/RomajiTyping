using System;
using System.Collections.Generic;

namespace RomajiTyping
{
    public sealed class RomajiConverter
    {
        readonly ConversionSearchMode defaultConversionSearchMode;
        readonly List<RomajiPair>[] romajiToKanaMap;
        readonly Dictionary<char, List<RomajiPair>> kanaToRomajiMap;

        readonly SimpleStringBuilder normalizedBuffer = new();

        readonly SimpleStringBuilder resultCache = new();

        static List<RomajiPair>[] ElementsToRomajiMap(IEnumerable<RomajiPair> elements)
        {
            var map = new List<RomajiPair>[27];
            for (int i = 0; i < 27; i++)
            {
                map[i] = new();
            }

            foreach (var element in elements)
            {
                map[element.Romaji[0] - 'a'].Add(element);
            }

            return map;
        }

        static Dictionary<char, List<RomajiPair>> ElementsToKanaMap(IEnumerable<RomajiPair> elements)
        {
            var map = new Dictionary<char, List<RomajiPair>>();
            foreach (var element in elements)
            {
                var firstChar = element.Kana[0];
                if (!map.TryGetValue(firstChar, out var list))
                {
                    list = new();
                    map[firstChar] = list;
                }

                list.Add(element);
            }

            return map;
        }

        static IEnumerable<RomajiPair> GetSimpleOnes()
        {
            return new[]
            {
                new RomajiPair("la", "ぁ"),
                new("xa", "ぁ"),
                new("a", "あ"),
                new("li", "ぃ"),
                new("lyi", "ぃ"),
                new("xi", "ぃ"),
                new("xyi", "ぃ"),
                new("i", "い"),
                new("yi", "い"),
                new("ye", "いぇ"),
                new("lu", "ぅ"),
                new("xu", "ぅ"),
                new("u", "う"),
                new("whu", "う"),
                new("wu", "う"),
                new("wha", "うぁ"),
                new("wi", "うぃ"),
                new("we", "うぇ"),
                new("whe", "うぇ"),
                new("who", "うぉ"),
                new("le", "ぇ"),
                new("lye", "ぇ"),
                new("xe", "ぇ"),
                new("xye", "ぇ"),
                new("e", "え"),
                new("lo", "ぉ"),
                new("xo", "ぉ"),
                new("o", "お"),
                new("ka", "か"),
                new("ca", "か"),
                new("ga", "が"),
                new("ki", "き"),
                new("kyi", "きぃ"),
                new("kye", "きぇ"),
                new("kya", "きゃ"),
                new("kyu", "きゅ"),
                new("kyo", "きょ"),
                new("gi", "ぎ"),
                new("gyi", "ぎぃ"),
                new("gye", "ぎぇ"),
                new("gya", "ぎゃ"),
                new("gyu", "ぎゅ"),
                new("gyo", "ぎょ"),
                new("ku", "く"),
                new("cu", "く"),
                new("qu", "く"),
                new("kwa", "くぁ"),
                new("qa", "くぁ"),
                new("qwa", "くぁ", ConversionMode.MS),
                new("kwa", "くぁ", ConversionMode.Google),
                new("qi", "くぃ"),
                new("qwi", "くぃ", ConversionMode.MS),
                new("qyi", "くぃ", ConversionMode.MS),
                new("kwi", "くぃ", ConversionMode.Google),
                new("qwu", "くぅ"),
                new("kwu", "くぅ", ConversionMode.Google),
                new("qe", "くぇ"),
                new("qwe", "くぇ", ConversionMode.MS),
                new("qye", "くぇ", ConversionMode.MS),
                new("kwe", "くぇ", ConversionMode.Google),
                new("qo", "くぉ"),
                new("qwo", "くぉ", ConversionMode.MS),
                new("kwo", "くぉ", ConversionMode.Google),
                new("qya", "くゃ", ConversionMode.MS),
                new("qyu", "くゅ", ConversionMode.MS),
                new("qyo", "くょ", ConversionMode.MS),
                new("gu", "ぐ"),
                new("gwa", "ぐぁ"),
                new("gwi", "ぐぃ"),
                new("gwu", "ぐぅ"),
                new("gwe", "ぐぇ"),
                new("gwo", "ぐぉ"),
                new("ke", "け"),
                new("ge", "げ"),
                new("ko", "こ"),
                new("co", "こ"),
                new("go", "ご"),
                new("sa", "さ"),
                new("za", "ざ"),
                new("shi", "し"),
                new("si", "し"),
                new("ci", "し"),
                new("syi", "しぃ"),
                new("sha", "しゃ"),
                new("sya", "しゃ"),
                new("shu", "しゅ"),
                new("syu", "しゅ"),
                new("she", "しぇ"),
                new("sye", "しぇ"),
                new("syo", "しょ"),
                new("sho", "しょ"),
                new("ji", "じ"),
                new("zi", "じ"),
                new("jyi", "じぃ"),
                new("zyi", "じぃ"),
                new("je", "じぇ"),
                new("jye", "じぇ"),
                new("zye", "じぇ"),
                new("ja", "じゃ"),
                new("jya", "じゃ"),
                new("zya", "じゃ"),
                new("ju", "じゅ"),
                new("jyu", "じゅ"),
                new("zyu", "じゅ"),
                new("jo", "じょ"),
                new("jyo", "じょ"),
                new("zyo", "じょ"),
                new("su", "す"),
                new("swa", "すぁ"),
                new("swi", "すぃ"),
                new("swu", "すぅ"),
                new("swe", "すぇ"),
                new("swo", "すぉ"),
                new("zu", "ず"),
                new("se", "せ"),
                new("ce", "せ"),
                new("ze", "ぜ"),
                new("so", "そ"),
                new("zo", "ぞ"),
                new("ta", "た"),
                new("da", "だ"),
                new("chi", "ち"),
                new("ti", "ち"),
                new("cyi", "ちぃ"),
                new("tyi", "ちぃ"),
                new("che", "ちぇ"),
                new("cye", "ちぇ"),
                new("tye", "ちぇ"),
                new("cha", "ちゃ"),
                new("tya", "ちゃ"),
                new("chu", "ちゅ"),
                new("cyu", "ちゅ"),
                new("tyu", "ちゅ"),
                new("cho", "ちょ"),
                new("cyo", "ちょ"),
                new("tyo", "ちょ"),
                new("di", "ぢ"),
                new("dyi", "ぢぃ"),
                new("dye", "ぢぇ"),
                new("dya", "ぢゃ"),
                new("dyu", "ぢゅ"),
                new("dyo", "ぢょ"),
                new("ltsu", "っ"),
                new("xtu", "っ"),
                new("tsu", "つ"),
                new("tu", "つ"),
                new("tsa", "つぁ"),
                new("tsi", "つぃ"),
                new("tse", "つぇ"),
                new("tso", "つぉ"),
                new("du", "づ"),
                new("te", "て"),
                new("thi", "てぃ"),
                new("the", "てぇ"),
                new("tha", "てゃ"),
                new("thu", "てゅ"),
                new("tho", "てょ"),
                new("de", "で"),
                new("dhi", "でぃ"),
                new("dhe", "でぇ"),
                new("dha", "でゃ"),
                new("dhu", "でゅ"),
                new("dho", "でょ"),
                new("to", "と"),
                new("twa", "とぁ"),
                new("twi", "とぃ"),
                new("twu", "とぅ"),
                new("t'u", "とぅ", ConversionMode.Google),
                new("twe", "とぇ"),
                new("two", "とぉ"),
                new("do", "ど"),
                new("dwa", "どぁ"),
                new("dwi", "どぃ"),
                new("dwu", "どぅ"),
                new("d'u", "どぅ", ConversionMode.Google),
                new("dwe", "どぇ"),
                new("dwo", "どぉ"),
                new("na", "な"),
                new("ni", "に"),
                new("nyi", "にぃ"),
                new("nye", "にぇ"),
                new("nya", "にゃ"),
                new("nyu", "にゅ"),
                new("nyo", "にょ"),
                new("nu", "ぬ"),
                new("ne", "ね"),
                new("no", "の"),
                new("ha", "は"),
                new("ba", "ば"),
                new("pa", "ぱ"),
                new("hi", "ひ"),
                new("hyi", "ひぃ"),
                new("hye", "ひぇ"),
                new("hya", "ひゃ"),
                new("hyu", "ひゅ"),
                new("hyo", "ひょ"),
                new("bi", "び"),
                new("byi", "びぃ"),
                new("bye", "びぇ"),
                new("bya", "びゃ"),
                new("byu", "びゅ"),
                new("byo", "びょ"),
                new("pi", "ぴ"),
                new("pyi", "ぴぃ"),
                new("pye", "ぴぇ"),
                new("pya", "ぴゃ"),
                new("pyu", "ぴゅ"),
                new("pyo", "ぴょ"),
                new("fu", "ふ"),
                new("hu", "ふ"),
                new("fa", "ふぁ"),
                new("fwa", "ふぁ"),
                new("fi", "ふぃ"),
                new("fwi", "ふぃ"),
                new("fyi", "ふぃ"),
                new("fwu", "ふぅ"),
                new("fe", "ふぇ"),
                new("fo", "ふぉ"),
                new("fwo", "ふぉ"),
                new("fya", "ふゃ"),
                new("fyu", "ふゅ"),
                new("fyo", "ふょ"),
                new("bu", "ぶ"),
                new("pu", "ぷ"),
                new("he", "へ"),
                new("be", "べ"),
                new("pe", "ぺ"),
                new("ho", "ほ"),
                new("bo", "ぼ"),
                new("po", "ぽ"),
                new("ma", "ま"),
                new("mi", "み"),
                new("myi", "みぃ"),
                new("mye", "みぇ"),
                new("mya", "みゃ"),
                new("myu", "みゅ"),
                new("myo", "みょ"),
                new("mu", "む"),
                new("me", "め"),
                new("mo", "も"),
                new("lya", "ゃ"),
                new("xya", "ゃ"),
                new("ya", "や"),
                new("lyu", "ゅ"),
                new("xyu", "ゅ"),
                new("yu", "ゆ"),
                new("lyo", "ょ"),
                new("xyo", "ょ"),
                new("yo", "よ"),
                new("ra", "ら"),
                new("ri", "り"),
                new("ryi", "りぃ"),
                new("rye", "りぇ"),
                new("rya", "りゃ"),
                new("ryu", "りゅ"),
                new("ryo", "りょ"),
                new("ru", "る"),
                new("re", "れ"),
                new("ro", "ろ"),
                new("lwa", "ゎ"),
                new("xwa", "ゎ"),
                new("wa", "わ"),
                new("wyi", "ゐ"),
                new("wye", "ゑ"),
                new("wo", "を"),
                new("nn", "ん"),
                new("xn", "ん"),
                new("n'", "ん"),
                new("vu", "ヴ"),
                new("va", "ヴぁ"),
                new("vi", "ヴぃ"),
                new("vyi", "ヴぃ"),
                new("ve", "ヴぇ"),
                new("vye", "ヴぇ"),
                new("vo", "ヴぉ"),
                new("vya", "ヴゃ"),
                new("vyu", "ヴゅ"),
                new("vyo", "ヴょ"),
                new("lka", "ヵ"),
                new("xka", "ヵ"),
                new("lke", "ヶ"),
                new("xke", "ヶ"),
            };
        }


        static IEnumerable<RomajiPair> DefaultAll()
        {
            yield return new("n", "ん");
            foreach (var pair in GetSimpleOnes())
            {
                yield return pair;
                var romaji = pair.Romaji;
                var hiragana = pair.Kana;
                yield return new(romaji, hiragana);

                if (romaji[0] is not ('a' or 'i' or 'u' or 'e' or 'o'))
                {
                    yield return pair with { Romaji = romaji[0] + romaji, Kana = "っ" + hiragana };
                }
            }
        }

        public static RomajiConverter Default { get; } = new(DefaultAll());

        public ConversionSearchMode DefaultConversionSearchMode => defaultConversionSearchMode;


        public RomajiConverter(IEnumerable<RomajiPair> elements, ConversionSearchMode defaultConversionSearchMode = ConversionSearchMode.Any)
        {
            this.defaultConversionSearchMode = defaultConversionSearchMode;
            romajiToKanaMap = ElementsToRomajiMap(elements);
            kanaToRomajiMap = ElementsToKanaMap(elements);
        }

        public char Normalize(char c)
        {
            switch (c)
            {
                case '・':
                    return '･';
                case 'ー':
                    return '-';
                case '。':
                    return '.';
                case '、':
                    return ',';
                case '＝':
                    return '=';
                case >= 'ァ' and <= 'ン': return (char)(c - 0x60);
                case >= 'ゔ' and <= 'ゖ': return (char)(c + 0x60);
                default:
                    {
                        var halfSpaced = c is >= '！' and <= '￦' ? (char)(c - 0xfee0) :　c;
                        return halfSpaced switch
                        {
                            >= 'A' and <= 'Z' => (char)(c + 0x20),
                            _ => c
                        };
                    }
            }
        }

        public void Normalize(ReadOnlySpan<char> text, SimpleStringBuilder result)
        {
            result.Clear();
            foreach (var c in text)
            {
                result.Add(Normalize(c));
            }
        }

        public int Convert(ReadOnlySpan<char> text, SimpleStringBuilder result, bool normalize = true, bool normalizeLowerRemainder = false, ConversionSearchMode? mode = null)
        {
            var searchMode = mode ?? defaultConversionSearchMode;
            result.Clear();
            ReadOnlySpan<char> normalizedText = text;
            if (normalize)
            {
                Normalize(text, normalizedBuffer);
                normalizedText = normalizedBuffer.AsSpan();
            }

            var remainCount = text.Length;
            var lastUnMatch = 0;
            while (0 < remainCount)
            {
                var start = text.Length - remainCount;
                var firstChar = normalizedText[start];
                if (firstChar is >= 'a' and <= 'z')
                {
                    foreach (var element in romajiToKanaMap[firstChar - 'a'])
                    {
                        if (element.MatchRomaji(normalizedText.Slice(start, remainCount),searchMode))
                        {
                            foreach (var c in element.Kana)
                            {
                                result.Add(c);
                            }

                            lastUnMatch = 0;
                            remainCount -= element.ConsumeCount;
                            break;
                        }
                    }
                }


                if (remainCount == 0) return lastUnMatch;
                if ((start + remainCount) != text.Length) continue;
                result.Add(normalizeLowerRemainder ? normalizedText[start] : text[start]);
                remainCount--;
                lastUnMatch++;
            }

            return lastUnMatch;
        }

        public string Convert(ReadOnlySpan<char> text)
        {
            var result = resultCache;
            result.Clear();
            Convert(text, result);

            return string.Create(result.Count, result, (span, list) =>
            {
                list.AsSpan().CopyTo(span);
                list.Clear();
            });
        }


        public bool GetBestPath(ReadOnlySpan<char> text, ReadOnlySpan<char> target, SimpleStringBuilder result, bool normalize = true, ConversionSearchMode? mode = null)
        {
            var searchMode = mode ?? defaultConversionSearchMode;
            result.Clear();
            var current = resultCache;
            current.Clear();
            var lastUnMatch = Convert(text, current);
            if (current.Length - lastUnMatch > target.Length)
                return false;
            var converted = current.AsSpan()[..^lastUnMatch];

            if (normalize)
            {
                Normalize(target, normalizedBuffer);
                target = normalizedBuffer.AsSpan();
            }

            for (int i = 0; i < converted.Length; i++)
            {
                if (target[i] != converted[i])
                    return false;
            }

            var remainingTarget = target[converted.Length..];
            ReadOnlySpan<char> currentInput = current.AsSpan()[^lastUnMatch..];

            if (remainingTarget.Length == 0)
            {
                return currentInput.Length == 0;
            }

            while (0 < remainingTarget.Length)
            {
                var firstChar = remainingTarget[0];
                if (kanaToRomajiMap.TryGetValue(firstChar, out var elements))
                {
                    RomajiPair? bestElement = null;
                    foreach (var element in elements)
                    {
                        if (element.MachHiragana(remainingTarget, searchMode))
                        {
                            var kana = element.Kana;
                            if (remainingTarget.Length < kana.Length)
                            {
                                continue;
                            }

                            if (0 < currentInput.Length)
                            {
                                var romaji = element.Romaji.AsSpan();
                                if (!romaji.StartsWith(currentInput))
                                {
                                    continue;
                                }
                            }

                            if (bestElement is null || bestElement.CompareTo(element) > 0)
                            {
                                bestElement = element;
                            }
                        }
                    }


                    if (bestElement is null) return false;

                    {
                        var romaji = bestElement.Romaji.AsSpan();
                        if (currentInput.Length != 0)
                        {
                            romaji = romaji[currentInput.Length..];
                            currentInput = default;
                        }

                        result.AddRange(romaji);

                        remainingTarget = remainingTarget[bestElement.Kana.Length..];
                    }
                }
                else
                {
                    result.Add(remainingTarget[0]);
                    remainingTarget = remainingTarget[1..];
                }
            }

            return true;
        }
    }
}