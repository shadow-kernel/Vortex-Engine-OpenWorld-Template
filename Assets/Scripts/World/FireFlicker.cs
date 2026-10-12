using Vortex;

// A living fire light: the Light on this entity breathes around its authored intensity — two detuned flickers, a slow
// swell and now and then a short dip, with the colour shifting a touch towards red when it burns low. Put it on the
// point light next to a campfire, a torch or a brazier (the Open World's cave has one at its far end).
public class FireFlicker : VortexBehaviour
{
    public float Amount = 0.35f;     // how far the intensity swings (0 = steady, 1 = from dark to double)
    public float Speed = 9f;         // the flicker's pace

    private Light _light;
    private float _base, _t, _dip, _seed;
    private System.Random _rng;

    public override void Start()
    {
        _light = GetLight();
        if (_light != null) _base = _light.Intensity;
        _seed = (EntityId % 97) * 0.37f;   // two fires never flicker in step
        _rng = new System.Random(unchecked((int)EntityId));
    }

    public override void Update(float dt)
    {
        if (_light == null) return;
        _t += dt;
        float f = Light.Flicker(_t + _seed, Speed) * 0.6f + Light.Flicker(_t * 0.37f + _seed * 2.1f, Speed * 0.5f) * 0.4f;
        float swell = 0.5f + 0.5f * (float)System.Math.Sin(_t * 0.7f + _seed);
        if (_dip <= 0f && _rng.NextDouble() < dt * 0.25f) _dip = 0.18f;   // a gust: the flame ducks for a moment
        float duck = _dip > 0f ? 0.55f : 1f;
        _dip -= dt;
        float k = (1f - Amount) + Amount * (0.6f * f + 0.4f * swell) * 2f;
        _light.Intensity = _base * k * duck;
        float warm = 0.42f + 0.12f * f;   // lower flame, redder light
        _light.SetColor(1f, warm, 0.18f);
    }
}
