using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Satellite.src;

public partial class RegentSatelliteOrbit : Node2D
{
	private NCreature _owner = null!;
	private Control _hitbox = null!;
	private OrbitLayer _orbitLayer = null!;
	private float _rotationSpeed = 1.7f;
	private float _time;
	private int _targetStarCount;
	private int _renderStarCount;
	private bool _isVanishing;
	private bool _isSpawning;
	private float _spawnFlash;
	private float _spawnFade = 1f;
	private float _vanishFlash;
	private float _vanishFade = 1f;

	public void Initialize(NCreature owner, Control hitbox)
	{
		_owner = owner;
		_hitbox = hitbox;

		_orbitLayer = new OrbitLayer();

		var parent = _owner.GetParent();
		parent.AddChild(_orbitLayer);

		int creatureIndex = _owner.GetIndex();
		parent.MoveChild(_orbitLayer, creatureIndex);

		QueueRedraw();
	}

	public void UpdateFromStars(int stars)
	{
		stars = Math.Max(stars, 0);
		if (_targetStarCount == stars) return;

		if (stars > _targetStarCount)
		{
			_spawnFlash = 1f;
			_spawnFade = 0f;
			_isSpawning = true;
			_isVanishing = false;
			_vanishFade = 1f;
			_renderStarCount = stars;
		}
		else if (stars == 0 && _targetStarCount > 0)
		{
			_isVanishing = true;
			_vanishFlash = 1f;
			_vanishFade = 1f;
			_renderStarCount = Math.Max(_renderStarCount, _targetStarCount);
		}
		else
		{
			_renderStarCount = stars;
		}

		_targetStarCount = stars;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(_owner) || !IsInstanceValid(_hitbox))
		{
			if (IsInstanceValid(_orbitLayer))
				_orbitLayer.QueueFree();
			QueueFree();
			return;
		}

		if (_spawnFlash > 0f)
		{
			_spawnFlash = Mathf.Max(0f, _spawnFlash - (float)delta * 6.5f);
		}
		if (_isSpawning)
		{
			_spawnFade = Mathf.Min(1f, _spawnFade + (float)delta * 7f);
			if (_spawnFade >= 1f)
			{
				_isSpawning = false;
			}
		}
		if (_isVanishing)
		{
			_vanishFlash = Mathf.Max(0f, _vanishFlash - (float)delta * 10f);
			_vanishFade = Mathf.Max(0f, _vanishFade - (float)delta * 3.5f);
			if (_vanishFade <= 0f)
			{
				_isVanishing = false;
				_renderStarCount = 0;
			}
		}

		Visible = _renderStarCount > 0;
		if (!Visible)
		{
			if (IsInstanceValid(_orbitLayer))
				_orbitLayer.Visible = false;
			return;
		}

		if (IsInstanceValid(_orbitLayer))
		{
			_orbitLayer.GlobalPosition = _hitbox.GlobalPosition + _hitbox.Size * 0.5f + new Vector2(-100f, 0f);
			_time += (float)delta;
			_orbitLayer.UpdateFrame(_renderStarCount, _time, _rotationSpeed, _spawnFlash, _spawnFade, _vanishFlash, _vanishFade, _isVanishing);
			_orbitLayer.Visible = true;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (IsInstanceValid(_orbitLayer))
			_orbitLayer.QueueFree();
	}

	private sealed partial class OrbitLayer : Node2D
	{
		private int _starCount;
		private float _time;
		private float _rotationSpeed = 1.7f;
		private float _spawnFlash;
		private float _spawnFade = 1f;
		private float _vanishFlash;
		private float _vanishFade = 1f;
		private bool _isVanishing;

		public void UpdateFrame(int starCount, float time, float rotationSpeed, float spawnFlash, float spawnFade, float vanishFlash, float vanishFade, bool isVanishing)
		{
			_starCount = starCount;
			_time = time;
			_rotationSpeed = rotationSpeed;
			_spawnFlash = spawnFlash;
			_spawnFade = spawnFade;
			_vanishFlash = vanishFlash;
			_vanishFade = vanishFade;
			_isVanishing = isVanishing;
			QueueRedraw();
		}

		public override void _Draw()
		{
			if (_starCount <= 0) return;

			const int maxRings = 5;
			int ringCount = Math.Min(maxRings, Math.Max(1, _starCount));
			float baseRadius = 58f;
			float orbitStep = 16f;
			float ringThickness = 2f;
			Color orbitColor = new Color("#FFA631", 0.6f);
			Color orbitGlow = new Color("#ffd154", 0.2f);
			Color starColor = new Color("#00BFFF", 0.8f);
			Color starGlow = new Color("#60A5FA", 0.4f);

			float whitePulse = Mathf.Clamp(_spawnFlash + _vanishFlash, 0f, 1f);
			float alphaMul = _isVanishing ? _vanishFade : _spawnFade;
			orbitColor = BlendToWhite(orbitColor, whitePulse, alphaMul);
			orbitGlow = BlendToWhite(orbitGlow, whitePulse, alphaMul);
			starColor = BlendToWhite(starColor, whitePulse, alphaMul);
			starGlow = BlendToWhite(starGlow, whitePulse, alphaMul);
			float spin = _time * _rotationSpeed;

			for (int ring = 0; ring < ringCount; ring++)
			{
				float radius = baseRadius + ring * orbitStep;
				DrawPerspectiveRing(radius, orbitGlow, ringThickness + 1f);
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
				float starSize = 7.0f + Math.Min(cycleIndex, 4) * 0.5f;
				DrawCrossStar(p, starSize, starColor, starGlow);
			}
		}

		private void DrawCrossStar(Vector2 center, float size, Color color, Color glowColor)
		{
			DrawLongDiamond(center, size + 2f, glowColor);
			DrawLongDiamond(center, size, color);
		}

		private void DrawLongDiamond(Vector2 center, float size, Color color)
		{
			Vector2[] points = new Vector2[4];
			points[0] = center + new Vector2(0, -size * 1.2f);
			points[1] = center + new Vector2(size * 0.8f, 0);
			points[2] = center + new Vector2(0, size * 1.2f);
			points[3] = center + new Vector2(-size * 0.8f, 0);

			DrawColoredPolygon(points, color);
		}

		private void DrawPerspectiveRing(float radius, Color color, float width)
		{
			const int segments = 192;
			Vector2[] points = new Vector2[segments + 1];
			for (int i = 0; i <= segments; i++)
			{
				float t = (float)i / segments;
				points[i] = ProjectOrbitPoint(Mathf.Tau * t, radius);
			}
			DrawPolyline(points, color, width, true);
		}

		private Vector2 ProjectOrbitPoint(float angle, float radius)
		{
			Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			float verticalScale = 1.2f;
			float horizontalShear = 0.13f;
			float x = p.X + p.Y * horizontalShear;
			float y = p.Y * verticalScale;
			return new Vector2(x, y);
		}

		private void DrawSatellite(Vector2 center, float size, Color starColor, Color glowColor)
		{
			DrawCircle(center, size + 2f, glowColor);
			DrawCircle(center, size, starColor);
		}

		private static Color BlendToWhite(Color baseColor, float amount, float alphaMul)
		{
			float t = Mathf.Clamp(amount, 0f, 1f);
			Color c = baseColor.Lerp(Colors.White, t);
			c.A *= Mathf.Clamp(alphaMul, 0f, 1f);
			return c;
		}
	}
}