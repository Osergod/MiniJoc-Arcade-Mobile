using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour {
    public TMPro.TextMeshProUGUI input;
    public InputAction touchInputAction;

    private void OnEnable() {
        touchInputAction.Enable();
    }

    private void OnDisable() {
        touchInputAction.Disable();
    }



    // Start is called before the first frame update
    void Start() {

    }   

    // Update is called once per frame
    void Update() {
        input.text = touchInputAction.ReadValue<Vector2>().ToString();
    }
}
