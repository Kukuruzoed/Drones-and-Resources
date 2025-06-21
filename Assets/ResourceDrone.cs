using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDrone : MonoBehaviour
{
    [System.Serializable]
    public class Faction
    {
        public string name;
        public Color color;
        public Transform baseTransform;
        public int resourcesCollected;
    }

    public GameObject dronePrefab;
    public GameObject resourcePrefab;
    public Faction[] factions;
    public float spawnRadius = 5f;
    public float avoidanceRadius = 2f;

    [Header("UI Elements")]
    public Slider droneCountSlider;
    public Slider droneSpeedSlider;
    public InputField spawnRateInput;
    public Toggle showPathToggle;
    public TextMeshProUGUI[] factionScoreTexts;

    private List<GameObject> drones = new List<GameObject>();
    private List<GameObject> resources = new List<GameObject>();
    private float nextSpawnTime;
    private bool simulationRunning = false;

    void Start()
    {
        // Initialize UI
        droneCountSlider.onValueChanged.AddListener(UpdateDroneCount);
        droneSpeedSlider.onValueChanged.AddListener(UpdateDroneSpeed);
        spawnRateInput.onEndEdit.AddListener(UpdateSpawnRate);
        showPathToggle.onValueChanged.AddListener(TogglePathVisibility);

        // Start simulation
        StartSimulation();
    }

    void Update()
    {
        if (simulationRunning && Time.time >= nextSpawnTime)
        {
            SpawnResource();
            UpdateSpawnTimer();
        }
    }

    public void StartSimulation()
    {
        ClearSimulation();
        CreateDrones();
        UpdateSpawnTimer();
        simulationRunning = true;
    }

    void ClearSimulation()
    {
        foreach (var drone in drones)
        {
            Destroy(drone);
        }
        drones.Clear();

        foreach (var resource in resources)
        {
            Destroy(resource);
        }
        resources.Clear();

        foreach (var faction in factions)
        {
            faction.resourcesCollected = 0;
        }
        UpdateScoreUI();
    }

    void CreateDrones()
    {
        int dronesPerFaction = (int)droneCountSlider.value;

        for (int i = 0; i < factions.Length; i++)
        {
            for (int j = 0; j < dronesPerFaction; j++)
            {
                Vector3 spawnPos = factions[i].baseTransform.position + Random.insideUnitSphere * spawnRadius;
                spawnPos.y = 1;
                GameObject drone = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
                drone.GetComponent<Renderer>().material.color = factions[i].color;

                DroneAI droneAI = drone.GetComponent<DroneAI>();
                droneAI.Initialize(this, factions[i]);
                droneAI.moveSpeed = droneSpeedSlider.value;
                droneAI.showPath = showPathToggle.isOn;

                drones.Add(drone);
            }
        }
    }

    void SpawnResource()
    {
        Vector3 spawnPos = Random.insideUnitSphere * spawnRadius * 2;
        spawnPos.y = 1;
        GameObject resource = Instantiate(resourcePrefab, spawnPos, Quaternion.identity);
        resources.Add(resource);
    }

    void UpdateSpawnTimer()
    {
        float spawnRate;
        if (float.TryParse(spawnRateInput.text, out spawnRate) && spawnRate > 0)
        {
            nextSpawnTime = Time.time + 1f / spawnRate;
        }
        else
        {
            nextSpawnTime = Time.time + 1f; // default 1 per second
        }
    }

    public void ResourceCollected(GameObject resource, Faction faction)
    {
        if (resources.Contains(resource))
        {
            resources.Remove(resource);
            Destroy(resource);

            faction.resourcesCollected++;
            UpdateScoreUI();

            // Visual effect
            StartCoroutine(ResourceDeliveryEffect(faction.baseTransform.position));
        }
    }

    IEnumerator ResourceDeliveryEffect(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.zero;
        effect.GetComponent<Renderer>().material.color = Color.yellow;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            effect.transform.localScale = Vector3.one * Mathf.Lerp(0, 1, t);
            yield return null;
        }

        Destroy(effect);
    }

    public GameObject FindNearestResource(Vector3 position, Faction excludingFaction)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (var resource in resources)
        {
            // Check if another drone is already targeting this resource
            bool resourceTaken = false;
            foreach (var drone in drones)
            {
                DroneAI ai = drone.GetComponent<DroneAI>();
                if (ai != null && ai.targetResource == resource)
                {
                    resourceTaken = true;
                    break;
                }
            }

            if (!resourceTaken)
            {
                float dist = Vector3.Distance(position, resource.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = resource;
                }
            }
        }

        return nearest;
    }

    public Vector3 GetAvoidanceOffset(Vector3 position, GameObject currentDrone)
    {
        Vector3 offset = Vector3.zero;
        int count = 0;

        foreach (var drone in drones)
        {
            if (drone == null || drone == currentDrone) continue;

            float dist = Vector3.Distance(position, drone.transform.position);
            if (dist < avoidanceRadius)
            {
                // „ем ближе дрон Ч тем сильнее отталкиваем
                float strength = (avoidanceRadius - dist) / avoidanceRadius; // от 0 до 1
                offset += (position - drone.transform.position).normalized * strength;
                count++;
            }
        }

        if (count > 0)
        {
            offset /= count;
        }

        return offset;
    }



    void UpdateDroneSpeed(float value)
    {
        foreach (var drone in drones)
        {
            DroneAI ai = drone.GetComponent<DroneAI>();
            if (ai != null)
            {
                ai.moveSpeed = value;
            }
        }
    }

    void UpdateDroneCount(float value)
    {
        StartSimulation();
    }

    void UpdateSpawnRate(string value)
    {
        UpdateSpawnTimer();
    }

    void TogglePathVisibility(bool show)
    {
        foreach (var drone in drones)
        {
            DroneAI ai = drone.GetComponent<DroneAI>();
            if (ai != null)
            {
                ai.showPath = show;
            }
        }
    }

    void UpdateScoreUI()
    {
        for (int i = 0; i < factions.Length; i++)
        {
            factionScoreTexts[i].text = $"{factions[i].name}: {factions[i].resourcesCollected}";
            factionScoreTexts[i].color = factions[i].color;
        }
    }
}