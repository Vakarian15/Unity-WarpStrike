using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WarpController : MonoBehaviour
{
    public List<Transform> screenTargets = new List<Transform>();
    public Transform target;
    public Transform sword;
    public Transform hand;
    public Material glowMaterial;
    public ParticleSystem whiteTrail;
    public ParticleSystem blueTrail;
    public ParticleSystem swordParticle;
    public ParticleSystem hitParticle;
    public Volume postVolume;
    public bool enableLensDistortion = true;
    public bool enableMotionBlur;

    public Image aim;
    public Image aimLock;

    private ThirdPersonController controller;
    private Animator anim;
    private CinemachineImpulseSource impulseSource;

    private LensDistortion lensDistortion;
    private MotionBlur motionBlur;
    private float lensDistortionDur = 1f;
    private float warpDuration = 0.3f;
    private SkinnedMeshRenderer[] skinMeshList;
    private Vector3 swordOrigPos;
    private Quaternion swordOrigRot;
    private MeshRenderer swordMesh;
    private bool isLocked;
    private Vector3 targetOffset=Vector3.up;
    private float UIChangeDur = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        isLocked = false;
        controller = GetComponent<ThirdPersonController>();
        anim = GetComponent<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        
        postVolume.profile.TryGet<LensDistortion>(out lensDistortion);
        postVolume.profile.TryGet<MotionBlur>(out motionBlur);

        skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
        swordOrigPos = sword.localPosition;
        swordOrigRot = sword.localRotation;
        swordMesh = sword.GetComponentInChildren<MeshRenderer>();
        swordMesh.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        SetAimUI();
        if (!controller.canMove)
            return;        

        if (screenTargets.Count < 1)
            return;

        if (!isLocked)
            target = screenTargets[GetTargetIndex()];

        if (Input.GetMouseButtonDown(1))
        {
            isLocked = true;
            SetLockUI(true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            isLocked = false;
            SetLockUI(false);
        }

        if (Input.GetMouseButtonDown(0) && isLocked)
        {
            controller.canMove = false;
            transform.LookAt(target);
            swordParticle.Play();
            swordMesh.enabled = true;
            anim.SetTrigger("Slash");
        }
    }

    int GetTargetIndex()
    {
        float[] distances = new float[screenTargets.Count];
        distances[0] = Vector2.Distance(Camera.main.WorldToScreenPoint(screenTargets[0].position), new Vector2(Screen.width / 2, Screen.height / 2));
        float minDistance = distances[0];
        int index = 0;

        for (int i = 1; i < screenTargets.Count; i++)
        {
            distances[i] = Vector2.Distance(Camera.main.WorldToScreenPoint(screenTargets[i].position), new Vector2(Screen.width / 2, Screen.height / 2));
            if (distances[i]<minDistance)
            {
                minDistance = distances[i];
                index = i;
            }
        }
        return index;
    }

    void SetAimUI()
    {
        aim.transform.position = Camera.main.WorldToScreenPoint(target.position+ targetOffset);
    }

    void SetLockUI(bool b)
    {
        float scale = b ? 1 : 2;
        float fade = b ? 1 : 0;
        aimLock.DOFade(fade, UIChangeDur);
        aimLock.transform.DOScale(scale, UIChangeDur).SetEase(Ease.OutBack);
        aimLock.transform.DORotate(Vector3.forward * 180f, UIChangeDur, RotateMode.LocalAxisAdd);
        aim.transform.DORotate(Vector3.forward * 90f, UIChangeDur, RotateMode.LocalAxisAdd);
    }

    public void WarpStrike()
    {
        GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
        Destroy(clone.GetComponent<Animator>());
        Destroy(clone.GetComponent<WarpController>().sword.gameObject);
        Destroy(clone.GetComponent<ThirdPersonController>());
        Destroy(clone.GetComponent<WarpController>());
        Destroy(clone.GetComponent<CharacterController>());

        SkinnedMeshRenderer[] cloneSkinMeshList = clone.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in cloneSkinMeshList)
        {
            smr.material = glowMaterial;
            smr.material.DOFloat(1.2f, "AlphaThreshold", 4f).OnComplete(() => Destroy(clone));
        }

        ShowBody(false);
        anim.speed = 0f;
        transform.DOMove(target.position, warpDuration).SetEase(Ease.InExpo).OnComplete(()=>FinishWarpStrike());
        
        sword.parent = null;
        sword.DOMove(target.position+ targetOffset, warpDuration / 1.2f);

        whiteTrail.Play();
        blueTrail.Play();

        //Motion Blur
        if (enableMotionBlur)
        {
            SetMotionBlur(1f);
        }

        //Lens Distortion
        if (enableLensDistortion)
        {
            DOVirtual.Float(0f, -0.5f, lensDistortionDur, SetLensDitortionIntensity);
            DOVirtual.Float(1f, 1.5f, lensDistortionDur, SetLensDitortionScale);
        }

    }

    void FinishWarpStrike()
    {
        ShowBody(true);

        sword.parent = hand;
        sword.localPosition = swordOrigPos;
        sword.localRotation = swordOrigRot;

        target.GetComponentInParent<Animator>().SetTrigger("BeHit");
        target.parent.DOMove(target.position + transform.forward, .5f);

        anim.speed = 1f;
        StartCoroutine(HideSword());
        StartCoroutine(StopParticles());
        hitParticle.Play();
        //Character glow effect
        DOVirtual.Float(20f, 0f, 0.5f, SetFresnelAmount);

        isLocked = false;
        SetLockUI(false);

        //Camera shake
        impulseSource.GenerateImpulse(Camera.main.transform.forward);

        //Disable Motion Blur
        if (enableMotionBlur)
        {
            SetMotionBlur(0f);
        }

        //Lens Distortion
        if (enableLensDistortion)
        {
            DOVirtual.Float(-0.5f, 0f, lensDistortionDur, SetLensDitortionIntensity);
            DOVirtual.Float(1.5f, 1f, lensDistortionDur, SetLensDitortionScale);
        }

    }

    void ShowBody(bool isMeshEnabled)
    {
        
        foreach (SkinnedMeshRenderer smr in skinMeshList)
        {
            smr.enabled = isMeshEnabled;
        }
    }

    void SetFresnelAmount(float x)
    {
        foreach (SkinnedMeshRenderer smr in skinMeshList)
        {
            smr.material.SetVector("FresnelAmount", new Vector4(x, x, x, x));
        }
    }

    IEnumerator HideSword()
    {
        yield return new WaitForSeconds(0.8f);
        swordParticle.Play();
        controller.canMove = true;

        GameObject swordClone = Instantiate(sword.gameObject, sword.position, sword.rotation);

        swordMesh.enabled = false;

        MeshRenderer swordCloneMesh = swordClone.GetComponentInChildren<MeshRenderer>();
      
        Material[] m = swordCloneMesh.materials;

        for (int i = 0; i < m.Length; i++)
        {
            m[i] = glowMaterial;
        }

        swordCloneMesh.materials = m;

        for (int i = 0; i < swordCloneMesh.materials.Length; i++)
        {
            swordCloneMesh.materials[i].DOFloat(1.2f, "AlphaThreshold", .4f).OnComplete(() => Destroy(swordClone));
        }
    }

    IEnumerator StopParticles()
    {
        yield return new WaitForSeconds(0.2f);
        whiteTrail.Stop();
        blueTrail.Stop();
    }

    void SetLensDitortionIntensity(float v)
    {
        lensDistortion.intensity.value = v;
    }

    void SetLensDitortionScale(float v)
    {
        lensDistortion.scale.value = v;
    }

    void SetMotionBlur(float v)
    {
        motionBlur.intensity.value = v;
    }
}
