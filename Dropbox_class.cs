using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Json;

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
    string? token1 = string.Empty;
    string pid_upload_session = string.Empty;
    int offset;

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
                    token1 = data["access_token"].ToString();
                }
            }
        }
    }

    public void sdsd()
    {
        /* string a = "asdfghjklş";
        string b = "asdf";
        string x = a-b;
        string[] y; 
        if (selectedFile.Contains('\\'))
        {
           y =  selectedFile.Split('\\');
        }else
        {
           y = selectedFile.Split('/');
        }
       Console.WriteLine( y.Last());
        string s = y.Last(); */

        var xs = Directory.GetDirectories(selectedFile, "*",SearchOption.AllDirectories);
        foreach (var item in xs)
        {
            System.Console.WriteLine(item);
            //s+= ""+ item.Split("/").Last();

        } 
    }

    public async Task CreateFolder()
    {    

        string[] allDirectories = Directory.GetDirectories(selectedFile, "*",SearchOption.AllDirectories);
        string? slash;
        if (selectedFile.Contains("/"))
        {
            slash = "/";
        }
        else
        {
            slash = "\\";
        }
        
          string a = selectedFile.Split(slash)[selectedFile.Split(slash).Count()-1];
            string b = selectedFile.Split(a)[0];
         foreach (var item in allDirectories)
        {
          string deletedString =item.Replace(b,slash);
            string ArgJson_create_folder = JsonSerializer.Serialize(
            new { autorename = false, path = deletedString}
        );
        using (
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "https://api.dropboxapi.com/2/files/create_folder_v2"
            )
        )        
        {
            request.Headers.TryAddWithoutValidation($"Authorization", $"Bearer {token1}");

            request.Content = new StringContent(ArgJson_create_folder);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var response = await httpClient.SendAsync(request);
            var responsebody = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine(responsebody);   
        }     
        } 
    }

    public async Task ListFolder()
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
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token1}");

            request.Content = new StringContent(ArgJson_list_folder);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

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

    public async Task UploadSessionStart()
    {
        string ArgJson_start = JsonSerializer.Serialize(new { close = false });
        using (
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "https://content.dropboxapi.com/2/files/upload_session/start"
            )
        )
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token1}");
            request.Headers.TryAddWithoutValidation("Dropbox-API-Arg", ArgJson_start);
            var byteFile = File.ReadAllBytes(selectedFile); //add a selectedFile varible

            request.Content = new ByteArrayContent(byteFile);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                "application/octet-stream"
            );
            var response = await httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
            if (json != null)
            {
                string session_id = json["session_id"];
                pid_upload_session = session_id;
                offset = byteFile.Count();
            }
        }
    }

    public async Task UploadSessionFinish()
    {
        string ArgJson_finish = JsonSerializer.Serialize(
            new
            {
                commit = new
                {
                    autorename = true,
                    mode = "add",
                    mute = false,
                    path = $"/{folderName}/{fileName}",
                    strict_conflict = false,
                },
                cursor = new { offset = offset, session_id = pid_upload_session },
            }
        );

        using (
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "https://content.dropboxapi.com/2/files/upload_session/finish"
            )
        )
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token1}");
            request.Headers.TryAddWithoutValidation("Dropbox-API-Arg", ArgJson_finish);

            request.Content = new ByteArrayContent(File.ReadAllBytes(selectedFile));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                "application/octet-stream"
            );

            var response = await httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody[2].ToString() == "e")
            {
                Console.WriteLine("\n\n\nError!\n" + responseBody);
            }
            else
            {
                Console.WriteLine("\n\n\nDone!\n" + responseBody);
            }
        }
    }
}
