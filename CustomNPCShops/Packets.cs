#region Using
using System;
using System.Collections.Concurrent;
using Terraria;
#endregion
namespace CustomNPCShops
{
    static class Packets
    {
        private static ConcurrentDictionary<int, ConcurrentDictionary<string, byte[][]>> Dictionary;

        public static bool Get(NPC NPC, out byte[][] Packets)
        {
            string name = NPC?.GivenName;
            if ((name == null)
                || !Dictionary.TryGetValue(NPC.netID, out ConcurrentDictionary<string, byte[][]> packets))
            {
                Packets = null;
                return false;
            }
            return (packets.TryGetValue(name, out Packets) || packets.TryGetValue("", out Packets));
        }

        public static void Initialize(NPCShopConfig Config)
        {
            var dictionary = new ConcurrentDictionary<int, ConcurrentDictionary<string, byte[][]>>();
            foreach (WorldShops worldShop in Config.Shops)
            {
                if (worldShop.WorldID != Main.worldID)
                    continue;
                foreach (Shop shop in worldShop.Shops)
                {
                    int id = shop.NPC.NetID;
                    if (!dictionary.ContainsKey(id))
                        dictionary.TryAdd(id, new ConcurrentDictionary<string, byte[][]>());
                    byte[][] packets = new byte[40][];
                    byte count = (byte)shop.Items.Length;
                    for (byte i = 0; i < count; i++)
                    {
                        if (i >= 40)
                            break;
                        ShopItem item = shop.Items[i];
                        byte[] idBytes = BitConverter.GetBytes(item.NetID);
                        byte[] priceBytes = BitConverter.GetBytes(Item.buyPrice(item.Price.Platinum,
                            item.Price.Gold, item.Price.Silver, item.Price.Copper));
                        packets[i] = new byte[]
                        {
                            14, 0, 104, i, idBytes[0], idBytes[1], 1, 0, shop.Items[i].Prefix,
                            priceBytes[0], priceBytes[1], priceBytes[2], priceBytes[3], 0
                        };
                    }
                    for (byte i = count; i < 40; i++)
                        packets[i] = new byte[] { 14, 0, 104, i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    dictionary[id].TryAdd(shop.NPC.Name ?? "", packets);
                }
            }

            Dictionary = dictionary;
        }
    }
}