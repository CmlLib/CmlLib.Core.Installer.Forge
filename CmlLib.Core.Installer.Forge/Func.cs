namespace CmlLib.Core.Installer.Forge
{
    public class Func
    {
        public static bool IsOldType(string mcVersion) => Convert.ToInt32(mcVersion.Split('.')[1]) < 12 ? true : false;
    }
}
