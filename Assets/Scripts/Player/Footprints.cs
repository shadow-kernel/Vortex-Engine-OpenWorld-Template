using Vortex;
// FOOTPRINTS — every stride presses a boot print into the sand: a projected decal (left, right, left …) under the foot,
// turned the way you walk, darker where the sand is pushed down, gone again after a couple of minutes. Only on terrain
// and only on its sand / damp layers — rock keeps no prints. Attach to the Player (the entity the camera and the
// controller sit on); the stride length and the print size are yours to tune.
public class Footprints : VortexBehaviour
{
    public float  Stride      = 0.78f;    // metres between prints (one foot)
    public float  SprintStride = 1.25f;
    public float  Width       = 0.17f;    // half the distance between the left and the right foot
    public float  Length      = 0.33f;    // print size (m)
    public float  Lifetime    = 150f;     // seconds until a print has faded
    public string LeftMaterial  = "Assets/Materials/Decals/Footprint_Sand_L.vmat";
    public string RightMaterial = "Assets/Materials/Decals/Footprint_Sand_R.vmat";
    public float  Opacity     = 0.85f;
    public int    RockLayer   = 2;        // the terrain layer that keeps no prints (-1 = print everywhere)
    public bool   TerrainOnly = true;

    private Vector3 _last;
    private float _acc;
    private bool _left;
    private float _dirX, _dirZ = 1f;

    public override void Start() { _last = WorldPosition; }

    public override void Update(float dt)
    {
        Vector3 p = WorldPosition;
        float dx = p.X - _last.X, dz = p.Z - _last.Z;
        _last = p;
        float moved = (float)System.Math.Sqrt(dx * dx + dz * dz);
        if (moved > 0.0005f) { _dirX = dx / moved; _dirZ = dz / moved; }
        if (!Physics.Grounded) return;
        _acc += moved;
        bool sprinting = Input.GetKey("LeftShift");
        if (_acc < (sprinting ? SprintStride : Stride)) return;
        _acc = 0f;
        _left = !_left;
        // the foot: half a shoulder to the side of the path
        float side = _left ? -Width : Width;
        float fx = p.X + (-_dirZ) * side, fz = p.Z + _dirX * side;
        RaycastHit hit;
        if (!Physics.Raycast(new Vector3(fx, p.Y + 0.6f, fz), new Vector3(0f, -1f, 0f), 2.5f, out hit, ~0, EntityId)) return;
        if (TerrainOnly && !Terrain.IsTerrain(hit.EntityId)) return;
        if (RockLayer >= 0 && Terrain.IsTerrain(hit.EntityId) && Terrain.LayerAt(hit.Point.X, hit.Point.Z) == RockLayer) return;
        float yaw = (float)(System.Math.Atan2(_dirX, _dirZ) * 180.0 / System.Math.PI);
        Decal.Spawn(hit.Point, hit.Normal, _left ? LeftMaterial : RightMaterial, new Vector3(Length * 0.5f, 0.25f, Length), Lifetime, yaw,
                    DecalBlend.Multiply, 0.78f, 0.7f, 0.6f, Opacity, 0.5f, 70f, 1);
    }
}
