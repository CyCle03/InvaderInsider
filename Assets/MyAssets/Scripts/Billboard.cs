using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    // Start is called before the first frame update
    private void LateUpdate()
    {
        transform.forward = _camera.transform.forward;
    }
}

