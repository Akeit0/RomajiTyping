using System;

namespace RomajiTyping
{
    public record RomajiPair(string Romaji, string Kana, ConversionMode conversionMode = ConversionMode.Any, int Priority = 0) : IComparable<RomajiPair>
    {
        public bool MatchRomaji(ReadOnlySpan<char> c, ConversionSearchMode searchMode = ConversionSearchMode.Any)
        {
            if (searchMode is ConversionSearchMode.All)
            {
                if (conversionMode is not ConversionMode.Any)
                    return false;
            }

            if (((byte)conversionMode & (byte)searchMode) == 0) return false;
            if (Romaji == "n")
            {
                if (c.Length < 2) return false;
                if (c[0] != 'n') return false;
                return c[1] is >= 'b' and <= 'z' and not ('e' or 'i' or 'o' or 'u' or 'n' or 'y');
            }

            return c.StartsWith(Romaji);
        }

        public bool MachHiragana(ReadOnlySpan<char> c, ConversionSearchMode searchMode = ConversionSearchMode.Any)
        {
            if (searchMode is ConversionSearchMode.All)
            {
                if (conversionMode is not ConversionMode.Any)
                    return false;
            }

            if (((byte)conversionMode & (byte)searchMode) == 0) return false;
            if (Romaji == "n")
            {
                if (c.Length < 2) return false;
                return c[0] is 'ん' && c[1] is not ('あ' or 'い' or 'う' or 'え' or 'お' or 'や' or 'ゆ' or 'よ' or 'ん');
            }

            return c.StartsWith(Kana);
        }

        public int ConsumeCount => Romaji.Length;

        public int CompareTo(RomajiPair? other)
        {
            if (other is null) return 1;
            if (Kana.Length != other.Kana.Length)
                return -Kana.Length.CompareTo(other.Kana.Length);
            if (Romaji.Length != other.Romaji.Length)
                return Romaji.Length.CompareTo(other.Romaji.Length);
            return Priority.CompareTo(other.Priority);
        }
    }
}