using System.Buffers;
using RomajiTyping.Internal;
using System.Runtime.CompilerServices;

namespace RomajiTyping;

public sealed class InversionRuleSet
{
    internal readonly CharKeyFrozenDictionary<KanaToRomajiRules> Map; // kana -> romaji

    // Unsafely shares ArrayPool
    class UnsafeArrayPool : ArrayPool<CharKeyFrozenDictionary<KanaToRomajiRules>.Entry>
    {
        readonly ArrayPool<CharKeyFrozenDictionary<List<ConversionRule>>.Entry> pool = ArrayPool<CharKeyFrozenDictionary<List<ConversionRule>>.Entry>.Shared;

        public override CharKeyFrozenDictionary<KanaToRomajiRules>.Entry[] Rent(int minimumLength)
        {
            return Unsafe.As<CharKeyFrozenDictionary<KanaToRomajiRules>.Entry[]>(pool.Rent(minimumLength));
        }

        public override void Return(CharKeyFrozenDictionary<KanaToRomajiRules>.Entry[] array, bool clearArray = false)
        {
            pool.Return(Unsafe.As<CharKeyFrozenDictionary<List<ConversionRule>>.Entry[]>(array), clearArray);
        }
    }

    public InversionRuleSet(ConversionRuleSet conversionRuleSet)
    {
        var kanaToRomajiMapBuilder = new CharKeyFrozenDictionary<KanaToRomajiRules>.Builder(new UnsafeArrayPool());

        foreach (var pair in conversionRuleSet.Map)
        {
            foreach (var rule in pair.Value)
            {
                char firstKanaChar = rule.To[0];
                ref var list = ref kanaToRomajiMapBuilder.GetValueRefOrAddDefault(firstKanaChar, out _);
                list ??= new();

                list.AddRule(rule);
            }
        }

        Map = kanaToRomajiMapBuilder.Build();
        foreach (var pair in Map)
        {
            pair.Value.BuildShortRules();
        }
    }

    // BFSの状態を表す
    readonly struct State(int pos, ConversionRule? lastRule) : IEquatable<State>
    {
        public bool IsValid => 0 <= Pos;
        public readonly int Pos = pos;
        readonly ConversionRule? lastRule = lastRule;
        public string Leftover => lastRule is null ? string.Empty : lastRule.ToPush;
        public SparseUShortBitSet? NextProhibitedChars => lastRule?.NextProhibitedChars;

        public static State Invalid => new(-1, null);

        public void Deconstruct(out int pos, out string leftover, out SparseUShortBitSet? nextProhibitedChars)
        {
            pos = this.Pos;
            leftover = this.Leftover;
            nextProhibitedChars = this.NextProhibitedChars;
        }

        public bool Equals(State other)
        {
            if (Pos != other.Pos) return false;
            if (lastRule == other.lastRule) return true;
            if (lastRule is null || other.lastRule is null) return false;
            if (lastRule.ToPush != other.lastRule.ToPush) return false;
            return lastRule.NextProhibitedChars == other.lastRule.NextProhibitedChars;
        }

        public override bool Equals(object? obj)
        {
            return obj is State other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Pos, Leftover, NextProhibitedChars);
        }
    }

    // BFSで各状態に対して保持する情報
    // - Cost : スタートからここまでの最小入力長
    // - Parent : 親状態 (どこから来たか)
    // - InputSegment : 親状態からこの状態へ遷移するときに追加した入力 (newInputPart)
    readonly record struct BfsRecord(int Cost, State Parent, SkippedString NewInputPart);

    readonly record struct SkippedString(string text, int skipCount)
    {
        public int Length => text.Length - skipCount;
        public ReadOnlySpan<char> AsSpan() => text.AsSpan(skipCount);
    }

    public bool TryFindShortestInput(
        ReadOnlySpan<char> partialInput,
        ReadOnlySpan<char> targetKana,
        ReversedStack<char> stack
    )
    {
        // 幅優先探索(BFS)のためのキュー
        using var queue = new ValueQueue<State>(16);

        // 訪問済み状態と、その状態に到達するための最短情報
        using var visited = new DictionarySlim<State, BfsRecord>();

        // 初期状態: pos=0, leftover=""
        var startState = new State(0, null);
        visited.GetValueRefOrAddDefault(startState, out _) = new(
            Cost: 0,
            Parent: State.Invalid, // 開始点なので親なし
            NewInputPart: new("", 0) // 開始時は何も入力していない
        );
        queue.Enqueue(startState);

        // BFS開始
        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            var (currentPos, currentLeftover, nextProhibitedChars) = currentState;
            var currentRecord = visited[currentState];
            var currentCost = currentRecord.Cost;

            // ゴール判定: かなをすべて生成し終え、かつ leftover が空
            if (currentPos == targetKana.Length && currentLeftover == "" && nextProhibitedChars is null)
            {
                // この時点での経路が最短なので、復元して返す
                ReconstructInput(stack, visited, currentState);
                return true;
            }

            // 変換対象の先頭文字から該当ルールを取り出してループ
            if (currentPos >= targetKana.Length) continue;
            char currentKanaHead = targetKana[currentPos];

            // partialInputを考慮する場合の候補の検索

            if (!Map.TryGetValue(currentKanaHead, out var candidateRules))
            {
                // この先頭文字から始まるルールが無いなら遷移不可能
                continue;
            }

            foreach (var rule in
                     //入力途中文字列があるときは全体の候補から
                     (currentCost < partialInput.Length
                         ? candidateRules.Rules
                         :
                         //入力途中文字列がないときはShortRulesから
                         candidateRules.ShortRules))
            {
                if (nextProhibitedChars is not null && nextProhibitedChars.IsSet(rule.From[0]))
                {
                    //Console.WriteLine($" Prohibited Char: {rule.Romaji[0]}");
                    continue;
                }

                // 1) Kana側がマッチするか確認
                //    現在位置currentPosからrule.Kana分だけ一致するか
                if (currentPos + rule.To.Length > targetKana.Length) continue;
                if (!targetKana.Slice(currentPos).StartsWith(rule.To)) continue;

                SkippedString newInputPart;
                if (currentCost < partialInput.Length)
                {
                    var remain = partialInput[currentCost..];
                    if (!rule.From.AsSpan().StartsWith(remain)) continue;
                    newInputPart = new(rule.From, currentLeftover.Length);
                    //Console.WriteLine($"new input part: {newInputPart.AsSpan().ToString()} remain: {remain.ToString()} leftover: {currentLeftover}");
                }
                else
                {
                    if (!rule.From.StartsWith(currentLeftover)) continue;
                    // 3) leftoverのぶんを取り除いた残りが新しく入力すべきローマ字
                    newInputPart = new(rule.From, currentLeftover.Length);
                }


                // 新しい入力の長さ(コスト)
                int nextCost = currentCost + newInputPart.Length;

                // 次の状態
                int nextPos = currentPos + rule.To.Length;
                var nextState = new State(nextPos, rule);

                // 既に到達済みの場合、より短い入力で到達できるなら更新
                ref var nextRecord = ref visited.GetValueRefOrAddDefault(nextState, out var exists);
                if (!exists
                    || nextCost < nextRecord.Cost)
                {
                    // 必要情報だけ覚えておく
                    nextRecord = new(
                        Cost: nextCost,
                        Parent: currentState, // どこから来たか
                        NewInputPart: newInputPart // 今回追加した入力
                    );

                    queue.Enqueue(nextState);

                    //Console.WriteLine($"Enqueued: Queue{queue.Count} {{Cost: {nextCost}, NewInputPart: {newInputPart.AsSpan().ToString()} }}");
                }
            }
        }

        // 探索が終わってもゴールに到達できなかった場合は変換不能
        return false;
    }

    /// <summary>
    /// BFSの探索結果(visited)をもとに、(pos=targetKana.Length, leftover="") の状態から
    /// 入力文字列を再構築して返す。
    /// </summary>
    static void ReconstructInput(ReversedStack<char> stack, DictionarySlim<State, BfsRecord> visited, State endState)
    {
        // 親を遡りながら、追加された入力セグメントを逆順に集めていく
        var builder = stack;

        var cur = endState;
        while (true)
        {
            var rec = visited[cur];
            // 親がinvalidの場合は開始状態
            if (!rec.Parent.IsValid)
            {
                break;
            }

            builder.Push(rec.NewInputPart.AsSpan());
            //Console.WriteLine($"Pushed: {rec.NewInputPart.AsSpan().ToString()}");
            cur = rec.Parent;
        }
    }
}