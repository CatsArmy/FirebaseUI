using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Annotations;
using Firebase;
using Firebase.Auth;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.Data.Remote;
using FirebaseUI.Auth.Util;
using Java.Lang;
using Java.Util;
using static FirebaseUI.Auth.AuthUI;

namespace FirebaseUI.Auth;

#pragma warning disable XAOBS001 // Type or member is obsolete

public sealed partial class AuthUI
{
    public const string PasswordProvider = "password";
    public const string PhoneProvider = "phone";
    public const string GoogleProvider = "google.com";

    public SignInIntentBuilder CreateSignInIntentBuilder() => new SignInIntentBuilder(this);

    public abstract partial class AuthIntentBuilder(AuthUI authUI)
    {
        public List<IdpConfig> Providers = [];
        public IdpConfig? DefaultProvider = null;
        public int Logo = -1;
        public int Theme = AuthUI.DefaultTheme;
        public string? TosUrl;
        public string? PrivacyPolicyUrl;
        public bool AlwaysShowProviderChoice = false;
        public bool LockOrientation = false;
        public bool EnableCredentials = true;
        public AuthMethodPickerLayout? AuthMethodPickerLayout = null;
        public ActionCodeSettings? PasswordSettings = null;
        protected abstract FlowParameters FlowParams { get; }

        public AuthIntentBuilder SetTheme(int theme)
        {
            this.Theme = Preconditions.CheckValidStyle(authUI.App.ApplicationContext, theme,
                "theme identifier is unknown or not a style definition", []);
            return (AuthIntentBuilder)this;
        }


        public AuthIntentBuilder SetLogo(int logo)
        {
            this.Logo = logo;
            return (AuthIntentBuilder)this;
        }

        /** @deprecated */
        public AuthIntentBuilder SetTosUrl(string? tosUrl)
        {
            this.TosUrl = tosUrl;
            return (AuthIntentBuilder)this;
        }

        /** @deprecated */
        public AuthIntentBuilder SetPrivacyPolicyUrl(string? privacyPolicyUrl)
        {
            this.PrivacyPolicyUrl = privacyPolicyUrl;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetTosAndPrivacyPolicyUrls(string tosUrl, string privacyPolicyUrl)
        {
            Preconditions.CheckNotNull(tosUrl, "tosUrl cannot be null", []);
            Preconditions.CheckNotNull(privacyPolicyUrl, "privacyPolicyUrl cannot be null", []);
            this.TosUrl = tosUrl;
            this.PrivacyPolicyUrl = privacyPolicyUrl;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetAvailableProviders(List<IdpConfig> idpConfigs)
        {
            if (idpConfigs == null)
            {
                throw new NullPointerException("idpConfigs cannot be null");
            }

            if (idpConfigs.Count == 1 && ((IdpConfig)idpConfigs[0]).ProviderId == ("anonymous"))
            {
                throw new IllegalStateException("Sign in as guest cannot be the only sign in method. In this case, sign the user in anonymously your self; no UI is needed.");
            }
            else
            {
                this.Providers.Clear();

                foreach (IdpConfig config in idpConfigs)
                {
                    if (this.Providers.Contains(config))
                    {
                        throw new IllegalArgumentException("Each provider can only be set once. " + config.ProviderId + " was set twice.");
                    }

                    this.Providers.Add(config);
                }

                return (AuthIntentBuilder)this;
            }
        }

        public AuthIntentBuilder SetDefaultProvider(IdpConfig? config)
        {
            if (config != null)
            {
                if (!this.Providers.Contains(config))
                {
                    throw new IllegalStateException("Default provider not in available providers list.");
                }

                if (this.AlwaysShowProviderChoice)
                {
                    throw new IllegalStateException("Can't set default provider and always show provider choice.");
                }
            }

            this.DefaultProvider = config;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetCredentialManagerEnabled(bool enableCredentials)
        {
            this.EnableCredentials = enableCredentials;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetAuthMethodPickerLayout(AuthMethodPickerLayout authMethodPickerLayout)
        {
            this.AuthMethodPickerLayout = authMethodPickerLayout;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetAlwaysShowSignInMethodScreen(bool alwaysShow)
        {
            if (alwaysShow && this.DefaultProvider != null)
            {
                throw new IllegalStateException("Can't show provider choice with a default provider.");
            }
            else
            {
                this.AlwaysShowProviderChoice = alwaysShow;
                return (AuthIntentBuilder)this;
            }
        }

        public AuthIntentBuilder SetLockOrientation(bool lockOrientation)
        {
            this.LockOrientation = lockOrientation;
            return (AuthIntentBuilder)this;
        }

        public AuthIntentBuilder SetResetPasswordSettings(ActionCodeSettings passwordSettings)
        {
            this.PasswordSettings = passwordSettings;
            return (AuthIntentBuilder)this;
        }

        //@CallSuper
        public virtual Intent? Build()
        {
            if (this.Providers.Count == 0)
            {
                this.Providers.Add((new IdpConfig.EmailBuilder()).Build());
            }

            return KickoffActivity.CreateIntent(authUI.App.ApplicationContext, this.FlowParams);
        }
    }

    public partial class SignInIntentBuilder(AuthUI authUI) : AuthIntentBuilder(authUI)
    {
        public SignInIntentBuilder() : this(AuthUI.Instance) { }
        private string? EmailLink;
        private bool EnableAnonymousUpgrade;

        protected override FlowParameters FlowParams => new(authUI.App.Name, this.Providers, this.DefaultProvider,
            this.Theme, this.Logo, this.TosUrl, this.PrivacyPolicyUrl, this.EnableCredentials, this.EnableAnonymousUpgrade,
            this.AlwaysShowProviderChoice, this.LockOrientation, this.EmailLink,
            this.PasswordSettings, this.AuthMethodPickerLayout);

        public SignInIntentBuilder SetEmailLink(string emailLink)
        {
            this.EmailLink = emailLink;
            return this;
        }

        public SignInIntentBuilder EnableAnonymousUsersAutoUpgrade()
        {
            this.EnableAnonymousUpgrade = true;
            this.ValidateEmailBuilderConfig();
            return this;
        }

        private void ValidateEmailBuilderConfig()
        {
            for (int i = 0; i < this.Providers.Count; ++i)
            {
                IdpConfig config = (IdpConfig)this.Providers[i];
                if (config.ProviderId == ("emailLink"))
                {
                    bool emailLinkForceSameDevice = config.Params.GetBoolean("force_same_device", true);
                    if (!emailLinkForceSameDevice)
                    {
                        throw new IllegalStateException("You must force the same device flow when using email link sign in with anonymous user upgrade");
                    }
                }
            }

        }
    }
}

#pragma warning restore XAOBS001 // Type or member is obsolete

