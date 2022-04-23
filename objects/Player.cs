using System;
using Godot;
using Immersion.Voxel.Blocks;
using Immersion.Voxel.Chunks;

public class Player : KinematicBody
{
	public float MouseSensitivity { get; set; } = 0.2F;

	/// <summary> Time after pressing the jump button a jump may occur late. </summary>
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

	public enum MovementMode { Default, Flying, NoClip }
	public MovementMode Movement { get; set; } = MovementMode.Default;


	private IWorld  _world    = null!;
	private Spatial _rotation = null!;
	private Camera  _camera   = null!;

	private Vector3 _velocity = Vector3.Zero;
	private DateTime? _jumpPressed = null;
	private DateTime? _lastOnFloor = null;

	public bool IsSprinting { get; private set; }


	public override void _Ready()
	{
		_world    = GetParent<World>() ?? throw new Exception();
		_rotation = GetNode<Spatial>("Rotation");
		_camera   = GetNode<Camera>("Rotation/Camera");
		// Input.SetMouseMode(Input.MouseMode.Captured);
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustPressed("cycle_movement_mode"))
			if (++Movement > MovementMode.NoClip)
				Movement = MovementMode.Default;

		var breaking = Input.IsActionJustPressed("action_break");
		var placing  = Input.IsActionJustPressed("action_place");
		if (breaking || placing) {
			var rayLength = 4.0F;
			var mousePos  = GetViewport().GetMousePosition();
			var startPos  = _camera.ProjectRayOrigin(mousePos);
			var endPos    = startPos + _camera.ProjectRayNormal(mousePos) * rayLength;

			var result = GetWorld().DirectSpaceState.IntersectRay(startPos, endPos);
			if (result.Count == 0) return;

			var pos    = (Vector3)result["position"];
			var normal = (Vector3)result["normal"];
			var block  = (pos + normal * (placing ? 0.5F : -0.5F)).ToBlockPos();

			var chunk     = _world.Chunks.GetChunkOrNull(block.ToChunkPos())!;
			var (x, y, z) = block.ToChunkRelative();
			chunk.Storage[x, y, z] = breaking ? Block.AIR : Block.STONE;
			// TODO: _world.Chunks.ForceUpdate(chunk);

			// foreach (var facing in BlockFacings.ALL) {
			// 	var neighborChunkPos = (block + facing).ToChunkPos();
			// 	if (neighborChunkPos == chunk.Position) continue;

			// 	var neighborChunk = _world.Chunks.GetChunkOrNull(neighborChunkPos);
			// 	if (neighborChunk != null)
			// 		_world.Chunks.ForceUpdate(neighborChunk);
			// }
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		IsSprinting = Input.IsActionPressed("move_sprint");

		var movementVector = new Vector3(
			Input.GetActionStrength("move_strafe_right") - Input.GetActionStrength("move_strafe_left"),
			Input.GetActionStrength("move_upward")       - Input.GetActionStrength("move_downward"),
			Input.GetActionStrength("move_backward")     - Input.GetActionStrength("move_forward"));

		if (Movement == MovementMode.Default) {

			_velocity.y += delta * Gravity;

			var dir = Vector3.Zero;
			var camTransform = _camera.GlobalTransform;
			dir += camTransform.basis.z.Normalized() * movementVector.z;
			dir += camTransform.basis.x.Normalized() * movementVector.x;
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

		} else {

			_velocity *= 1 - FrictionAir * delta;

			var cameraRotation = _camera.GlobalTransform.basis.RotationQuat();
			var dir    = cameraRotation.Xform(movementVector);
			var target = dir * MaxMoveSpeed;
			var accel  = (dir.Dot(_velocity) > 0) ? MoveAccel : FrictionAir;
			target *= 4; accel *= 4;

			if (IsSprinting) { target *= 5; accel *= 5; }
			_velocity = _velocity.LinearInterpolate(target, accel * delta);

		}

		if (Movement == MovementMode.NoClip)
			Translate(_velocity * delta);
		else _velocity = MoveAndSlide(_velocity, Vector3.Up,
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
