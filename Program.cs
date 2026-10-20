using System.Diagnostics;
using System.Text.Json;
using System.Text;

static bool CopyOutput(string text)
{
    List<string[]> commands = new List<string[]>();

    if (OperatingSystem.IsWindows()) commands.Add(new[] { "clip" });
    
    else if (OperatingSystem.IsMacOS()) commands.Add(new[] { "pbcopy" });
    
    else
    {
        commands.Add(new[] { "wl-copy" });
        commands.Add(new[] { "xclip", "-selection", "clipboard" });
        commands.Add(new[] { "xsel", "--clipboard", "--input" });
    }

    foreach (string[] command in commands)
    {
        try
        {
            ProcessStartInfo info = new ProcessStartInfo(command[0]);
            
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
    
            for (int i = 1; i < command.Length; i++) info.ArgumentList.Add(command[i]);
    
            using Process process = Process.Start(info);
            
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
            
            if (process.ExitCode == 0) return true;
        }
        catch (Exception) { }
    }

    string clip = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    
    Console.Write("\u001b]52;c;" + clip + "\u0007");
    return true;
}

string[] notes = { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };


Dictionary<string, int> tonics = new Dictionary<string, int>
{
    {"C",0},{"C#",1},{"Db",1},{"D",2},{"D#",3},{"Eb",3},{"E",4},{"F",5},{"F#",6},{"Gb",6},{"G",7},{"G#",8},{"Ab",8},{"A",9},{"A#",10},{"Bb",10},{"B",11}
};


// semitones 
Dictionary<string, int[]> scales = new Dictionary<string, int[]>
{
    {"major", new int[]{0,2,4,5,7,9,11}},
    {"minor", new int[]{0,2,3,5,7,8,10}},
    {"dorian", new int[]{0,2,3,5,7,9,10}},
    {"phrygian", new int[]{0,1,3,5,7,8,10}},
    {"lydian", new int[]{0,2,4,6,7,9,11}},
    {"mixolydian", new int[]{0,2,4,5,7,9,10}},
    {"locrian", new int[]{0,1,3,5,6,8,10}},
    {"harmonicminor", new int[]{0,2,3,5,7,8,11}},
    {"phrygiandominant", new int[]{0,1,4,5,7,8,10}}
};

Console.Write("Enter song's BPM: ");

string bpm = Console.ReadLine();

Console.Write("How many JSON parts? ");

int partCount = int.Parse(Console.ReadLine());

List<string> songBars = new List<string>();
List<string> songChords = new List<string>();
List<string> songBass = new List<string>();

int songBeatsPerBar = 4;

for (int part = 0; part < partCount; part++)
{

Console.Write($"Paste JSON part {part + 1}: ");

string json = Console.ReadLine();

JsonDocument parsed;

try
{
    parsed = JsonDocument.Parse(json);
}

catch (JsonException)

{
    Console.WriteLine("bruh ts not json >?!/1");
    return;
}

JsonElement doc = parsed.RootElement;
JsonElement key = doc.GetProperty("keys")[0];

string tonicName = key.GetProperty("tonic").GetString() ?? "";
tonicName = tonicName.Replace("♭", "b").Replace("♯", "#");

if (!tonics.TryGetValue(tonicName, out int tonic))
{
    Console.WriteLine("unknown tonic in json make a pr or smthin to fix bugs: " + tonicName);
    return;
}

string sc = key.GetProperty("scale").GetString()?.ToLower().Replace(" ", "").Replace("-", "") ?? "";

if (!scales.TryGetValue(sc, out int[] scale))
{
    Console.WriteLine("unknown scale in json make a pr or smthin to fix bugs: " + sc);
    return;
}


int beatsPerBar = 4;
JsonElement meters = doc.GetProperty("modern").GetProperty("payload").GetProperty("meters");

if (meters.GetArrayLength() > 0)
{
    JsonElement meter = meters[0];

    if (meter.TryGetProperty("meter", out JsonElement sig) && sig.ValueKind == JsonValueKind.String && sig.GetString().Contains('/'))
    {
        beatsPerBar = int.Parse(sig.GetString().Split('/')[0]);
    }
    
    else
    {
        foreach (string name in new[] { "beatsPerBar", "numerator", "beats" })
        {
            if (meter.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number)
            {
                beatsPerBar = v.GetInt32();
                break;
            }
        }
    }
}

if (beatsPerBar < 1) beatsPerBar = 4;
songBeatsPerBar = beatsPerBar;

int barLen = beatsPerBar * 4;


List<(string tok, int len)> all = new List<(string tok, int len)>();
int pos = 0;


// conversions
foreach (JsonElement item in doc.GetProperty("notes").EnumerateArray())
{
    double beat = item.GetProperty("beat").GetDouble();
    double duration = item.GetProperty("duration").GetDouble();
    
    int start = (int)Math.Round((beat - 1) * 4);
    int end = (int)Math.Round((beat + duration - 1) * 4);
    
    if (start < pos) start = pos;
    if (end <= start) continue;

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

    if (start > pos) all.Add(("~", start - pos));
    
    all.Add((token, end - start));
    pos = end;
}


double chordEnd = 1;

List<string> chordtokens = new List<string>();
List<string> bass = new List<string>();

int[] chordscale = sc == "minor" ? new int[] { 0, 2, 3, 5, 7, 8, 11 } : scale;

foreach (JsonElement c in doc.GetProperty("chords").EnumerateArray())
{
    chordEnd = Math.Max(chordEnd, c.GetProperty("beat").GetDouble() + c.GetProperty("duration").GetDouble());

    int root = c.GetProperty("root").GetInt32();
    int inversion = c.GetProperty("inversion").GetInt32();

    int[] offsets = new int[] { 0, 2, 4 };

    if (inversion == 1) offsets = new int[] { 2, 4, 7 };
    if (inversion == 2) offsets = new int[] { 4, 7, 9 };

    List<string> triad = new List<string>();

    foreach (int step in offsets)
    {
        int idx = root - 1 + step;
        int midi = 60 + tonic + 12 * (-1 + idx / 7) + chordscale[idx % 7];
        
        triad.Add(notes[midi % 12] + (midi / 12 - 1));
    }

    chordtokens.Add("[" + string.Join(",", triad) + "]@" + c.GetProperty("duration").GetDouble() / beatsPerBar);

    int bassmidi = 60 + tonic + 12 * (-2 + (root - 1) / 7) + chordscale[(root - 1) % 7];

    bass.Add(notes[bassmidi % 12] + (bassmidi / 12 - 1) + "@" + c.GetProperty("duration").GetDouble() / beatsPerBar);
}

int target = (int)Math.Round((chordEnd - 1) * 4);

target = (target + barLen - 1) / barLen * barLen;

int total = 0;

foreach (var e in all) total += e.len;
while (total > target && all.Count > 0)
{
    var last = all[all.Count - 1];

    int cut = Math.Min(total - target, last.len);

    total -= cut;

    if (cut == last.len) all.RemoveAt(all.Count - 1);
    else all[all.Count - 1] = (last.tok, last.len - cut);
}

if (total < target) all.Add(("~", target - total));

List<string> bars = new List<string>();
List<string> events = new List<string>();

int used = 0;

foreach (var e in all)
{
    int left = e.len;
    
    while (left > 0)
    {
        int take = Math.Min(left, barLen - used);
        
        events.Add(e.tok + "@" + take);
        used += take;
        left -= take;

        if (used == barLen)
        {
            bars.Add(string.Join(" ", events));
            events = new List<string>();
            used = 0;
        }
    }
}
if (events.Count > 0) bars.Add(string.Join(" ", events));


songBars.AddRange(bars);
songChords.AddRange(chordtokens);
songBass.AddRange(bass);
}

// output
List<string> output = new List<string>();
output.Add($"setcps({bpm}/60/{songBeatsPerBar})");
output.Add("");
output.Add("$: cat(");

for (int i = 0; i < songBars.Count; i++)
{
    string comma = i < songBars.Count - 1 ? "," : "";
    output.Add($"  note(\"{songBars[i]}\"){comma}");
}

output.Add(").sound(\"piano\")");
output.Add("");
output.Add("$: note(\"<" + string.Join(" ", songChords) + ">\").sound(\"piano\")");
output.Add("");
output.Add("$: note(\"<" + string.Join(" ", songBass) + ">\").sound(\"piano\")");

string code = string.Join(Environment.NewLine, output);

Console.Clear();

Console.WriteLine(code);

Console.Write("[c] to copy code, any other key to finish: ");

if (Console.ReadKey(true).KeyChar == 'c')
    Console.WriteLine(CopyOutput(code) ? "copied" : "cant open clipboard just copy the boxed text manually");
