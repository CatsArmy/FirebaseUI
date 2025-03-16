using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Runtime;
using AndroidX.Core.View;
using Firebase.Auth;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.UI.Idp;
using FirebaseUI.Auth.Util.Data;
using Google.Android.Material.TextField;
using Java.Lang;

namespace FirebaseUI.Auth.UI.Email;

#pragma warning disable XAOBS001 // Type or member is obsolete

[Activity()]
public class EmailActivityV2 : AppCompatBase,
    CheckEmailFragment.ICheckEmailListener,
    RegisterEmailFragment.IAnonymousUpgradeListener,
    EmailLinkFragment.ITroubleSigningInListener,
    TroubleSigningInFragment.IResendEmailListener
{
    private IList<AuthUI.IdpConfig> Providers
    {
        get
        {
            IList<AuthUI.IdpConfig> providers = [];
            foreach (AuthUI.IdpConfig idpConfig in this.FlowParams!.Providers)
            {
                providers.Add(idpConfig);
            }

            return providers;
        }
    }
    public EmailActivityV2()
    {
    }

    public static Intent CreateIntent(Context context, FlowParameters flowParams)
    {
        return CreateBaseIntent(context, typeof(EmailActivityV2).Class(), flowParams)!;
    }

    public static Intent CreateIntent(Context context, FlowParameters flowParams, string email)
    {
        return CreateBaseIntent(context, typeof(EmailActivityV2).Class(), flowParams)!.PutExtra("extra_email", email);
    }

    public static Intent CreateIntentForLinking(Context context, FlowParameters flowParams, IdpResponse responseForLinking)
    {
        return CreateIntent(context, flowParams, responseForLinking.Email!).PutExtra("extra_idp_response", responseForLinking);
    }

    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContentView(Resource.Layout.fui_activity_register_email);
        if (savedInstanceState == null)
        {
            var email = this.Intent!.Extras!.GetString("extra_email");
            if (email != null && this.Intent!.Extras!.GetParcelable("extra_idp_response") is IdpResponse responseForLinking)
            {
                var emailConfig = ProviderUtils.GetConfigFromIdpsOrThrow(this.Providers, "emailLink");
                var actionCodeSettings = emailConfig.Params
                    .GetParcelable("action_code_settings") as ActionCodeSettings;
                EmailLinkPersistenceManager.Instance!.SaveIdpResponseForLinking(this.Application!, responseForLinking);
                var forceSameDevice = emailConfig.Params.GetBoolean("force_same_device");
                var fragment = EmailLinkFragment.NewInstance(email, actionCodeSettings!,
                    responseForLinking, forceSameDevice);
                this.SwitchFragment(fragment!, Resource.Id.fragment_register_email, "EmailLinkFragment");
            }
            else
            {
                AuthUI.IdpConfig emailConfig = ProviderUtils.GetConfigFromIdps(this.Providers, "password")!;
                if (emailConfig != null)
                {
                    email = emailConfig.Params.GetString("extra_default_email");
                }

                var fragment = CheckEmailFragment.NewInstance(email);
                this.SwitchFragment(fragment!, Resource.Id.fragment_register_email, "CheckEmailFragment");
            }
        }
    }

    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        this.OnActivityResult(requestCode, (int)requestCode, data!);
    }

    protected void OnActivityResult(int requestCode, int resultCode, Intent data)
    {
        if (requestCode == 104 || requestCode == 103)
        {
            this.Finish(resultCode, data);
        }
    }

    public void OnExistingEmailUser(User user)
    {
        if (user.ProviderId == "emailLink")
        {
            AuthUI.IdpConfig emailConfig = ProviderUtils.GetConfigFromIdpsOrThrow(this.Providers, "emailLink");
            this.ShowRegisterEmailLinkFragment(emailConfig, user.Email!);
        }
        else
        {
            this.StartActivityForResult(WelcomeBackPasswordPrompt.CreateIntent(this, this.FlowParams,
                new IdpResponse.Builder(user).Build()), 104);
            this.SetSlideAnimation();
        }

    }

    public void OnExistingIdpUser(User user)
    {
        this.StartActivityForResult(WelcomeBackIdpPrompt.CreateIntent(this, this.FlowParams, user), 103);
        this.SetSlideAnimation();
    }

    public void OnNewUser(User user)
    {
        var emailLayout = this.FindViewById<TextInputLayout>(Resource.Id.email_layout);
        var emailConfig = ProviderUtils.GetConfigFromIdps(this.Providers, "password");
        emailConfig ??= ProviderUtils.GetConfigFromIdps(this.Providers, "emailLink");

        if (emailConfig!.Params.GetBoolean("extra_allow_new_emails", true))
        {
            var ft = this.SupportFragmentManager.BeginTransaction();
            if (emailConfig.ProviderId.Equals("emailLink"))
            {
                this.ShowRegisterEmailLinkFragment(emailConfig, user.Email!);
            }
            else
            {
                var fragment = RegisterEmailFragment.NewInstance(user);
                ft.Replace(Resource.Id.fragment_register_email, fragment!, "RegisterEmailFragment");
                if (emailLayout != null)
                {
                    var emailFieldName = this.GetString(Resource.String.fui_email_field_name);
                    ViewCompat.SetTransitionName(emailLayout, emailFieldName);
                    ft.AddSharedElement(emailLayout, emailFieldName);
                }

                ft.DisallowAddToBackStack().Commit();
            }
        }
        else
        {
            emailLayout!.Error = (this.GetString(Resource.String.fui_error_email_does_not_exist));
        }

    }

    public void OnTroubleSigningIn(string email)
    {
        var troubleSigningInFragment = TroubleSigningInFragment.NewInstance(email);
        this.SwitchFragment(troubleSigningInFragment!,
            Resource.Id.fragment_register_email, "TroubleSigningInFragment", true, true);
    }

    public void OnClickResendEmail(string email)
    {
        if (this.SupportFragmentManager.BackStackEntryCount > 0)
        {
            this.SupportFragmentManager.PopBackStack();
        }

        var emailConfig = ProviderUtils.GetConfigFromIdpsOrThrow(this.Providers, "emailLink");
        this.ShowRegisterEmailLinkFragment(emailConfig, email);
    }

    public void OnSendEmailFailure(Java.Lang.Exception e)
    {
        this.FinishOnDeveloperError(e);
    }

    public void OnDeveloperFailure(Java.Lang.Exception e)
    {
        this.FinishOnDeveloperError(e);
    }

    private void FinishOnDeveloperError(Java.Lang.Exception e)
    {
        this.Finish(0, IdpResponse.GetErrorIntent(new FirebaseUiException(3, e.Message!)));
    }

    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
    private void SetSlideAnimation()
    {
        this.OverridePendingTransition(Resource.Animation.fui_slide_in_right, Resource.Animation.fui_slide_out_left);
    }

    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
    private void ShowRegisterEmailLinkFragment(AuthUI.IdpConfig emailConfig, string email)
    {
        var actionCodeSettings = emailConfig.Params.GetParcelable("action_code_settings") as ActionCodeSettings;
        var fragment = EmailLinkFragment.NewInstance(email, actionCodeSettings!);
        this.SwitchFragment(fragment!, Resource.Id.fragment_register_email, nameof(EmailLinkFragment));
    }

    public override void ShowProgress(int message)
    {
        throw new UnsupportedOperationException("Email fragments must handle progress updates.");
    }

    public override void HideProgress()
    {
        throw new UnsupportedOperationException("Email fragments must handle progress updates.");
    }

    public void OnMergeFailure(IdpResponse response)
    {
        this.Finish(5, response.ToIntent());
    }
}

#pragma warning restore XAOBS001 // Type or member is obsolete

