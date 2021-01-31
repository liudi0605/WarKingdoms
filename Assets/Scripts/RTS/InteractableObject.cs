﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class InteractableObject : MonoBehaviour
{
    protected static int layerObjectsVisible = -1;
    protected static int layerObjectsHidden = -1;
    protected static int layerMiniMapVisible = -1;
    protected static int layerMiniMapHidden = -1;

    public bool visible;
    public float visionFadeTime = 1f;

    protected Transform modelHolder;
    private Transform modelHolder2;
    protected MeshRenderer selectionCircle;
    private Animator anim;
    private NavMeshObstacle navObstacle;
    public int health;

    protected readonly float decayIntoGroundDistance = 7f;


    public float sizeRadius
    {
        get
        {
            return selectionCircle.transform.localScale.x * 0.5f;
        }
    }

    protected virtual void Awake()
    {
        modelHolder = transform.Find("Model");
        modelHolder2 = transform.Find("Model2");
        selectionCircle = transform.Find("SelectionCircle").GetComponent<MeshRenderer>();
        anim = GetComponentInChildren<Animator>();
        navObstacle = GetComponent<NavMeshObstacle>();

        SetLayers();
    }

    private void Start()
    {
        SetVisibility(true, true);
    }

    protected static void SetLayers()
    {
        if (layerObjectsVisible == -1)
        {
            layerObjectsVisible = LayerMask.NameToLayer("Units");
            layerObjectsHidden = LayerMask.NameToLayer("Units Hidden");
            layerMiniMapVisible = LayerMask.NameToLayer("MiniMap Only");
            layerMiniMapHidden = LayerMask.NameToLayer("MiniMap Hidden");
        }
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (selectionCircle == null)
        {
            selectionCircle = transform.Find("SelectionCircle")?.GetComponent<MeshRenderer>();
        }
        if (selectionCircle != null)
        {
            UnityEditor.Handles.color = !IsDeadOrNull(this) && health > 0 ? Color.blue : Color.red;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, sizeRadius);
        }
    }
#endif

    public static bool IsDeadOrNull(InteractableObject unit)
    {
        if (unit is ClickableObject)
        {
            return ClickableObject.IsDeadOrNull(unit as ClickableObject);
        }
        else
        {
            return unit == null || unit.health == 0;
        }
    }

    public virtual void SetVisibility(bool visibility, bool force = false)
    {
        if (!force && visibility == visible)
        {
            return;
        }

        visible = visibility;

        GameObject[] parts = GetComponentsInChildren<Transform>().Where(form =>
            form.gameObject.layer == layerObjectsVisible ||
            form.gameObject.layer == layerObjectsHidden ||
            form.gameObject.layer == layerMiniMapVisible ||
            form.gameObject.layer == layerMiniMapHidden
        ).Select(form => form.gameObject).ToArray();

        foreach (GameObject part in parts)
        {
            if (part.layer == layerObjectsVisible || part.layer == layerObjectsHidden)
            {
                if (visibility)
                {
                    part.layer = layerObjectsVisible;
                }
                else
                {
                    part.layer = layerObjectsHidden;
                }
            }
            else
            {
                if (visibility)
                {
                    part.layer = layerMiniMapVisible;
                }
                else
                {
                    part.layer = layerMiniMapHidden;
                }
            }
        }
    }

    public virtual void SufferAttack(int damage)
    {
        if (health <= 0)
        {
            return;
        }

        health -= damage;

        if (health <= 0)
        {
            Die();
        }
        else
        {
            anim?.SetBool("DoHit", true);
        }
    }

    protected virtual void Die()
    {
        health = 0;

        anim?.SetBool("DoDeath", true);

        navObstacle.enabled = false;

        //To avoid the object participating in any Raycast or tag search
        //gameObject.tag = "Untagged";
        gameObject.layer = 0;

        StartCoroutine(DecayIntoGround());

        //Remove unneeded Components
        GetComponent<Collider>().enabled = false;
    }

    protected IEnumerator DecayIntoGround()
    {
        yield return Yielders.Get(5f);
        float startY = transform.position.y;
        float depth = decayIntoGroundDistance;
        while (modelHolder.position.y > startY - depth)
        {
            modelHolder.position += Vector3.down * Time.deltaTime * 0.1f;
            yield return null;
        }
        Destroy(gameObject);
    }
}