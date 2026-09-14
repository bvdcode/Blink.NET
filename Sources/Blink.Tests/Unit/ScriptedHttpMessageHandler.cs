using System.Net;
using Xunit;

namespace Blink.Tests.Unit
{
    internal class ScriptedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> _steps = new();

        public HttpClient CreateClient() => new(this, disposeHandler: false);

        public void Expect(HttpMethod method, string url, HttpStatusCode status, string body = "",
            Action<HttpRequestMessage, string>? inspect = null, string? location = null)
        {
            _steps.Enqueue(async request =>
            {
                Assert.Equal(method, request.Method);
                Assert.Equal(url, request.RequestUri!.GetLeftPart(UriPartial.Path));
                string content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
                inspect?.Invoke(request, content);

                HttpResponseMessage response = new(status) { Content = new StringContent(body) };
                if (location is not null)
                {
                    response.Headers.Location = new Uri(location);
                }
                return response;
            });
        }

        public void AssertComplete() => Assert.Empty(_steps);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotEmpty(_steps);
            return _steps.Dequeue()(request);
        }
    }
}
