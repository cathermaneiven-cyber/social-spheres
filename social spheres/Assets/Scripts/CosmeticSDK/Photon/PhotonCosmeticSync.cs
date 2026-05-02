using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Syncs cosmetic loadouts across the Photon Realtime room using Player Custom Properties.
    //
    // This is written for Photon REALTIME (PhotonRealtime / PhotonUnityNetworking from your
    // project's Photon folder), NOT PUN 2's MonoBehaviourPunCallbacks.
    //
    // How it works:
    //   - Local player: calling NetworkEquip() equips locally AND sets a Photon custom property.
    //   - Remote players: OnPlayerPropertiesUpdate fires and re-applies the loadout on that avatar.
    //   - Late joiners: the custom property is already set, so Start() reads it immediately.
    //
    // Setup:
    //   1. Add this component to your Player prefab alongside AvatarCosmeticController.
    //   2. Assign the localPlayerId field for the local player (usually from PhotonNetwork or your
    //      own player manager — see the comment below).
    //   3. Make sure your Photon ILoadBalancingCallbacks or IInRoomCallbacks listener calls
    //      OnPlayerPropertiesUpdate on this component (or wire it via your existing callback system).

    [RequireComponent(typeof(AvatarCosmeticController))]
    public class PhotonCosmeticSync : MonoBehaviour, IInRoomCallbacks
    {
        public const string LoadoutKey = "COSMETICS";

        [Header("Photon")]
        [Tooltip("Is this the local player's avatar? Set this from your player spawn logic.")]
        public bool isLocalPlayer = false;

        [Tooltip("Reference to this avatar's Photon Player. Assigned from your spawn/join code.")]
        public Player photonPlayer;

        private AvatarCosmeticController _controller;
        private LoadBalancingClient _client;

        private void Awake()
        {
            _controller = GetComponent<AvatarCosmeticController>();
        }

        private void Start()
        {
            if (!isLocalPlayer && photonPlayer != null)
                LoadFromPlayer(photonPlayer);
        }

        // Call this to connect the Photon client so we receive property callbacks.
        // Pass in your LoadBalancingClient from wherever you manage Photon in your game.
        public void Init(LoadBalancingClient client, Player player, bool local)
        {
            _client     = client;
            photonPlayer = player;
            isLocalPlayer = local;

            _client.AddCallbackTarget(this);

            if (!isLocalPlayer)
                LoadFromPlayer(photonPlayer);
            else
                PublishLoadout(_controller.GetCurrentLoadout());
        }

        private void OnDestroy()
        {
            _client?.RemoveCallbackTarget(this);
        }

        // ── Public API (local player only) ────────────────────────────────────

        public void NetworkEquip(string cosmeticId, Color? tint = null)
        {
            if (!isLocalPlayer) return;
            _controller.EquipItem(cosmeticId, tint);
            PublishLoadout(_controller.GetCurrentLoadout());
        }

        public void NetworkUnequip(EquipSlot slot)
        {
            if (!isLocalPlayer) return;
            _controller.UnequipSlot(slot);
            PublishLoadout(_controller.GetCurrentLoadout());
        }

        public void NetworkApplyLoadout(CosmeticLoadout loadout)
        {
            if (!isLocalPlayer) return;
            _controller.ApplyLoadout(loadout);
            PublishLoadout(loadout);
        }

        // ── IInRoomCallbacks ──────────────────────────────────────────────────

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (isLocalPlayer) return;
            if (targetPlayer.ActorNumber != photonPlayer?.ActorNumber) return;

            if (changedProps.TryGetValue(LoadoutKey, out object raw))
            {
                var loadout = CosmeticLoadout.Deserialize(raw as string);
                _controller.ApplyLoadout(loadout);
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            // Re-broadcast our loadout so the new player sees us correctly.
            if (isLocalPlayer)
                PublishLoadout(_controller.GetCurrentLoadout());
        }

        public void OnPlayerLeftRoom(Player otherPlayer) { }
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
        public void OnMasterClientSwitched(Player newMasterClient) { }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PublishLoadout(CosmeticLoadout loadout)
        {
            if (_client?.LocalPlayer == null) return;
            var props = new Hashtable { [LoadoutKey] = loadout.Serialize() };
            _client.LocalPlayer.SetCustomProperties(props);
        }

        private void LoadFromPlayer(Player player)
        {
            if (player.CustomProperties.TryGetValue(LoadoutKey, out object raw))
            {
                var loadout = CosmeticLoadout.Deserialize(raw as string);
                _controller.ApplyLoadout(loadout);
            }
        }
    }
}
