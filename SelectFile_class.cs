public class FilePathSelector
{
    public string selectFile()
    {
        Console.WriteLine(
            $"\n*if you want to select file, add '-' in front of the path.\n example: -{Directory.GetCurrentDirectory()}\n*if you want to search file, write a path.\n example: /home/name/folder\n"
        );
        while (true)
        {
            string dir = Console.ReadLine() ?? "Null";
            if (dir.StartsWith('-') == false)
            {
                string[] Folders = Directory.GetDirectories(dir);
                string[] Files = Directory.GetFiles(dir);
                Console.WriteLine("\n");
                foreach (var folder in Folders)
                {
                    Console.WriteLine("Folder: " + folder);
                }
                foreach (var file in Files)
                {
                    Console.WriteLine("File: " + file);
                }
            }
            else if (dir.StartsWith('-') == true)
            {
                string pathString = dir.Remove(0, 1);
                return pathString;
            }
        }
    }

    public string Filename(string path)
    {
        string[] sf_name;
        if (path.Contains('\\'))
        {
            sf_name = path.Split('\\');
        }
        else
        {
            sf_name = path.Split('/');
        }
        string filename = sf_name[sf_name.Count() - 1];

        return filename;
    }
}

public class Entries
{
    public string? Name { get; set; }
    public string? Id { get; set; }
}

public class Name
{
    public List<Entries>? Entries { get; set; }
    public string? Cursor { get; set; }
}
