using RomajiTyping.Internal;

namespace RomajiTyping;

public sealed class ConversionRuleSet
{
    internal readonly CharKeyFrozenDictionary<List<ConversionRule>> Map;

    public ConversionRuleSet(IEnumerable<ConversionRuleElement> rules)
    {
        var romajiToKanaMapBuilder = new CharKeyFrozenDictionary<List<ConversionRule>>.Builder();
        var index = 0;
        foreach (var rule in rules)
        {
            var firstChar = rule.From[0];
            ref var list = ref romajiToKanaMapBuilder.GetValueRefOrAddDefault(firstChar, out _);
            list ??= new();
            list.Add(new(rule, index++));
        }

        Map = romajiToKanaMapBuilder.Build();


        foreach (var pair in Map)
        {
            foreach (var rule in pair.Value)
            {
                bool isNextProhibitedCharsBuilderCreated = false;
                CharKeyFrozenDictionary<ulong>.Builder nextProhibitedCharsBuilder = default;
                foreach (var otherRule in pair.Value)
                {
                    if (otherRule.From.Length > rule.From.Length && otherRule.From.StartsWith(rule.From))
                    {
                        if (!isNextProhibitedCharsBuilderCreated)
                        {
                            nextProhibitedCharsBuilder = new();
                        }

                        SparseUShortBitSet.Set(ref nextProhibitedCharsBuilder, otherRule.From[rule.From.Length]);
                        isNextProhibitedCharsBuilderCreated = true;
                    }
                }

                if (isNextProhibitedCharsBuilderCreated)
                {
                    rule.NextProhibitedChars = new(nextProhibitedCharsBuilder.Build());
                }
            }
        }
    }

    ConversionRule? GetPairFromRomaji(ReadOnlySpan<char> input)
    {
        if (input.Length == 0)
            return null;
        var c = input[0];
        if (Map.TryGetValue(c, out var pairs))
        {
            ConversionRule? bestPair = null;
            foreach (var pair in pairs)
            {
                if (!input.StartsWith(pair.From)) continue;
                bestPair = pair;
                if (pair.NextProhibitedChars is null)
                {
                    break;
                }
            }

            if (bestPair is not null && (bestPair.NextProhibitedChars is null || (input.Length > 1 && !bestPair.NextProhibitedChars.IsSet(input[1]))))
            {
                return bestPair;
            }
        }

        return null;
    }

    public void Convert(ReversedStack<char> consumeBuffer, SimpleList<char> result, SimpleList<ConversionRule>? appliedRules)
    {
        result.Clear();
        appliedRules?.Clear();
        while (!consumeBuffer.IsEmpty)
        {
            var remainingInput = consumeBuffer.AsSpan();
            var rule = GetPairFromRomaji(remainingInput);
            if (rule is null) return;
            result.Add(rule.To);
            consumeBuffer.Pop(rule.From.Length);
            consumeBuffer.Push(rule.ToPush);
            appliedRules?.Add(rule);
        }
    }

    public int CountOriginalLength(int convertedLength, SimpleList<ConversionRule> appliedRules)
    {
        var length = 0;
        var remaining = convertedLength;
        foreach (var rule in appliedRules.AsSpan())
        {
            remaining -= rule.To.Length;
            length += rule.From.Length - rule.ToPush.Length;
            if (remaining <= 0) break;
        }

        return length;
    }

    public ReadOnlySpan<char> ConvertAsPossible(ReversedStack<char> consumeBuffer, ReadOnlySpan<char> target, SimpleList<ConversionRule>? appliedRules)
    {
        appliedRules?.Clear();
        while (!consumeBuffer.IsEmpty)
        {
            var remainingInput = consumeBuffer.AsSpan();
            var rule = GetPairFromRomaji(remainingInput);
            if (rule is null) return target;
            if (!target.StartsWith(rule.To)) return target;
            target = target[rule.To.Length..];
            consumeBuffer.Pop(rule.From.Length);
            consumeBuffer.Push(rule.ToPush);
            appliedRules?.Add(rule);
        }

        return target;
    }

    public bool HasSuccessor(ReadOnlySpan<char> input)
    {
        if (input.Length == 0)
            return false;
        var c = input[0];
        if (Map.TryGetValue(c, out var pairs))
        {
            foreach (var pair in pairs)
            {
                if (pair.From.StartsWith(pair.From))
                {
                    return true;
                }
            }
        }

        return false;
    }
}