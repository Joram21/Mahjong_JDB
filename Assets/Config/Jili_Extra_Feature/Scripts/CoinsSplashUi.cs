using UnityEngine;
using System.Collections.Generic;

public class CoinsSplashUi : MonoBehaviour
{
    public float AliveTime = 0.4f;
    public float TheDist = 1000;
    public float RotateSpeed = 20;
    public GameObject CoinPref;
    public int Count = 20;
    public List<GameObject> Spawned = new List<GameObject>();
    public Vector2[] TargetPos;
    float[] delaytime;
    public float Speed = 50;
    float tempspeed = 0;
    public AnimationCurve thecurve;
    public float StartOffset = 5;
    public bool StartSpawn;
    public bool OnCircle = true;
    public Transform TheParent;
    float spawntimestamp;
    private void OnEnable()
    {
        Clear();
        if (StartSpawn)
        {
            Spawn();
        }
    }
    private void Update()
    {
        tempspeed += Speed*Time.deltaTime;
        for (int i = 0; i < Spawned.Count; i++)
        {

            if (Spawned[i]&& delaytime[i] < Time.time)
            {
                float thescale = thecurve.Evaluate(1 - (Vector2.Distance(Spawned[i].transform.localPosition, TargetPos[i]) / TheDist));

                Spawned[i].transform.localPosition = Vector2.Lerp(Spawned[i].transform.localPosition, TargetPos[i], tempspeed * Time.deltaTime);
                Spawned[i].transform.localScale = Vector3.one * thescale;
                Spawned[i].transform.Rotate(new Vector3(0, 0, 1) * RotateSpeed * Time.deltaTime);
            }
        }
    }
    void Clear()
    {
        for (int i = 0; i < Spawned.Count; i++)
        {
            if (Spawned[i])
            {
              //  Destroy(Spawned[i]);
            }
        }
        Spawned.Clear();
    }
    [ContextMenu("Spawn")]
    public void Spawn()
    {
        Clear();
        tempspeed = 0;
    
        TargetPos = new Vector2[Count];
        delaytime = new float[Count];
        for(int i = 0; i < Count; i++)
        {
            delaytime[i] = Time.time + Random.Range(0, 0.2f);
            GameObject go = Instantiate(CoinPref, Parent());
            Spawned.Add(go);
            if (OnCircle)
            {
                TargetPos[i] = Random.insideUnitCircle.normalized * TheDist;
            }
            else
            {
                TargetPos[i] = Random.insideUnitCircle * TheDist;

            }
            go.transform.localPosition = TargetPos[i]/StartOffset;
            Destroy(go, AliveTime);
;        }
       
    }
    public Transform Parent()
    {
        if (TheParent)
        {
            return TheParent;
        }
        return transform;
    }
}
