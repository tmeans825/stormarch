using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Use: Library to house common/shared functions
public static class TEMLib
{
    /// <summary>
    /// Sets an Image's color alpha to the given value. Returns the same Image for chaining.
    /// </summary>
    public static Image ChangeAlpha(Image g, float newAlpha)
    {
        var color = g.color;
        color.a = newAlpha;
        g.color = color;
        return g;
    }


    /// <summary>
    /// To be used to achieve a smooth lerp. Use in the following context:
    /// time = 0.0f;
    /// while (time < duration)
    ///    {
    ///        float t = TEMLib.getSmoothLerpT(time, duration);
    ///        transform.position = Vector3.Lerp(startPosition, endPosition, t);
    ///        time += Time.deltaTime;
    ///        yield return null;
    ///    }
    /// </summary>
    /// <param name="time">Value that should start at 0f and be slowly incremented in a coroutine loop</param>
    /// <param name="duration">Total duration of lerp in seconds (determines speed)</param>
    /// <returns></returns>
    public static float getSmoothLerpT(float time, float duration)
    {
        float t = time / duration;
        t = t * t * (3f - 2f * t);

        return t;
    }

    /// <summary>
    /// A 2D implementation of the 3D version of AddExplosionForce - from https://stackoverflow.com/questions/34250868/unity-addexplosionforce-to-2d
    /// </summary>
    /// <param name="rb"></param>
    /// <param name="explosionForce"></param>
    /// <param name="explosionPosition"></param>
    /// <param name="explosionRadius"></param>
    /// <param name="upwardsModifier"></param>
    /// <param name="mode"></param>
    public static void AddExplosionForce(this Rigidbody2D rb, float explosionForce, Vector2 explosionPosition, float explosionRadius, float upwardsModifier = 0.0F, ForceMode2D mode = ForceMode2D.Force)
    {
        var explosionDir = rb.position - explosionPosition;
        var explosionDistance = (explosionDir.magnitude / explosionRadius);

        // Normalize without computing magnitude again
        if (upwardsModifier == 0)
        {
            explosionDir /= explosionDistance;
        }
        else
        {
            // If you pass a non-zero value for the upwardsModifier parameter, the direction
            // will be modified by subtracting that value from the Y component of the centre point.
            explosionDir.y += upwardsModifier;
            explosionDir.Normalize();
        }

        rb.AddForce(Mathf.Lerp(0, explosionForce, (1 - explosionDistance)) * explosionDir, mode);
    }

    /// <summary>
    /// Get the direction given a vector representing the difference between two points.
    /// </summary>
    /// <param name="otherPos">Vector that represents the difference between the two positions.</param>
    /// <returns>0 if up, 1 if right, 2 if down, 3 if left </returns>
    public static int getDirection(Vector3 difference)
    {
        float angle = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;

        if (angle < 0.0f)
            angle += 360.0f;

        // use angle to determine which anim to play
        if (angle >= 45 && angle < 135) //up
            return 0;
        else if (angle >= 135 && angle < 225) //left
            return 3;
        else if (angle >= 225 && angle < 315) //down
            return 2;
        else //right
            return 1;
    }

    /// <summary>
    /// Get the direction between you and an object.
    /// </summary>
    /// <param name="yourPos">Transform.position of the caller</param>
    /// <param name="otherPos">Transform.position of the target</param>
    /// <returns>0 if up, 1 if right, 2 if down, 3 if left </returns>
    public static int getDirection(Vector3 yourPos, Vector3 otherPos)
    {
        return (getDirection(otherPos- yourPos));
    }

    public static Vector2 getRelativePos(Vector3 originPoint, Vector3 targetPoint)
    {
        Vector2 relativePosition = targetPoint - originPoint;
        return relativePosition;
    }

    /// <summary>
    /// Checks if two Vector2 directions represent the same cardinal direction.
    /// </summary>
    /// <param name="currentDirection"></param>
    /// <param name="previousDirection"></param>
    /// <returns></returns>
    public static bool areDirectionsSameCardinal(Vector2 currentDirection, Vector2 previousDirection)
    {
        // Get the cardinal directions for each vector
        Vector2 currentCardinal = GetCardinalDirection(currentDirection);
        Vector2 previousCardinal = GetCardinalDirection(previousDirection);

        // Compare cardinal directions
        return currentCardinal == previousCardinal;
    }

    private static Vector2 GetCardinalDirection(Vector2 direction)
    {
        // Normalize the vector to focus on its direction
        direction = direction.normalized;

        // Determine the cardinal direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return direction.y > 0 ? Vector2.up : Vector2.down;
        }
    }

}
