using UnityEngine;

public class ComponentDropPickup : MonoBehaviour
{
    [SerializeField] private ParticleSystem ambientParticles;
    [SerializeField] private ParticleSystem childBurst;
    [SerializeField] private GameObject lightObject;
    [SerializeField] private Collider2D triggerCollider;

    private bool consumed = false;

    void Start()
    {
        GameEvents.current.onPreviousSceneDataLoaded += setParticleEmissionRate;
        if (triggerCollider == null) triggerCollider = GetComponentInChildren<Collider2D>(true);
    }

    private void setParticleEmissionRate()
    {
        var emission = ambientParticles.emission;
        emission.rateOverTime = PlayerController.instance.deathDropComponentCount * 0.33f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (consumed) return;
        if (!collision.CompareTag("Player")) return;

        int amount = PlayerController.instance.deathDropComponentCount;
        if (amount <= 0) { Destroy(gameObject); return; }
        consumed = true;

        PlayerController.instance.updateCurrencies(0, amount);

        // Detach the burst before destroying the parent so its 5s lifetime can
        // play out — same world-rooted-clone pattern CLAUDE.md mandates for
        // enemy death VFX.
        if (childBurst != null)
        {
            childBurst.transform.SetParent(null);
            childBurst.transform.position = collision.transform.position;
            childBurst.gameObject.SetActive(true);
            childBurst.Play();
            Destroy(childBurst.gameObject, 5f);
        }

        AudioMaster.instance.PlaySound("Autosave");

        if (triggerCollider != null) triggerCollider.enabled = false;
        if (lightObject != null) lightObject.SetActive(false);
        ambientParticles.Stop();

        // Cleared fields ride along on the next regular save (camp, transition,
        // next death). Quitting before that next save resurrects the drop —
        // accepted per design.
        PlayerController.instance.deathDropScene = "";
        PlayerController.instance.deathDropPos = Vector3.zero;
        PlayerController.instance.deathDropComponentCount = 0;

        // Root carries ambientParticles; delay destroy so existing particles fade.
        Destroy(gameObject, 5f);
    }

    void OnDestroy()
    {
        GameEvents.current.onPreviousSceneDataLoaded -= setParticleEmissionRate;
    }
}
