using EmotionBank;
using Unity.Netcode;
using UnityEngine;

public class PlayerUIController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {

        Debug.Log($"Spawned Player object. IsOwner: {IsOwner}");

        if (IsOwner)
        {
            // 1. Make sure the name matches EXACTLY (Case sensitive!)
            GameObject ui = GameObject.Find("SignInCanvas");

            if (ui != null)
            {
                Debug.Log("Found UI! Deactivating it now.");
                ui.SetActive(false);
            }
            else
            {
                Debug.LogError("COULD NOT FIND UI with name 'SignInCanvas'. Check the hierarchy name!");
            }


            // 1. Tell Vivox "I am the local player, track my position"
            if (SimpleVivoxManager.Instance != null)
            {
                SimpleVivoxManager.Instance.SetLocalPlayer(transform);

                // 2. TRIGGER THE CONNECTION NOW
                SimpleVivoxManager.Instance.JoinGameVoice();
            }
        }
    }
}