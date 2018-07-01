using System;
using Godot;

public class Player : KinematicBody
{
	private Camera _camera;
	private Spatial _rotation;
	private Vector3 _velocity;
	
	public float MouseSensitivity { get; set; } = 0.2F;
	public float Gravity { get; set; } = -12.0F;
	public float MaxMoveSpeed { get; set; } = 20.0F;
	public float MoveAcceleration { get; set; } = -4.5F;
	public float MoveDeacceleration { get; set; } = -16.0F;
	public float MaxSlopeAngle { get; set; } = 40.0F;
	public float JumpVelocity { get; set; } = 160.0F;
	
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
		
		// var movementVector = Vector2.Zero;
		// if (Input.IsActionPressed("move_forward")) movementVector.y += 1;
		// if (Input.IsActionPressed("move_back")) movementVector.y -= 1;
		// if (Input.IsActionPressed("move_strafe_left")) movementVector.x -= 1;
		// if (Input.IsActionPressed("move_strafe_right")) movementVector.x += 1;
		// movementVector = movementVector.Normalized();
		
		// var lookVector = _camera.GetGlobalTransform();
		// movementVector.Rotated(_rotation.Rotation.y);
		
		// GD.Print($"Jump: { Input.IsActionJustPressed("move_jump") }");
		GD.Print($"Floor: { IsOnFloor() }");
		
		if (Input.IsActionJustPressed("move_jump") && IsOnFloor())
			_velocity.y = -JumpVelocity;
		
		_velocity = MoveAndSlide(_velocity, Vector3.Up, floorMaxAngle: Mathf.Deg2Rad(MaxSlopeAngle));
		// GD.Print($"Velocity: { _velocity }");
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
}
