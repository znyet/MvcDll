using System;
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

        private async Task RequestCommon()
        {
            _files = new ConcurrentDictionary<string, byte[]>();
            if (Request.Content.IsFormData())
            {
                _form = await Request.Content.ReadAsFormDataAsync();
            }
            else if (Request.Content.IsMimeMultipartContent())
            {
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
            else
            {
                _form = new NameValueCollection();
            }
        }

        protected NameValueCollection RequestQuery()
        {
            if (_queryString != null)
                return _queryString;
            _queryString = new NameValueCollection();
            var data = Request.GetQueryNameValuePairs();
            foreach (var item in data)
            {
                _queryString.Add(item.Key, item.Value);
            }
            return _queryString;
        }

        protected async Task<NameValueCollection> RequestForm()
        {
            if (_form != null)
                return _form;
            await RequestCommon();
            return _form;
        }

        protected async Task<ConcurrentDictionary<string, byte[]>> RequestFiles()
        {
            if (_files != null)
                return _files;
            await RequestCommon();
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
            FileStream stream = null;
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                if (fileName == null)
                    fileName = Path.GetFileName(path);

                stream = new FileStream(path, FileMode.Open);
                result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream)
                };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                return result;
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Dispose();
                result.Content = new StringContent(ex.Message, Encoding.UTF8, "text/plain");
                return result;
            }


        }

        protected HttpResponseMessage FileData(Stream stream, string fileName)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                return result;
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Dispose();
                result.Content = new StringContent(ex.Message, Encoding.UTF8, "text/plain");
                return result;
            }

        }

        protected HttpResponseMessage StaticData(string url)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            FileStream stream = null;
            try
            {
                var ext = Path.GetExtension(url);
                var path = AppDomain.CurrentDomain.BaseDirectory + url;

                if (ext == ".html" || ext == ".htm")
                {
                    var html = File.ReadAllText(path);
                    result.Content = new StringContent(html, Encoding.UTF8, "text/html");
                    return result;
                }

                string mediaType;
                switch (ext)
                {
                    case ".js":
                        mediaType = "application/x-javascript";
                        break;
                    case ".css":
                        mediaType = "text/css";
                        break;
                    case ".jpg":
                        mediaType = "image/jpeg";
                        break;
                    case ".gif":
                        mediaType = "image/gif";
                        break;
                    case ".png":
                        mediaType = "image/png";
                        break;
                    case ".zip":
                        mediaType = "application/zip";
                        break;
                    case ".rar":
                        mediaType = "application/x-rar-compressed";
                        break;
                    default:
                        mediaType = "application/octet-stream";
                        break;
                }

                var fileName = Path.GetFileName(url);
                stream = new FileStream(path, FileMode.Open);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
                return result;

            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Dispose();

                result.Content = new StringContent(ex.Message, Encoding.UTF8, "text/plain");
                return result;
            }

        }

        #endregion


    }
}
