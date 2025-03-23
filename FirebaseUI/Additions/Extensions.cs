namespace FirebaseUI;

internal static class Extensions
{
    public static Java.Lang.Class Class(this Type type) => Java.Lang.Class.FromType(type);
}