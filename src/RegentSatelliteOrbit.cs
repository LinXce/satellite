using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Satellite.src;

public partial class RegentSatelliteOrbit : Node2D
{
	private NCreature _owner = null!;

	private Control _hitbox = null!;

	private OrbitLayer _backLayer = null!;

	private OrbitLayer _frontLayer = null!;

	private float _rotationSpeed = 1.7f;

	private float _time;

	private int _starCount;

	private static readonly Shader _occlusionShader = GD.Load<Shader>("res://src/regent_satellite_occlusion.shader");

	public void Initialize(NCreature owner, Control hitbox)
	{
		_owner = owner;
		_hitbox = hitbox;
		_backLayer = CreateLayer("BackOrbitLayer", 0f);
		_frontLayer = CreateLayer("FrontOrbitLayer", 0f);
		AddChild(_backLayer);
		AddChild(_frontLayer);
		ZIndex = -5;
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
			_backLayer.Visible = false;
			_frontLayer.Visible = false;
			return;
		}

		GlobalPosition = _hitbox.GlobalPosition + _hitbox.Size * 0.5f;
		_time += (float)delta;
		_backLayer.UpdateFrame(_starCount, _time, _rotationSpeed);
		_frontLayer.UpdateFrame(_starCount, _time, _rotationSpeed);
	}

	private static OrbitLayer CreateLayer(string name, float side)
	{
		OrbitLayer layer = new OrbitLayer();
		layer.Name = name;
		layer.ZIndex = (int)side;
		layer.SetOcclusionShader(_occlusionShader, side);
		return layer;
	}

	private sealed partial class OrbitLayer : Node2D
	{
		private int _starCount;

		private float _time;

		private float _rotationSpeed = 1.7f;

		private float _side;

		private ShaderMaterial _material = null!;

		public void SetOcclusionShader(Shader shader, float side)
		{
			_side = side;
			_material = new ShaderMaterial
			{
				Shader = shader
			};
			_material.SetShaderParameter("side", side);
			Material = _material;
		}

		public void UpdateFrame(int starCount, float time, float rotationSpeed)
		{
			_starCount = starCount;
			_time = time;
			_rotationSpeed = rotationSpeed;
			Visible = _starCount > 0;
			QueueRedraw();
		}

		public override void _Draw()
		{
			if (_starCount <= 0)
			{
				return;
			}

			const int maxRings = 5;
			int ringCount = Math.Min(maxRings, Math.Max(1, _starCount));
			float baseRadius = 58f;
			float orbitStep = 16f;
			float ringThickness = 1.8f;
			Color orbitColor = new Color("f0c65d", 0.6f);
			Color orbitGlow = new Color("ffeec0", 0.28f);
			Color starColor = new Color("ffe8a8");
			Color starGlow = new Color("fff7de", 0.45f);
			float spin = _time * _rotationSpeed;

			for (int ring = 0; ring < ringCount; ring++)
			{
				float radius = baseRadius + ring * orbitStep;
				DrawPerspectiveRing(radius, orbitGlow, ringThickness + 3f);
				DrawPerspectiveRing(radius, orbitColor, ringThickness);
			}

			for (int i = 0; i < _starCount; i++)
			{
				int ringIndex = i % ringCount;
				int cycleIndex = i / ringCount;
				float radius = baseRadius + ringIndex * orbitStep;
				float ringAngle = spin + Mathf.Tau * ringIndex / ringCount + cycleIndex * 0.72f;
				if ((cycleIndex & 1) == 1)
				{
					ringAngle += 0.28f;
				}
				Vector2 p = ProjectOrbitPoint(ringAngle, radius);
				float starSize = 4.0f + Math.Min(cycleIndex, 4) * 0.35f;
				DrawSatellite(p, starSize, starColor, starGlow);
			}
		}

		private void DrawPerspectiveRing(float radius, Color color, float width)
		{
			const int segments = 96;
			Vector2 prev = ProjectOrbitPoint(0f, radius);
			for (int i = 1; i <= segments; i++)
			{
				float t = (float)i / segments;
				Vector2 next = ProjectOrbitPoint(Mathf.Tau * t, radius);
				DrawLine(prev, next, color, width, true);
				prev = next;
			}
		}

		private Vector2 ProjectOrbitPoint(float angle, float radius)
		{
			Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			float verticalScale = 0.1f;
			float horizontalShear = 0.33f;
			float x = p.X + p.Y * horizontalShear;
			float y = p.Y * verticalScale;
			return new Vector2(x, y);
		}

		private void DrawSatellite(Vector2 center, float size, Color starColor, Color glowColor)
		{
			DrawCircle(center, size + 2f, glowColor);
			DrawCircle(center, size, starColor);
		}
	}
}
