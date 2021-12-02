using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    public int defaultCount = 10;
    public int maxCount = 200;
    public AntBrain baseAnt;

    private ObjectPool<AntBrain> antPool;

    private void Start()
    {
        antPool = new ObjectPool<AntBrain>(() =>
        {
            return Instantiate(baseAnt);
        }, ant => {
            ant.gameObject.SetActive(true);
        }, ant => {
            ant.gameObject.SetActive(false);
        }, ant => {
            Destroy(ant.gameObject);
        }, false, defaultCount, maxCount);
    }

    public AntBrain AntSpawn()
    {
        AntBrain newAnt = antPool.Get();
        newAnt.antHealth.SetDeath(DeathToAnt);
        return newAnt;
    }

    public void DeathToAnt(CreatureHealth thisAnt)
    {
        antPool.Release(thisAnt.antController);
    }
}