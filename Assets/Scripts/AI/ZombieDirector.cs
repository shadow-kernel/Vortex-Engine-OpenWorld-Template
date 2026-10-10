using Vortex;
// THE HORDE DIRECTOR — keeps the desert populated: spawns zombies on the navmesh in a ring around the player (never in view
// distance's closest band, never on top of them), tops the number up as they fall, and lets you call a wave with Z.
// Put it on an empty entity ("Zombies"); the prefab carries the body, the brain and the tree.
public class ZombieDirector : VortexBehaviour
{
    public string Prefab       = "Assets/Prefabs/Zombie.ventity";
    public int    MaxAlive     = 10;
    public int    FirstWave    = 6;
    public float  MinDistance  = 40f;
    public float  MaxDistance  = 130f;
    public float  SpawnInterval = 7f;     // seconds between top-ups
    public string PlayerTag    = "Player";
    public string WaveKey      = "Z";
    public int    WaveSize     = 4;
    public bool   FirstWaveAhead = true;  // the first ones come at you from the front, 30–60 m out — you see them coming
    public bool   Diagnostics  = false;

    private readonly System.Collections.Generic.List<long> _alive = new System.Collections.Generic.List<long>();
    private float _timer = 1.5f;
    private bool _first = true;
    private System.Random _rng = new System.Random(17);

    public int Alive { get { return _alive.Count; } }

    public override void Update(float dt)
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            long z = _alive[i];
            var brain = Scene.GetBehaviour<Zombie>(z);
            if (brain == null || brain.IsDead) _alive.RemoveAt(i);
        }
        if (Input.GetKeyDown(WaveKey)) { for (int i = 0; i < WaveSize; i++) Spawn(MinDistance * 0.6f, MinDistance * 1.2f); }
        _timer -= dt;
        if (_timer > 0f) return;
        _timer = SpawnInterval;
        int want = _first ? FirstWave : 1;
        bool ahead = _first && FirstWaveAhead;
        _first = false;
        for (int i = 0; i < want && _alive.Count < MaxAlive; i++) { if (ahead) SpawnAhead(30f, 60f, 35f); else Spawn(MinDistance, MaxDistance); }
    }

    /// <summary>One zombie inside the player's view cone (±spread degrees of their facing), min..max metres out.</summary>
    public long SpawnAhead(float min, float max, float spread)
    {
        long player = Player();
        if (player == 0 || !Navigation.IsAvailable) return 0;
        Vector3 pp = Scene.WorldPositionOf(player);
        // the view: the Tactical rig publishes its camera yaw (the entity's rotation is only the spawn pose)
        float yawDeg = PlayerRig.Yaw; if (float.IsNaN(yawDeg) || (yawDeg == 0f && Scene.RotationOf(player).Y != 0f && !Input.WindowFocused)) yawDeg = Scene.RotationOf(player).Y;
        float yaw = yawDeg * (float)System.Math.PI / 180f;
        for (int tries = 0; tries < 8; tries++)
        {
            double a = yaw + (_rng.NextDouble() * 2.0 - 1.0) * spread * System.Math.PI / 180.0, r = min + _rng.NextDouble() * (max - min);
            var want = new Vector3(pp.X + (float)(System.Math.Sin(a) * r), pp.Y, pp.Z + (float)(System.Math.Cos(a) * r));
            Vector3 p;
            if (!Navigation.SamplePosition(want, 12f, out p) || Water.IsUnderwater(p)) continue;   // never in a lake
            long z = Scene.Instantiate(Prefab, p, (float)(_rng.NextDouble() * 360.0));
            if (z != 0) { _alive.Add(z); if (Diagnostics) Debug.Log("ZombieDirector: spawned ahead at " + p.X.ToString("0") + ", " + p.Y.ToString("0") + ", " + p.Z.ToString("0") + " (player " + pp.X.ToString("0") + ", " + pp.Y.ToString("0") + ", " + pp.Z.ToString("0") + " yaw " + Scene.RotationOf(player).Y.ToString("0") + ")"); return z; }
        }
        return Spawn(min, max);
    }

    private long Player()
    {
        long[] tagged = Scene.FindByTag(PlayerTag);
        if (tagged != null && tagged.Length > 0) return tagged[0];
        return Scene.Find("Player");
    }

    /// <summary>One zombie on the navmesh between min and max metres from the player (eight tries).</summary>
    public long Spawn(float min, float max)
    {
        long player = Player();
        if (player == 0 || !Navigation.IsAvailable) return 0;
        Vector3 pp = Scene.WorldPositionOf(player);
        for (int tries = 0; tries < 8; tries++)
        {
            double a = _rng.NextDouble() * System.Math.PI * 2.0, r = min + _rng.NextDouble() * (max - min);
            var want = new Vector3(pp.X + (float)(System.Math.Cos(a) * r), pp.Y, pp.Z + (float)(System.Math.Sin(a) * r));
            Vector3 p;
            if (!Navigation.SamplePosition(want, 12f, out p) || Water.IsUnderwater(p)) continue;   // never in a lake
            float dx = p.X - pp.X, dz = p.Z - pp.Z;
            if (dx * dx + dz * dz < min * min * 0.5f) continue;
            long z = Scene.Instantiate(Prefab, p, (float)(_rng.NextDouble() * 360.0));
            if (z != 0) { _alive.Add(z); return z; }
        }
        return 0;
    }
}
