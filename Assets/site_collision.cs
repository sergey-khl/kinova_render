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
        public float smoothingFactor = 10f;
        private Vector3 currentForce = Vector3.zero;
        private Vector3 targetForce = Vector3.zero;

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
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            data = new byte[1024];
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 10001);    // ensure that this port is the same as the one youre sending data from
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(ip);
            IPEndPoint sender = new IPEndPoint(IPAddress.Parse("192.168.1.2"), 10001);
            remote = (EndPoint)(sender);
        }

        // Update is called once per frame
        void Update()
        {
            // Debug.Log(site.SensorReading);
        }
        // void FixedUpdate() {
            // unsafe {
            //     MujocoLib.mjModel_* model = MjScene.Instance.Model;
            //     MujocoLib.mjData_* data = MjScene.Instance.Data;
            //     MujocoLib.mjContact_* contacts = data->contact;
            //     // double* sensor_data = data->sensordata;
            //     for (int i = 0; i < data->ncon; i++) {
            //         int geom1Id = contacts[i].geom[0];
            //         int geom2Id = contacts[i].geom[1];
            //         double* pos = contacts[i].pos;
            //         double* daforce = stackalloc double[6];
                    
            //         MujocoLib.mj_contactForce(model, data, i, daforce);

            //         // Get geometry names from model
            //         // int geom1Name = model->names[model->name_geomadr[geom1Id]];
            //         // int geom2Name = model->names[model->name_geomadr[geom2Id]];
            //         // string geom2Name = MujocoLib.mj_id2name(model, 5, geom2Id);

            //         // Debug.Log($"Contact {i}: geom1 ID = {geom1Id} (Name: {geom1Name}), geom2 ID = {geom2Id} (Name: {geom2Name})");
            //         if (geom1Id != 0 || geom2Id != 5) {

            //             Debug.Log($"Contact {i}: geom1 ID = {geom1Id} {geom2Id} at position {pos[0]} {pos[1]} {pos[2]} with a force of {daforce[0]} {daforce[1]} {daforce[2]}");
            //         }

            //     }
            //     // foreach (var contact in contacts) {
            //     //     Debug.Log($"Collision between {contact.geom1} and {contact.geom2}. Force: {contact.force}");
            //     // }

                
            // }
        // }

        // void OnDrawGizmos()
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
                    int geom1Id = contacts[i].geom[0];
                    int geom2Id = contacts[i].geom[1];
                    double* pos = contacts[i].pos;
                    double* frame = contacts[i].frame;
                    double* daforce = stackalloc double[6];
                    double* daforceGlobal = stackalloc double[6];
                    

                    // Get geometry names from model
                    IntPtr namePtr1 = IntPtr.Add((IntPtr)model->names, model->name_geomadr[geom1Id]); 
                    IntPtr namePtr2 = IntPtr.Add((IntPtr)model->names, model->name_geomadr[geom2Id]); 
                    string geom1Name = Marshal.PtrToStringAnsi(namePtr1);
                    string geom2Name = Marshal.PtrToStringAnsi(namePtr2);
                    string touch_name = "touch_tip";

                    // IntPtr bodyPtr1 = IntPtr.Add((IntPtr)model->names, model->body_geomadr[geom1Id]); 
                    // IntPtr bodyPtr2 = IntPtr.Add((IntPtr)model->names, model->body_geomadr[geom2Id]); 

                    // Debug.Log($"Contact {i}: geom1 ID = {geom1Id} (Name: {geom1Name}, geom2 ID = {geom2Id} (Name: {geom2Name})");
                    if (geom1Name.StartsWith(touch_name) || geom2Name.StartsWith(touch_name)) {
                        pen_contact = true;
                        MujocoLib.mj_contactForce(model, data, i, daforce);
                        MujocoLib.mju_mulMatTVec(daforceGlobal, frame, daforce, 3, 3);

                        // Debug.Log($"Contact {i}: geom1 ID = {geom1Name} {geom2Name} at position {pos[0]} {pos[1]} {pos[2]} with a force of {daforce[0]} {daforce[1]} {daforce[2]} ({daforce[3]} {daforce[4]} {daforce[5]}) ({daforceGlobal[0]} {daforceGlobal[1]} {daforceGlobal[2]})");
                        Debug.Log($"Contact {i}: geom1 ID = {geom1Name} {geom2Name} ({daforceGlobal[1]} {daforceGlobal[2]} {daforceGlobal[0]})");
                        
                        Vector3 contactPosition = new Vector3((float)pos[0], (float)pos[2], (float)pos[1]);
                        targetForce = new Vector3((float)daforceGlobal[1], (float)daforceGlobal[2], (float)daforceGlobal[0]);

                       

                        // Draw force vector as a line in Scene View
                        // Gizmos.color = Color.red;
                        // Gizmos.DrawLine(contactPosition, contactPosition + forceVector);
                        // Gizmos.DrawSphere(contactPosition, 0.02f); // Small sphere for contact point
                    }

                    // Convert to Unity types
                }

                if (!pen_contact) {
                    targetForce = Vector3.zero;
                }

                currentForce = Vector3.Lerp(currentForce, targetForce, smoothingFactor * Time.fixedDeltaTime);


                byte[] force_message = new byte[12];
                Buffer.BlockCopy(BitConverter.GetBytes(currentForce.x), 0, force_message, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(currentForce.y), 0, force_message, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(currentForce.z), 0, force_message, 8, 4);
                UDPsend(force_message);
            }
        }
        
        // sends pose and jaw messages to dVRK over UDP connection
        public void UDPsend(byte[] force_message)
        {
            // send json strings to dVRK //
            socket.SendTo(force_message, remote);
        }
    }
}