using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera _camera;
    public GameObject _gameObj;

    private void Start()
    {
        _camera = Camera.main;
    }

    // Start is called before the first frame update
    private void LateUpdate()
    {
        _gameObj.transform.forward = _camera.transform.forward;
    }
}

