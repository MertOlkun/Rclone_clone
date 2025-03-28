using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Dropbox_class
{
    readonly HttpListener listener;
    readonly HttpClient httpClient;
    readonly string appKey;
    readonly string appSecret;
    readonly string hostAndPort;
    readonly string selectedFile;
    readonly string fileName;

    public Dropbox_class(
        HttpListener listener,
        HttpClient httpClient,
        string appKey,
        string appSecret,
        string hostAndPort,
        string selectedFile,
        string fileName
    )
    {
        this.listener = listener;
        this.httpClient = httpClient;
        this.appKey = appKey;
        this.appSecret = appSecret;
        this.hostAndPort = hostAndPort;
        this.selectedFile = selectedFile;
        this.fileName = fileName;
    }

    string? folderName = string.Empty;
    string? token = string.Empty;
    readonly List<string> pid_upload_session = new();
    readonly List<string> modifiedFilePath = new();

    public void ProcessStartInfo()
    {
        Process.Start(
            new ProcessStartInfo()
            {
                FileName =
                    $"https://www.dropbox.com/oauth2/authorize?client_id={appKey}&redirect_uri={hostAndPort}&response_type=code",
                UseShellExecute = true,
            }
        );
    }

    public async Task GetToken()
    {
        HttpListenerContext httpContext = listener.GetContext();
        if (httpContext.Request.Url is not null)
        {
            string[] requestUrl = httpContext.Request.Url.ToString().Split("=");
            string code = requestUrl[1];

            using (
                var request = new HttpRequestMessage(
                    new HttpMethod("POST"),
                    "https://api.dropbox.com/oauth2/token"
                )
            )
            {
                var contentList = new List<string>();

                contentList.Add($"code={code}");
                contentList.Add("grant_type=authorization_code");
                contentList.Add($"redirect_uri={hostAndPort}");
                contentList.Add($"client_id={appKey}");
                contentList.Add($"client_secret={appSecret}");
                request.Content = new StringContent(string.Join("&", contentList));
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    "application/x-www-form-urlencoded"
                );

                var response = await httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
                if (data != null)
                {
                    token = data["access_token"].ToString();
                }
            }
        }
    }

    public async Task CreateFolder()
    {
        string slash = "/";

        if (selectedFile.Contains('\\'))
        {
            slash = "\\";
        }
        if (Directory.Exists(selectedFile))
        {
            string[] allDirectories = Directory.GetDirectories(
                selectedFile,
                "*",
                SearchOption.AllDirectories
            );

            string a = selectedFile.Split(slash)[selectedFile.Split(slash).Count() - 1];
            string b = selectedFile.Split(a)[0];
            foreach (var item in allDirectories)
            {
                string deletedString = item.Replace(b, "/");
                string ArgJson_create_folder = JsonSerializer.Serialize(
                    new { autorename = false, path = deletedString.Replace("\\", "/") }
                );
                using (
                    var request = new HttpRequestMessage(
                        new HttpMethod("POST"),
                        "https://api.dropboxapi.com/2/files/create_folder_v2"
                    )
                )
                {
                    request.Headers.TryAddWithoutValidation($"Authorization", $"Bearer {token}");

                    request.Content = new StringContent(ArgJson_create_folder);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                        "application/json"
                    );

                    var response = await httpClient.SendAsync(request);
                    var responsebody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responsebody);
                }
            }
        }
    }

    public async Task ListFolder()
    {
        if (File.Exists(selectedFile))
        {
            string ArgJson_list_folder = JsonSerializer.Serialize(
                new
                {
                    include_deleted = false,
                    include_has_explicit_shared_members = false,
                    include_media_info = false,
                    include_mounted_folders = true,
                    include_non_downloadable_files = true,
                    path = "",
                    recursive = false,
                }
            );

            using (
                var request = new HttpRequestMessage(
                    new HttpMethod("POST"),
                    "https://api.dropboxapi.com/2/files/list_folder"
                )
            )
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

                request.Content = new StringContent(ArgJson_list_folder);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    "application/json"
                );

                var response = await httpClient.SendAsync(request);

                var responseBody = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json = JsonSerializer.Deserialize<Name>(responseBody, option);
                if (json != null && json.Entries != null)
                {
                    Console.WriteLine("\n\n\nDropbox folders:\n");
                    foreach (var item in json.Entries)
                    {
                        Console.WriteLine(item.Name);
                    }
                    Console.Write("\nEnter folder name: ");
                    folderName = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Folder not found in Dropbox.");
                }
            }
        }
    }

    public async Task Test() { }

    public async Task SyncAllFiles()
    {
        List<object> entries = [];
        List<string> hash1 = [];
        List<string> hash2 = [];
        List<string> modifiedItem = [];
        List<string> filePathList = [];
        IDictionary<string, string> hashDictionary = new Dictionary<string, string>();
        string slash = "/";

        if (selectedFile.Contains('\\'))
        {
            slash = "\\";
        }

        foreach (var item in File.ReadAllText("FolderPaths.txt").Split('*'))
        {
            if (File.Exists(item))
            {
                filePathList.Add(item);
            }
            else if (Directory.Exists(item))
            {
                filePathList.AddRange(Directory.GetFiles(item, "*", SearchOption.AllDirectories));
            }
        }

        const int chunkSize = 4 * 1024 * 1024;
        using (SHA256 sha256 = SHA256.Create())
        {
            foreach (var filePath in filePathList)
            {
                using (
                    FileStream stream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    )
                )
                {
                    List<byte[]> chunkHashes = new List<byte[]>();
                    byte[] buffer = new byte[chunkSize];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
                    {
                        byte[] chunkHash = sha256.ComputeHash(buffer.Take(bytesRead).ToArray());
                        chunkHashes.Add(chunkHash);
                    }

                    byte[] finalHash = sha256.ComputeHash(chunkHashes.SelectMany(b => b).ToArray());
                    string dropboxHash = BitConverter
                        .ToString(finalHash)
                        .Replace("-", "")
                        .ToLower();
                    hash1.Add(dropboxHash);

                    hashDictionary[dropboxHash] = filePath;
                }
            }
        }
        string ArgJson_list_folder = JsonSerializer.Serialize(
            new
            {
                include_deleted = false,
                include_has_explicit_shared_members = false,
                include_media_info = true,
                include_mounted_folders = false,
                include_non_downloadable_files = true,
                path = "",
                recursive = true,
            }
        );

        using (
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "https://api.dropboxapi.com/2/files/list_folder"
            )
        )
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

            request.Content = new StringContent(ArgJson_list_folder);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var response = await httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Name? json = JsonSerializer.Deserialize<Name>(responseBody, option);

            if (json != null && json.Entries != null)
            {
                Console.WriteLine("\n\n\nDropbox folders:\n");
                foreach (var item1 in json.Entries)
                {
                    if (item1.Content_hash != null)
                    {
                        hash2.Add(item1.Content_hash);
                        modifiedItem.Add(item1.Path_display);
                    }
                }
            }
            else
            {
                Console.WriteLine("Folder not found in Dropbox.");
            }
        }
        var hashDiff = hash1.Except(hash2).ToList();
        for (int i = 0; i < hashDictionary.Count; i++)
        {
            for (int k = 0; k < hashDiff.Count; k++)
            {
                if (!modifiedFilePath.Contains(hashDictionary[hashDiff[k]]))
                {
                    modifiedFilePath.Add(hashDictionary[hashDiff[k]]);
                }
            }
        }
        for (int i = 0; i < modifiedItem.Count; i++)
        {
            foreach (var item in modifiedFilePath)
            {
                if (item.Contains("\\"))
                {
                    slash = "\\";
                }
                if (modifiedItem[i].Contains(item.Split(slash)[item.Split(slash).Length - 1]))
                {
                    entries.Add(new { path = modifiedItem[i] });
                    Console.WriteLine("Synchronizing file named :" + modifiedItem[i]);
                }
            }
        }

        string ArgJson_delete = JsonSerializer.Serialize(new { entries });
        using (var httpClient = new HttpClient())
        {
            using (
                var request = new HttpRequestMessage(
                    new HttpMethod("POST"),
                    "https://api.dropboxapi.com/2/files/delete_batch"
                )
            )
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

                request.Content = new StringContent(ArgJson_delete);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    "application/json"
                );

                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
            }
        }
    }

    public async Task UploadSession()
    {
        List<int> indexList = [];
        string[] splittedFilePath;
        string last_file = string.Empty;
        List<object> entries = [];
        string slash = "/";

        if (selectedFile.Contains('\\'))
        {
            slash = "\\";
        }
        string folderPathTxt = Directory.GetCurrentDirectory() + slash + "FolderPaths.txt";
        string filePathsTxt = Directory.GetCurrentDirectory() + slash + "FilePaths.txt";

        if (!File.ReadAllText(folderPathTxt).Contains(selectedFile))
        {
            if (selectedFile != "//sync")
            {
                File.AppendAllText(folderPathTxt, selectedFile + "*");
            }
        }

        PathValidator pthValid = new PathValidator(selectedFile);
        string replaced_item = pthValid.pthValid();

        string[] allFiles = pthValid.allFiles;

        if (selectedFile == "//sync")
        {
            allFiles = modifiedFilePath.ToArray<string>();

            if (selectedFile.Contains("\\"))
            {
                slash = "\\";
            }
        }
        int number = 0;

        string filePaths = File.ReadAllText(filePathsTxt);
        if (selectedFile == "//sync")
        {
            splittedFilePath = filePaths.Split('*');

            for (int i = 0; i < allFiles.Length; i++)
            {
                last_file = allFiles[i].Split("/")[allFiles[i].Split("/").Length - 1];

                for (int t = 0; t < filePaths.Split('*').Length - 1; t++)
                {
                    if (splittedFilePath[t].Contains(last_file))
                    {
                        indexList.Add(t);
                    }
                }
            }
        }
        foreach (var item in allFiles)
        {
            byte[] sssss = File.ReadAllBytes(item);
            long offset = sssss.Length;
            string newPath = item.Replace(replaced_item, "/");
            if (selectedFile == "//sync")
            {
                newPath = filePaths.Split("*")[indexList[number]];
            }
            string ArgJson_start = JsonSerializer.Serialize(new { close = true });
            using (
                var request = new HttpRequestMessage(
                    new HttpMethod("POST"),
                    "https://content.dropboxapi.com/2/files/upload_session/start"
                )
            )
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                request.Headers.TryAddWithoutValidation("Dropbox-API-Arg", ArgJson_start);
                var byteFile = File.ReadAllBytes(item);

                request.Content = new ByteArrayContent(sssss);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    "application/octet-stream"
                );
                var response = await httpClient.SendAsync(request);

                var responseBody = await response.Content.ReadAsStringAsync();

                var json = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                if (json != null)
                {
                    string session_id = json["session_id"];
                    pid_upload_session.Add(session_id);
                }
            }
            if (File.Exists(selectedFile))
            {
                if (folderName != string.Empty)
                {
                    newPath = $"/{folderName}/{fileName}";
                }
                else
                {
                    newPath = $"/{fileName}";
                }
            }
            if (!File.ReadAllText(filePathsTxt).Contains(newPath))
            {
                File.AppendAllText(filePathsTxt, newPath + "*");
            }

            entries.Add(
                new
                {
                    commit = new
                    {
                        autorename = true,
                        mode = "add",
                        mute = false,
                        path = newPath,
                        strict_conflict = false,
                    },
                    cursor = new { offset, session_id = pid_upload_session[number] },
                }
            );
            number += 1;
        }
        using (
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "https://api.dropboxapi.com/2/files/upload_session/finish_batch_v2"
            )
        )
        {
            var ArgJson_finish = new { entries };
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(ArgJson_finish));

            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseBody);
        }
    }
}
