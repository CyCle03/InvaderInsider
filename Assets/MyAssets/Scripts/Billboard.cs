using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera _camera;
    GameObject _gameObj;

    private void Start()
    {
        _camera = Camera.main;
        _gameObj = gameObject;
    }

    // Start is called before the first frame update
    private void LateUpdate()
    {
        _gameObj.transform.forward = _camera.transform.forward;
    }
}

