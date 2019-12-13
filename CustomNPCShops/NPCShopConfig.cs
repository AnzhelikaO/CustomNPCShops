#region Using
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
#endregion
namespace CustomNPCShops
{
    public class NPCShopConfig
    {
        #region Data

        [JsonProperty("update_timer_milliseconds")]
        public int UpdateTimer;
        [JsonProperty("world_shops")]
        public WorldShops[] Shops = new WorldShops[0];

        #endregion

        #region Read

        public static NPCShopConfig Read(string Path, bool Validate = false)
        {
            if (!File.Exists(Path))
                #region Default

                new NPCShopConfig()
                {
                    UpdateTimer = 500,
                    Shops = new WorldShops[]
                    {
                        new WorldShops()
                        {
                            Enabled = false,
                            WorldID = 123456789,
                            Shops = new Shop[]
                            {
                                new Shop()
                                {
                                    Enabled = false,
                                    NPC = new ShopNPC()
                                    {
                                        Name = null,
                                        NetID = NPCID.Merchant
                                    },
                                    Items = new ShopItem[]
                                    {
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.Torch,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 99,
                                                Silver = 3,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        },
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.LesserHealingPotion,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 99,
                                                Silver = 12,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        },
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.IronskinPotion,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 99,
                                                Silver = 49,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        },
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.RecallPotion,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 99,
                                                Silver = 99,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        }
                                    }
                                },
                                new Shop()
                                {
                                    Enabled = false,
                                    NPC = new ShopNPC()
                                    {
                                        Name = "Miner",
                                        NetID = NPCID.Merchant
                                    },
                                    Items = new ShopItem[]
                                    {
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.IronPickaxe,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 99,
                                                Silver = 9,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        },
                                        new ShopItem()
                                        {
                                            Enabled = true,
                                            NetID = ItemID.DirtBlock,
                                            Prefix = 0,
                                            Price = new Price()
                                            {
                                                Copper = 5,
                                                Silver = 0,
                                                Gold = 0,
                                                Platinum = 0
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }.Write(Path);

            #endregion
            NPCShopConfig config = JsonConvert.DeserializeObject<NPCShopConfig>(File.ReadAllText(Path));
            #region Validate

            if (Validate)
            {
                List<WorldShops> worldShops = new List<WorldShops>();
                if (config.UpdateTimer < 16)
                    throw new NPCShopConfigException("update_timer must be greater than 15.");
                if (config.Shops != null)
                    for (int ws = 0; ws < config.Shops.Length; ws++)
                    {
                        WorldShops worldShop = config.Shops[ws];
                        if (!worldShop.Enabled)
                            continue;
                        string start = $"world_shops[{ws}]:";

                        List<Shop> shops = new List<Shop>();
                        if (worldShop.WorldID < 0)
                            throw new NPCShopConfigException($"{start} world_id must be greater than -1.");
                        if (worldShops.Any(_ws => (_ws.WorldID == worldShop.WorldID)))
                            throw new NPCShopConfigException($"{start} world_id must be unique.");
                        if ((worldShop.Shops?.Length ?? 0) < 1)
                            throw new NPCShopConfigException($"{start} shops is NULL.");
                        for (int s = 0; s < worldShop.Shops.Length; s++)
                        {
                            Shop shop = worldShop.Shops[s];
                            if (!shop.Enabled)
                                continue;
                            string savedStart = (start += $" shops[{s}]:");

                            List<ShopItem> items = new List<ShopItem>();
                            if ((shop.NPC.NetID < NPCID.BigHornetStingy) || (shop.NPC.NetID >= NPCID.Count))
                                throw new NPCShopConfigException($"{start} npc id must be greater than -66 and less than {NPCID.Count}.");
                            if (shop.NPC.Name == "")
                                throw new NPCShopConfigException($"{start} npc name must be NULL or not empty.");
                            if (worldShops.Any(_ws => _ws.Shops.Any(_s => (_s.NPC == shop.NPC))))
                                throw new NPCShopConfigException($"{start} npc must be unique.");
                            if ((shop.Items?.Length ?? 0) < 1)
                                throw new NPCShopConfigException($"{start} items is NULL or empty.");
                            for (int i = 0; i < shop.Items.Length; i++)
                            {
                                ShopItem item = shop.Items[i];
                                if (!item.Enabled)
                                    continue;
                                start += $" items[{i}]:";

                                if ((item.NetID < 0) || (item.NetID >= Main.maxItemTypes))
                                    throw new NPCShopConfigException($"{start} id must be greater than 0 and less than {Main.maxItemTypes}.");
                                if (item.Prefix >= PrefixID.Count)
                                    throw new NPCShopConfigException($"{start} prefix must be less than {PrefixID.Count}.");
                                start += " price: amount of";
                                if (item.Price.Copper > 99)
                                    throw new NPCShopConfigException($"{start} copper must be less than 100.");
                                if (item.Price.Silver > 99)
                                    throw new NPCShopConfigException($"{start} silver must be less than 100.");
                                if (item.Price.Gold > 99)
                                    throw new NPCShopConfigException($"{start} gold must be less than 100.");
                                if (item.Price.Platinum < 0)
                                    throw new NPCShopConfigException($"{start} platinum must be greater than 0.");
                                items.Add(item);
                            }
                            if (items.Count > 40)
                                throw new NPCShopConfigException($"{savedStart} items count must be less than 41.");
                            shops.Add(shop);
                        }
                        worldShops.Add(worldShop);
                    }

                return new NPCShopConfig()
                {
                    UpdateTimer = config.UpdateTimer,
                    Shops = worldShops.ToArray()
                };
            }

            #endregion
            return config;
        }

        #endregion
        #region Write

        public void Write(string Path) =>
            File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));

        #endregion
    }

    #region NPCShopConfigException

    public class NPCShopConfigException : Exception
    {
        public NPCShopConfigException(string Message) : base(Message) { }
    }

    #endregion

    #region WorldShops

    public struct WorldShops
    {
        [JsonProperty("enabled")]
        public bool Enabled;
        [JsonProperty("world_id")]
        public int WorldID;
        [JsonProperty("shops")]
        public Shop[] Shops;
    }

    #endregion
    #region Shop

    public struct Shop
    {
        [JsonProperty("enabled")]
        public bool Enabled;
        [JsonProperty("npc")]
        public ShopNPC NPC;
        [JsonProperty("items")]
        public ShopItem[] Items;
    }

    #endregion
    #region ShopNPC

    public struct ShopNPC
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("id")]
        public short NetID;

        #region Operators, Equals, GetHashCode

        public static bool operator ==(ShopNPC Left, ShopNPC Right) =>
            ((Left.Name == Right.Name) && (Left.NetID == Right.NetID));

        public static bool operator !=(ShopNPC Left, ShopNPC Right) =>
            ((Left.Name != Right.Name) || (Left.NetID != Right.NetID));

        public override bool Equals(object Object) =>
            ((Object is ShopNPC nPC) && (Name == nPC.Name) && (NetID == nPC.NetID));

        public override int GetHashCode()
        {
            int hashCode = 230481819;
            hashCode = ((hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Name));
            hashCode = ((hashCode * -1521134295) + NetID.GetHashCode());
            return hashCode;
        }

        #endregion
    }

    #endregion
    #region ShopItem

    public struct ShopItem
    {
        [JsonProperty("enabled")]
        public bool Enabled;
        [JsonProperty("id")]
        public short NetID;
        [JsonProperty("prefix")]
        public byte Prefix;
        [JsonProperty("price")]
        public Price Price;
    }

    #endregion
    #region Price

    public struct Price
    {
        [JsonProperty("copper")]
        public byte Copper;
        [JsonProperty("silver")]
        public byte Silver;
        [JsonProperty("gold")]
        public byte Gold;
        [JsonProperty("platinum")]
        public short Platinum;
    }

    #endregion
}