using RomajiTyping;


var target = "きょうはいいひだったネ";
Console.WriteLine(target);

var reader = RomajiConverter.Default;
var inputs = new SimpleStringBuilder();
var remain = new SimpleStringBuilder();
reader.GetBestPath([], target, remain);
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
        target = reader.Convert(Console.ReadLine());
        Console.Clear();
    }
    else if (key.Key == ConsoleKey.Backspace)
    {
        if(inputs.Length > 0)
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
        Console.WriteLine(target);
        if (reader.GetBestPath(inputs.AsSpan(), target, remain))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(inputs.ToString());
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(remain.ToString());

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
            reader.GetBestPath([], target, remain);
            Console.WriteLine(remain.ToString());
            Console.WriteLine("No Match");
        }
    }
}