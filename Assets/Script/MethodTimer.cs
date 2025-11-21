using UnityEngine;

public class MethodTimer : MonoBehaviour
{
    private float startTime;
    private bool timerRunning = false;
    
    public void StartTimer()
    {
        startTime = Time.time;
        timerRunning = true;
    }
    
    public float StopTimer()
    {
        if (!timerRunning)
        {
            return 0f;
        }
        
        float elapsedTime = Time.time - startTime;
        timerRunning = false;
        
        return elapsedTime;
    }
    
    public bool IsTimerRunning()
    {
        return timerRunning;
    }
}