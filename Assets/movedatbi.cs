using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Linq;
using UnityEngine.Rendering.Universal;

public class JointController : MonoBehaviour
{
    // Reference to the MuJoCo simulation instance
    private MjScene mjScene;
    
    // List to hold all joints in the model
    private List<MjActuator> actuators = new List<MjActuator>();
    
    // Index of the currently selected joint
    private int currentActIndex = 0;
    
    // Force magnitude to apply (adjust as needed)
    public float forceMagnitude = 0.01f;
    
    // For on-screen display
    private string currentJointName = "";

    void Start()
    {
        // Get the MjScene component
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
        
        if (actuators.Count == 0)
        {
            Debug.LogWarning("No MjJoint components found in the scene!");
        }
        else
        {
            // Set initial selected joint
            UpdateSelectedActuator();
        }
    }
    
    void Update()
    {
        if (actuators.Count == 0) return;
        
        // Switch joints with up/down arrows
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentActIndex = (currentActIndex + 1) % actuators.Count;
            UpdateSelectedActuator();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentActIndex--;
            if (currentActIndex < 0) currentActIndex = actuators.Count - 1;
            UpdateSelectedActuator();
        }
        
        // Apply force with left/right arrows
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            ApplyForceToActuator(-forceMagnitude);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            ApplyForceToActuator(forceMagnitude);
        }
    }
    
    void UpdateSelectedActuator()
    {
        if (currentActIndex >= 0 && currentActIndex < actuators.Count)
        {
            currentJointName = actuators[currentActIndex].name;
            Debug.Log("Selected joint: " + currentJointName);
        }
    }
    
    void ApplyForceToActuator(float force)
    {
        if (currentActIndex >= 0 && currentActIndex < actuators.Count)
        {
            MjActuator actuator = actuators[currentActIndex];
            unsafe {
                MujocoLib.mjData_* data = mjScene.Data;
                
                // Different joint types might require different approaches to applying force
                if (mjScene && mjScene.isActiveAndEnabled && actuator)
                {
                    actuator.Control += force;

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
                    
                    Debug.Log($"Applied force {force} to {actuator.name} has values F {actuator.Force} V:{actuator.Velocity} L:{actuator.Length}");
                }
            }
        }
    }
    
    void OnGUI()
    {
        // Display the selected joint on screen
        GUI.Label(new Rect(10, 10, 300, 20), "Selected Joint: " + currentJointName);
    }
}
