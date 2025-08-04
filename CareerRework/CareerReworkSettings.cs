using UnityModManagerNet;

namespace CareerRework
{
    public enum StartupMode
    {
        Preset,
        Custom
    }	
    public enum StarterLocoType
    {
        DM3,
        S060
    }
    public class CareerReworkSettings : UnityModManager.ModSettings
	{
		public StarterLocoType selectedStarterLoco = StarterLocoType.DM3;
		public int startingMoney = 227500;
		public StartupMode startupMode = StartupMode.Preset;
		public int priceMultiplierKeysGadgets = 10;
		
		// Lizenzpreise
		public int priceTrainDriver = 50000;
        public int priceShunting = 100000;
        public int priceLogisticalHaul = 200000;
        public int priceFreightHaul = 300000;
        public int priceFragile = 200000;
        public int priceHazmat1 = 350000;
        public int priceHazmat2 = 500000;
        public int priceHazmat3 = 8000000;
        public int priceMilitary1 = 1000000;
        public int priceMilitary2 = 2000000;
        public int priceMilitary3 = 4000000;
        public int priceConcurrentJobs1 = 200000;
        public int priceConcurrentJobs2 = 300000;
        public int priceTrainLength1 = 200000;
        public int priceTrainLength2 = 300000;
        public int priceMultipleUnit = 400000;
        public int priceManualService = 200000;
        public int priceMuseum = 300000;
        public int priceDispatcher = 400000;
        public int priceDE2 = 75000;
        public int priceDM3 = 75000;
        public int priceS060 = 75000;
        public int priceDH4 = 500000;
        public int priceS282 = 750000;
        public int priceDE6 = 1000000;
		
		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}
	}
}
