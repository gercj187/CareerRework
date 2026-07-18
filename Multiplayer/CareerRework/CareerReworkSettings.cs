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
		DE2,
		DM3,
		S060
	}
    public class CareerReworkSettings : UnityModManager.ModSettings
	{
		public StarterLocoType starterLocoPrimary    = StarterLocoType.DE2; // SteelMill Yard Track 1
		public StarterLocoType starterLocoSecondary  = StarterLocoType.DM3; // SteelMill Yard Track 1
		public int startingMoney = 200000;
		public StartupMode startupMode = StartupMode.Preset;
		public bool startWithConcurrentJobs1 = false;
		public bool startWithConcurrentJobs2 = false;
		public int priceMultiplierKeysGadgets = 10;
		//public int restorationPriceMultiplier = 10;
		
		// Lizenzpreise
		public int priceTrainDriver = 50000;
        public int priceShunting = 50000;
        public int priceLogisticalHaul = 100000;
        public int priceFreightHaul = 200000;
        public int priceFragile = 100000;
        public int priceHazmat1 = 250000;
        public int priceHazmat2 = 500000;
        public int priceHazmat3 = 8000000;
        public int priceMilitary1 = 1000000;
        public int priceMilitary2 = 2000000;
        public int priceMilitary3 = 4000000;
        public int priceConcurrentJobs1 = 100000;
        public int priceConcurrentJobs2 = 300000;
        public int priceTrainLength1 = 100000;
        public int priceTrainLength2 = 300000;
        public int priceMultipleUnit = 400000;
        public int priceManualService = 100000;
        public int priceMuseum = 200000;
        public int priceDispatcher = 400000;
        public int priceDE2 = 75000;
        public int priceDM3 = 75000;
        public int priceS060 = 75000;
        public int priceDH4 = 500000;
        public int priceS282 = 750000;
        public int priceDE6 = 1000000;
		
		
		public CareerReworkSettings Clone()
		{
			return new CareerReworkSettings
			{
				starterLocoPrimary = starterLocoPrimary,
				starterLocoSecondary = starterLocoSecondary,
				startingMoney = startingMoney,
				startupMode = startupMode,
				startWithConcurrentJobs1 = startWithConcurrentJobs1,
				startWithConcurrentJobs2 = startWithConcurrentJobs2,
				priceMultiplierKeysGadgets = priceMultiplierKeysGadgets,

				priceTrainDriver = priceTrainDriver,
				priceShunting = priceShunting,
				priceLogisticalHaul = priceLogisticalHaul,
				priceFreightHaul = priceFreightHaul,
				priceFragile = priceFragile,
				priceHazmat1 = priceHazmat1,
				priceHazmat2 = priceHazmat2,
				priceHazmat3 = priceHazmat3,
				priceMilitary1 = priceMilitary1,
				priceMilitary2 = priceMilitary2,
				priceMilitary3 = priceMilitary3,
				priceConcurrentJobs1 = priceConcurrentJobs1,
				priceConcurrentJobs2 = priceConcurrentJobs2,
				priceTrainLength1 = priceTrainLength1,
				priceTrainLength2 = priceTrainLength2,
				priceMultipleUnit = priceMultipleUnit,
				priceManualService = priceManualService,
				priceMuseum = priceMuseum,
				priceDispatcher = priceDispatcher,
				priceDE2 = priceDE2,
				priceDM3 = priceDM3,
				priceS060 = priceS060,
				priceDH4 = priceDH4,
				priceS282 = priceS282,
				priceDE6 = priceDE6
			};
		}

		public void CopyFrom(CareerReworkSettings source)
		{
			if (source == null)
				return;

			starterLocoPrimary = source.starterLocoPrimary;
			starterLocoSecondary = source.starterLocoSecondary;
			startingMoney = source.startingMoney;
			startupMode = source.startupMode;

			startWithConcurrentJobs1 =
				source.startWithConcurrentJobs1;

			startWithConcurrentJobs2 =
				source.startWithConcurrentJobs1 &&
				source.startWithConcurrentJobs2;

			priceMultiplierKeysGadgets =
				source.priceMultiplierKeysGadgets;

			priceTrainDriver = source.priceTrainDriver;
			priceShunting = source.priceShunting;
			priceLogisticalHaul = source.priceLogisticalHaul;
			priceFreightHaul = source.priceFreightHaul;
			priceFragile = source.priceFragile;
			priceHazmat1 = source.priceHazmat1;
			priceHazmat2 = source.priceHazmat2;
			priceHazmat3 = source.priceHazmat3;
			priceMilitary1 = source.priceMilitary1;
			priceMilitary2 = source.priceMilitary2;
			priceMilitary3 = source.priceMilitary3;
			priceConcurrentJobs1 = source.priceConcurrentJobs1;
			priceConcurrentJobs2 = source.priceConcurrentJobs2;
			priceTrainLength1 = source.priceTrainLength1;
			priceTrainLength2 = source.priceTrainLength2;
			priceMultipleUnit = source.priceMultipleUnit;
			priceManualService = source.priceManualService;
			priceMuseum = source.priceMuseum;
			priceDispatcher = source.priceDispatcher;
			priceDE2 = source.priceDE2;
			priceDM3 = source.priceDM3;
			priceS060 = source.priceS060;
			priceDH4 = source.priceDH4;
			priceS282 = source.priceS282;
			priceDE6 = source.priceDE6;
		}
		
		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}
	}
}
