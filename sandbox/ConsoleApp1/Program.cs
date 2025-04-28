using System.Runtime.CompilerServices;
using RomajiTyping;


var rulesText =File.ReadAllText(GetAbsolutePath("rules.txt"));

var rules = (rulesText
    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
    .Select(line => line.Split(['\t', ' '],StringSplitOptions.RemoveEmptyEntries))
    .Where(parts => parts.Length >= 2)
    .Select(parts => new ConversionRuleElement(parts[0], parts[1], parts.Length >= 3 ? parts[2] : "")));

var target = "こんなことがあっていいのか？";
Console.WriteLine(target);

var reader = new RomajiConverter(rules);
var inputs = new SimpleList<char>();
var remain = new ReversedStack<char>();
reader.GetBestPath([], target, remain, out _);
Console.WriteLine(remain.AsSpan().ToString());
while (true)
{
    var key = Console.ReadKey(false);
    if (key.Key == ConsoleKey.Enter)
    {
        inputs.Clear();
        Console.Clear();
        Console.WriteLine("Please input target text");
        Console.CursorVisible = true;
        target = (Console.ReadLine()) ?? "";
        Console.Clear();
    }
    else if (key.Key == ConsoleKey.Backspace)
    {
        if (inputs.Length > 0)
            inputs.RemoveAtSwapBack(inputs.Count - 1);
    }
    else if (key.Key == ConsoleKey.Escape)
    {
        break;
    }
    else if ('!' <= key.KeyChar)
    {
        inputs.Add(key.KeyChar);
    }

    {
        Console.Clear();

        if (reader.GetBestPath(inputs.AsSpan(), target, remain, out var currentTargetMatchCount))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(target[0..currentTargetMatchCount]);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(target[currentTargetMatchCount..]);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(inputs.ToString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(remain.AsSpan().ToString());

            if (remain.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Perfect Match");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorVisible = false;
        }
        else
        {
            Console.WriteLine(inputs.ToString());
            reader.GetBestPath([], target, remain, out _);
            Console.WriteLine(remain.AsSpan().ToString());
            Console.WriteLine("No Match");
        }
    }
}

static string GetAbsolutePath(string relativePath, [CallerFilePath] string callerFilePath = "")
{
    return Path.Combine(Path.GetDirectoryName(callerFilePath)!, relativePath);
}