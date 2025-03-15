using Android.Content;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.Viewmodel;

namespace FirebaseUI.Auth.Data.Remote;
#pragma warning disable XAOBS001 // Type or member is obsolete

public class IdpResponceObserver(KickoffActivityV2 activity) : ResourceObserver(activity)
{
    protected override void OnFailure(Java.Lang.Exception e)
    {
        switch (e)
        {
            case UserCancellationException:
                activity!.Finish(0, null);
                break;
            case FirebaseAuthAnonymousUpgradeException:
                activity.Finish(0, new Intent().PutExtra("extra_idp_response",
                    ((FirebaseAuthAnonymousUpgradeException)e).Response));
                break;

            default:
                activity.Finish(0, IdpResponse.GetErrorIntent(e));
                break;
        }
    }

    protected override void OnSuccess(Java.Lang.Object p0)
        => activity.Finish(-1, ((IdpResponse)p0).ToIntent());
}

#pragma warning restore XAOBS001 // Type or member is obsolete
