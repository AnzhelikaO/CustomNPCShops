#region Using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
#endregion
namespace CustomNPCShops
{
    [ApiVersion(2, 1)]
    public class CustomNPCShopsPlugin : TerrariaPlugin
    {
        #region Description

        public override string Name => "CustomNPCShops";
        public override string Author => "Anzhelika";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Description => "Set custom items in NPC shops.";
        public CustomNPCShopsPlugin(Main game) : base(game) { }

        #endregion
        private int UpdateTick, TicksBetweenUpdate;
        private ConcurrentDictionary<byte, short> ActiveNPCs = new ConcurrentDictionary<byte, short>();

        #region Initialize

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            GeneralHooks.ReloadEvent += OnReload;
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Start/Stop/Update UpdateHook

        private void StartUpdate(byte Index, short NPC)
        {
            if (ActiveNPCs.TryAdd(Index, NPC) && (ActiveNPCs.Count == 1))
            {
                UpdateTick = 0;
                ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            }
        }

        private void StopUpdate(byte Index)
        {
            if (ActiveNPCs.TryRemove(Index, out _) && (ActiveNPCs.Count == 0))
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
        }

        private void UpdateHook(byte Index, short NPC)
        {
            if (NPC >= 0)
                StartUpdate(Index, NPC);
            else
                StopUpdate(Index);
        }

        #endregion

        #region OnPostInitialize

        private void OnPostInitialize(EventArgs args) =>
            OnReload(null);

        #endregion
        #region OnServerLeave

        private void OnServerLeave(LeaveEventArgs args) =>
            StopUpdate((byte)args.Who);

        #endregion
        #region OnReload

        private void OnReload(ReloadEventArgs args)
        {
            NPCShopConfig config = NPCShopConfig.Read(Path.Combine(TShock.SavePath,
                "custom-NPC-shop-config.json"));
            TicksBetweenUpdate = (config.UpdateTimer / 16);
            Packets.Initialize(config);
            args.Player?.SendSuccessMessage("[Custom NPC Shops] Successfully reloaded config!");
        }

        #endregion
        #region OnGetData

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled || (args.MsgID != PacketTypes.NpcTalk))
                return;

            byte index = args.Msg.readBuffer[args.Index];
            short npc = (short)(args.Msg.readBuffer[args.Index + 1]
                             + (args.Msg.readBuffer[args.Index + 2] << 8));
            if (index != args.Msg.whoAmI)
                return;
            Player player = Main.player[index];
            if ((player != null) && (npc != player.talkNPC))
                UpdateHook(index, npc);
        }

        #endregion
        #region OnUpdate

        private void OnUpdate(EventArgs args)
        {
            if (++UpdateTick % TicksBetweenUpdate != 0)
                return;
            foreach (KeyValuePair<byte, short> pair in ActiveNPCs)
            {
                TSPlayer player = TShock.Players[pair.Key];
                if (player?.Active != true)
                    continue;
                try
                {
                    if (Packets.Get(Main.npc[pair.Value], out byte[][] packets))
                        foreach (byte[] packet in packets)
                            player.SendRawData(packet);
                }
                catch { }
            }
        }

        #endregion
    }
}