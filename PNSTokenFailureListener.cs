using Android.Gms.Tasks;

namespace TestApp.My.Mobile.Platforms.Android
{
    public class PNSTokenFailureListener : Java.Lang.Object, IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception?> _onFailure;

        public PNSTokenFailureListener(Action<Java.Lang.Exception?> onFailure) => _onFailure = onFailure;

        public void OnFailure(Java.Lang.Exception? e) => _onFailure?.Invoke(e);
    }
}
