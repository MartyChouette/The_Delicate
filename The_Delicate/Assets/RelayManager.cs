using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TMP_Text joinCodeText;
    public TMP_Text statusText; // ASSIGN THIS to see errors on screen!

    private async void Start()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateRelay()
    {
        UpdateStatus("Creating Relay...");
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (joinCodeText != null) joinCodeText.text = "Code: " + joinCode;
            UpdateStatus("Host Ready. Code: " + joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            UpdateStatus("Host Failed: " + e.Message);
        }
    }

    public async void JoinRelay()
    {
        // 1. TRIM INPUT (Removes invisible spaces!)
        string joinCode = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            UpdateStatus("Please enter a code.");
            return;
        }

        UpdateStatus("Connecting to Relay...");

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            UpdateStatus("Client Started. Waiting for Host...");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay Join Failed: " + e);
            UpdateStatus("Join Failed: " + e.Message);
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Relay] " + msg);
    }
}