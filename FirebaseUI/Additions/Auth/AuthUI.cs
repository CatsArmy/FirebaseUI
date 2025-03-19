using Android.Content;
using Firebase;
using Firebase.Auth;
using FirebaseUI.Auth.Data.Model;
using FirebaseUI.Auth.Data.Remote;
using Java.Lang;

namespace FirebaseUI.Auth;

#pragma warning disable XAOBS001 // Type or member is obsolete

public sealed partial class AuthUI
{
    public const string PasswordProvider = "password";
    public const string PhoneProvider = "phone";
    public const string GoogleProvider = "google.com";

    public class AuthIntent
    {
        public const int DefaultLogo = -1;

        public List<IdpConfig> Providers
        {
            get; set
            {
                if (value.Count == 1 && value[0].ProviderId == AuthUI.AnonymousProvider)
                {
                    throw new IllegalStateException("Sign in as guest cannot be the only sign in method. In this case, sign the user in anonymously your self; no UI is needed.");
                }

                field.Clear();

                foreach (var config in value)
                {
                    if (field.Contains(config))
                        throw new IllegalArgumentException($"Each provider can only be set once. {config.ProviderId} was set twice.");

                    field.Add(config);
                }
            }
        } = [];

        public IdpConfig? DefaultProvider
        {
            get; set
            {
                if (value != null)
                {
                    if (!this.Providers.Contains(value))
                    {
                        throw new IllegalStateException("Default provider not in available providers list.");
                    }

                    if (this.AlwaysShowProviderChoice)
                    {
                        throw new IllegalStateException("Can't set default provider and always show provider choice.");
                    }
                }

                field = value;
            }
        }

        public bool AlwaysShowProviderChoice
        {
            get; set
            {
                if (value && this.DefaultProvider != null)
                {
                    throw new IllegalStateException("Can't show provider choice with a default provider.");
                }

                field = value;
            }
        } = false;

        public int Logo { get; set; } = DefaultLogo;

        public int Theme { get; set; } = AuthUI.DefaultTheme;

        public string? TermsOfServiceUrl { get; set; } = null;

        public string? PrivacyPolicyUrl { get; set; } = null;

        public string? EmailLink { get; set; } = null;

        public bool EnableAnonymousUpgrade = false;

        public bool LockOrientation { get; set; } = false;

        public bool EnableCredentials { get; set; } = true;

        public bool EnableHints { get; set; } = true;

        public AuthMethodPickerLayout? AuthMethodPickerLayout { get; set; } = null;

        public ActionCodeSettings? PasswordSettings { get; set; } = null;

        protected FlowParameters FlowParams => new(FirebaseApp.Instance.Name, this.Providers, this.DefaultProvider,
            this.Theme, this.Logo, this.TermsOfServiceUrl, this.PrivacyPolicyUrl, this.EnableCredentials, this.EnableHints,
            this.EnableAnonymousUpgrade, this.AlwaysShowProviderChoice, this.LockOrientation, this.EmailLink, this.PasswordSettings,
            this.AuthMethodPickerLayout);

        public Intent Build()
        {
            if (this.Providers.Count == 0)
            {
                this.Providers.Add(new IdpConfig.EmailBuilder().Build());
            }

            return KickoffActivityV2.CreateIntent(FirebaseApp.Instance!.ApplicationContext!, this.FlowParams)!;
        }

        public void SetIsSmartLockEnabled(bool enabled) => this.SetIsSmartLockEnabled(enabled, enabled);

        public void SetIsSmartLockEnabled(bool enableCredentials, bool enableHints)
        {
            this.EnableCredentials = enableCredentials;
            this.EnableHints = enableHints;
        }

        public void EnableAnonymousUsersAutoUpgrade()
        {
            this.EnableAnonymousUpgrade = true;
            this.ValidateEmailBuilderConfig();
        }

        private void ValidateEmailBuilderConfig()
        {
            for (int i = 0; i < this.Providers.Count; i++)
            {
                var config = this.Providers[i];
                if (config.ProviderId != AuthUI.EmailLinkProvider)
                    continue;

                bool emailLinkForceSameDevice = config.Params.GetBoolean("force_same_device", defaultValue: true);
                if (!emailLinkForceSameDevice)
                    throw new IllegalStateException("You must force the same device flow when using email link sign in with anonymous user upgrade");
            }
        }
    }
}

#pragma warning restore XAOBS001 // Type or member is obsolete
