using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public float maxSpeed = 10f;
    public bool canMove;

    float inputX = 0f;
    float inputZ = 0f;
    float speed = 0f;
    int speedHash;

    CharacterController controller;
    Camera cam;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        canMove = true;
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        cam = Camera.main;

    }

    // Update is called once per frame
    void Update()
    {
        if (!canMove)
        {
            return;
        }

        inputX = Input.GetAxis("Horizontal");
        inputZ = Input.GetAxis("Vertical");
        speed = new Vector2(inputX, inputZ).magnitude;
        speedHash = Animator.StringToHash("Speed");
        if (speed>0.1f)
        {
            HandleMovement();
        }
        
        HandleAnimation();

    }

    void HandleMovement()
    {
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction = inputX * camRight + inputZ * camForward;
        Quaternion targetAngle = Quaternion.LookRotation(direction);

        //        StartCoroutine(RotateOverTime(targetAngle, rotateDuration));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetAngle, 0.2f);
        controller.Move(direction * Time.deltaTime * maxSpeed);
    }

    void HandleAnimation()
    {
        anim.SetFloat(speedHash, speed);
    }
    /*
    IEnumerator RotateOverTime(Quaternion targetAngle, float duration)
    {
        if (duration>0f)
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            Quaternion originalAngle = transform.rotation;
            while (Time.time<endTime)
            {
                float progress = (Time.time - startTime) / duration;
                transform.rotation = Quaternion.Slerp(originalAngle, targetAngle, progress);
                yield return null;
            }
        }
        transform.rotation = targetAngle;
    }
    */
}
