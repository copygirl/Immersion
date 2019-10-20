using System;
using Godot;

namespace Immersion
{
	public class Player : KinematicBody
	{
		public float MouseSensitivity { get; set; } = 0.2F;
		
		/// <summary>  Time after pressing the jump button a jump may occur late. </summary>
		public TimeSpan JumpEarlyTime { get; set; } = TimeSpan.FromSeconds(0.2);
		
		/// <summary> Time after leaving a jumpable surface when a jump may still occur. </summary>
		public TimeSpan JumpCoyoteTime { get; set; } = TimeSpan.FromSeconds(0.2);
		
		public float Gravity       { get; set; } = -12.0F;
		public float JumpVelocity  { get; set; } =   5.0F;
		public float MoveAccel     { get; set; } =   6.0F;
		public float MaxMoveSpeed  { get; set; } =   4.0F;
		public float FrictionFloor { get; set; } =  12.0F;
		public float FrictionAir   { get; set; } =   2.0F;
		
		public float FloorMaxAngle { get; set; } = Mathf.Deg2Rad(45.0F);
		
		
		#pragma warning disable 8618
		private Camera _camera;
		private Spatial _rotation;
		#pragma warning restore 8618
		
		private Vector3 _velocity = Vector3.Zero;
		private DateTime? _jumpPressed = null;
		private DateTime? _lastOnFloor = null;
		
		public bool IsSprinting { get; private set; }
		
		
		public override void _Ready()
		{
			_rotation = GetNode<Spatial>("Rotation");
			_camera   = GetNode<Camera>("Rotation/Camera");
			Input.SetMouseMode(Input.MouseMode.Captured);
		}
		
		public override void _Process(float delta)
		{
		}
		
		public override void _PhysicsProcess(float delta)
		{
			IsSprinting = Input.IsActionPressed("move_sprint");
			
			_velocity.y += delta * Gravity;
			
			var movementVector = new Vector2(
				Input.GetActionStrength("move_strafe_right") - Input.GetActionStrength("move_strafe_left"),
				Input.GetActionStrength("move_forward")      - Input.GetActionStrength("move_backward"));
			if (movementVector.LengthSquared() > 1.0F)
				movementVector = movementVector.Normalized();
			
			var dir = Vector3.Zero;
			var camTransform = _camera.GlobalTransform;
			dir += -camTransform.basis.z.Normalized() * movementVector.y;
			dir +=  camTransform.basis.x.Normalized() * movementVector.x;
			dir.y = 0;
			dir = dir.Normalized() * movementVector.Length();
			
			var hvel = _velocity;
			hvel.y = 0;
			
			var target   = dir * MaxMoveSpeed;
			var friction = IsOnFloor() ? FrictionFloor : FrictionAir;
			var accel    = (dir.Dot(hvel) > 0) ? MoveAccel : friction;
			if (IsSprinting) { target *= 5; accel *= 5; }
			hvel = hvel.LinearInterpolate(target, accel * delta);
			
			_velocity.x = hvel.x;
			_velocity.z = hvel.z;
			
			// Sometimes, when pushing into a wall, jumping wasn't working.
			// Possibly due to `IsOnFloor` returning `false` for some reason.
			// The `JumpEarlyTime` feature seems to avoid this issue, thankfully.
			if (Input.IsActionJustPressed("move_jump"))
				_jumpPressed = DateTime.Now;
			if (IsOnFloor())
				_lastOnFloor = DateTime.Now;
			
			if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime)
			 && ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
				_velocity.y  = IsSprinting ? JumpVelocity * 5 : JumpVelocity;
				_jumpPressed = null;
				_lastOnFloor = null;
			}
			
			_velocity = MoveAndSlide(_velocity, Vector3.Up,
				floorMaxAngle: FloorMaxAngle);
		}
		
		public override void _UnhandledInput(InputEvent ev)
		{
			if (ev.IsActionPressed("ui_cancel"))
			{
				Input.SetMouseMode((Input.GetMouseMode() == Input.MouseMode.Visible)
					? Input.MouseMode.Captured : Input.MouseMode.Visible);
			}
			else if (ev is InputEventMouseMotion motion)
			{
				if (Input.GetMouseMode() != Input.MouseMode.Captured) return;
				
				_camera.RotateX(Mathf.Deg2Rad(motion.Relative.y * -MouseSensitivity));
				_rotation.RotateY(Mathf.Deg2Rad(motion.Relative.x * -MouseSensitivity));
				
				var rotation = _camera.RotationDegrees;
				rotation.x = Mathf.Clamp(rotation.x, -80, 80);
				_camera.RotationDegrees = rotation;
			}
		}
	}
}
