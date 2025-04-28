using System;

namespace RomajiTyping;

/// <summary>
/// ローマ字をひらがなに変換するためのルール
/// </summary>
/// <param name="From">ローマ字</param>
/// <param name="To">ひらがな</param>
/// <param name="ToPush">変換時にひらがなの後に追加するローマ字</param>
public readonly record struct ConversionRuleElement(string From, string To, string ToPush = "");