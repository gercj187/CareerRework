//FILE:		Bootstrap.cs

using System;
using MPAPI;
using MPAPI.Interfaces;
using MPAPI.Interfaces.Packets;
using MPAPI.Types;

namespace CareerRework.Multiplayer
{
	// Einstellungen des Hosts als Byte-Block (Format bestimmt die Hauptmod, siehe Mp.cs)
	public class CareerReworkSettingsPacket : IPacket
	{
		public byte[] Data { get; set; } = Array.Empty<byte>();
	}

	// =========================
	// EINSTIEG (per Reflection aus CareerRework.Mp aufgerufen)
	// =========================
	// Nur Basistypen in den öffentlichen Signaturen → die Hauptmod braucht keinen Verweis auf MultiplayerAPI.
	public static class Bootstrap
	{
		private static IServer? server;
		private static Action<byte[]>? onState;
		private static Func<byte[]>? buildState;
		private static Action? onClientStopped;
		private static Action<string>? log;

		public static bool Initialize(string modId, Action<byte[]> stateReceived, Func<byte[]> stateBuilder,
			Action clientStopped, Action<string> logger)
		{
			if (!MultiplayerAPI.IsMultiplayerLoaded || MultiplayerAPI.Instance == null)
				return false;

			onState = stateReceived;
			buildState = stateBuilder;
			onClientStopped = clientStopped;
			log = logger;

			// Host und alle Clients brauchen die Mod (gleiche Preise/Voraussetzungen auf allen Rechnern)
			MultiplayerAPI.Instance.SetModCompatibility(modId, MultiplayerCompatibility.All);

			MultiplayerAPI.ServerStarted += OnServerStarted;
			MultiplayerAPI.ServerStopped += OnServerStopped;
			MultiplayerAPI.ClientStarted += OnClientStarted;
			MultiplayerAPI.ClientStopped += OnClientStopped;

			log?.Invoke($"API {MultiplayerAPI.LoadedApiVersion}, Multiplayer {MultiplayerAPI.MultiplayerVersion}");
			return true;
		}

		// --- Rolle ---
		public static bool IsClient()
		{
			var api = MultiplayerAPI.Instance;
			return api != null && api.IsConnected && !api.IsHost;
		}

		public static bool IsHost()
		{
			var api = MultiplayerAPI.Instance;
			return api != null && api.IsConnected && api.IsHost && server != null;
		}

		// --- Host → alle Clients (nicht an den eigenen lokalen Client) ---
		public static void Broadcast(byte[] data)
		{
			if (server == null || data == null)
				return;

			server.SendPacketToAll(new CareerReworkSettingsPacket { Data = data }, true, true);
		}

		// =========================
		// EREIGNISSE
		// =========================
		private static void OnServerStarted(IServer s)
		{
			server = s;

			// Clients senden nichts – Registrierung nur, damit der Pakettyp bekannt ist
			s.RegisterPacket<CareerReworkSettingsPacket>((packet, sender) => { });

			// Neu verbundener Spieler bekommt sofort die Einstellungen des Hosts
			s.OnPlayerReady += player =>
			{
				try
				{
					if (buildState != null)
						s.SendPacketToPlayer(new CareerReworkSettingsPacket { Data = buildState() }, player, true);
				}
				catch (Exception e)
				{
					log?.Invoke("send to new player failed: " + e.Message);
				}
			};

			log?.Invoke("server started → this instance is host");
		}

		private static void OnServerStopped()
		{
			server = null;
		}

		private static void OnClientStarted(IClient client)
		{
			client.RegisterPacket<CareerReworkSettingsPacket>(packet =>
			{
				// der lokale Client des Hosts wertet nichts aus (Host hat die Werte selbst)
				if (IsHost())
					return;

				onState?.Invoke(packet.Data);
			});
		}

		private static void OnClientStopped()
		{
			onClientStopped?.Invoke();
		}
	}
}
