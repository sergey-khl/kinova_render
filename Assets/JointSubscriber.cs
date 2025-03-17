using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Mujoco;
using System.Linq;
using UnityEngine.Rendering.Universal;

public class JointSubscriber : MonoBehaviour
{
    public static byte[] data;
    public static Socket socket;
    public static EndPoint remote;
    public static byte[] send_msg;
    public static string omni_msg;
    public static string force_message;

    public static int read_msg_count = 0;
    bool pause = false;

    private MjScene mjScene;
    
    // List to hold all joints in the model
    private List<MjActuator> actuators = new List<MjActuator>();

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        data = new byte[1024];
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, 10000);    // ensure that this port is the same as the one youre sending data from
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(ip);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)(sender);

        mjScene = FindFirstObjectByType<MjScene>();
        if (mjScene == null)
        {
            Debug.LogError("MjScene not found in the scene!");
            return;
        }
        
        // Wait one frame to ensure MuJoCo is fully initialized
        StartCoroutine(InitializeAfterMuJoCo());
    }

    IEnumerator InitializeAfterMuJoCo()
    {
        // Wait one frame
        yield return null;
        
        // Get all joints from the model
        actuators = FindObjectsOfType<MjActuator>().ToList();
        actuators.Sort((a, b) => GetJointIndex(a.name).CompareTo(GetJointIndex(b.name)));

    }

    int GetJointIndex(string actuatorName)
    {
        // Assuming the actuator name follows the pattern 'joint_X' (e.g., joint_1, joint_2, etc.)
        // Extract the number after "joint_"
        if (actuatorName.StartsWith("joint_"))
        {
            string indexString = actuatorName.Substring(6);  // Remove "joint_"
            if (int.TryParse(indexString, out int index))
            {
                return index;
            }
        }
        return -1; // Return a default value if the name doesn't match expected format
    }

    //  void FixedUpdate()
     void Update()
    {

        read_msg_count = 0;

        while (read_msg_count < 2)
        {
            // read in message from dVRK
            data = new byte[1024];
            socket.ReceiveFrom(data, ref remote);
            omni_msg = Encoding.UTF8.GetString(data);
            // if jaw message
            var jsonData = JsonConvert.DeserializeObject<JsonData>(omni_msg);

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

                    // For a hinge joint (1 DoF)
                    // mjScene.SetJointForce(joint, new double[] { force });
                    // data->qpos[joint.QposAddress] = force;
                    // float mass = 1.0f / (float)mjScene.Model->body_invweight0[2*body.MujocoId];
                    // Vector3 unityForce = springStiffness * mass;
                    // unityForce -= bodyVel * Mathf.Sqrt(springStiffness) * mass;
                    // Vector3 mjForce = MjEngineTool.MjVector3(unityForce);
                    // scene.Data->xfrc_applied[6*body.MujocoId + 0] = mjForce.x;
                    // scene.Data->xfrc_applied[6*body.MujocoId + 1] = mjForce.y;
                    // scene.Data->xfrc_applied[6*body.MujocoId + 2] = mjForce.z;
                    
                }
            }

            // // if pose message
            // else if (parser.StringMatch(dVRK_msg, "\"setpoint_cp\":"))
            // {
            //     // extract rot and pos
            //     EE_quat = QuaternionFromMatrix(parser.GetMatrix4X4(dVRK_msg));
            //     Matrix4x4 temp = parser.GetMatrix4X4(dVRK_msg);
            //     //Debug.Log("dVRK rot: " + temp.rotation);
            //     EE_pos = parser.GetPos(dVRK_msg);

            //     // record data
            //     if (ReaddVRKmsg)
            //     {
            //         Debug.Log(pause);
            //         if (!pause)
            //         {
            //             read.text = "Recording";
            //             using (StreamWriter writer = new StreamWriter(incoming_pose, true))
            //             {
            //                 timer += Time.deltaTime;
            //                 string pose = "\n " + timer + " " + EE_pos[0].ToString("R") + " " + EE_pos[1].ToString("R") + " " + EE_pos[2].ToString("R") + " " + EE_quat[0].ToString("R") + " " + EE_quat[1].ToString("R") + " " + EE_quat[2].ToString("R") + " " + EE_quat[3].ToString("R");
            //                 writer.WriteLine(pose);

            //             }

            //             using (StreamWriter writer = new StreamWriter(hololens_transform_pinch, true))
            //             {
            //                 delta_timer_holo += Time.deltaTime;
            //                 timer_holo = Time.unscaledDeltaTime;
            //                 string holo_transform = "\n" + timer_holo + " " + timer + " " + hololens.transform.position[0].ToString("R") + " " + hololens.transform.position[1].ToString("R") + " " + hololens.transform.position[2].ToString("R") + " " + hololens.transform.rotation[0].ToString("R") + " " + hololens.transform.rotation[1].ToString("R") + " " + hololens.transform.rotation[2].ToString("R") + " " + hololens.transform.rotation[3].ToString("R");
            //                 writer.WriteLine(holo_transform);
            //             }
            //         }
            //         else
            //         {
            //             Debug.Log("paused");
            //             read.text = "Recording paused";
            //         }
            //     }
            //     else
            //     {
            //         read.text = "Recording complete";
            //     }
            // }

            // // if neither, probably restarted
            // else
            // {
            //     HandTrack.new_EE_pos = Vector3.zero;
            // }

            read_msg_count += 1;
        }
    }


    public void PauseRecord()
    {
        pause = !pause;
    }
}
