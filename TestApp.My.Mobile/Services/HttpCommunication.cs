using System.Net.Http.Headers;
using System.Text;
using TestApp.My.Mobile.Interfaces;

namespace TestApp.My.Mobile.Services
{
    /// <summary>
    /// <see cref="HttpClient"/> based implementation of <see cref="IHttpCommunication"/>.
    /// </summary>
    public sealed class HttpCommunication : IHttpCommunication
    {
        private readonly HttpClient _httpClient;

        public HttpCommunication(HttpClient httpClient) => _httpClient = httpClient;

        public Task<HttpResponseMessage> PostAsync(Uri uri, object? content, string? accessToken)
            => SendAsync(HttpMethod.Post, uri, content, accessToken);

        public Task<HttpResponseMessage> GetAsync(Uri uri, string accessToken)
            => SendAsync(HttpMethod.Get, uri, null, accessToken);

        public Task<HttpResponseMessage> DeleteAsync(Uri uri, string accessToken)
            => SendAsync(HttpMethod.Delete, uri, null, accessToken);

        private Task<HttpResponseMessage> SendAsync(HttpMethod method, Uri uri, object? content, string? accessToken)
        {
            var request = new HttpRequestMessage(method, uri);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (content is not null)
            {
                request.Content = content as HttpContent
                    ?? new StringContent(JsonSerializerHelper.Serialize(content), Encoding.UTF8, "application/json");
            }

            return _httpClient.SendAsync(request);
        }
    }

    internal static class JsonSerializerHelper
    {
        public static string Serialize(object value)
            => System.Text.Json.JsonSerializer.Serialize(value);
    }
}
