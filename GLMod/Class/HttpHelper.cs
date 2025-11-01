using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GLMod.Class
{
    public static class HttpHelper
    {
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            Proxy = null,
            UseProxy = false
        };

        public static readonly HttpClient Client = new HttpClient(handler);
    }
}
