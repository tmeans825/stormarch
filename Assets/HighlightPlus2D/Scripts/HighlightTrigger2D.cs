using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HighlightPlus2D
{


	public enum TriggerMode
	{
		ColliderEvents = 0,
		Raycast = 1
	}

	public enum RayCastSource
	{
		MousePosition = 0,
		CameraDirection = 1
	}

	public enum HighlightOnEvent
	{
		OnOverAndClick = 0,
		OnlyOnOver = 1,
		OnlyOnClick = 2
	}


	[RequireComponent(typeof(HighlightEffect2D))]
	[HelpURL("https://kronnect.com/support")]
	public class HighlightTrigger2D : MonoBehaviour
	{

		public HighlightOnEvent highlightEvent = HighlightOnEvent.OnOverAndClick;
		public float highlightDuration;
		[Tooltip("Used to trigger automatic highlighting including children objects.")]
		public TriggerMode triggerMode = TriggerMode.ColliderEvents;
		public Camera raycastCamera;
		public RayCastSource raycastSource = RayCastSource.MousePosition;

		Collider2D currentCollider;

		void OnEnable()
		{
			Init();
		}

		void Start()
		{
			Collider2D collider = GetComponent<Collider2D>();
			if (collider == null)
			{
				if (GetComponent<SpriteRenderer>() != null)
				{
					gameObject.AddComponent<BoxCollider2D>();
				}
			}
			if (triggerMode == TriggerMode.Raycast)
			{
				if (raycastCamera == null)
				{
					raycastCamera = HighlightManager2D.GetCamera();
					if (raycastCamera == null)
					{
						Debug.LogError("Highlight Trigger 2D on " + gameObject.name + ": no camera found!");
					}
				}
				StartCoroutine(DoRayCast());
			}
		}

		public void Init()
		{
			if (raycastCamera == null)
			{
				raycastCamera = HighlightManager2D.GetCamera();
			}
		}

		void OnMouseDown()
		{
			if (highlightEvent != HighlightOnEvent.OnlyOnClick && highlightEvent != HighlightOnEvent.OnOverAndClick)
				return;
			if (triggerMode == TriggerMode.ColliderEvents)
			{
				Highlight(true);
				if (highlightDuration > 0)
				{
					CancelInvoke();
					Invoke("CancelHighlight", highlightDuration);
				}
			}
		}

		public void manualOverrideMouseEnter() // TEM Added for pressure plates
        {
			OnMouseEnter();
        }

		public void manualOverrideMouseExit() // TEM added
        {
			OnMouseExit();
        }

		void OnMouseEnter()
		{
			if (highlightEvent != HighlightOnEvent.OnlyOnOver && highlightEvent != HighlightOnEvent.OnOverAndClick)
				return;
			if (triggerMode == TriggerMode.ColliderEvents)
			{
				if (!PlayerController.instance.frozen) // *TEM I added this line
				{
					ObjectSelector.current.PointerSelect(this.gameObject); // and this one
					gameObject.GetComponent<Interactable>().mousedOver(); // and this			
					Highlight(true);
					if (highlightDuration > 0)
					{
						CancelInvoke();
						Invoke("CancelHighlight", highlightDuration);
					}
				}
			}
		}

		void OnMouseExit()
		{
			if (highlightDuration > 0)
				return;
			if (triggerMode == TriggerMode.ColliderEvents)
			{
				ObjectSelector.current.PointerUnselect(); // *TEM I also added this line
				HUDUIBehavior.instance.closePopup(); // and this
				Highlight(false);
			}
		}

		void Highlight(bool state)
		{
			HighlightEffect2D hb = transform.GetComponent<HighlightEffect2D>();
			if (hb == null && state)
			{
				hb = gameObject.AddComponent<HighlightEffect2D>();
			}
			if (hb != null)
			{
				if (state && hb.outline == 1.0f)
					return;
				if (!state && hb.outline == 0.0f)
					return;
				//hb.SetHighlighted (state);
				float amt = 1.0f;
				if (!state)
					amt = 0.0f;
				hb.outline = amt;
				hb.Refresh();
			}
		}

		void CancelHighlight()
		{
			Highlight(false);
		}

		IEnumerator DoRayCast()
		{
			while (triggerMode == TriggerMode.Raycast)
			{
				if (raycastCamera == null)
				{
					raycastCamera = HighlightManager2D.GetCamera();
				}
				if (raycastCamera != null)
				{
					Ray ray;
					if (raycastSource == RayCastSource.MousePosition)
					{
						ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
					}
					else
					{
						ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
					}

					RaycastHit2D hitInfo2D = Physics2D.GetRayIntersection(ray); 


					Collider2D collider = hitInfo2D.collider;
					bool hit = false;
					if (collider != null && collider.gameObject == gameObject)
					{
						hit = true;
						if (collider != currentCollider)
						{
							SwitchCollider(collider);
						}
					}
					if (!hit && currentCollider != null)
					{
						SwitchCollider(null);
					}
				}
				yield return null;
			}
		}


		void SwitchCollider(Collider2D newCollider)
		{
			currentCollider = newCollider;
			if (currentCollider != null)
			{
				Highlight(true);
			}
			else
			{
				Highlight(false);
			}
		}


	}

}