namespace TestApp.My.Mobile.Interfaces
{
    /// <summary>
    /// Minimal HTTP abstraction so the registration service can be unit tested.
    /// </summary>
    public interface IHttpCommunication
    {
        Task<HttpResponseMessage> PostAsync(Uri uri, object? content, string? accessToken);

        Task<HttpResponseMessage> GetAsync(Uri uri, string accessToken);

        Task<HttpResponseMessage> DeleteAsync(Uri uri, string accessToken);
    }
}
