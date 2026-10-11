using System.Text.Json;


Dictionary<string, int> tonics = new()
{
    ["C"] = 0, ["C#"] = 1, ["D"] = 2, ["D#"] = 3, ["E"] = 4, ["F"] = 5, ["F#"] = 6, ["G"] = 7, ["G#"] = 8, ["A"] = 9, ["A#"] = 10, ["B"] = 11
};

Dictionary<string, int[]> scales = new()
{
    ["major"] = new[] { 0, 2, 4, 5, 7, 9, 11 },["minor"] = new[] { 0, 2, 3, 5, 7, 8, 10 },
};

string[] names = { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };


List<string> elements = new List<string>();

Console.WriteLine("Press 0 to stop");

while (true)
{
    Console.Write("Enter:- ");
    string all = Console.ReadLine();
    if (all == "0")
    {
        break;
    }

    elements.Add(all);
}


foreach (string stuff in elements)
{
    using JsonDocument doc = JsonDocument.Parse(stuff);
    
    JsonElement key = doc.RootElement.GetProperty("keys").EnumerateArray().First();
    JsonElement notes = doc.RootElement.GetProperty("notes");

    int tonic = tonics[key.GetProperty("tonic").GetString()!];
    string scaleName = key.GetProperty("scale").GetString()!;
    
    int[] scale = scales.GetValueOrDefault(scaleName, scales["major"]);

    List<string> events = new();
    
    foreach (JsonElement item in notes.EnumerateArray())
    {
        string sd = item.GetProperty("sd").GetString()!;
        int octave = item.GetProperty("octave").GetInt32();
        int acc = 0;

        if (sd[0] == '#') { acc = 1; sd = sd[1..]; }
        else if (sd[0] == 'b') { acc = -1; sd = sd[1..]; }

        int midi = 60 + tonic + 12 * octave + scale[int.Parse(sd) - 1] + acc;
        events.Add(item.GetProperty("isRest").GetBoolean() ? "~" : names[midi % 12] + (midi / 12 - 1));
    }

    Console.WriteLine($"note(\"{string.Join(" ", events)}\")");
}
