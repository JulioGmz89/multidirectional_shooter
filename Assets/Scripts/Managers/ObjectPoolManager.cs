using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An interface for objects that can be managed by the ObjectPoolManager.
/// </summary>
public interface IPooledObject
{
    string PoolTag { get; set; }
    void OnObjectSpawn();
}

/// <summary>
/// Manages pools of reusable game objects to optimize performance by avoiding frequent instantiation and destruction.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    // Singleton instance
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [Header("Pools")]
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the pool dictionary
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    private void Start()
    {
        // Start can be empty now or used for other purposes
    }

    /// <summary>
    /// Spawns an object from the pool.
    /// </summary>
    /// <param name="tag">The tag of the pool to spawn from.</param>
    /// <param name="position">The position to spawn the object at.</param>
    /// <param name="rotation">The rotation to spawn the object with.</param>
    /// <returns>The spawned GameObject.</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        Queue<GameObject> poolQueue = poolDictionary[tag];

        if (poolQueue.Count == 0)
        {
            // Optionally, grow the pool if it's empty.
            // For now, we'll just log a warning.
            Debug.LogWarning($"Pool with tag {tag} is empty. Consider increasing its size.");
            // Find the pool settings to instantiate a new object
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null)
            {
                GameObject newObj = Instantiate(pool.prefab);
                poolQueue.Enqueue(newObj); // Add to the queue for future reuse
            }
            else
            {
                return null; // Should not happen if tag exists
            }
        }

        GameObject objectToSpawn = poolQueue.Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.PoolTag = tag; // Assign the tag before spawning
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Returns an object to its pool.
    /// </summary>
    /// <param name="tag">The tag of the pool the object belongs to.</param>
    /// <param name="objectToReturn">The GameObject to return.</param>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            Destroy(objectToReturn); // Destroy if it doesn't belong to any pool
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}
