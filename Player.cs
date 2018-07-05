using System;
using Godot;

public class Player : KinematicBody
{
	private Camera _camera;
	private Spatial _rotation;
	
	private Vector3 _velocity = Vector3.Zero;
	
	public float MouseSensitivity { get; set; } = 0.2F;
	public float Gravity { get; set; } = -19.8F;
	public float MaxMoveSpeed { get; set; } = 20.0F;
	public float MoveAccel { get; set; } = 4.5F;
	public float MoveDeaccel { get; set; } = 16.0F;
	public float JumpVelocity { get; set; } = 16.0F;
	
	public override void _Ready()
	{
		_rotation = (Spatial)GetNode("Rotation");
		_camera   = (Camera)GetNode("Rotation/Camera");
		Input.SetMouseMode(Input.MouseMode.Captured);
	}
	
	public override void _Process(float delta)
	{
	}
	
	public override void _PhysicsProcess(float delta)
	{
		_velocity.y += delta * Gravity;
		
		var movementVector = Vector2.Zero;
		if (Input.IsActionPressed("move_forward")) movementVector.y += 1;
		if (Input.IsActionPressed("move_back")) movementVector.y -= 1;
		if (Input.IsActionPressed("move_strafe_left")) movementVector.x -= 1;
		if (Input.IsActionPressed("move_strafe_right")) movementVector.x += 1;
		
		var dir = Vector3.Zero;
		var camTransform = _camera.GlobalTransform;
		dir += -camTransform.basis.z.Normalized() * movementVector.y;
		dir +=  camTransform.basis.x.Normalized() * movementVector.x;
		dir.y = 0;
		dir = dir.Normalized() * Math.Min(1.0F, movementVector.Length());
		
		var hvel = _velocity;
		hvel.y = 0;
		
		var target = dir * MaxMoveSpeed;
		var accel  = (dir.Dot(hvel) > 0) ? MoveAccel : MoveDeaccel;
		hvel = hvel.LinearInterpolate(target, accel * delta);
		
		_velocity.x = hvel.x;
		_velocity.z = hvel.z;
		
		if (Input.IsActionJustPressed("move_jump") && OnFloor)
			_velocity.y = JumpVelocity;
		
		_velocity = MoveAndSlideNew(_velocity, delta);
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.SetMouseMode((Input.GetMouseMode() == Input.MouseMode.Visible)
				? Input.MouseMode.Captured : Input.MouseMode.Visible);
		}
		else if (@event is InputEventMouseMotion motion)
		{
			if (Input.GetMouseMode() != Input.MouseMode.Captured) return;
			
			_camera.RotateX(Mathf.Deg2Rad(motion.Relative.y * -MouseSensitivity));
			_rotation.RotateY(Mathf.Deg2Rad(motion.Relative.x * -MouseSensitivity));
			
			var rotation = _camera.RotationDegrees;
			rotation.x = Mathf.Clamp(rotation.x, -80, 80);
			_camera.RotationDegrees = rotation;
		}
	}
	
	
	// Move and slide, reimplemented.
	
	private Vector3 _floorVelocity;
	
	private Vector3 _floorNormal = Vector3.Up;
	private float _slopeStopMinVelocity = 0.05F;
	private float _maxSlides = 4;
	private bool _infiniteInertia = true;
	
	public float MaxSlopeAngle { get; set; } = Mathf.Deg2Rad(40.0F);
	
	public bool OnFloor { get; private set; }
	public bool OnWall { get; private set; }
	public bool OnCeiling { get; private set; }
	
	public Vector3 MoveAndSlideNew(Vector3 linearVelocity, float delta)
	{
		var lv     = linearVelocity;
		var motion = (_floorVelocity + lv) * delta;
		
		OnFloor   = false;
		OnWall    = false;
		OnCeiling = false;
		_floorVelocity = Vector3.Zero;
		
		for (var slides = _maxSlides; slides >= 0; slides--) {
			var collision = MoveAndCollide(motion, _infiniteInertia);
			if (collision == null) break;
			GD.Print(collision);
			
			if (_floorNormal != Vector3.Zero) {
				if (collision.Normal.Dot(_floorNormal) >= Math.Cos(MaxSlopeAngle)) {
					OnFloor = true;
					_floorVelocity = collision.ColliderVelocity;
					
					var rel_v = lv - _floorVelocity;
					var hv = rel_v - _floorNormal * _floorNormal.Dot(rel_v);
					
					if ((collision.Travel.Length() < 0.05F) && (hv.Length() < _slopeStopMinVelocity)) {
						var gt = GlobalTransform;
						gt.origin -= collision.Travel;
						GlobalTransform = gt;
						return _floorVelocity - _floorNormal * _floorNormal.Dot(_floorVelocity);
					}
				} else if (collision.Normal.Dot(-_floorNormal) >= Math.Cos(MaxSlopeAngle)) {
					OnCeiling = true;
				} else OnWall = true;
			} else OnWall = true;
			
			var n = collision.Normal;
			motion = motion.Slide(n);
			lv = lv.Slide(n);
			
			if (motion == Vector3.Zero) break;
		}
		
		return lv;
	}
}
