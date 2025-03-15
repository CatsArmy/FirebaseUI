using Android.Content;
using Android.Gms.Common;
using Android.Gms.Extensions;
using Android.Runtime;
using AndroidX.Lifecycle;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.UI;
using Java.Lang;

namespace FirebaseUI.Auth.Data.Remote;

#pragma warning disable XAOBS001 // Type or member is obsolete

public partial class KickoffActivityV2 : InvisibleActivityBase
{
    private SignInKickstarterV2? Kickstarter;

    public static Intent CreateIntent(Context context, FlowParameters flowParams)
        => CreateBaseIntent(context, typeof(KickoffActivityV2).ToClass(), flowParams)!;

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (new ViewModelProvider(this).Get(typeof(SignInKickstarterV2).ToClass()) is not SignInKickstarterV2 signInKickstarter)
            throw new IllegalStateException();

        this.Kickstarter = signInKickstarter;

        this.Kickstarter.Init(this.FlowParams);
        this.Kickstarter!.Operation!.Observe(this, new IdpResponceObserver(this));

        if (this.FlowParams!.IsPlayServicesRequired)
        {
            var checkPlayServicesTask = GoogleApiAvailability.Instance.MakeGooglePlayServicesAvailable(this).AsAsync();
            await checkPlayServicesTask;

            if (checkPlayServicesTask.IsCompletedSuccessfully)
            {
                if (savedInstanceState != null)
                    return;

                this.Kickstarter.Start();
            }
            else if (checkPlayServicesTask.IsFaulted)
            {
                this.Finish(0, IdpResponse.GetErrorIntent(new FirebaseUiException(2,
                    Throwable.FromException(checkPlayServicesTask.Exception))));
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
            this.InvalidateEmailLink();
        }

        this.Kickstarter!.OnActivityResult(requestCode, resultCode, data);
    }

    public void InvalidateEmailLink()
    {
        var flowParameters = this.FlowParams!;
        flowParameters.EmailLink = null;
        this.Intent = this.Intent!.PutExtra("extra_flow_params", flowParameters);
    }
}

#pragma warning restore XAOBS001 // Type or member is obsolete
