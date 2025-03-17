using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Mujoco;
using System.Linq;
using UnityEngine.Rendering.Universal;

public class JointSubscriber : MonoBehaviour
{
    private static byte[] data;
    private static Socket socket;
    private static EndPoint remote;
    private static string omni_msg;

    private MjScene mjScene;
    private List<MjActuator> actuators = new List<MjActuator>();
    public int port = 10000;

    private UdpClient udp_client;
    private readonly Queue<string> incoming_queue = new Queue<string>();
    Thread receive_thread;
    private bool thread_running = false;

    public class MeasuredJs
    {
        public bool AutomaticTimestamp { get; set; }
        public float[] Effort { get; set; }
        public string[] Name { get; set; }
        public float[] Position { get; set; }
        public double Timestamp { get; set; }
        public bool Valid { get; set; }
        public object Velocity { get; set; }
    }

    public class JsonData
    {
        public MeasuredJs measured_js { get; set; }
    }

    // recieve udp from omni
    void Start()
    {
        data = new byte[1024];

        try { udp_client = new UdpClient(port); }
        catch (Exception e)
        {
            Debug.Log("Failed to listen for UDP at port " + port + ": " + e.Message);
            return;
        }

        mjScene = FindFirstObjectByType<MjScene>();
        if (mjScene == null)
        {
            Debug.LogError("MjScene not found in the scene!");
            return;
        }
        
        // Wait one frame to ensure MuJoCo is fully initialized
        StartCoroutine(InitializeAfterMuJoCo());
        

        // start recieving data asynchronously
        receive_thread = new Thread(() => ListenForMessages(udp_client));
        receive_thread.IsBackground = true;
        thread_running = true;
        receive_thread.Start();
    }

    // used this guy: https://gist.github.com/hyakugei/39ab8dc1b88829c8b18153840c4b3bc8
    private void ListenForMessages(UdpClient client)
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
 
        while (thread_running)
        {
            try
            {
                data = client.Receive(ref remote);
                omni_msg = Encoding.UTF8.GetString(data);
 
                lock (incoming_queue)
                {
                    incoming_queue.Enqueue(omni_msg);
                }
            }
            catch (SocketException e)
            {
                // 10004 thrown when socket is closed
                if (e.ErrorCode != 10004) Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
            }
            catch (Exception e)
            {
                Debug.Log("Error receiving data from udp client: " + e.Message);
            }
            Thread.Sleep(1);
        }
    }

     public string[] GetMessages()
    {
        string[] pending_messages = new string[0];
        lock (incoming_queue)
        {
            pending_messages = new string[incoming_queue.Count];
            int i = 0;

            while (incoming_queue.Count != 0)
            {
                pending_messages[i] = incoming_queue.Dequeue();
                i++;
            }
        }
 
        return pending_messages;
    }

    IEnumerator InitializeAfterMuJoCo()
    {
        // Wait one frame
        yield return null;
        
        // Get all joints from the model and sort by the number
        actuators = FindObjectsOfType<MjActuator>().ToList();
        actuators.Sort((a, b) => GetJointIndex(a.name).CompareTo(GetJointIndex(b.name)));
    }

    int GetJointIndex(string actuatorName)
    {
        // Extract the number after "joint_"
        if (actuatorName.StartsWith("joint_"))
        {
            string indexString = actuatorName.Substring(6);
            if (int.TryParse(indexString, out int index))
            {
                return index;
            }
        }
        // not found
        return -1;
    }

     void FixedUpdate()
    {
        string[] msgs = GetMessages();
        // need at least one udp msg to update
        if (msgs.Length == 0) {
            return;
        }
        var jsonData = JsonConvert.DeserializeObject<JsonData>(msgs[msgs.Length - 1]);

        // Extract the Position array
        var positions = jsonData.measured_js.Position;

        // Print the Position array values
        unsafe {
            MujocoLib.mjData_* data = mjScene.Data;
            
            // Different joint types might require different approaches to applying force
            if (mjScene && mjScene.isActiveAndEnabled)
            {
                for (int i = 0; i < 4; i++) {
                    // TODO: change the mujoco config file to properly correspond for the base joint
                    if (i == 0) {
                        positions[i] *= -1;
                    }
                    actuators[i].Control = positions[i];
                    // Debug.Log($"Applied force {positions[i]} to {actuators[i].name} has values F {i} ");
                }
            }
        }
    }

    public void Stop()
    {
        thread_running = false;
        receive_thread.Abort();
        udp_client.Close();
    }
}
