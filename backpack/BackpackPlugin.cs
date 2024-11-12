using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BackpackPlugin", "Melodia", "1.0.0")]
    [Description("Adds a backpack feature with different slots based on player rank")]

    public class BackpackPlugin : RustPlugin
    {
        private Dictionary<ulong, List<Item>> backpacks = new Dictionary<ulong, List<Item>>();

        private const int BasicSlots = 4;
        private const int VipSlots = 5;
        private const int AdminSlots = 10;

        [PluginReference] private Plugin Permissions;

        private void Init()
        {
            permission.RegisterPermission("backpack.vip", this);
            permission.RegisterPermission("backpack.admin", this);
        }

        [ChatCommand("backpack")]
        private void OpenBackpackCommand(BasePlayer player)
        {
            OpenBackpack(player);
        }

        private void OpenBackpack(BasePlayer player)
        {
            int slots = GetBackpackSlots(player);
            List<Item> backpack = GetOrCreateBackpack(player.userID, slots);

            CuiElementContainer container = CreateBackpackUI(slots);
            CuiHelper.AddUi(player, container);
        }

        private int GetBackpackSlots(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "backpack.admin"))
                return AdminSlots;
            if (permission.UserHasPermission(player.UserIDString, "backpack.vip"))
                return VipSlots;
            return BasicSlots;
        }

        private List<Item> GetOrCreateBackpack(ulong playerId, int slots)
        {
            if (!backpacks.ContainsKey(playerId))
            {
                backpacks[playerId] = new List<Item>();
                for (int i = 0; i < slots; i++)
                {
                    backpacks[playerId].Add(null); // Empty slots
                }
            }
            return backpacks[playerId];
        }

        private CuiElementContainer CreateBackpackUI(int slots)
        {
            var container = new CuiElementContainer();

            // Backpack Icon/Button
            container.Add(new CuiButton
            {
                Button = { Command = "backpack.open", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.9 0.02", AnchorMax = "0.95 0.07" },
                Text = { Text = "", FontSize = 22, Align = TextAnchor.MiddleCenter }
            }, "Hud", "backpack_icon");

            // Backpack Inventory UI
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.8" },
                RectTransform = { AnchorMin = "0.4 0.4", AnchorMax = "0.6 0.6" }
            }, "Overlay", "backpack_ui");

            for (int i = 0; i < slots; i++)
            {
                var slot = new CuiButton
                {
                    Button = { Command = $"backpack.slot {i}", Color = "0.3 0.3 0.3 0.8" },
                    RectTransform = { AnchorMin = $"{0.1 + 0.2 * (i % 2)} {0.7 - 0.3 * (i / 2)}", AnchorMax = $"{0.3 + 0.2 * (i % 2)} {0.9 - 0.3 * (i / 2)}" },
                    Text = { Text = $"Slot {i + 1}", FontSize = 18, Align = TextAnchor.MiddleCenter }
                };
                container.Add(slot, "backpack_ui");
            }

            return container;
        }

        [ConsoleCommand("backpack.open")]
        private void CmdOpenBackpack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            OpenBackpack(player);
        }

        [ConsoleCommand("backpack.slot")]
        private void CmdHandleSlot(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;

            int slot;
            if (!int.TryParse(arg.Args[0], out slot)) return;

            List<Item> backpack = GetOrCreateBackpack(player.userID, GetBackpackSlots(player));

            // Example interaction: Clear item if clicked (this can be replaced with inventory management logic)
            if (slot >= 0 && slot < backpack.Count)
            {
                backpack[slot] = null;
                player.ChatMessage($"Slot {slot + 1} has been cleared.");
            }
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player == null || !backpacks.ContainsKey(player.userID)) return;

            // Preserve backpack contents on death (no action needed since items remain in the dictionary)
            player.ChatMessage("Your backpack items are preserved!");
        }

        private void Unload()
        {
            // Clear UI elements on unload
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "backpack_icon");
                CuiHelper.DestroyUi(player, "backpack_ui");
            }
        }
    }
}
