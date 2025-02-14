using System.Net;
using DotNetEnv;

Env.Load();
string? appKey = Environment.GetEnvironmentVariable("DropboxClient_id");
string? appSecret = Environment.GetEnvironmentVariable("DropboxClient_secret");
string? hostAndPort= Environment.GetEnvironmentVariable("HostAndPort");
if (appKey != null && appSecret != null && hostAndPort != null)
{
    FilePathSelector filePath = new();

string? selectedFile = filePath.selectFile();   
string fileName = filePath.Filename(selectedFile);

HttpListener listener = new();
listener.Prefixes.Add(hostAndPort);
listener.Start();

var httpClient = new HttpClient();



Dropbox_class dropbox_Class = new(listener,httpClient,appKey,appSecret,hostAndPort,selectedFile,fileName);


dropbox_Class.ProcessStartInfo();

await dropbox_Class.GetToken();
//dropbox_Class.sdsd();
await dropbox_Class.CreateFolder();

//await dropbox_Class.ListFolder();

//await dropbox_Class.UploadSessionStart();

//await dropbox_Class.UploadSessionFinish();

}
else
{
    Console.WriteLine(".env null or wrong");
}

//-/home/mert/test/test123/mert.txt