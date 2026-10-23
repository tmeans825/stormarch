using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class componentParticleBehavior : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem subEmitter;


    private void Start()
    {
        ps = GetComponent<ParticleSystem>();

        //subEmitter = transform.GetChild(0).GetComponent<ParticleSystem>();

        if (ps.trigger.GetCollider(0) == null)
            ps.trigger.SetCollider(0, PlayerController.instance.GetComponent<BoxCollider2D>());

        //var main = subEmitter.main;
        //main.customSimulationSpace = PlayerController.instance.transform;

        ps.Stop();
    }

    private void OnParticleTrigger()
    {
        List<ParticleSystem.Particle> particles = new List<ParticleSystem.Particle>();
        int triggeredParticles = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);

        for(int i = 0; i < triggeredParticles; i++)
        {
            ParticleSystem.Particle p = particles[i];
            p.remainingLifetime = 0;
            particles[i] = p;

            // TO DO - add tech to player inventory
            PlayerController.instance.updateCurrencies(0, 1);
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);
    }
}
