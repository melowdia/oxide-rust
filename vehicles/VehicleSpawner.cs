using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Spawner", "Melodia", "1.0.0")]
    [Description("Allows players to spawn vehicles with specific cooldowns and fuel")]

    public class VehicleSpawner : RustPlugin
    {
        private Dictionary<ulong, DateTime> miniCooldowns = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, DateTime> boatCooldowns = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, DateTime> carCooldowns = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, DateTime> zodCooldowns = new Dictionary<ulong, DateTime>();

        private const int MiniCooldownMinutes = 10;
        private const int BoatCooldownHours = 24;
        private const int CarCooldownHours = 24;
        private const int ZodCooldownMinutes = 10;
        private const int FuelAmount = 50;

        [ChatCommand("mymini")]
        private void SpawnMiniCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanSpawnVehicle(player, miniCooldowns, MiniCooldownMinutes, "minicopter"))
                return;

            var position = player.transform.position + player.transform.forward * 5;
            BaseVehicle miniCopter = (BaseVehicle)GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab", position);
            if (miniCopter != null)
            {
                miniCopter.Spawn();
                miniCopter.GetFuelContainer().AddFuel(FuelAmount);
                miniCooldowns[player.userID] = DateTime.Now;
                SendReply(player, "Votre minicopter a été spawn avec 50 unités de carburant.");
            }
        }

        [ChatCommand("myboat")]
        private void SpawnBoatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanSpawnVehicle(player, boatCooldowns, BoatCooldownHours * 60, "tugboat"))
                return;

            var position = player.transform.position + player.transform.forward * 5;
            BaseVehicle tugBoat = (BaseVehicle)GameManager.server.CreateEntity("assets/content/vehicles/boats/tugboat/tugboat.entity.prefab", position);
            if (tugBoat != null)
            {
                tugBoat.Spawn();
                boatCooldowns[player.userID] = DateTime.Now;
                SendReply(player, "Votre tugboat a été spawn. Vous pouvez l'utiliser une fois par jour.");
            }
        }

        [ChatCommand("mycar")]
        private void SpawnCarCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanSpawnVehicle(player, carCooldowns, CarCooldownHours * 60, "caravan"))
                return;

            var position = player.transform.position + player.transform.forward * 5;
            BaseVehicle caravan = (BaseVehicle)GameManager.server.CreateEntity("assets/content/vehicles/car/caravan.entity.prefab", position);
            if (caravan != null)
            {
                caravan.Spawn();
                caravan.GetFuelContainer().AddFuel(FuelAmount);
                carCooldowns[player.userID] = DateTime.Now;
                SendReply(player, "Votre caravane a été spawn avec 50 unités de carburant.");
            }
        }

        [ChatCommand("myzod")]
        private void SpawnZodCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanSpawnVehicle(player, zodCooldowns, ZodCooldownMinutes, "Zodiac"))
                return;

            var position = player.transform.position + player.transform.forward * 5;
            BaseVehicle zodiac = (BaseVehicle)GameManager.server.CreateEntity("assets/content/vehicles/boats/rowboat/rowboat.entity.prefab", position);
            if (zodiac != null)
            {
                zodiac.Spawn();
                zodCooldowns[player.userID] = DateTime.Now;
                SendReply(player, "Votre Zodiac a été spawn. Vous pouvez l'utiliser toutes les 10 minutes.");
            }
        }

        private bool CanSpawnVehicle(BasePlayer player, Dictionary<ulong, DateTime> cooldowns, int cooldownMinutes, string vehicleName)
        {
            ulong playerId = player.userID;

            if (cooldowns.ContainsKey(playerId) && cooldowns[playerId] > DateTime.Now)
            {
                var remainingTime = cooldowns[playerId] - DateTime.Now;
                SendReply(player, $"Vous devez attendre encore {remainingTime.Minutes} minutes et {remainingTime.Seconds} secondes pour faire spawn un(e) {vehicleName}.");
                return false;
            }

            cooldowns[playerId] = DateTime.Now.AddMinutes(cooldownMinutes);
            return true;
        }
    }
}
