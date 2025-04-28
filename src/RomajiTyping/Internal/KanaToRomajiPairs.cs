namespace RomajiTyping.Internal;

internal sealed class KanaToRomajiRules
{
    readonly List<ConversionRule> rules = new();
    List<ConversionRule>? shortRules;

    public List<ConversionRule> Rules => rules;
    public List<ConversionRule> ShortRules => shortRules ?? rules;

    public void AddRule(ConversionRule rule)
    {
        rules.Add(rule);
    }

    public void BuildShortRules()
    {
        if (shortRules != null) return;
        rules.Sort();
        using var listBuilder = new ValueListBuilder<ConversionRule>(rules.Count);
        foreach (var rule in rules)
        {
            listBuilder.Append(rule);
        }


        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.NextProhibitedChars is null)
            {
                for (var j = 0; j < rules.Count; j++)
                {
                    if (i == j) continue;
                    var otherRule = rules[j];
                    if (otherRule.NextProhibitedChars is null && (otherRule.From.Length < rule.From.Length || (otherRule.From.Length == rule.From.Length && rule.Order > otherRule.Order)))
                    {
                        goto Next;
                    }
                }
            }

            listBuilder.Append(rule);
            continue;
        Next: ;
        }

        if (listBuilder.Length != rules.Count)
        {
            shortRules = new List<ConversionRule>(listBuilder.Length);
            foreach (var rule in listBuilder.AsSpan())
            {
                shortRules.Add(rule);
            }
        }
    }
}