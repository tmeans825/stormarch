using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playSoundOnBurst : MonoBehaviour
{
    public string[] soundArray;
    private ParticleSystem ps;
    private AudioManager am;
    private int currentParticleCount = 0;

    void Start()
    {
        ps = this.GetComponent<ParticleSystem>();
        am = this.GetComponent<AudioManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(currentParticleCount < ps.particleCount)
        {
            // PLAY SOUND
            int index = Random.Range(0, soundArray.Length);
            am.PlaySound(soundArray[index]);
            currentParticleCount = ps.particleCount;
        }
        else if(currentParticleCount > ps.particleCount)
        {
            currentParticleCount = ps.particleCount; 
        }
    }
}
