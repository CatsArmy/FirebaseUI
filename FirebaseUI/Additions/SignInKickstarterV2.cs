namespace FirebaseUI.Auth.Data.Remote;

#pragma warning disable XAOBS001 // Type or member is obsolete
public partial class SignInKickstarterV2(Application application) : SignInViewModelBase(application)
{
    private void startAuthMethodChoice()
    {
        if (!((FlowParameters)this.Arguments!).ShouldShowProviderChoice())
        {
            AuthUI.IdpConfig firstIdpConfig = ((FlowParameters)this.Arguments).DefaultOrFirstProvider!;
            switch (firstIdpConfig!.ProviderId)
            {
                case "emailLink":
                case "password":
                    this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(EmailActivity.CreateIntent(
                        this.Application as Application, (FlowParameters)this.Arguments)!, 106)));
                    break;
                case "phone":
                    this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(PhoneActivity.CreateIntent
                        (this.Application as Application, (FlowParameters)this.Arguments, firstIdpConfig.Params)!, 107)));
                    break;
                default:
                    this.redirectSignIn(default, Java.Lang.String.ValueOf(null));
                    break;
            }
            return;
        }

        this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(AuthMethodPickerActivity.CreateIntent(
            this.Application as Application, (FlowParameters)this.Arguments)!, 105)));
    }

    private void redirectSignIn(String provider, String id)
    {
        switch (provider)
        {
            case "password":
                this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(EmailActivity.CreateIntent(this.Application as Application, (FlowParameters)this.Arguments, id), 106)));
                break;
            case "phone":
                Bundle args = new Bundle();
                args.PutString("extra_phone_number", id);
                this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(PhoneActivity.CreateIntent(this.Application as Application, (FlowParameters)this.Arguments, args), 107)));
                break;

            default:
                this.SetResult(Model.Resource.ForFailure(new IntentRequiredException(SingleSignInActivity.CreateIntent(this.Application as Application, (FlowParameters)this.Arguments, (new User.Builder(provider, id)).Build()), 109)));
                break;
        }

    }

    private List<string> getCredentialAccountTypes()
    {
        List<string> accounts = [];

        foreach (AuthUI.IdpConfig idpConfig in ((FlowParameters)this.Arguments).Providers)
        {
            var providerId = idpConfig.ProviderId;
            if (providerId.Equals("google.com"))
            {
                accounts.Add(ProviderUtils.ProviderIdToAccountType(providerId));
            }
        }

        return accounts;
    }

    public void OnActivityResult(int requestCode, int resultCode, Intent data)
    {
        switch (requestCode)
        {
            case 101:
                if (resultCode == -1)
                {
                    this.handleCredential((Credential)data.GetParcelableExtra("com.google.android.gms.credentials.Credential"));
                    break;
                }
                this.startAuthMethodChoice();
                break;

            case 102 or 103 or 104 or 108:
                break;

            default: break;
            case 105 or 106 or 107 or 109:
                if (resultCode == 113 || resultCode == 114)
                {
                    this.startAuthMethodChoice();
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

    private async void handleCredential(Credential credential)
    {
        var id = credential.Id;
        var password = credential.Password;
        if (TextUtils.IsEmpty(password))
        {
            var identity = credential.AccountType;
            if (identity == null)
            {
                this.startAuthMethodChoice();
            }
            else
            {
                this.redirectSignIn(ProviderUtils.AccountTypeToProviderId(credential.AccountType), id);
            }
        }
        else
        {
            var response = (new IdpResponse.Builder((new User.Builder("password", id)).Build())!).Build();
            this.SetResult(Model.Resource.ForLoading());
            var task = this.Auth.SignInWithEmailAndPasswordAsync(id, password);
            var result = await task;
            if (task.IsCompletedSuccessfully)
            {
                this.HandleSuccess(response, result);
            }
            else if (task.IsFaulted)
            {
                if (task.Exception is FirebaseAuthInvalidUserException || task.Exception is FirebaseAuthInvalidCredentialsException)
                {
                    GoogleApiUtils.GetCredentialsClient(this.Application as Application).Delete(credential);
                }

                this.startAuthMethodChoice();
            }
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
                IdpResponse response = (new IdpResponse.Builder((new User.Builder(authResult.Credential?.Provider,
                    authResult.User?.Email)).Build()!)).Build()!;
                this.HandleSuccess(response, authResult);
            }
            if (pendingResultTask.IsFaulted)
                this.SetResult(Model.Resource.ForFailure(new Java.Lang.Exception(Throwable.FromException(pendingResultTask.Exception))));
        }
        else
        {
            bool supportPasswords = ProviderUtils.GetConfigFromIdps(((FlowParameters)this.Arguments).Providers, "password") != null;
            List<string> accountTypes = this.getCredentialAccountTypes();
            bool willRequestCredentials = supportPasswords || accountTypes.Count > 0;
            if (((FlowParameters)this.Arguments).EnableCredentials && willRequestCredentials)
            {
                this.SetResult(Model.Resource.ForLoading());
                GoogleApiUtils.GetCredentialsClient(this.Application as Application)
                    .Request((new CredentialRequest.Builder())
                    .SetPasswordLoginSupported(supportPasswords)
                    .SetAccountTypes(accountTypes.ToArray()).Build())
                    .addOnCompleteListener((task)-> {
                    try
                    {
                        this.handleCredential(((CredentialRequestResponse)task.getResult(ApiException.class)).getCredential());
                                } catch (ResolvableApiException e) {
                                    if (e.getStatusCode() == 6) {
                                        this.SetResult(Model.Resource.ForFailure(new PendingIntentRequiredException(e.getResolution(), 101)));
                                    } else
{
    this.startAuthMethodChoice();
}
                                } catch (ApiException var4) {
                                    this.startAuthMethodChoice();
                                }

                            });
                        } else
{
    this.startAuthMethodChoice();
}

                    }

    }
}
#pragma warning restore XAOBS001 // Type or member is obsolete
