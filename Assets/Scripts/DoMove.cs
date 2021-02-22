using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DoMove : MonoBehaviour
{
    public Transform target;
    private Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger("Slash");
        }

    }

    public void Warp()
    {
        transform.DOMove(target.position, 0.25f);
    }
}
