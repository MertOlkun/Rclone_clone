using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public class Dropbox_class
{
    HttpListener listener;
    HttpClient httpClient;
    string appKey;
    string appSecret;
    string selectedFile;
    string fileName;
    
    public Dropbox_class(HttpListener listener,HttpClient httpClient,string appKey,string appSecret,string selectedFile,string fileName)
    {
        this.listener = listener;
        this.httpClient = httpClient;
        this.appKey = appKey;
        this.appSecret = appSecret;
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
                    "https://www.dropbox.com/oauth2/authorize?client_id=jbnnlr6pfg1o5wu&redirect_uri=http://localhost:5081/&response_type=code",
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
                contentList.Add("redirect_uri=http://localhost:5081/");
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
            if (json != null)
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

            string session_id = json["session_id"];

            pid_upload_session = session_id;
            offset = byteFile.Count();
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
            Console.WriteLine("\n\n\nDone!\n" + responseBody);
        }
    }
}
