using TestApp.My.Mobile.LocalServices.Interfaces;

namespace TestApp.My.Mobile.LocalServices.Services
{
    /// <summary>
    /// <see cref="SecureStorage"/> backed cache for tokens, tags and identifiers.
    /// </summary>
    public sealed class SecureStorageService : ISecureStorageService
    {
        public async Task<string?> GetCacheItemAsync(string key)
        {
            try
            {
                return await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task SaveItemToCacheAsync(string key, string? value)
            => SecureStorage.Default.SetAsync(key, value ?? string.Empty);

        public Task RemoveCacheItemAsync(string key)
        {
            SecureStorage.Default.Remove(key);
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessToken() => GetCacheItemAsync("AccessToken");

        public Task<string?> GetUserId() => GetCacheItemAsync("UserId");
    }
}
