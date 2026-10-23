using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class FireBombExplosionVFX : MonoBehaviour
{
    public ParticleSystem randomFlamesPS;
    public Light2D glow;

    public void setValues(float radius)
    {
        var shape = randomFlamesPS.shape;
        shape.radius = radius;
        glow.pointLightOuterRadius = radius;
    }
}
