using Android.Gms.Tasks;

namespace FirebaseUI.Auth.Data.Remote;

public class OnCompleteListener(Action<Android.Gms.Tasks.Task> action) : Java.Lang.Object, IOnCompleteListener
{
    public void OnComplete(Android.Gms.Tasks.Task task) => action(task);
}
#pragma warning restore XAOBS001 // Type or member is obsolete
