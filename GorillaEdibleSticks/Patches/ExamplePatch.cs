using GorillaEdibleSticks.Behaviours;
using HarmonyLib;
using System.Linq;

namespace GorillaEdibleSticks.Patches
{
    [HarmonyPatch(typeof(VRRig), "Awake"), HarmonyWrapSafe]
    public class ExamplePatch
    {
        [HarmonyPrefix]
        public static void Prefix(VRRig __instance)
        {
            BodyDockPositions bodyDockPositions = __instance.GetComponent<BodyDockPositions>();
            TransferrableObject modStick = bodyDockPositions.allObjects.First(to => to.name == "MOD STICK");
            modStick.gameObject.AddComponent<EdibleStick>();
        }
    }
}
