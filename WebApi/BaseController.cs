using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestWebApi.Controllers
{
    public class BaseController : ApiController
    {
        private NameValueCollection _queryString;

        private NameValueCollection _form;

        private ConcurrentDictionary<string, byte[]> _files;

        private byte[] _stream;

        protected NameValueCollection RequestQuery()
        {
            if (_queryString == null)
            {
                _queryString = new NameValueCollection();
                var data = Request.GetQueryNameValuePairs();
                foreach (var item in data)
                {
                    _queryString.Add(item.Key, item.Value);
                }
            }
            return _queryString;
        }

        protected async Task<NameValueCollection> RequestForm()
        {
            return _form ?? (_form = await Request.Content.ReadAsFormDataAsync());
        }

        protected async Task<ConcurrentDictionary<string, byte[]>> RequestFiles()
        {
            if (_files == null)
            {
                _files = new ConcurrentDictionary<string, byte[]>();
                _form = new NameValueCollection();

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);
                foreach (var item in provider.Contents)
                {
                    var bytes = await item.ReadAsByteArrayAsync();

                    if (item.Headers.ContentDisposition.FileName == null) //post参数
                    {
                        var name = item.Headers.ContentDisposition.Name.Replace("\"", "");
                        var value = Encoding.UTF8.GetString(bytes);
                        _form.Add(name, value);
                    }
                    else //文件  
                    {
                        if (bytes.Length != 0)
                        {
                            var name = item.Headers.ContentDisposition.FileName.Replace("\"", ""); //文件名称
                            _files.TryAdd(name, bytes);
                        }
                    }
                }
            }
            return _files;

        }

        protected async Task<byte[]> RequestStream()
        {
            return _stream ?? (_stream = await Request.Content.ReadAsByteArrayAsync());
        }

        #region 输出

        protected HttpResponseMessage StringData(string data)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(data, Encoding.UTF8, "text/plain")
                //Content = new StringContent(jsonStr, Encoding.UTF8, "text/json")
            };
            return result;
        }

        protected HttpResponseMessage FileData(string path, string fileName = null)
        {
            if (fileName == null)
                fileName = Path.GetFileName(path);

            var stream = new FileStream(path, FileMode.Open);
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            return result;
        }

        protected HttpResponseMessage FileData(Stream stream, string fileName)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            return result;
        }  


        #endregion


    }
}
