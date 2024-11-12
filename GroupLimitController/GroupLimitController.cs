using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Group Limit Controller", "Melodia", "1.0.0")]
    [Description("Limits the number of players in a group, grants TC and door access automatically, and allows group limit configuration")]

    public class GroupLimitController : RustPlugin
    {
        private int maxGroupSize = 4;  // Default group size limit
        private Dictionary<ulong, List<ulong>> playerGroups = new Dictionary<ulong, List<ulong>>();

        [ChatCommand("setgrouplimit")]
        private void SetGroupLimitCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "Vous n'avez pas la permission d'utiliser cette commande.");
                return;
            }

            if (args.Length == 0 || !int.TryParse(args[0], out int newLimit))
            {
                SendReply(player, "Veuillez entrer un nombre valide pour la limite de groupe.");
                return;
            }

            maxGroupSize = newLimit;
            PrintToChat($"La limite de groupe a été définie à {maxGroupSize} joueurs.");
        }

        [ChatCommand("joingroup")]
        private void JoinGroupCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "Veuillez spécifier le nom du groupe.");
                return;
            }

            string groupName = args[0].ToLower();
            ulong playerId = player.userID;

            if (!playerGroups.ContainsKey(playerId))
            {
                playerGroups[playerId] = new List<ulong> { playerId };
                GrantTCAccess(player, playerId);
                GrantCodeLockAccess(player, playerId);
                SendReply(player, $"Vous avez rejoint le groupe '{groupName}'.");
                return;
            }

            List<ulong> groupMembers = playerGroups[playerId];
            if (groupMembers.Count >= maxGroupSize)
            {
                PrintToChat($"La team '{groupName}' a tenté de dépasser le nombre maximum de {maxGroupSize} joueurs par groupe !");
                return;
            }

            groupMembers.Add(playerId);
            GrantTCAccess(player, playerId);
            GrantCodeLockAccess(player, playerId);
            SendReply(player, $"Vous avez rejoint le groupe '{groupName}'.");
        }

        private void GrantTCAccess(BasePlayer player, ulong playerId)
        {
            BuildingPrivlidge tc = player.GetBuildingPrivilege();
            if (tc != null && !tc.authorizedPlayers.Exists(ap => ap.userid == playerId))
            {
                tc.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                {
                    userid = playerId,
                    username = player.displayName
                });
                tc.SendNetworkUpdate();
            }
        }

        private void GrantCodeLockAccess(BasePlayer player, ulong playerId)
        {
            foreach (var door in BaseEntity.serverEntities)
            {
                if (door is Door doorEntity && doorEntity.HasSlot(BaseEntity.Slot.Lock) &&
                    doorEntity.GetSlot(BaseEntity.Slot.Lock) is CodeLock codeLock)
                {
                    if (!codeLock.whitelistPlayers.Contains(playerId))
                    {
                        codeLock.whitelistPlayers.Add(playerId);
                        codeLock.SendNetworkUpdate();
                    }
                }
            }
        }
    }
}
