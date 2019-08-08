using System.Net;

namespace Downloader
{
    class CustomWebClient : WebClient
    {
        public int XRowIndex { get; set; }
        public string XFileName { get; set; }

        public CustomWebClient() : base()
        {
        }

        public static bool RemoteUrlExists(string url)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    return (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
