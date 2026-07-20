using pworld.Scripts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScoutInACannon
{
    internal class LaunchThisGuy : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(disableCollidersForASecond());
            StartCoroutine(LaunchPlayer());
        }
        void LaunchTarget()
        {
            Character c = GetComponent<Character>();
            c.data.launchedByCannon = true;
            c.RPCA_Fall(1);
            c.AddForce(Plugin.tubeForward * 2000f, 1f, 1f);// launchForce is 2000 for players
        }
        System.Collections.IEnumerator LaunchPlayer()
        {
            LaunchTarget();
            yield return new WaitForSeconds(Plugin.stopTime.Value);

            foreach (Rigidbody rb in this.GetComponentsInChildren<Rigidbody>())
            {
                if (rb.isKinematic) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        System.Collections.IEnumerator disableCollidersForASecond()
        {
            if (this == null) yield break;

            var collidersToRestore = new List<Collider>();

            foreach (var col in this.GetComponentsInChildren<Collider>())
            {
                if (col != null && col.enabled)
                {
                    collidersToRestore.Add(col);
                    col.enabled = false; // Turn it off
                }
            }

            yield return new WaitForSeconds(0.25f);
            foreach (var col in collidersToRestore)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }
    }
}
