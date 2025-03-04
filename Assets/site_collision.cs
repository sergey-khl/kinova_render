using System;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Mujoco {
    public class site_collision : MonoBehaviour
    {
        private MjSiteScalarSensor site;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            site = FindFirstObjectByType<MjSiteScalarSensor>();
            if (site == null)
            {
                Debug.LogError("MuJoCoModel component not found in the scene.");
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Debug.Log(site.SensorReading);
        }
        void FixedUpdate() {
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
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            unsafe {
                MujocoLib.mjModel_* model = MjScene.Instance.Model;
                MujocoLib.mjData_* data = MjScene.Instance.Data;
                MujocoLib.mjContact_* contacts = data->contact;
                // double* sensor_data = data->sensordata;
                for (int i = 0; i < data->ncon; i++) {
                    int geom1Id = contacts[i].geom[0];
                    int geom2Id = contacts[i].geom[1];
                    double* pos = contacts[i].pos;
                    double* daforce = stackalloc double[6];
                    
                    MujocoLib.mj_contactForce(model, data, i, daforce);

                    // Get geometry names from model
                    // int geom1Name = model->names[model->name_geomadr[geom1Id]];
                    // int geom2Name = model->names[model->name_geomadr[geom2Id]];
                    // string geom2Name = MujocoLib.mj_id2name(model, 5, geom2Id);

                    // Debug.Log($"Contact {i}: geom1 ID = {geom1Id} (Name: {geom1Name}), geom2 ID = {geom2Id} (Name: {geom2Name})");
                    if (geom1Id != 0 || geom2Id != 5) {

                        Debug.Log($"Contact {i}: geom1 ID = {geom1Id} {geom2Id} at position {pos[0]} {pos[1]} {pos[2]} with a force of {daforce[0]} {daforce[1]} {daforce[2]}");
                    }

                    // Convert to Unity types
                    Vector3 contactPosition = new Vector3((float)pos[0], (float)pos[2], (float)pos[1]);
                    Vector3 forceVector = new Vector3((float)daforce[0], (float)daforce[2], (float)daforce[1]);

                    // Draw force vector as a line in Scene View
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(contactPosition, contactPosition + forceVector);
                    Gizmos.DrawSphere(contactPosition, 0.02f); // Small sphere for contact point

                }
                // foreach (var contact in contacts) {
                //     Debug.Log($"Collision between {contact.geom1} and {contact.geom2}. Force: {contact.force}");
                // }


            }
        }
    }
}