using System.Collections.Generic;
using RomajiTyping.Internal;

namespace RomajiTyping;

public sealed class ConversionRule(string from, string to, string toPush, int order) : IComparable<ConversionRule>
{
    public readonly string From = from;
    public readonly string To = to;
    public readonly string ToPush = toPush;
    public readonly int Order = order;

    public ConversionRule(ConversionRuleElement ruleElement, int order) : this(ruleElement.From, ruleElement.To, ruleElement.ToPush, order)
    {
    }

    SparseUShortBitSet? nextProhibitedChars;

    internal SparseUShortBitSet? NextProhibitedChars
    {
        get => nextProhibitedChars;
        set
        {
            if (nextProhibitedChars is not null)
            {
                Throw();
                void Throw() => throw new InvalidOperationException("Cannot set NextProhibitedChars twice.");
            }

            nextProhibitedChars = value;
        }
    }

    public int CompareTo(ConversionRule? other)
    {
        if (other is null) return 1;
        return Order.CompareTo(other.Order);
    }

    public override string ToString()
    {
        return $"{From} ->{To} + {ToPush}";
    }
}