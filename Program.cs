using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using DotNetEnv;

Env.Load();
string? appKey = Environment.GetEnvironmentVariable("DropboxClient_id");
string? appSecret = Environment.GetEnvironmentVariable("DropboxClient_secret");

FilePathSelector filePath = new();
string? selectedFile = filePath.selectFile();

string[] sf_name = selectedFile.Split('/');
string filename = sf_name[sf_name.Count() - 1];

string? folderName = string.Empty;

HttpListener listener = new();
listener.Prefixes.Add("http://localhost:5081/");
listener.Start();
var httpClient = new HttpClient();

Process.Start(
    new ProcessStartInfo()
    {
        FileName =
            "https://www.dropbox.com/oauth2/authorize?client_id=jbnnlr6pfg1o5wu&redirect_uri=http://localhost:5081/&response_type=code",
        UseShellExecute = true,
    }
);
string token = "";

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
        string? token1 = data["access_token"].ToString();
        token = token1 ?? string.Empty;
    }
}

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


{
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

        var folder_response = await response.Content.ReadAsStringAsync();
        var option = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        var json = JsonSerializer.Deserialize<Name>(folder_response, option);
        if (json != null)
        {
            Console.WriteLine("Dropbox folders:");
            foreach (var item in json.Entries)
        {
            Console.WriteLine(item.Name);
        }
            System.Console.WriteLine("Enter folder name.");
            folderName = Console.ReadLine();
        }
        else
        {
            Console.WriteLine("Folder not found in Dropbox.");
        }
        
        
    }
}

//-/home/mert/test/test123/mert


string pid_upload_session;
string ArgJson_start = JsonSerializer.Serialize(new { close = false });
using (
    var request = new HttpRequestMessage(
        new HttpMethod("POST"),
        "https://content.dropboxapi.com/2/files/upload_session/start"
    )
)
{
    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
    request.Headers.TryAddWithoutValidation("Dropbox-API-Arg", ArgJson_start);
    var byteFile = File.ReadAllBytes(selectedFile); //add a selectedFile varible
    request.Content = new ByteArrayContent(byteFile);
    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
    var response = await httpClient.SendAsync(request);

    var responseBody = await response.Content.ReadAsStringAsync();

    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);

    string session_id = json["session_id"];

    pid_upload_session = session_id;
}

string ArgJson_finish = JsonSerializer.Serialize(
    new
    {
        commit = new
        {
            autorename = true,
            mode = "add",
            mute = false,
            path = $"/{folderName}/{filename}",
            strict_conflict = false,
        },
        cursor = new { offset = 14, session_id = pid_upload_session },
    }
);

 using (
    var request = new HttpRequestMessage(
        new HttpMethod("POST"),
        "https://content.dropboxapi.com/2/files/upload_session/finish"
    )
)
{
    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
    request.Headers.TryAddWithoutValidation("Dropbox-API-Arg", ArgJson_finish);
    var byteFile = File.ReadAllBytes(selectedFile);
    request.Content = new ByteArrayContent(byteFile);
    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

    var response = await httpClient.SendAsync(request);
    System.Console.WriteLine(ArgJson_finish);

    var responseBody = await response.Content.ReadAsStringAsync();
    System.Console.WriteLine("FFFFFFFFF" + responseBody);
}
 
