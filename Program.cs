using System.Net;
using DotNetEnv;

Env.Load();
string? appKey = Environment.GetEnvironmentVariable("DropboxClient_id");
string? appSecret = Environment.GetEnvironmentVariable("DropboxClient_secret");


    FilePathSelector filePath = new();

   string? selectedFile = filePath.selectFile();   
   string fileName = filePath.Filename(selectedFile);

HttpListener listener = new();
listener.Prefixes.Add("http://localhost:5081/");
listener.Start();

var httpClient = new HttpClient();

Dropbox_class dropbox_Class = new(listener,httpClient,appKey,appSecret,selectedFile,fileName);


dropbox_Class.ProcessStartInfo();

await dropbox_Class.GetToken();

await dropbox_Class.ListFolder();

await dropbox_Class.UploadSessionStart();

await dropbox_Class.UploadSessionFinish();

//-/home/mert/test/test123/mert.txt