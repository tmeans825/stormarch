using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBehavior : MonoBehaviour
{
	public bool infiniteScroll = false;

	private Vector3 StartPos;
	private Vector3 originalScrollPos;

	public float moveModifier;
	public float verticalRestriction = 1;
	public float infiniteScrollSpeed = 0.0f;
	public float scrollBoundary = 0.0f;

	// Use this for initialization
	void Start()
	{
		StartPos = transform.position;
		if (infiniteScroll)
			originalScrollPos = transform.position;
	}

	// Update is called once per frame
	void Update()
	{
		//Debug.Log(Camera.main.ScreenToWorldPoint(Input.mousePosition));
		Vector3 pz = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		pz.z = 0;
		pz.x = Mathf.Clamp(pz.x, -1.0f, 1.0f);
		pz.y = Mathf.Clamp(pz.y, -0.6f, 0.6f);
		//gameObject.transform.position = pz;
		//Debug.Log("Mouse Position: " + pz);

		if(infiniteScroll)
        {
			StartPos += new Vector3(infiniteScrollSpeed * Time.deltaTime, 0.0f, 0.0f);

			if (StartPos.x <= scrollBoundary)
				StartPos = originalScrollPos;
        }

		float xPos = StartPos.x + (pz.x * moveModifier);
		float yPos = StartPos.y + (pz.y * (moveModifier / verticalRestriction));

		transform.position = new Vector3(xPos, yPos, 0);
		//transform.position = new Vector3(StartPos.x + (pz.x * moveModifier), 0, 0);
		//move based on the starting position and its modified value.
	}

}
