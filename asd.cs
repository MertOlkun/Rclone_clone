/* using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;

class Program
{
    static async Task Main()
    {
        var listener = new HttpListener();
        listener.Start();
        listener.Prefixes.Add("http://localhost:5080/");

        var context = await listener.GetContextAsync();
        
        var client = new HttpClient();
        
        // Dropbox API URL
        var url = "https://api.dropbox.com/oauth2/token";
        
        // Request body (form data)
        var formData = new Dictionary<string, string>
        {
            { "code", "<AUTHORIZATION_CODE>" },
            { "grant_type", "authorization_code" },
            { "redirect_uri", "http://localhost:5080/" },
            { "client_id", "jbnnlr6pfg1o5wu" },
            { "client_secret", "wszp1kqr21cmdt3" }
        };

        // HTTP request oluşturuluyor
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        
        // Content-Type başlığı ekleniyor
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        
        // HTTP isteğini gönderme ve yanıtı almak
        var response = await client.SendAsync(request);
        
        // Yanıt kontrolü ve içerik okuma
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Yanıtın çıktısını yazdırma
        Console.WriteLine($"Response: {responseBody}");

        Console.WriteLine($"Context: {context}");
    }
}
 */