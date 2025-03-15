using Bumptech.Glide.Load;
using Bumptech.Glide.Load.Model;
using Firebase.Storage;

namespace FirebaseUI.Storage.Images;

public partial class FirebaseImageLoader
{
    public bool Handles(StorageReference reference) => this.Handles((Java.Lang.Object)reference);

    public ModelLoaderLoadData? BuildLoadData(StorageReference reference, int height, int width, Options options)
        => this.BuildLoadData((Java.Lang.Object)reference, height, width, options);
}
