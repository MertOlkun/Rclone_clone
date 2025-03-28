using System.Reflection;
using System.Reflection.Emit;

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
            if (dir == "//sync")
            {
                return "//sync";
            }
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
    public string? Path_display { get; set; }
    public string? Content_hash { get; set; }
}

public class Name
{
    public List<Entries>? Entries { get; set; }
    public string? Cursor { get; set; }
}

public class PathValidator
{
    string selectedFile;
    public string[] allFiles = Array.Empty<string>();

    public PathValidator(string selectedFile)
    {
        this.selectedFile = selectedFile;
    }

    public string pthValid()
    {
        if (Directory.Exists(selectedFile))
        {
            allFiles = Directory.GetFiles(selectedFile, "*", SearchOption.AllDirectories);
        }
        else if (File.Exists(selectedFile))
        {
            allFiles = [selectedFile];
        }

        string slash = "/";

        if (selectedFile.Contains('\\'))
        {
            slash = "\\";
        }
        string last_item = selectedFile.Split(slash)[selectedFile.Split(slash).Count() - 1];
        string replaced_item = selectedFile.Split(last_item)[0];
        return replaced_item;
    }
}
