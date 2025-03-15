namespace FirebaseUI;

public static class Extensions
{
    public static Java.Lang.Class ToClass(this Type type) => Java.Lang.Class.FromType(type);

}