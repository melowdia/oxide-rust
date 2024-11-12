using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("EpicLootCrate", "LungWang", "1.2.0")]
    [Description("Spawns a locked crate with loot, protected by bots and possibly a Bradley, every 3 to 4 hours. Admins can also spawn it manually.")]

    public class EpicLootCrate : RustPlugin
    {
        private const int BotCount = 10;
        private const float BradleyChance = 0.3f; // 30% chance to spawn Bradley
        private List<BaseEntity> spawnedEntities = new List<BaseEntity>();
        private const int MinSpawnInterval = 3 * 60 * 60; // Minimum 3 hours in seconds
        private const int MaxSpawnInterval = 4 * 60 * 60; // Maximum 4 hours in seconds

        private void OnServerInitialized()
        {
            ScheduleNextSpawn();
        }

        private void ScheduleNextSpawn()
        {
            int nextSpawnInterval = Random.Range(MinSpawnInterval, MaxSpawnInterval);
            timer.Once(nextSpawnInterval, SpawnLootCrate);
        }

        private void SpawnLootCrate()
        {
            Vector3 position = GetRandomPosition();
            BaseEntity crate = GameManager.server.CreateEntity("assets/bundled/prefabs/rnd/lockedcratehackable.prefab", position);
            
            if (crate != null)
            {
                crate.Spawn();
                spawnedEntities.Add(crate);
                CreateBotsAround(position);
                SendReplyToAll($"Une caisse de loot spéciale est apparue en {position}. Préparez-vous à affronter des gardiens !");

                // Possibilité de faire spawn Bradley si le butin est exceptionnel
                if (Random.value <= BradleyChance)
                {
                    SpawnBradley(position);
                    SendReplyToAll("Attention ! Un tank Bradley protège également la caisse !");
                }
            }

            // Planification du prochain spawn automatique
            ScheduleNextSpawn();
        }

        private void CreateBotsAround(Vector3 position)
        {
            for (int i = 0; i < BotCount; i++)
            {
                Vector3 botPosition = position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
                BaseEntity bot = GameManager.server.CreateEntity("assets/rust.ai/agents/npc/player/player_corpse.prefab", botPosition);
                if (bot != null)
                {
                    bot.Spawn();
                    spawnedEntities.Add(bot);
                }
            }
        }

        private void SpawnBradley(Vector3 position)
        {
            BaseEntity bradley = GameManager.server.CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", position + new Vector3(0, 1, 0));
            if (bradley != null)
            {
                bradley.Spawn();
                spawnedEntities.Add(bradley);
            }
        }

        private Vector3 GetRandomPosition()
        {
            float x = Random.Range(-3000f, 3000f);
            float z = Random.Range(-3000f, 3000f);
            float y = TerrainMeta.HeightMap.GetHeight(new Vector3(x, 0, z));
            return new Vector3(x, y, z);
        }

        private void Unload()
        {
            foreach (var entity in spawnedEntities)
            {
                if (entity != null && !entity.IsDestroyed)
                {
                    entity.Kill();
                }
            }
            spawnedEntities.Clear();
        }

        private void SendReplyToAll(string message)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage(message);
            }
        }

        [ChatCommand("spawncrate")]
        private void SpawnCrateCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("Vous n'avez pas la permission d'utiliser cette commande.");
                return;
            }

            SpawnLootCrate();
            player.ChatMessage("Une caisse de loot spéciale a été générée.");
        }
    }
}
