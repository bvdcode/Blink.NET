using System;
using System.Net;
using System.Net.Http;

namespace Blink
{
    public partial class BlinkClient
    {
        private readonly Func<HttpClient> _httpClientFactory;
        private readonly Func<HttpClient> _oauthHttpClientFactory;

        /// <summary>
        /// Creates a client for the Blink API.
        /// </summary>
        public BlinkClient() : this(() => new HttpClient(), CreateOAuthHttpClient)
        {
        }

        internal BlinkClient(Func<HttpClient> httpClientFactory, Func<HttpClient> oauthHttpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _oauthHttpClientFactory = oauthHttpClientFactory;
        }

        private static HttpClient CreateOAuthHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
            };

            return new HttpClient(handler, disposeHandler: true);
        }
    }
}
