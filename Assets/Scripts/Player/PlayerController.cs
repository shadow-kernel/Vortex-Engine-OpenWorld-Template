using Vortex;

// Smooth first-person player movement — 100% GAME-side (this script), driven via the Vortex API.
// Attached to the Main Camera. Mouse = look, WASD = move, Shift = sprint, Space = jump, Ctrl/C = crouch.
// Velocity eases toward the target each frame (accel/decel) for the smooth "real multiplayer" feel.
// The engine has NO hardcoded movement: every field below is yours to tune.
public class PlayerController : VortexBehaviour
{
    public float WalkSpeed   = 6f;     // units/sec
    public float SprintSpeed = 10f;    // while holding Shift
    public float CrouchSpeed = 3f;     // while crouching
    public float Accel       = 14f;    // how fast velocity ramps to the target (higher = snappier)
    public float MouseSens   = 0.10f;  // degrees per pixel of mouse movement
    public float JumpSpeed   = 7.5f;
    public float Gravity     = 20f;
    public float CrouchDrop  = 0.7f;   // how far the eye lowers when crouching

    private float _standEyeY;  // standing eye height, captured at spawn
    private float _vx, _vz;    // smoothed horizontal velocity
    private float _vy;         // vertical velocity
    private bool  _grounded;
    private bool  _jumpHeld;    // edge-trigger jump (holding Space must not re-fire every frame)
    private float _pitch, _yaw; // look angles (degrees), accumulated for smoothness

    public override void Start()
    {
        _standEyeY = Position.Y;
        var r = Rotation; _pitch = r.X; _yaw = r.Y;
        _vx = _vz = _vy = 0f;
        _grounded = true;
    }

    public override void Update(float dt)
    {
        if (dt <= 0f) return;

        // ---- Look: mouse (locked while playing, ESC frees) + arrow keys; clamp pitch, no roll ----
        _yaw   += Input.MouseDeltaX * MouseSens;
        _pitch += Input.MouseDeltaY * MouseSens;
        if (Input.GetKey("Left"))  _yaw   -= 90f * dt;
        if (Input.GetKey("Right")) _yaw   += 90f * dt;
        if (Input.GetKey("Up"))    _pitch -= 90f * dt;
        if (Input.GetKey("Down"))  _pitch += 90f * dt;
        // Clamp pitch, wrap yaw, reject NaN/Inf BEFORE writing the transform — a bad Euler value would
        // reach the native quaternion math and crash the engine on matrix normalize.
        if (_pitch > 89f) _pitch = 89f; else if (_pitch < -89f) _pitch = -89f;
        _yaw %= 360f;
        if (float.IsNaN(_yaw)   || float.IsInfinity(_yaw))   _yaw   = 0f;
        if (float.IsNaN(_pitch) || float.IsInfinity(_pitch)) _pitch = 0f;
        Rotation = new Vector3(_pitch, _yaw, 0f);

        // ---- Target horizontal velocity (relative to facing); Shift = sprint, Ctrl/C = crouch ----
        bool crouch = Input.GetKey("LeftCtrl") || Input.GetKey("C");
        bool sprint = Input.GetKey("LeftShift");
        float speed = crouch ? CrouchSpeed : (sprint ? SprintSpeed : WalkSpeed);

        // Move on the ground plane from YAW ONLY (decoupled from camera pitch).
        double yawRad = _yaw * System.Math.PI / 180.0;
        Vector3 f = new Vector3((float)System.Math.Sin(yawRad), 0f, (float)System.Math.Cos(yawRad));
        Vector3 r = new Vector3((float)System.Math.Cos(yawRad), 0f, (float)-System.Math.Sin(yawRad));
        float dx = 0f, dz = 0f;
        if (Input.GetKey("W")) { dx += f.X; dz += f.Z; }
        if (Input.GetKey("S")) { dx -= f.X; dz -= f.Z; }
        if (Input.GetKey("D")) { dx += r.X; dz += r.Z; }
        if (Input.GetKey("A")) { dx -= r.X; dz -= r.Z; }
        float len = (float)System.Math.Sqrt(dx * dx + dz * dz);
        float tx = 0f, tz = 0f;
        if (len > 0.001f) { tx = dx / len * speed; tz = dz / len * speed; }

        // ---- Ease velocity toward the target (smooth start/stop) ----
        float kk = Accel * dt; if (kk > 1f) kk = 1f;
        _vx += (tx - _vx) * kk;
        _vz += (tz - _vz) * kk;

        // ---- Apply move + jump/gravity (kinematic; lands on the eye-height floor) ----
        if (float.IsNaN(_vx) || float.IsInfinity(_vx)) _vx = 0f;
        if (float.IsNaN(_vz) || float.IsInfinity(_vz)) _vz = 0f;
        if (float.IsNaN(_vy) || float.IsInfinity(_vy)) _vy = 0f;

        float eyeY = crouch ? _standEyeY - CrouchDrop : _standEyeY;
        Vector3 p = Position;
        p.X += _vx * dt;
        p.Z += _vz * dt;

        bool jump = Input.GetKey("Space");
        if (_grounded)
        {
            if (jump && !_jumpHeld) { _vy = JumpSpeed; _grounded = false; } // edge-triggered: tap to jump
            else { p.Y = eyeY; }
        }
        if (!_grounded)
        {
            _vy -= Gravity * dt;
            p.Y += _vy * dt;
            if (p.Y <= eyeY) { p.Y = eyeY; _vy = 0f; _grounded = true; }
        }
        _jumpHeld = jump;

        // Finite-guard the position before it reaches the native transform (a NaN/Inf there crashes the engine).
        if (float.IsNaN(p.X) || float.IsInfinity(p.X)) p.X = 0f;
        if (float.IsNaN(p.Y) || float.IsInfinity(p.Y)) p.Y = eyeY;
        if (float.IsNaN(p.Z) || float.IsInfinity(p.Z)) p.Z = 0f;
        Position = p;
    }
}
