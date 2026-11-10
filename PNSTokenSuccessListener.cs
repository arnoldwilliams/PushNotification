using Android.Gms.Tasks;

namespace TestApp.My.Mobile.Platforms.Android
{
    public class PNSTokenSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly Action<string> _onToken;

        public PNSTokenSuccessListener(Action<string> onToken) => _onToken = onToken;

        public void OnSuccess(Java.Lang.Object? result)
        {
            var token = result?.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                _onToken?.Invoke(token!);
            }
        }
    }
}
