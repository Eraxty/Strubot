using System.Text.Json;

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

    string tonic = key.GetProperty("tonic").GetString()!;
    string scale = key.GetProperty("scale").GetString()!;
    double beat = key.GetProperty("beat").GetDouble()

    foreach (JsonElement item in notes.EnumerateArray());
    {
        string sd = item.GetProperty("sd").GetString()!;
        double beat = item.GetProperty("beat").GetDouble();

        Console.WriteLine($"Beat {beat}, scale degree {sd}");
    }
}
