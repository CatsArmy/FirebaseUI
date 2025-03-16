using FirebaseUI.Auth.Data.Model;

namespace FirebaseUI.Auth.UI.Email;

public partial class CheckEmailFragment
{
    public interface ICheckEmailListener
    {
        public void OnExistingEmailUser(User var1);
        public void OnExistingIdpUser(User var1);
        public void OnNewUser(User var1);
        public void OnDeveloperFailure(Java.Lang.Exception var1);
    }
}
