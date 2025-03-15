﻿using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Gms.Auth.Api.Credentials;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Android.Text;
using Firebase.Auth;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.UI.Email;
using FirebaseUI.Auth.UI.Idp;
using FirebaseUI.Auth.UI.Phone;
using FirebaseUI.Auth.Util;
using FirebaseUI.Auth.Util.Data;
using FirebaseUI.Auth.Viewmodel;
using Java.Lang;

namespace FirebaseUI.Auth.Data.Remote;

#pragma warning disable XAOBS001 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

public partial class SignInKickstarterV2(Application application) : SignInViewModelBase(application)
{
    private void StartAuthMethodChoice()
    {
        if (!((FlowParameters)this.Arguments!).ShouldShowProviderChoice())
        {

            AuthUI.IdpConfig firstIdpConfig = ((FlowParameters)this.Arguments).DefaultOrFirstProvider!;
            switch (firstIdpConfig!.ProviderId)
            {
                case "password" or AuthUI.EmailLinkProvider:
                    this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(EmailActivity.CreateIntent(
                        this.Application as Application, (FlowParameters)this.Arguments)!, 106)));
                    break;

                case "phone":
                    this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(PhoneActivity.CreateIntent
                        (this.Application as Application, (FlowParameters)this.Arguments, firstIdpConfig.Params)!, 107)));
                    break;

                default:
                    this.RedirectSignIn(default, Java.Lang.String.ValueOf(null));
                    break;
            }
            return;
        }

        this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(AuthMethodPickerActivity.CreateIntent(
            this.Application as Application, (FlowParameters)this.Arguments)!, 105)));
    }

    private void RedirectSignIn(string? provider, string id)
    {
        if (provider is null)
            return;
        switch (provider)
        {
            case AuthUI.PasswordProvider:
                this.SetResult(Model.Resource.ForFailure(
                    new IntentRequiredException(EmailActivity.CreateIntent(this.Application as Application,
                    (FlowParameters)this.Arguments!, id)!, 106)));
                break;

            case AuthUI.PhoneProvider:
                var args = new Bundle();
                args.PutString("extra_phone_number", id);
                this.SetResult(Model.Resource.ForFailure(
                    new IntentRequiredException(PhoneActivity.CreateIntent(this.Application as Application,
                    (FlowParameters)this.Arguments!, args)!, 107)));
                break;

            default:
                this.SetResult(Model.Resource.ForFailure(
                    new IntentRequiredException(SingleSignInActivity.CreateIntent(this.Application as Application,
                    (FlowParameters)this.Arguments!, new User.Builder(provider, id).Build())!, 109)));
                break;
        }
    }

    private List<string> GetCredentialAccountTypes()
    {
        List<string> accounts = [];

        foreach (AuthUI.IdpConfig idpConfig in (this.Arguments as FlowParameters)!.Providers)
        {
            var providerId = idpConfig.ProviderId;
            if (providerId == AuthUI.GoogleProvider)
            {
                accounts.Add(ProviderUtils.ProviderIdToAccountType(providerId)!);
            }
        }

        return accounts;
    }

    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "<Pending>")]
    public void OnActivityResult(int requestCode, int resultCode, Intent data)
    {
        switch (requestCode)
        {
            case 101:
                if (resultCode == -1)
                {
                    this.HandleCredential((data.GetParcelableExtra("com.google.android.gms.credentials.Credential") as Credential)!);
                    break;
                }
                this.StartAuthMethodChoice();
                break;

            case 102 or 103 or 104 or 108:
                break;

            default: break;

            case 105 or 106 or 107 or 109:
                if (resultCode == 113 || resultCode == 114)
                {
                    this.StartAuthMethodChoice();
                    return;
                }

                var response = IdpResponse.FromResultIntent(data);
                if (response == null)
                {
                    this.SetResult(Model.Resource.ForFailure(new UserCancellationException()));
                }
                else if (response.IsSuccessful)
                {
                    this.SetResult(Model.Resource.ForSuccess(response));
                }
                else if (response.Error?.ErrorCode == 5)
                {
                    this.HandleMergeFailure(response);
                }
                else
                {
                    this.SetResult(Model.Resource.ForFailure(response.Error!));
                }
                break;
        }

    }

    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "<Pending>")]
    private async void HandleCredential(Credential credential)
    {
        var id = credential.Id;
        var password = credential.Password;
        if (TextUtils.IsEmpty(password))
        {
            var identity = credential.AccountType;
            if (identity == null)
            {
                this.StartAuthMethodChoice();
                return;
            }
            this.RedirectSignIn(ProviderUtils.AccountTypeToProviderId(credential.AccountType), id);
            return;
        }

        var response = new IdpResponse.Builder(new User.Builder(AuthUI.PasswordProvider, id)!.Build()!)!.Build();
        this.SetResult(Model.Resource.ForLoading());
        var task = this.Auth!.SignInWithEmailAndPasswordAsync(id, password);
        var result = await task;
        if (task.IsCompletedSuccessfully)
        {
            this.HandleSuccess(response!, result);
        }
        else if (task.IsFaulted)
        {
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
            if (task.Exception is FirebaseAuthInvalidUserException || task.Exception is FirebaseAuthInvalidCredentialsException)
#pragma warning restore CS0184 // 'is' expression's given expression is never of the provided type
            {
                await GoogleApiUtils.GetCredentialsClient((this.Application as Application)!).Delete(credential);
            }

            this.StartAuthMethodChoice();
        }
    }

    public async void Start()
    {
        if (!TextUtils.IsEmpty(((FlowParameters)this.Arguments!).EmailLink))
        {
            this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(EmailLinkCatcherActivity.CreateIntent(
                this.Application as Application, (FlowParameters)this.Arguments)!, 106)));
            return;
        }

        var pendingResultTask = this.Auth?.GetPendingAuthResult()?.AsAsync<IAuthResult>();
        if (pendingResultTask != null)
        {
            var authResult = await pendingResultTask;
            if (pendingResultTask.IsCompletedSuccessfully)
            {
                var response = new IdpResponse.Builder(new User.Builder(authResult.Credential?.Provider!,
                    authResult.User?.Email).Build()!).Build()!;
                this.HandleSuccess(response, authResult);
            }

            if (pendingResultTask.IsFaulted)
                this.SetResult(Model.Resource.ForFailure(new(Throwable.FromException(pendingResultTask.Exception))));
            return;
        }

        var supportPasswords = null != ProviderUtils.GetConfigFromIdps((this.Arguments as FlowParameters)!.Providers
            as IList<AuthUI.IdpConfig>, AuthUI.PasswordProvider);
        var accountTypes = this.GetCredentialAccountTypes();
        if (!((FlowParameters)this.Arguments).EnableCredentials || !supportPasswords && accountTypes.Count <= 0)
        {
            this.StartAuthMethodChoice();
            return;
        }

        this.SetResult(Model.Resource.ForLoading());
        await GoogleApiUtils.GetCredentialsClient((this.Application as Application)!)
            .Request(new CredentialRequest.Builder()
            .SetPasswordLoginSupported(supportPasswords)
            .SetAccountTypes([.. accountTypes]).Build())
            .AddOnCompleteListener(new OnCompleteListener((task) =>
            {
                try
                {
                    this.HandleCredential((task.GetResult(typeof(ApiException).ToClass()) as CredentialRequestResponse)!.Credential);
                }
                catch (ResolvableApiException e)
                {
                    if (e.StatusCode != 6)
                    {
                        this.StartAuthMethodChoice();
                        return;
                    }

                    this.SetResult(Model.Resource.ForFailure(new PendingIntentRequiredException(e.Resolution, 101)));
                    return;
                }
                catch (ApiException)
                {
                    this.StartAuthMethodChoice();
                }
            })).AsAsync();
    }
}
#pragma warning restore XAOBS001 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
