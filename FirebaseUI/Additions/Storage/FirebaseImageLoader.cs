using Bumptech.Glide.Load;
using Bumptech.Glide.Load.Model;
using Firebase.Storage;

namespace FirebaseUI.Storage.Images;

public partial class FirebaseImageLoader
{
    public bool Handles(StorageReference @ref)
        => this.Handles(@ref as Java.Lang.Object);

    public ModelLoaderLoadData? BuildLoadData(StorageReference @ref, int height, int width, Options options)
        => this.BuildLoadData(@ref as Java.Lang.Object, height, width, options);
}
