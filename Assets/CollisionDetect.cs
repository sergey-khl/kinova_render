using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Linq;

public class CollisionDetect : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.contacts.Length);
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log($"Collision at {contact.point} and noraml of {contact.normal} inertia of {contact.impulse}, {collision.gameObject.name}");
        }
    }
}
