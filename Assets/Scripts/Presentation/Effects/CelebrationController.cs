using System.Collections;
using UnityEngine;

public class CelebrationController : MonoBehaviour
{
    [Header("Celebration Effects")]
    [SerializeField] private ParticleSystem[] celebrationParticles;
    [SerializeField] private float victoryDuration = 4f;

    private Coroutine celebrationCoroutine;

    private void Awake()
    {
        foreach (var particle in celebrationParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void PlayCelebration(bool isWin)
    {
        if (celebrationCoroutine != null)
        {
            StopCoroutine(celebrationCoroutine);
        }
        celebrationCoroutine = StartCoroutine(PlayCelebrationSequence(isWin));
    }

    private IEnumerator PlayCelebrationSequence(bool isWin)
    {
        foreach (var particles in celebrationParticles)
        {
            particles.Play();
        }
        
        yield return new WaitForSeconds(victoryDuration);
        
        foreach (var particles in celebrationParticles)
        {
            particles.Stop();
        }
        
        if (isWin)
        {
            GameStateController.Instance.ReturnToMainMenu();
        }
    }

    public void StopCelebration()
    {
        if (celebrationCoroutine != null)
        {
            StopCoroutine(celebrationCoroutine);
            celebrationCoroutine = null;
        }
        
        foreach (var particles in celebrationParticles)
        {
            particles.Stop();
        }
    }
}

