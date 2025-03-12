using System;

namespace RomajiTyping
{
    public record RomajiPair(string Romaji, string Kana, ConversionMode conversionMode = ConversionMode.Any, int Priority = 0) : IComparable<RomajiPair>
    {
        public bool IsAvailable(ConversionSearchMode searchMode = ConversionSearchMode.Any)
        {
            if (searchMode is ConversionSearchMode.None)
            {
                if (conversionMode is not ConversionMode.Any)
                    return false;
            }

            return (((byte)conversionMode & (byte)searchMode) != 0) ;
        }
        
        
        public bool MatchRomaji(ReadOnlySpan<char> c)
        {
            if (Romaji == "n")
            {
                if (c.Length < 2) return false;
                if (c[0] != 'n') return false;
                
                return c[1] is  not ('e' or 'i' or 'o' or 'u' or 'n' or 'y');
            }

            return c.StartsWith(Romaji);
        }

        public bool MachHiragana(ReadOnlySpan<char> c)
        {
            if (Romaji == "n")
            {
                if (c.Length < 2) return false;
                return c[0] is 'ん' && c[1] is not ((>='あ' and <='お') or( >='な' and <='の') or 'や' or 'ゆ' or 'よ' or 'ん');
            }

            return c.StartsWith(Kana);
        }

        public int CompareTo(RomajiPair? other)
        {
            if (other is null) return 1;
            if (Kana.Length != other.Kana.Length)
                return Kana.Length.CompareTo(other.Kana.Length);
            if (Romaji.Length != other.Romaji.Length)
                return -Romaji.Length.CompareTo(other.Romaji.Length);
            return Priority.CompareTo(other.Priority);
        }
    }
}