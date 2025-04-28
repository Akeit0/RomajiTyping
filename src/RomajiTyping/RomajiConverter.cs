using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RomajiTyping.Internal;

namespace RomajiTyping
{
    public sealed class RomajiConverter
    {
        public RomajiConverter(IEnumerable<ConversionRuleElement> conversionRules)
        {
            var array = conversionRules.ToArray();

            conversionRuleSet = new ConversionRuleSet(array);
            inversionRuleSet = new InversionRuleSet(conversionRuleSet);
        }

        readonly ConversionRuleSet conversionRuleSet;
        readonly InversionRuleSet inversionRuleSet;


        readonly Pool<ReversedStack<char>> reversedCharStackPool = new(() => new(), x => x.Clear());
        readonly Pool<SimpleList<char>> stringBuilderPool = new(() => new(), x => x.Clear());
        readonly Pool<SimpleList<ConversionRule>> rulesPool = new(() => new(), x => x.Clear());

        public InversionRuleSet InversionRuleSet => inversionRuleSet;
        public Pool<ReversedStack<char>> ReversedCharStackPool => reversedCharStackPool;
        public Pool<SimpleList<char>> StringBuilderPool => stringBuilderPool;

        //
        // public bool IsCaseSensitive { get; set; } = true;
        //
        // public static char HalfKanaToFullKana(char c)
        // {
        //     {
        //         return c switch
        //         {
        //             'ｧ' => 'ァ',
        //             'ｱ' => 'ア',
        //             'ｨ' => 'ィ',
        //             'ｲ' => 'イ',
        //             'ｩ' => 'ゥ',
        //             'ｳ' => 'ウ',
        //             'ｪ' => 'ェ',
        //             'ｴ' => 'エ',
        //             'ｫ' => 'ォ',
        //             'ｵ' => 'オ',
        //             'ｶ' => 'カ',
        //             'ｷ' => 'キ',
        //             'ｸ' => 'ク',
        //             'ｹ' => 'ケ',
        //             'ｺ' => 'コ',
        //             'ｻ' => 'サ',
        //             'ｼ' => 'シ',
        //             'ｽ' => 'ス',
        //             'ｾ' => 'セ',
        //             'ｿ' => 'ソ',
        //             'ﾀ' => 'タ',
        //             'ﾁ' => 'チ',
        //             'ｯ' => 'ッ',
        //             'ﾂ' => 'ツ',
        //             'ﾃ' => 'テ',
        //             'ﾄ' => 'ト',
        //             'ﾅ' => 'ナ',
        //             'ﾆ' => 'ニ',
        //             'ﾇ' => 'ヌ',
        //             'ﾈ' => 'ネ',
        //             'ﾉ' => 'ノ',
        //             'ﾊ' => 'ハ',
        //             'ﾋ' => 'ヒ',
        //             'ﾌ' => 'フ',
        //             'ﾍ' => 'ヘ',
        //             'ﾎ' => 'ホ',
        //             'ﾏ' => 'マ',
        //             'ﾐ' => 'ミ',
        //             'ﾑ' => 'ム',
        //             'ﾒ' => 'メ',
        //             'ﾓ' => 'モ',
        //             'ｬ' => 'ャ',
        //             'ﾔ' => 'ヤ',
        //             'ｭ' => 'ュ',
        //             'ﾕ' => 'ユ',
        //             'ｮ' => 'ョ',
        //             'ﾖ' => 'ヨ',
        //             'ﾗ' => 'ラ',
        //             'ﾘ' => 'リ',
        //             'ﾙ' => 'ル',
        //             'ﾚ' => 'レ',
        //             'ﾛ' => 'ロ',
        //             'ﾜ' => 'ワ',
        //             'ﾝ' => 'ン',
        //             _ => c
        //         };
        //     }
        // }
        //
        // public static char AddVoiceSound(char c, bool isSemi)
        // {
        //     return (c, isSemi) switch
        //     {
        //         ('う', false) => 'ヴ',
        //         ('か', false) => 'が',
        //         ('き', false) => 'ぎ',
        //         ('く', false) => 'ぐ',
        //         ('け', false) => 'げ',
        //         ('こ', false) => 'ご',
        //         ('さ', false) => 'ざ',
        //         ('し', false) => 'じ',
        //         ('す', false) => 'ず',
        //         ('せ', false) => 'ぜ',
        //         ('そ', false) => 'ぞ',
        //         ('た', false) => 'だ',
        //         ('ち', false) => 'ぢ',
        //         ('つ', false) => 'づ',
        //         ('て', false) => 'で',
        //         ('と', false) => 'ど',
        //         ('は', false) => 'ば',
        //         ('ひ', false) => 'び',
        //         ('ふ', false) => 'ぶ',
        //         ('へ', false) => 'べ',
        //         ('ほ', false) => 'ぼ',
        //         ('は', true) => 'ぱ',
        //         ('ひ', true) => 'ぴ',
        //         ('ふ', true) => 'ぷ',
        //         ('へ', true) => 'ぺ',
        //         ('ほ', true) => 'ぽ',
        //         ('わ', false) => 'ヷ',
        //         ('ゐ', false) => 'ヸ',
        //         ('ゑ', false) => 'ヹ',
        //         ('を', false) => 'ヺ',
        //         _ => throw new InvalidOperationException("Invalid Voice Sound")
        //     };
        // }
        //
        //
        // /// <summary>
        // /// 文字を正規化
        // /// e.g. ａｂｃｄｅ -> abcde,アイウエオ -> あいうえお
        // /// </summary>
        // /// <param name="c">文字</param>
        // /// <param name="isCaseSensitive">大文字アルファベットを小文字にするか　e.g. A ->a </param>
        // public static char Normalize(char c, bool isCaseSensitive = false)
        // {
        //     switch (c)
        //     {
        //         case '・': return '/';
        //         case 'ー': return '-';
        //         case '「': return '[';
        //         case '」': return ']';
        //         case '。': return '.';
        //         case '、': return ',';
        //         case '＝': return '=';
        //         case 'ｦ': return 'ヲ';
        //         case '￥': return '\\';
        //         case 'ｰ': return '-';
        //         case >= 'ァ' and <= 'ン': return (char)(c - ('ァ' - 'ぁ'));
        //         case >= 'ゔ' and <= 'ゖ': return (char)(c + ('ァ' - 'ぁ'));
        //         case >= 'ｧ'　and <= 'ﾝ': return (char)(HalfKanaToFullKana(c) - ('ァ' - 'ぁ'));
        //         default:
        //         {
        //             var halfSpaced = c is >= '！' and <= '￦' ? (char)(c - ('！' - '!')) :　c;
        //             if (isCaseSensitive) return halfSpaced;
        //             return halfSpaced switch
        //             {
        //                 >= 'A' and <= 'Z' => (char)(halfSpaced + 0x20),
        //                 _ => halfSpaced
        //             };
        //         }
        //     }
        // }


        /// <summary>
        /// ローマ字をひらがなに変換
        /// </summary>
        /// <param name="input">ローマ字での入力</param>
        /// <param name="result">結果の格納</param>
        /// <returns></returns>
        public int Convert(ReadOnlySpan<char> input, SimpleList<char> result)
        {
            result.Clear();
            using var inputBufferLease = reversedCharStackPool.Get();
            var inputBuffer = inputBufferLease.Value;
            inputBuffer.Clear();
            inputBuffer.Push(input);
            conversionRuleSet.Convert(inputBuffer, result, null);
            return inputBuffer.Length;
        }

        /// <summary>
        /// ローマ字をひらがなに変換
        /// </summary>
        /// <param name="input">ローマ字での入力、それ以外はそのまま出力</param>
        /// <returns></returns>
        public string Convert(ReadOnlySpan<char> input)
        {
            using var resultLease = stringBuilderPool.Get();
            var result = resultLease.Value;
            result.Clear();
            var remain = Convert(input, result);

            return string.Create(result.Count, result, static (span, list) =>
            {
                list.AsSpan().CopyTo(span);
                list.Clear();
            });
        }

        /// <summary>
        ///  最適なパスを取得
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <param name="target">変換後文字列</param>
        /// <param name="result">最適な残りの入力</param>
        /// <param name="currentTargetMatchCount"></param>
        /// <returns></returns>
        public bool GetBestPath(ReadOnlySpan<char> input, ReadOnlySpan<char> target, ReversedStack<char> result, out int currentTargetMatchCount)
        {
            result.Clear();
            if (input.IsEmpty)
            {
                if (inversionRuleSet.TryFindShortestInput(input, target, result))
                {
                    currentTargetMatchCount = target.Length;
                    return true;
                }

                currentTargetMatchCount = 0;
                return false;
            }

            using var normalizedBufferLease = stringBuilderPool.Get();
            using var consumeBufferLease = reversedCharStackPool.Get();
            normalizedBufferLease.Value.Clear();
            consumeBufferLease.Value.Clear();
            consumeBufferLease.Value.Push(input);
            {
                using var applyRuleLease = rulesPool.Get();
                conversionRuleSet.Convert(consumeBufferLease.Value, normalizedBufferLease.Value, null);
                currentTargetMatchCount = normalizedBufferLease.Value.Length;
                if (!target.StartsWith(normalizedBufferLease.Value.AsSpan()))
                {
                    return false;
                }

                target = target[normalizedBufferLease.Value.Length..];
            }


            // 未入力の部分
            var remainingTarget = target;
            //　未変換の部分
            ReadOnlySpan<char> currentRemainingInput = consumeBufferLease.Value.AsSpan();
            result.Clear();
            if (remainingTarget.Length == 0)
            {
                return currentRemainingInput.Length == 0;
            }

            if (inversionRuleSet.TryFindShortestInput(currentRemainingInput, remainingTarget, result))
            {
                result.Pop(currentRemainingInput.Length);
                return true;
            }

            return false;
        }
    }
}