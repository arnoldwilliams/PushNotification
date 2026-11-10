namespace TestApp.My.Mobile.LocalServices.Interfaces
{
    /// <summary>
    /// Small key/value cache backed by <see cref="SecureStorage"/>.
    /// </summary>
    public interface ISecureStorageService
    {
        Task<string?> GetCacheItemAsync(string key);

        Task SaveItemToCacheAsync(string key, string? value);

        Task RemoveCacheItemAsync(string key);

        Task<string?> GetAccessToken();

        Task<string?> GetUserId();
    }
}
