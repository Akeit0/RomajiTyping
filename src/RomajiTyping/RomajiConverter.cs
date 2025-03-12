using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RomajiTyping
{
    public sealed partial class RomajiConverter
    {
        readonly Dictionary<char, List<RomajiPair>> kanaToRomajiMap;

        readonly SimpleStringBuilder normalizedBuffer = new();

        readonly SimpleStringBuilder resultCache = new();
        readonly List<RomajiPair>[] romajiToKanaMap;


        public RomajiConverter(IEnumerable<RomajiPair> pairs, ConversionSearchMode conversionSearchMode = ConversionSearchMode.Any)
        {
            this.ConversionSearchMode = conversionSearchMode;
            romajiToKanaMap = pairsToRomajiMap(pairs);
            kanaToRomajiMap = pairsToKanaMap(pairs);
        }

        public static RomajiConverter Default { get; } = new(DefaultPairs());

        public ConversionSearchMode ConversionSearchMode { get; set; }
        public bool IsCaseSensitive { get; set; } = true;

        static List<RomajiPair>[] pairsToRomajiMap(IEnumerable<RomajiPair> pairs)
        {
            var map = new List<RomajiPair>[27];
            for (int i = 0; i < 27; i++)
            {
                map[i] = new();
            }

            foreach (var pair in pairs)
            {
                map[pair.Romaji[0] - 'a'].Add(pair);
            }

            return map;
        }

        static Dictionary<char, List<RomajiPair>> pairsToKanaMap(IEnumerable<RomajiPair> pairs)
        {
            var map = new Dictionary<char, List<RomajiPair>>();
            foreach (var pair in pairs)
            {
                var firstChar = pair.Kana[0];
                if (!map.TryGetValue(firstChar, out var list))
                {
                    list = new();
                    map[firstChar] = list;
                }

                list.Add(pair);
            }

            return map;
        }

        public static char HalfKanaToFullKana(char c)
        {
            {
                return c switch
                {
                    'ｧ' => 'ァ',
                    'ｱ' => 'ア',
                    'ｨ' => 'ィ',
                    'ｲ' => 'イ',
                    'ｩ' => 'ゥ',
                    'ｳ' => 'ウ',
                    'ｪ' => 'ェ',
                    'ｴ' => 'エ',
                    'ｫ' => 'ォ',
                    'ｵ' => 'オ',
                    'ｶ' => 'カ',
                    'ｷ' => 'キ',
                    'ｸ' => 'ク',
                    'ｹ' => 'ケ',
                    'ｺ' => 'コ',
                    'ｻ' => 'サ',
                    'ｼ' => 'シ',
                    'ｽ' => 'ス',
                    'ｾ' => 'セ',
                    'ｿ' => 'ソ',
                    'ﾀ' => 'タ',
                    'ﾁ' => 'チ',
                    'ｯ' => 'ッ',
                    'ﾂ' => 'ツ',
                    'ﾃ' => 'テ',
                    'ﾄ' => 'ト',
                    'ﾅ' => 'ナ',
                    'ﾆ' => 'ニ',
                    'ﾇ' => 'ヌ',
                    'ﾈ' => 'ネ',
                    'ﾉ' => 'ノ',
                    'ﾊ' => 'ハ',
                    'ﾋ' => 'ヒ',
                    'ﾌ' => 'フ',
                    'ﾍ' => 'ヘ',
                    'ﾎ' => 'ホ',
                    'ﾏ' => 'マ',
                    'ﾐ' => 'ミ',
                    'ﾑ' => 'ム',
                    'ﾒ' => 'メ',
                    'ﾓ' => 'モ',
                    'ｬ' => 'ャ',
                    'ﾔ' => 'ヤ',
                    'ｭ' => 'ュ',
                    'ﾕ' => 'ユ',
                    'ｮ' => 'ョ',
                    'ﾖ' => 'ヨ',
                    'ﾗ' => 'ラ',
                    'ﾘ' => 'リ',
                    'ﾙ' => 'ル',
                    'ﾚ' => 'レ',
                    'ﾛ' => 'ロ',
                    'ﾜ' => 'ワ',
                    'ﾝ' => 'ン',
                    _ => c
                };
            }
        }

        /// <summary>
        /// 文字を正規化
        /// e.g. ａｂｃｄｅ -> abcde,アイウエオ -> あいうえお
        /// </summary>
        /// <param name="c">文字</param>
        /// <param name="isCaseSensitive">大文字アルファベットを小文字にするか　e.g. A ->a </param>
        public static char Normalize(char c, bool isCaseSensitive = false)
        {
            switch (c)
            {
                case '・':
                    return '/';
                case 'ー':
                    return '-';
                case '。':
                    return '.';
                case '、':
                    return ',';
                case '＝':
                    return '=';
                case 'ｦ':
                    return 'ヲ';
                case 'ｰ':
                    return '-';
                case >= 'ァ' and <= 'ン': return (char)(c - ('ァ' - 'ぁ'));
                case >= 'ゔ' and <= 'ゖ': return (char)(c + ('ァ' - 'ぁ'));
                case >= 'ｧ'　and <= 'ﾝ': return (char)(HalfKanaToFullKana(c) - ('ァ' - 'ぁ'));
                default:
                    {
                        var halfSpaced = c is >= '！' and <= '￦' ? (char)(c - ('！' - '!')) :　c;
                        if (isCaseSensitive) return halfSpaced;
                        return halfSpaced switch
                        {
                            >= 'A' and <= 'Z' => (char)(halfSpaced + 0x20),
                            _ => halfSpaced
                        };
                    }
            }
        }

        /// <summary>
        /// <inheritdoc cref="Normalize(char,bool)"/>
        /// <see cref="Normalize(char,bool)"/>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="result"></param>
        /// <param name="isCaseSensitive"></param>
        public static void Normalize(ReadOnlySpan<char> input, SimpleStringBuilder result, bool isCaseSensitive = false)
        {
            result.Clear();
            char last = default;
            foreach (var c in input)
            {
                if (c is 'ﾞ' or 'ﾟ')
                {
                    result[^1] = (char)(last + (c == 'ﾞ' ? 1 : 2));
                    continue;
                }

                result.Add(last = Normalize(c, isCaseSensitive));
            }
        }

        /// <summary>
        /// ローマ字をひらがなに変換するペアデータを取得
        /// </summary>
        /// <param name="romaji">ローマ字</param>
        ///  <param name="result">取得したペア</param>
        public bool TryGetPairFromRomaji(ReadOnlySpan<char> romaji, [NotNullWhen(true)] out RomajiPair? result)
        {
            result = null;
            if (romaji.Length == 0)
                return false;
            var firstChar = romaji[0];
            if (firstChar is >= 'a' and <= 'z')
            {
                foreach (var pair in romajiToKanaMap[firstChar - 'a'])
                {
                    if (pair.IsAvailable(ConversionSearchMode) && pair.MatchRomaji(romaji))
                    {
                        result = pair;
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// ローマ字をひらがなに変換
        /// </summary>
        /// <param name="input">ローマ字での入力</param>
        /// <param name="result">結果の格納</param>
        /// <param name="normalize">文字列を正規化 <see cref="Normalize(char,bool)"/> </param>
        /// <returns></returns>
        public int Convert(ReadOnlySpan<char> input, SimpleStringBuilder result, bool normalize = true)
        {
            result.Clear();
            if (normalize)
            {
                Normalize(input, normalizedBuffer, IsCaseSensitive);
                input = normalizedBuffer.AsSpan();
            }

            var remainCount = input.Length;
            // 未変換の文字数
            var lastUnMatch = 0;
            while (0 < remainCount)
            {
                var start = input.Length - remainCount;
                if (TryGetPairFromRomaji(input.Slice(start, remainCount), out var pair))
                {
                    foreach (var c in pair.Kana)
                    {
                        result.Add(c);
                    }

                    lastUnMatch = 0;
                    remainCount -= pair.Romaji.Length;
                    continue;
                }

                result.Add(input[start]);
                remainCount--;
                lastUnMatch++;
            }

            return lastUnMatch;
        }

        /// <summary>
        /// ローマ字をひらがなに変換
        /// </summary>
        /// <param name="input">ローマ字での入力、それ以外はそのまま出力</param>
        /// <returns></returns>
        public string Convert(ReadOnlySpan<char> input)
        {
            var result = resultCache;
            result.Clear();
            Convert(input, result);

            return string.Create(result.Count, result, (span, list) =>
            {
                list.AsSpan().CopyTo(span);
                list.Clear();
            });
        }

        /// <summary>
        /// 最も一致するペアを取得
        /// 優先度 -> ローマ字の長さ > かなの長さ の順で比較
        /// </summary>
        /// <param name="target">変換語の文字列</param>
        /// <param name="currentInput">未変換の入力中文字列</param>
        /// <param name="bestPair">最適なペアデータ</param>
        /// <returns></returns>
        public bool TryGetBestPairFromConverted(ReadOnlySpan<char> target, ReadOnlySpan<char> currentInput, [NotNullWhen(true)] out RomajiPair? bestPair)
        {
            bestPair = null;
            if (target.Length == 0)
                return false;
            var firstChar = target[0];
            if (kanaToRomajiMap.TryGetValue(firstChar, out var pairs))
            {
                foreach (var pair in pairs)
                {
                    if (pair.IsAvailable(ConversionSearchMode) && pair.MachHiragana(target))
                    {
                        var kana = pair.Kana;
                        if (target.Length < kana.Length)
                        {
                            continue;
                        }

                        if (0 < currentInput.Length)
                        {
                            var romaji = pair.Romaji.AsSpan();
                            if (!romaji.StartsWith(currentInput))
                            {
                                continue;
                            }
                        }

                        if (bestPair is null || bestPair.CompareTo(pair) < 0)
                        {
                            bestPair = pair;
                        }
                    }
                }
            }

            return bestPair is not null;
        }

        /// <summary>
        ///  最適なパスを取得
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <param name="target">変換後文字列</param>
        /// <param name="result">最適な残りの入力</param>
        /// <param name="normalize">inputとtargetを正規化するか <see cref="Normalize(char,bool)"/>></param>
        /// <returns></returns>
        public bool GetBestPath(ReadOnlySpan<char> input, ReadOnlySpan<char> target, SimpleStringBuilder result, bool normalize = true)
        {
            result.Clear();
            var current = resultCache;
            current.Clear();
            var lastUnMatch = Convert(input, current, normalize);
            if (current.Length - lastUnMatch > target.Length)
                return false;
            var converted = current.AsSpan()[..^lastUnMatch];

            if (normalize)
            {
                Normalize(target, normalizedBuffer, IsCaseSensitive);
                target = normalizedBuffer.AsSpan();
            }

            // 入力がターゲットの先頭と一致しない場合は失敗
            if (!target.StartsWith(converted))
            {
                return false;
            }

            // 未入力の部分
            var remainingTarget = target[converted.Length..];
            //　未変換の部分
            ReadOnlySpan<char> remainingInput = current.AsSpan()[^lastUnMatch..];

            // 未変換の部分がターゲットの残りの最初と一致する場合
            while (0 < remainingInput.Length && 0 < remainingTarget.Length && remainingInput[0] == remainingTarget[0])
            {
                remainingInput = remainingInput[1..];
                remainingTarget = remainingTarget[1..];
            }

            if (remainingTarget.Length == 0)
            {
                return remainingInput.Length == 0;
            }

            while (0 < remainingTarget.Length)
            {
                var firstChar = remainingTarget[0];
                if (TryGetBestPairFromConverted(remainingTarget, remainingInput, out var bestPair))
                {
                    var romaji = bestPair.Romaji.AsSpan();
                    if (remainingInput.Length != 0)
                    {
                        romaji = romaji[remainingInput.Length..];

                        remainingInput = default;
                    }

                    result.AddRange(romaji);

                    remainingTarget = remainingTarget[bestPair.Kana.Length..];
                }
                else
                {
                    if (remainingInput.Length != 0)
                    {
                        //未変換のものが残っていて、それと一致しない場合は失敗
                        if (remainingInput[0] != firstChar)
                        {
                            Console.WriteLine("Failed" + remainingInput.ToString() + " " + remainingInput.ToString());
                            return false;
                        }

                        remainingInput = remainingInput[1..];
                    }
                    else
                    {
                        // 有効なASCIIでない場合は失敗
                        if (firstChar is < '!' or > '~')
                        {
                            Console.WriteLine("Failed" + remainingInput.ToString());
                            return false;
                        }

                        result.Add(firstChar);
                    }

                    remainingTarget = remainingTarget[1..];
                }
            }

            return true;
        }
    }
}