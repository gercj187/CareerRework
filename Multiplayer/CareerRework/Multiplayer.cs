// File: Multiplayer.cs
// CareerRework multiplayer synchronization.

using System;
using MPAPI;
using MPAPI.Interfaces;
using MPAPI.Interfaces.Packets;
using UnityEngine;

namespace CareerRework
{
    public sealed class ServerBoundCareerReworkReadyPacket : IPacket
    {
        public bool Ready { get; set; }
    }

    public sealed class ClientBoundCareerReworkSettingsPacket : IPacket
    {
        public int StartupMode { get; set; }
        public int StarterLocoPrimary { get; set; }
        public int StarterLocoSecondary { get; set; }
        public int StartingMoney { get; set; }
        public int PriceMultiplierKeysGadgets { get; set; }

        public int PriceTrainDriver { get; set; }
        public int PriceShunting { get; set; }
        public int PriceLogisticalHaul { get; set; }
        public int PriceFreightHaul { get; set; }
        public int PriceFragile { get; set; }
        public int PriceHazmat1 { get; set; }
        public int PriceHazmat2 { get; set; }
        public int PriceHazmat3 { get; set; }
        public int PriceMilitary1 { get; set; }
        public int PriceMilitary2 { get; set; }
        public int PriceMilitary3 { get; set; }
        public int PriceConcurrentJobs1 { get; set; }
        public int PriceConcurrentJobs2 { get; set; }
        public int PriceTrainLength1 { get; set; }
        public int PriceTrainLength2 { get; set; }
        public int PriceMultipleUnit { get; set; }
        public int PriceManualService { get; set; }
        public int PriceMuseum { get; set; }
        public int PriceDispatcher { get; set; }
        public int PriceDE2 { get; set; }
        public int PriceDM3 { get; set; }
        public int PriceS060 { get; set; }
        public int PriceDH4 { get; set; }
        public int PriceS282 { get; set; }
        public int PriceDE6 { get; set; }

        public int Passenger1Price { get; set; }
        public int Passenger2Price { get; set; }
    }

    internal static class CR_Multiplayer
    {
        private static GameObject? runtimeObject;

        public static bool IsHost
        {
            get
            {
                try
                {
                    return MultiplayerAPI.Instance != null &&
                           MultiplayerAPI.Server != null &&
                           MultiplayerAPI.Instance.IsHost;
                }
                catch { return false; }
            }
        }

        public static bool IsClient
        {
            get
            {
                try
                {
                    return MultiplayerAPI.Instance != null &&
                           MultiplayerAPI.Client != null &&
                           !MultiplayerAPI.Instance.IsHost;
                }
                catch { return false; }
            }
        }

        public static bool IsMultiplayer => IsHost || IsClient;
        public static bool CanModifyWorld => !IsMultiplayer || IsHost;

        public static void Initialize()
        {
            if (runtimeObject != null)
                return;

            runtimeObject = new GameObject("CareerRework_Multiplayer");
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<CareerReworkMPClient>();
            runtimeObject.AddComponent<CareerReworkMPServer>();
            Main.Mod?.Logger.Log("[MP] CareerRework multiplayer runtime created.");
        }

        public static void NotifySettingsChanged()
        {
            if (IsHost)
                CareerReworkMPServer.Instance?.BroadcastSettings();
        }
    }

    internal sealed class CareerReworkMPClient : MonoBehaviour
    {
        public static CareerReworkMPClient? Instance { get; private set; }

        private IClient? client;
        private bool registered;
        private bool snapshotReceived;
        private float nextRequestTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            IClient? currentClient = MultiplayerAPI.Client;
            if (!ReferenceEquals(client, currentClient))
            {
                client = currentClient;
                registered = false;
                snapshotReceived = false;
                nextRequestTime = 0f;
            }

            if (!registered)
                TryRegister();

            TryRequestSnapshot();
        }

        private void TryRegister()
        {
            client = MultiplayerAPI.Client;
            if (client == null)
                return;

            client.RegisterPacket<ClientBoundCareerReworkSettingsPacket>(OnSettingsReceived);
            registered = true;
            Main.Mod?.Logger.Log("[MP] Client settings packet registered.");
        }

        private void TryRequestSnapshot()
        {
            if (!registered || client == null || !CR_Multiplayer.IsClient || snapshotReceived)
                return;

            if (!AStartGameData.carsAndJobsLoadingFinished || Time.unscaledTime < nextRequestTime)
                return;

            client.SendPacketToServer(
                new ServerBoundCareerReworkReadyPacket { Ready = true },
                reliable: true);

            nextRequestTime = Time.unscaledTime + 2f;
        }

        private static void ApplyPacket(ClientBoundCareerReworkSettingsPacket p)
        {
            CareerReworkSettings? s = Main.settings;
            if (s == null)
                return;

            s.startupMode = (StartupMode)Mathf.Clamp(p.StartupMode, 0, 1);
            s.starterLocoPrimary = (StarterLocoType)Mathf.Clamp(p.StarterLocoPrimary, 0, 2);
            s.starterLocoSecondary = (StarterLocoType)Mathf.Clamp(p.StarterLocoSecondary, 0, 2);
            s.startingMoney = Math.Max(0, p.StartingMoney);
            s.priceMultiplierKeysGadgets = Mathf.Clamp(p.PriceMultiplierKeysGadgets, 1, 10);

            s.priceTrainDriver = Math.Max(0, p.PriceTrainDriver);
            s.priceShunting = Math.Max(0, p.PriceShunting);
            s.priceLogisticalHaul = Math.Max(0, p.PriceLogisticalHaul);
            s.priceFreightHaul = Math.Max(0, p.PriceFreightHaul);
            s.priceFragile = Math.Max(0, p.PriceFragile);
            s.priceHazmat1 = Math.Max(0, p.PriceHazmat1);
            s.priceHazmat2 = Math.Max(0, p.PriceHazmat2);
            s.priceHazmat3 = Math.Max(0, p.PriceHazmat3);
            s.priceMilitary1 = Math.Max(0, p.PriceMilitary1);
            s.priceMilitary2 = Math.Max(0, p.PriceMilitary2);
            s.priceMilitary3 = Math.Max(0, p.PriceMilitary3);
            s.priceConcurrentJobs1 = Math.Max(0, p.PriceConcurrentJobs1);
            s.priceConcurrentJobs2 = Math.Max(0, p.PriceConcurrentJobs2);
            s.priceTrainLength1 = Math.Max(0, p.PriceTrainLength1);
            s.priceTrainLength2 = Math.Max(0, p.PriceTrainLength2);
            s.priceMultipleUnit = Math.Max(0, p.PriceMultipleUnit);
            s.priceManualService = Math.Max(0, p.PriceManualService);
            s.priceMuseum = Math.Max(0, p.PriceMuseum);
            s.priceDispatcher = Math.Max(0, p.PriceDispatcher);
            s.priceDE2 = Math.Max(0, p.PriceDE2);
            s.priceDM3 = Math.Max(0, p.PriceDM3);
            s.priceS060 = Math.Max(0, p.PriceS060);
            s.priceDH4 = Math.Max(0, p.PriceDH4);
            s.priceS282 = Math.Max(0, p.PriceS282);
            s.priceDE6 = Math.Max(0, p.PriceDE6);

            PJcompatibility.SetConfiguredPassengerPrice(Math.Max(0, p.Passenger1Price));
            PJcompatibility.SetConfiguredPassenger2Price(Math.Max(0, p.Passenger2Price));

            LicenseManager_LoadData_Patch.ReapplyHostConfiguration();
            PJcompatibility.ApplyPassengerBaseTuning();
            PJcompatibility.ApplyPassenger2Pricing();
            PJcompatibility.ApplyPassengerSwitchedRequirementIfNeeded();
        }

        private void OnSettingsReceived(ClientBoundCareerReworkSettingsPacket packet)
		{
			if (packet == null)
				return;

			ApplyPacket(packet);
			snapshotReceived = true;

			DE2BlockerWatcher.RequestRefresh();

			Main.Mod?.Logger.Log(
				"[MP] Host settings, license prices and dependencies applied.");

			Main.Mod?.Logger.Log(
				"[MP] Local DE2 blocker refresh requested.");
		}

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    internal sealed class CareerReworkMPServer : MonoBehaviour
    {
        public static CareerReworkMPServer? Instance { get; private set; }

        private IServer? server;
        private bool registered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!registered)
                TryRegister();
        }

        private void TryRegister()
        {
            server = MultiplayerAPI.Server;
            if (server == null)
                return;

            server.RegisterPacket<ServerBoundCareerReworkReadyPacket>(OnClientReady);
            registered = true;
            Main.Mod?.Logger.Log("[MP] Server ready packet registered.");
        }

        private void OnClientReady(ServerBoundCareerReworkReadyPacket packet, IPlayer sender)
        {
            if (packet == null || sender == null || !packet.Ready)
                return;

            SendSettings(sender);
        }

        private static ClientBoundCareerReworkSettingsPacket CreatePacket()
        {
            CareerReworkSettings s = Main.settings ?? new CareerReworkSettings();

            return new ClientBoundCareerReworkSettingsPacket
            {
                StartupMode = (int)s.startupMode,
                StarterLocoPrimary = (int)s.starterLocoPrimary,
                StarterLocoSecondary = (int)s.starterLocoSecondary,
                StartingMoney = s.startingMoney,
                PriceMultiplierKeysGadgets = s.priceMultiplierKeysGadgets,

                PriceTrainDriver = s.priceTrainDriver,
                PriceShunting = s.priceShunting,
                PriceLogisticalHaul = s.priceLogisticalHaul,
                PriceFreightHaul = s.priceFreightHaul,
                PriceFragile = s.priceFragile,
                PriceHazmat1 = s.priceHazmat1,
                PriceHazmat2 = s.priceHazmat2,
                PriceHazmat3 = s.priceHazmat3,
                PriceMilitary1 = s.priceMilitary1,
                PriceMilitary2 = s.priceMilitary2,
                PriceMilitary3 = s.priceMilitary3,
                PriceConcurrentJobs1 = s.priceConcurrentJobs1,
                PriceConcurrentJobs2 = s.priceConcurrentJobs2,
                PriceTrainLength1 = s.priceTrainLength1,
                PriceTrainLength2 = s.priceTrainLength2,
                PriceMultipleUnit = s.priceMultipleUnit,
                PriceManualService = s.priceManualService,
                PriceMuseum = s.priceMuseum,
                PriceDispatcher = s.priceDispatcher,
                PriceDE2 = s.priceDE2,
                PriceDM3 = s.priceDM3,
                PriceS060 = s.priceS060,
                PriceDH4 = s.priceDH4,
                PriceS282 = s.priceS282,
                PriceDE6 = s.priceDE6,

                Passenger1Price = PJcompatibility.GetConfiguredPassengerPrice(),
                Passenger2Price = PJcompatibility.GetConfiguredPassenger2Price()
            };
        }

        private void SendSettings(IPlayer player)
        {
            if (server == null)
                return;

            server.SendPacketToPlayer(CreatePacket(), player, reliable: true);
        }

        public void BroadcastSettings()
        {
            if (!registered || server == null || !CR_Multiplayer.IsHost)
                return;

            server.SendPacketToAll(CreatePacket(), reliable: true, excludeSelf: true);
            Main.Mod?.Logger.Log("[MP] Host settings broadcast.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}