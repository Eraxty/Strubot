using System.Text.Json;

string[] notes = { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };


Dictionary<string, int> tonics = new Dictionary<string, int>
{
    {"C",0},{"C#",1},{"D",2},{"D#",3},{"E",4},{"F",5},{"F#",6},{"G",7},{"G#",8},{"A",9},{"A#",10},{"B",11}
};



Dictionary<string, int[]> scales = new Dictionary<string, int[]>
{
    {"major", new int[]{0,2,4,5,7,9,11}},
    {"minor", new int[]{0,2,3,5,7,8,10}}
};

string json = File.ReadAllText(args[0]);


JsonElement doc = JsonDocument.Parse(json).RootElement;
JsonElement key = doc.GetProperty("keys")[0];

int tonic = tonics[key.GetProperty("tonic").GetString()];
string sc = key.GetProperty("scale").GetString();
int[] scale = scales.ContainsKey(sc) ? scales[sc] : scales["major"];



List<(string tok, int len)> all = new List<(string tok, int len)>();

double prevEnd = 1;


// conversions
foreach (JsonElement item in doc.GetProperty("notes").EnumerateArray())
{
    double beat = item.GetProperty("beat").GetDouble();
    double duration = item.GetProperty("duration").GetDouble();

    string token = "~";

    if (!item.GetProperty("isRest").GetBoolean())
    {
        string sd = item.GetProperty("sd").GetString();
        int acc = 0;
        
        if (sd[0] == '#') { acc = 1; sd = sd.Substring(1); }
        else if (sd[0] == 'b') { acc = -1; sd = sd.Substring(1); }

        int octave = item.GetProperty("octave").GetInt32();
        int midi = 60 + tonic + 12 * octave + scale[int.Parse(sd) - 1] + acc;
        
        token = notes[midi % 12] + (midi / 12 - 1);
    }

    int gap = (int)Math.Round((beat - prevEnd) * 4);
    
    if (gap > 0) all.Add(("~", gap));

    all.Add((token, (int)Math.Round(duration * 4)));
    prevEnd = beat + duration;
}


double endBeat = prevEnd;

List<string> chordtokens = new List<string>();

foreach (JsonElement c in doc.GetProperty("chords").EnumerateArray())
{
    endBeat = Math.Max(endBeat, c.GetProperty("beat").GetDouble() + c.GetProperty("duration").GetDouble());

    int root = c.GetProperty("root").GetInt32();

    List<string> triad = new List<string>();

    foreach (int step in new int[] { 0, 2, 4 })
    {
        int idx = root - 1 + step;
        int midi = 60 + tonic + 12 * (-1 + idx / 7) + scale[idx % 7];
        
        triad.Add(notes[midi % 12] + (midi / 12 - 1));
    }

    chordtokens.Add("[" + string.Join(",", triad) + "]@" + (int)Math.Round(c.GetProperty("duration").GetDouble() * 4));
}

int endGap = (int)Math.Round((endBeat - prevEnd) * 4);

if (endGap > 0) all.Add(("~", endGap));


List<string> bars = new List<string>();
List<string> events = new List<string>();

int used = 0;

foreach (var e in all)
{
    int left = e.len;
    
    while (left > 0)
    {
        int take = Math.Min(left, 16 - used);
        
        events.Add(e.tok + "@" + take);
        used += take;
        left -= take;

        if (used == 16)
        {
            bars.Add(string.Join(" ", events));
            events = new List<string>();
            used = 0;
        }
    }
}
if (events.Count > 0) bars.Add(string.Join(" ", events));


Console.Write("Enter song's BPM: ");
string bpm = Console.ReadLine();

// writing
Console.WriteLine($"setcps({bpm}/60/4)\n\n$: cat(");

for (int i = 0; i < bars.Count; i++)
{
    string comma = i < bars.Count - 1 ? "," : "";
    Console.WriteLine($"  note(\"{bars[i]}\"){comma}");
}

Console.WriteLine(").sound(\"piano\")");

Console.WriteLine("\n$: note(\"" + string.Join(" ", chordtokens) + "\").sound(\"piano\")");
