using Android.Gms.Tasks;
using Task = Android.Gms.Tasks.Task;

namespace TestApp.My.Mobile.Platforms.Android
{
    /// <summary>
    /// Bridges a Google Play Services task completion callback to a plain delegate
    /// so the platform code does not have to implement <see cref="IOnCompleteListener"/> everywhere.
    /// </summary>
    public class PNSTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
    {
        private readonly Action<Task> _onComplete;

        public PNSTokenCompleteListener(Action<Task> onComplete) => _onComplete = onComplete;

        public void OnComplete(Task task) => _onComplete?.Invoke(task);
    }
}
