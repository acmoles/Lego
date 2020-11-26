using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class InteractionEventReceiver : MonoBehaviour
{
	public InteractionEventSender sender;
	public MeshRenderer meshRenderer;

	protected float startOpacity;
	public float targetOpacity = 0.8f;

	void Start()
	{
		startOpacity = meshRenderer.material.color.a;
	}

	void OnEnable()
	{
		sender.OnHoverBegin += OnHoverBegin;
		sender.OnHoverEnd += OnHoverEnd;
		sender.OnPinchBegin += OnPinchBegin;
		sender.OnPinchEnd += OnPinchEnd;
		sender.OnTouch += OnTouch;
		sender.OnRelease += OnRelease;
	}

	private void OnDisable()
	{
		sender.OnHoverBegin -= OnHoverBegin;
		sender.OnHoverEnd -= OnHoverEnd;
		sender.OnPinchBegin -= OnPinchBegin;
		sender.OnPinchEnd -= OnPinchEnd;
		sender.OnTouch -= OnTouch;
		sender.OnRelease -= OnRelease;
	}

	protected virtual void OnHoverBegin() {}

	protected virtual void OnHoverEnd() {}

	protected virtual void OnPinchBegin() {}

	protected virtual void OnPinchEnd() {}

	protected virtual void OnTouch() { }

	protected virtual void OnRelease() {}


	protected void SetOpacity(float to, float t = 0.15f)
	{
		Color c = Color.white;
		c.a = to;

		if (meshRenderer.material.HasProperty("_Color"))
		{
			meshRenderer.material.DOColor(c, t);
		}
		else
		{
			meshRenderer.material.SetColor("_TintColor", c);
		}
	}
}
