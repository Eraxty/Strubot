Console.Write("Enter no of tabs: ");
int num_of_tabs = int.Parse(Console.ReadLine());

List<string> elements = new List<string>();

for (int i = 0; i < num_of_tabs; i++){
    Console.Write("Enter:- ");
    string all = Console.ReadLine();
    elements.Add((all));
}

Console.WriteLine(string.Join(", ", elements));