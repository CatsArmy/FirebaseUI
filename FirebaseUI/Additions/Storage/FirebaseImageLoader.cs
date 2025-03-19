using Bumptech.Glide.Load;
using Bumptech.Glide.Load.Model;
using Firebase.Storage;

namespace FirebaseUI.Storage.Images;

public partial class FirebaseImageLoader
{
    public bool Handles(StorageReference reference)
        => this.Handles(reference as Java.Lang.Object);

    public ModelLoaderLoadData? BuildLoadData(StorageReference reference, int height, int width, Options options)
        => this.BuildLoadData(reference as Java.Lang.Object, height, width, options);
}
