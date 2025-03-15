using Android.Content;
using Android.Gms.Common;
using Android.Gms.Extensions;
using Android.Runtime;
using AndroidX.Lifecycle;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.UI;
using FirebaseUI.Auth.Viewmodel;

namespace FirebaseUI.Auth.Data.Remote;

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
    {
        activity.Finish(-1, (p0 as IdpResponse).ToIntent());
    }
}

public partial class KickoffActivityV2 : InvisibleActivityBase
{
    private SignInKickstarterV2 mKickstarter;
    public KickoffActivityV2() { }

    public static Intent CreateIntent(Context context, FlowParameters flowParams)
        => CreateBaseIntent(context, typeof(KickoffActivityV2).ToClass(), flowParams)!;

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (new ViewModelProvider(this).Get(typeof(SignInKickstarterV2).ToClass()) is SignInKickstarterV2 signInKickstarter)
            this.mKickstarter = signInKickstarter;

        this.mKickstarter.Init(this.FlowParams);
        this.mKickstarter.Operation.Observe(this, new IdpResponceObserver(this));

        if (this.FlowParams.IsPlayServicesRequired)
        {
            var checkPlayServicesTask = GoogleApiAvailability.Instance.MakeGooglePlayServicesAvailable(this).AsAsync();
            await checkPlayServicesTask;

            if (checkPlayServicesTask.IsCompletedSuccessfully)
            {
                if (savedInstanceState == null)
                {
                    this.mKickstarter.Start();
                }
            }
            else if (checkPlayServicesTask.IsFaulted)
            {
                this.Finish(0, IdpResponse.GetErrorIntent(new FirebaseUiException(2,
                    Java.Lang.Throwable.FromException(checkPlayServicesTask.Exception))));
            }
        }
    }

    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        this.OnActivityResult(requestCode, (int)resultCode, data!);
    }

    protected void OnActivityResult(int requestCode, int resultCode, Intent data)
    {
        if (requestCode == 106 && (resultCode == 113 || resultCode == 114))
        {
            this.invalidateEmailLink();
        }

        this.mKickstarter.OnActivityResult(requestCode, resultCode, data);
    }

    public void invalidateEmailLink()
    {
        FlowParameters flowParameters = this.FlowParams;
        flowParameters.EmailLink = null;
        this.Intent = this.Intent!.PutExtra("extra_flow_params", flowParameters);
    }
}
