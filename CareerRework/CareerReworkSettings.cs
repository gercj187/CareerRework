using UnityModManagerNet;

namespace CareerRework
{
    public enum StarterLocoType
    {
        DM3,
        S060
    }

    public class CareerReworkSettings : UnityModManager.ModSettings
    {
        public StarterLocoType selectedStarterLoco = StarterLocoType.DM3;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
