using System.Diagnostics;
using DotNetEnv;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;

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

HttpListenerContext httpContext = listener.GetContext();
if (httpContext.Request.Url is not null)
{
    string[] requestUrl = httpContext.Request.Url.ToString().Split("=");
    string code = requestUrl[1];
    Console.WriteLine(code);


using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.dropbox.com/oauth2/token"))
    {
        var contentList = new List<string>();
        contentList.Add($"code={code}");
        contentList.Add("grant_type=authorization_code");
        contentList.Add("redirect_uri=http://localhost:5081/");
        contentList.Add($"client_id={Environment.GetEnvironmentVariable("DropboxClient_id")}");
        contentList.Add($"client_secret={Environment.GetEnvironmentVariable("DropboxClient_secreet")}");
        request.Content = new StringContent(string.Join("&", contentList));
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"); 

        var response = await httpClient.SendAsync(request);
    }
}