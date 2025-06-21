using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneAI : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float collectionTime = 2f;
    public float avoidanceStrength = 10f;
    public bool showPath = false;

    [HideInInspector] public ResourceDrone.Faction faction;
    [HideInInspector] public GameObject targetResource;

    private ResourceDrone simulation;
    private LineRenderer pathRenderer;
    private DroneState state = DroneState.Searching;
    private float collectionTimer;
    private Vector3[] pathPoints = new Vector3[2]; 
    private bool hasResource = false;


    private enum DroneState
    {
        Searching,
        MovingToResource,
        Collecting,
        ReturningToBase,
        Delivering
    }

    public void Initialize(ResourceDrone sim, ResourceDrone.Faction fact)
    {
        simulation = sim;
        faction = fact;

        // Setup path visualization
        pathRenderer = gameObject.AddComponent<LineRenderer>();
        pathRenderer.startWidth = 0.1f;
        pathRenderer.endWidth = 0.1f;
        pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        pathRenderer.startColor = faction.color;
        pathRenderer.endColor = faction.color;
        pathRenderer.positionCount = 0;
    }

    void Update()
    {
        switch (state)
        {
            case DroneState.Searching:
                targetResource = simulation.FindNearestResource(transform.position, faction);
                if (targetResource != null)
                {
                    state = DroneState.MovingToResource;
                }
                break;

            case DroneState.MovingToResource:
                {
                    if (targetResource == null)
                    {
                        state = DroneState.Searching;
                        break;
                    }

                    Vector3 targetPos = targetResource.transform.position;
                    Vector3 toTarget = (targetPos - transform.position).normalized;

                    Vector3 avoidance = simulation.GetAvoidanceForce(transform.position, gameObject);
                    float avoidanceWeight = 5f; // усилие отталкивания, можно вынести в public

                    Vector3 desiredDir = (toTarget + avoidance * avoidanceWeight).normalized;

                    if (desiredDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * 5f);
                    }

                    transform.position += desiredDir * moveSpeed * Time.deltaTime;


                    if (showPath)
                    {
                        pathPoints[0] = transform.position;
                        pathPoints[1] = targetPos;
                        pathRenderer.positionCount = 2;
                        pathRenderer.SetPositions(pathPoints);
                    }
                    else
                    {
                        pathRenderer.positionCount = 0;
                    }

                    if (Vector3.Distance(transform.position, targetPos) < 0.5f)
                    {
                        state = DroneState.Collecting;
                        collectionTimer = collectionTime;
                    }
                    break;
                }
            case DroneState.Collecting:
                collectionTimer -= Time.deltaTime;
                if (collectionTimer <= 0)
                {
                    // Убираем ресурс со сцены
                    if (targetResource != null)
                    {
                        targetResource.SetActive(false); // Можно и Destroy, если хочешь
                        hasResource = true;
                    }
                    if (targetResource != null)
                    {
                        targetResource.SetActive(false);
                        hasResource = true;

                        // Создаём эффект сбора ресурса
                        StartCoroutine(CollectionEffect(targetResource.transform.position));
                    }

                    state = DroneState.ReturningToBase;
                }

                break;

            case DroneState.ReturningToBase:
                {
                    Vector3 basePos = faction.baseTransform.position;

                    Vector3 toTarget = (basePos - transform.position).normalized;

                    Vector3 avoidance = simulation.GetAvoidanceForce(transform.position, gameObject);
                    float avoidanceWeight = 5f; // усилие отталкивания, можно вынести в public

                    Vector3 desiredDir = (toTarget + avoidance * avoidanceWeight).normalized;

                    if (desiredDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * 5f);
                    }

                    transform.position += desiredDir * moveSpeed * Time.deltaTime;


                    if (showPath)
                    {
                        pathPoints[0] = transform.position;
                        pathPoints[1] = basePos;
                        pathRenderer.positionCount = 2;
                        pathRenderer.SetPositions(pathPoints);
                    }

                    if (Vector3.Distance(transform.position, basePos) < 1f)
                    {
                        state = DroneState.Delivering;
                    }
                    break;
                }
            case DroneState.Delivering:
                if (hasResource && targetResource != null)
                {
                    simulation.ResourceCollected(targetResource, faction);
                    targetResource = null;
                    hasResource = false;
                }
                // Независимо от условий — возвращаемся к поиску
                state = DroneState.Searching;
                break;

        }

        IEnumerator CollectionEffect(Vector3 position)
        {
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.zero;

            var renderer = effect.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                effect.transform.localScale = Vector3.one * Mathf.Lerp(0, 1f, t);
                yield return null;
            }

            Destroy(effect);
        }

    }
}