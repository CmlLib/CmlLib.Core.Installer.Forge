
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmlLib.Core.Installer.Forge
{
    public class Func
    {
        /* 1.7.10 - 1.9.4 */
        public string GetLegacyForgeName(string mcVersion, string forgeVersion) => $"forge-{mcVersion}-{forgeVersion}-{mcVersion}";

        /* 1.12 - *.*.* */
        public string GetForgeName(string mcVersion, string forgeVersion) => $"{mcVersion}-forge-{forgeVersion}";

        /*1.10 - 1.11.2 */
        public string GetOldForgeName(string mcVersion, string forgeVersion) => $"forge-{mcVersion}-{forgeVersion}";

        /* 1.7.10 - 1.11.2 */
        public string GetLegacyFolderName(string mcVersion, string forgeVersion) => mcVersion == "1.7.10" ?
            $"{mcVersion}-Forge-{forgeVersion}-{mcVersion}" :
            $"{mcVersion}-forge{mcVersion}-{forgeVersion}";

        public static bool IsOldType(string mcVersion) => Convert.ToInt32(mcVersion.Split('.')[1]) < 12 ? true : false;

    }
}
