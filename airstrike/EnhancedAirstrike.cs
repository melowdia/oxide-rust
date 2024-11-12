using Oxide.Game.Rust.Cui;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("EnhancedAirstrike", "Melodia", "1.0.0")]
    [Description("Allows players to call an MLRS strike on other players, with admin-exclusive features and cooldown management.")]

    public class EnhancedAirstrike : RustPlugin
    {
        private const int PlayerMlrsCount = 6;
        private const int AdminMlrsCount = 10;
        private int cooldownMinutes = 5; // Default cooldown time in minutes
        private Dictionary<ulong, DateTime> lastStrikeTime = new Dictionary<ulong, DateTime>();

        private void Init()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsAdmin)
                {
                    ShowAdminBombIcon(player);
                }
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                ShowAdminBombIcon(player);
            }
        }

        private void ShowAdminBombIcon(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiButton
            {
                Button = { Command = "adminairstrike.megastrike", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.05 0.02", AnchorMax = "0.1 0.07" },
                Text = { Text = "", FontSize = 22, Align = TextAnchor.MiddleCenter }
            }, "Hud", "admin_bomb_icon");

            CuiHelper.AddUi(player, container);
        }

        [ChatCommand("strike")]
        private void CmdStrike(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.ChatMessage("Usage: /strike <nom_du_joueur>");
                return;
            }

            if (IsOnCooldown(player))
            {
                player.ChatMessage($"Veuillez attendre encore {GetRemainingCooldown(player)} avant d'utiliser cette commande à nouveau.");
                return;
            }

            BasePlayer targetPlayer = FindPlayerByName(args[0]);
            if (targetPlayer == null)
            {
                player.ChatMessage("Joueur non trouvé.");
                return;
            }

            if (!HasMlrs(player, PlayerMlrsCount))
            {
                player.ChatMessage("Vous n'avez pas assez de roquettes MLRS (6 requis) !");
                return;
            }

            RemoveMlrs(player, PlayerMlrsCount);
            CallMlrsStrike(targetPlayer.transform.position, PlayerMlrsCount);
            player.ChatMessage($"Attaque MLRS lancée sur {targetPlayer.displayName} !");
            lastStrikeTime[player.userID] = DateTime.Now;
        }

        [ChatCommand("adminstrike")]
        private void CmdAdminStrike(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("Cette commande est réservée aux administrateurs.");
                return;
            }

            if (args.Length < 1)
            {
                player.ChatMessage("Usage: /adminstrike <nom_du_joueur>");
                return;
            }

            BasePlayer targetPlayer = FindPlayerByName(args[0]);
            if (targetPlayer == null)
            {
                player.ChatMessage("Joueur non trouvé.");
                return;
            }

            CallMlrsStrike(targetPlayer.transform.position, AdminMlrsCount);
            player.ChatMessage($"Attaque MLRS de 10 roquettes lancée sur {targetPlayer.displayName} !");
        }

        [ConsoleCommand("adminairstrike.megastrike")]
        private void CmdMegaStrike(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !player.IsAdmin) return;

            Vector3 position = player.transform.position;
            CallMlrsStrike(position, 1);
            DestroySurroundings(position, 5);
            player.ChatMessage("Mega Airstrike lancé !");
        }

        [ChatCommand("setcooldown")]
        private void CmdSetCooldown(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("Vous n'avez pas la permission de modifier le cooldown.");
                return;
            }

            if (args.Length < 1 || !int.TryParse(args[0], out int newCooldown))
            {
                player.ChatMessage("Usage: /setcooldown <minutes>");
                return;
            }

            cooldownMinutes = newCooldown;
            player.ChatMessage($"Le cooldown pour la commande /strike a été défini à {cooldownMinutes} minutes.");
        }

        private bool IsOnCooldown(BasePlayer player)
        {
            if (lastStrikeTime.TryGetValue(player.userID, out DateTime lastUse))
            {
                return (DateTime.Now - lastUse).TotalMinutes < cooldownMinutes;
            }
            return false;
        }

        private string GetRemainingCooldown(BasePlayer player)
        {
            if (lastStrikeTime.TryGetValue(player.userID, out DateTime lastUse))
            {
                double remainingMinutes = cooldownMinutes - (DateTime.Now - lastUse).TotalMinutes;
                return $"{Math.Max(0, Math.Ceiling(remainingMinutes))} minutes";
            }
            return "0 minutes";
        }

        private void CallMlrsStrike(Vector3 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 strikePosition = position + new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
                Vector3 startPosition = strikePosition + new Vector3(0, 150, 0);

                BaseEntity rocket = GameManager.server.CreateEntity("assets/prefabs/npc/mlrs/mlrs_rocket.prefab", startPosition, Quaternion.identity);
                if (rocket != null)
                {
                    rocket.SetVelocity(new Vector3(0, -1, 0));
                    rocket.Spawn();
                }
            }
        }

        private void DestroySurroundings(Vector3 position, float radius)
        {
            foreach (var entity in BaseNetworkable.serverEntities.Where(ent => ent is BaseEntity))
            {
                BaseEntity baseEntity = entity as BaseEntity;
                if (baseEntity != null && Vector3.Distance(baseEntity.transform.position, position) <= radius)
                {
                    baseEntity.Kill();
                }
            }
        }

        private bool HasMlrs(BasePlayer player, int count)
        {
            int mlrsInInventory = player.inventory.GetAmount(-1775234707); // MLRS rocket item ID
            return mlrsInInventory >= count;
        }

        private void RemoveMlrs(BasePlayer player, int count)
        {
            player.inventory.Take(null, -1775234707, count); // Remove MLRS rocket item ID
        }

        private BasePlayer FindPlayerByName(string name)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(name.ToLower()))
                {
                    return player;
                }
            }
            return null;
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "admin_bomb_icon");
            }
        }
    }
}
