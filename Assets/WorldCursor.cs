using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCursor : MonoBehaviour {

    MeshRenderer meshRenderer;

    void Start() {
        meshRenderer = this.gameObject.GetComponentInChildren<MeshRenderer>();
    }

    void Update() {
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo)) {
            meshRenderer.enabled = true;
            transform.position = hitInfo.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        } else {
            meshRenderer.enabled = false;
        }
    }
}
