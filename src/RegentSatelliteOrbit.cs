using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Satellite.src;

public partial class RegentSatelliteOrbit : Node2D
{
	private NCreature _owner = null!;

	private Control _hitbox = null!;

	private float _rotationSpeed = 1.7f;

	private float _time;

	private int _starCount;

	public void Initialize(NCreature owner, Control hitbox)
	{
		_owner = owner;
		_hitbox = hitbox;
		ZIndex = 200;
		QueueRedraw();
	}

	public void UpdateFromStars(int stars)
	{
		stars = Math.Max(stars, 0);
		if (_starCount == stars)
		{
			return;
		}
		_starCount = stars;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(_owner) || !IsInstanceValid(_hitbox))
		{
			QueueFree();
			return;
		}
		Visible = _starCount > 0;
		if (!Visible)
		{
			return;
		}
		GlobalPosition = _hitbox.GlobalPosition + _hitbox.Size * 0.5f;
		_time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_starCount <= 0)
		{
			return;
		}

		int count = Math.Min(_starCount, 10);
		float baseRadius = 60f;
		float orbitStep = 15f;
		float ringThickness = 2f;
		Color orbitColor = new Color("f0c65d", 0.6f);
		Color bodyColor = new Color("ffe8a8");
		Color glowColor = new Color("fff7de", 0.45f);
		float spin = _time * _rotationSpeed;

		for (int i = 0; i < count; i++)
		{
			float radius = baseRadius + i * orbitStep;
			float angle = spin + Mathf.Tau * i / count;
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			Vector2 p = dir * radius;
			float satelliteSize = 4f + i * 0.15f;

			DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 48, orbitColor, ringThickness);
			DrawCircle(p, satelliteSize + 2f, glowColor);
			DrawCircle(p, satelliteSize, bodyColor);
		}
	}
}
