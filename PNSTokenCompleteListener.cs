using Android.Gms.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = Android.Gms.Tasks.Task;

namespace TestApp.My.Mobile.Platforms.Android
{
    public class PNSTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
    {

        private readonly Action<Task> _onComplete;

        public PNSTokenCompleteListener(Action<Task> onComplete) => _onComplete = onComplete;

        public void OnComplete(Task task) => _onComplete?.Invoke(task);
    }
}
