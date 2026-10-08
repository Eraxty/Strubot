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
