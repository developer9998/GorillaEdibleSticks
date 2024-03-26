using BepInEx;
using HarmonyLib;

namespace GorillaEdibleSticks
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            new Harmony(Constants.GUID).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
