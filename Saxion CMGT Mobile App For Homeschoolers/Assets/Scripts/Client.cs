using SharedLibrary.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class Client : MonoBehaviour
{
    private async void Start()
    {
        var client = new NetClient();
        bool ok = await Task.Run(() => client.ConnectAsync("127.0.0.1", 5000));

        if (!ok)
        {
            Debug.Log("Failed to connect.");
            return;
        }

        Debug.Log("Connected. Sending test sequence...");

        // Send some test TCP messages (chat, rpc, etc.)
        await client.SendTcpTestMessage("Hello from client!");

        // Send a few UDP movement states
        for (int i = 0; i < 5; i++)
        {
            var state = new PlayerStateUdp
            {
                Sequence = (ulong)i,
                EntityId = 1,
                Px = i * 1.0f,
                Py = 0,
                Pz = 0,
                Qx = 0,
                Qy = 0,
                Qz = 0,
                Qw = 1
            };
            client.SendPlayerState(state);
            Debug.Log($"UDP PlayerState sent #{i}");
            await Task.Delay(500);
        }

        Debug.Log("Test sequence complete");
    }

}
