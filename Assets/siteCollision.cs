using System;
using System.Xml;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Mujoco;
using System.Linq;
using UnityEngine.Rendering.Universal;


namespace Mujoco {
    public class site_collision : MonoBehaviour
    {
        public float smoothing_factor = 10f;
        private Vector3 current_force = Vector3.zero;
        private Vector3 target_force = Vector3.zero;

        private static Socket socket;
        private static EndPoint remote;
        public string remote_ip = "127.0.0.1";
        public int port = 10001;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint sender = new IPEndPoint(IPAddress.Parse(remote_ip), port);
            remote = (EndPoint)(sender);
        }

        void FixedUpdate()
        {
            if (!Application.isPlaying) return;
            unsafe {
                MujocoLib.mjModel_* model = MjScene.Instance.Model;
                MujocoLib.mjData_* data = MjScene.Instance.Data;
                MujocoLib.mjContact_* contacts = data->contact;
                
                bool pen_contact = false;

                // double* sensor_data = data->sensordata;
                for (int i = 0; i < data->ncon; i++) {
                    int geom1_id = contacts[i].geom[0];
                    int geom2_id = contacts[i].geom[1];
                    double* pos = contacts[i].pos;
                    double* frame = contacts[i].frame;
                    double* local_force = stackalloc double[6];
                    double* global_force = stackalloc double[6];

                    // Get geometry names from model
                    IntPtr name_ptr1 = IntPtr.Add((IntPtr)model->names, model->name_geomadr[geom1_id]); 
                    IntPtr name_ptr2 = IntPtr.Add((IntPtr)model->names, model->name_geomadr[geom2_id]); 
                    string geom1_name = Marshal.PtrToStringAnsi(name_ptr1);
                    string geom2_name = Marshal.PtrToStringAnsi(name_ptr2);
                    string touch_name = "touch_tip";

                    // Debug.Log($"Contact {i}: geom1 ID = {geom1Id} (Name: {geom1Name}, geom2 ID = {geom2Id} (Name: {geom2Name})");
                    if (geom1_name.StartsWith(touch_name) || geom2_name.StartsWith(touch_name)) {
                        pen_contact = true;
                        // local_force[0] will be along the main normal axis that points from geom0 to geom1. the others are tangent and idk how they work o_o
                        MujocoLib.mj_contactForce(model, data, i, local_force);
                        // the force is found according to the normal and we can find this in the global frame using this
                        MujocoLib.mju_mulMatTVec(global_force, frame, local_force, 3, 3);

                        // Debug.Log($"Contact {i}: geom1 ID = {geom1Name} {geom2Name} at position {pos[0]} {pos[1]} {pos[2]} with a force of {daforce[0]} {daforce[1]} {daforce[2]} ({daforce[3]} {daforce[4]} {daforce[5]}) ({daforceGlobal[0]} {daforceGlobal[1]} {daforceGlobal[2]})");
                        Debug.Log($"Contact {i}: geom1 ID = {geom1_name} {geom2_name} ({global_force[1]} {global_force[2]} {global_force[0]})");
                        
                        target_force = new Vector3((float)global_force[1], (float)global_force[2], (float)global_force[0]);
                    }
                }

                if (!pen_contact) {
                    target_force = Vector3.zero;
                }
                // smooth out the force
                current_force = Vector3.Lerp(current_force, target_force, smoothing_factor * Time.fixedDeltaTime);

                byte[] force_message = new byte[12];
                Buffer.BlockCopy(BitConverter.GetBytes(current_force.x), 0, force_message, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(current_force.y), 0, force_message, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(current_force.z), 0, force_message, 8, 4);
                UDPsend(force_message);
            }
        }
        
        // sends pose and jaw messages to dVRK over UDP connection
        public void UDPsend(byte[] force_message)
        {
            socket.SendTo(force_message, remote);
        }
    }
}