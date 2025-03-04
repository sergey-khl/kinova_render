using System.Collections.Generic;
using UnityEngine;

public class CollisionDetect : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log($"Collision at {contact.point} and noraml of {contact.normal} inertia of {contact.impulse}, {collision.gameObject.name}");
        }
    }
}
