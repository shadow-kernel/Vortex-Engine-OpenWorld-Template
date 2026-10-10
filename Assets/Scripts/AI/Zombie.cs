using Vortex;
// THE INFECTED — the zombies of the open world: operators who did not make it out of the desert. The decisions live in
// Assets/AI/Zombie.vbt (chase on sight, go to noises, search the last known spot, otherwise wander) and drive the Nav
// Agent and the AI Perception through the engine's built-in tasks; this script is the body:
//   * a hunched spine and a dragging shamble while wandering, a flat-out sprint once the player is seen
//   * the lunge: within AttackRange the zombie winds up, leaps (the jump clip) and bites — "damage" to the player
//   * it takes the player's bullets ("damage" like the bots), flinches, and at zero health falls into a ragdoll with the
//     bullet's impulse and despawns a while later
//   * gunfire and footsteps are noises the perception hears — a shot pulls every zombie within hearing range
// Everything is a project script — the engine ticks the tree, moves the agent and senses; the game decides what it means.
public class Zombie : VortexBehaviour
{
    public string TreePath    = "Assets/AI/Zombie.vbt";
    public string IdleClip    = "Idle";
    public string WalkClip    = "walk";
    public string RunClip     = "run";
    public string AttackClip  = "jump";
    public string DeathClip   = "death";
    public float  MaxHealth   = 90f;
    public float  AttackDamage = 24f;
    public float  AttackRange = 1.8f;
    public float  AttackCooldown = 1.3f;
    public float  AttackWindup = 0.4f;      // seconds between the lunge starting and the bite landing
    public float  WanderSpeed = 0.9f;
    public float  ChaseSpeed  = 4.4f;
    public float  RunThreshold = 2.2f;      // agent speed above which the run clip plays
    public float  WanderRadius = 30f;
    public float  Fade        = 0.2f;
    public float  DespawnAfter = 14f;
    public string SpineBone   = "mixamorig:Spine1";
    public float  Hunch       = 22f;        // degrees the spine bends forward while shambling

    private float _hp, _sinceAttack = 99f, _windup = -1f, _despawn = -1f, _flinch, _wanderWait;
    private string _clip = "", _state = "wander";
    private bool _dead, _lunging;
    private long _target;

    public bool IsDead { get { return _dead; } }
    public string State { get { return _state; } }

    public override void Start()
    {
        _hp = MaxHealth;
        if (!BehaviorTree.IsRunning(EntityId)) BehaviorTree.Run(EntityId, TreePath);
        Navigation.SetSpeed(EntityId, WanderSpeed);
        PlayClip(IdleClip);
        Animation.SetBoneAdditiveRotation(EntityId, SpineBone, new Vector3(Hunch, 0f, 0f));
        PickWanderPoint();
    }

    public bool Diagnostics = false;   // log state / tree task / path / sight every 3 s (debugging a silent horde)
    private float _diag;
    public override void Update(float dt)
    {
        _diag += dt;
        if (Diagnostics && _diag > 3f)
        {
            _diag = 0f;
            Vector3 me = Scene.WorldPositionOf(EntityId);
            Debug.Log("Zombie " + EntityId + ": " + _state + " tree=" + (BehaviorTree.IsRunning(EntityId) ? BehaviorTree.ActiveTask(EntityId) : "stopped") + " dest=" + Navigation.HasDestination(EntityId) + " moving=" + Navigation.IsMoving(EntityId) + " path=" + Navigation.PathStatus(EntityId) + " sees=" + Perception.CanSee(EntityId) + " at " + me.X.ToString("0") + "," + me.Y.ToString("0") + "," + me.Z.ToString("0") + " clip=" + _clip);
        }
        if (_dead)
        {
            if (_despawn > 0f) { _despawn -= dt; if (_despawn <= 0f) Scene.Destroy(EntityId); }
            return;
        }
        _sinceAttack += dt;
        if (_flinch > 0f) { _flinch -= dt * 3f; Animation.SetBoneAdditiveRotation(EntityId, SpineBone, new Vector3(Hunch - 25f * System.Math.Max(0f, _flinch), 0f, 0f)); }

        // the lunge lands after the wind-up — if the player is still within reach
        if (_lunging)
        {
            _windup -= dt;
            if (_windup <= 0f)
            {
                _lunging = false;
                if (_target != 0 && Dist(Scene.WorldPositionOf(_target)) <= AttackRange + 0.6f) SendMessage(_target, "damage", AttackDamage);
            }
            return;
        }

        // the bite: close enough while chasing, and the cooldown is over
        if (_state == "chase" && _target != 0 && _sinceAttack >= AttackCooldown && Dist(Scene.WorldPositionOf(_target)) <= AttackRange)
        {
            _sinceAttack = 0f; _lunging = true; _windup = AttackWindup;
            PlayClip(AttackClip);
            return;
        }

        // a fresh wander point now and then while nobody is in sight
        if (_state == "wander") { _wanderWait -= dt; if (_wanderWait <= 0f) PickWanderPoint(); }

        // locomotion from the agent's real velocity: the tree says where, the feet say how fast
        Vector3 v = Navigation.Velocity(EntityId);
        float speed = (float)System.Math.Sqrt(v.X * v.X + v.Z * v.Z);
        string want = speed < 0.2f ? IdleClip : (speed < RunThreshold ? WalkClip : RunClip);
        if (want != _clip) { PlayClip(want); Animation.SetSpeed(EntityId, want == WalkClip ? 0.75f : 1f); }
    }

    private float Dist(Vector3 p)
    {
        Vector3 me = Scene.WorldPositionOf(EntityId);
        float dx = p.X - me.X, dz = p.Z - me.Z;
        return (float)System.Math.Sqrt(dx * dx + dz * dz);
    }

    private void PickWanderPoint()
    {
        _wanderWait = 4f + (float)(new System.Random(EntityId.GetHashCode() ^ System.Environment.TickCount).NextDouble() * 6f);
        Vector3 p;
        if (Navigation.RandomPoint(Scene.WorldPositionOf(EntityId), WanderRadius, out p) && !Water.IsUnderwater(p))   // the lakes are not for shambling
        {
            var bb = BehaviorTree.BlackboardOf(EntityId);
            if (bb != null) bb.Set("WanderPoint", p);
        }
    }

    private void PlayClip(string clip) { _clip = clip; PlayAnimation(clip, Fade); }

    // ---- the tree talks: SendMessage nodes in Zombie.vbt; the player's weapons: "damage" ----
    public override void OnMessage(string message, object arg)
    {
        if (_dead) return;
        switch (message)
        {
            case "damage":
                _hp -= arg is float ? (float)arg : 20f;
                _flinch = 1f;
                if (_hp <= 0f) Die();
                break;
            case "bt.chase":
                _state = "chase"; _target = Perception.VisibleTarget(EntityId);
                Navigation.SetSpeed(EntityId, ChaseSpeed);
                Animation.SetBoneAdditiveRotation(EntityId, SpineBone, new Vector3(Hunch * 0.4f, 0f, 0f));   // it straightens up to run
                break;
            case "bt.investigate": _state = "investigate"; Navigation.SetSpeed(EntityId, ChaseSpeed * 0.55f); break;
            case "bt.search":      _state = "search";      Navigation.SetSpeed(EntityId, ChaseSpeed * 0.6f); break;
            case "bt.wander":
                _state = "wander"; Navigation.SetSpeed(EntityId, WanderSpeed);
                Animation.SetBoneAdditiveRotation(EntityId, SpineBone, new Vector3(Hunch, 0f, 0f));
                PickWanderPoint();
                break;
        }
    }

    private void Die()
    {
        _dead = true; _lunging = false;
        BehaviorTree.Stop(EntityId);
        Navigation.Stop(EntityId);
        PlayClip(DeathClip);
        _despawn = DespawnAfter;
        // the ragdoll takes over with a shove away from the player
        Vector3 me = Scene.WorldPositionOf(EntityId);
        Vector3 away = new Vector3(0f, 0.3f, 1f);
        if (_target != 0)
        {
            Vector3 p = Scene.WorldPositionOf(_target);
            float dx = me.X - p.X, dz = me.Z - p.Z, len = (float)System.Math.Sqrt(dx * dx + dz * dz);
            if (len > 0.01f) away = new Vector3(dx / len, 0.3f, dz / len);
        }
        Ragdoll.Activate(EntityId, away * 160f, new Vector3(me.X, me.Y + 1.2f, me.Z));
        Animation.ClearBoneOverrides(EntityId);
    }
}
