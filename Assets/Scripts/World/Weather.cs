using Vortex;
// THE WEATHER — the air of the open world, blended between presets at runtime: the fog and its height profile, the sun's
// strength and colour, the sky's exposure, the ambient light, the wind every plant sways in, the dust and sand effects
// that travel with the player, and the wind you hear. O steps to the next preset, L to the previous one; a script or a
// trigger can call Set("sandstorm") itself. The editor applies the same presets through Claude's set_weather tool.
//
//   clear      a hot, clear desert day with a thin warm haze far out
//   haze       dust in the air, the sun softer, a steady breeze, dust drifting past
//   sandstorm  the sky brown, the sun a disc, visibility 80 m, sand sheets racing past, a storm in the palms
//   dusk       low orange sun, long shadows, the air cooling
//
// Everything is a project script — the engine renders fog, sky and particles; the game decides the weather.
public class Weather : VortexBehaviour
{
    public string StartPreset = "clear";
    public float  TransitionSeconds = 12f;
    public string SkyPath   = "Assets/Skies/goegap_2k.hdr";
    public string SunName   = "Sun";
    public string HazeName  = "Weather Haze";   // ParticleSystem entities under the player (the template places them)
    public string SandName  = "Weather Sand";
    public string WindClip  = "Assets/Audio/wind_desert_loop.wav";
    public float  WindVolume = 0.55f;
    public string NextKey = "O";
    public string PrevKey = "L";

    // ---- the presets (keep in step with the editor's set_weather tool) ----
    private struct Preset
    {
        public string Name;
        public float FogDensity, FogHeightY, FogFalloff, FogR, FogG, FogB;
        public bool FogFromSky;   // the haze is the sky's own horizon colour, FogR/G/B a tint on it (Atmosphere.SetFog fromSky)
        public float SunIntensity, SunR, SunG, SunB;
        public float SkyExposure, Ambient, Wind;
        public float SceneExposure, FogSunGlow;   // stops before the tonemapper; the sun's glow in the fog
        public bool Haze, Storm;
        public string Sky; public float SunPitch, SunYaw;   // the preset's HDRI and where its sun is (tools/hdri-analyze.py)
        public float TopR, TopG, TopB, HorR, HorG, HorB, BotR, BotG, BotB;   // or a gradient sky (a sandstorm has no blue sky)
    }
    private static readonly Preset[] Presets =
    {
        new Preset { Name = "clear",     FogDensity = 0.00015f, FogHeightY = 0f, FogFalloff = 0f, FogR = 1f, FogG = 1f, FogB = 1f, FogFromSky = true, SunIntensity = 8f, SunR = 1f, SunG = 0.84f, SunB = 0.62f, SkyExposure = 0.4f, Ambient = 0.55f, Wind = 0.5f, SceneExposure = 1.7f, FogSunGlow = 0.2f, Sky = "Assets/Skies/syferfontein_18d_clear_puresky_2k.hdr", SunPitch = 18.19f, SunYaw = -144.05f },
        new Preset { Name = "noon",      FogDensity = 0.0011f, FogHeightY = -4f, FogFalloff = 0.012f, FogR = 1f, FogG = 1f, FogB = 1f, FogFromSky = true, SunIntensity = 5.5f, SunR = 1f, SunG = 0.93f, SunB = 0.82f, SkyExposure = 1.0f, Ambient = 1.0f, Wind = 0.5f, SceneExposure = 1.7f, FogSunGlow = 0.45f, Sky = "Assets/Skies/goegap_2k.hdr", SunPitch = 46.32f, SunYaw = -141.24f },
        new Preset { Name = "haze",      FogDensity = 0.0032f, FogHeightY = 2f,  FogFalloff = 0.02f,  FogR = 0.46f, FogG = 0.44f, FogB = 0.42f, SunIntensity = 4.6f, SunR = 1f, SunG = 0.88f, SunB = 0.72f, SkyExposure = 0.9f, Ambient = 0.95f,  Wind = 1.1f, SceneExposure = 1.5f, FogSunGlow = 0.8f, Haze = true, Sky = "Assets/Skies/goegap_2k.hdr", SunPitch = 46.32f, SunYaw = -141.24f },
        new Preset { Name = "sandstorm", FogDensity = 0.012f,  FogHeightY = 12f, FogFalloff = 0.012f, FogR = 0.42f, FogG = 0.32f, FogB = 0.20f, SunIntensity = 2.4f, SunR = 1f, SunG = 0.74f, SunB = 0.48f, SkyExposure = 0.6f,  Ambient = 1.0f,  Wind = 2.6f, SceneExposure = 1.2f, FogSunGlow = 0.6f, Haze = true, Storm = true, SunPitch = 46.32f, SunYaw = -141.24f, TopR = 0.62f, TopG = 0.50f, TopB = 0.36f, HorR = 0.80f, HorG = 0.64f, HorB = 0.44f, BotR = 0.66f, BotG = 0.50f, BotB = 0.34f },
        new Preset { Name = "dusk",      FogDensity = 0.0022f, FogHeightY = -2f, FogFalloff = 0.025f, FogR = 1f, FogG = 1f, FogB = 1f, FogFromSky = true, SunIntensity = 3.6f, SunR = 1f, SunG = 0.64f, SunB = 0.38f, SkyExposure = 1.0f,  Ambient = 0.9f,  Wind = 0.4f, SceneExposure = 1.3f, FogSunGlow = 1.6f, Sky = "Assets/Skies/rogland_sunset_2k.hdr", SunPitch = 12.57f, SunYaw = -144.05f },
    };

    private int _from, _to; private float _t = 1f;
    private long _sun, _haze, _sand;
    private bool _hazeOn, _sandOn;
    private float _lastExposure = -1f;
    private string _lastSky = "";
    private AudioSource _wind;

    public string Current { get { return Presets[_to].Name; } }

    public override void Start()
    {
        _sun = Scene.Find(SunName);
        _haze = Scene.Find(HazeName);
        _sand = Scene.Find(SandName);
        _wind = Scene.GetAudioSource(EntityId);
        if (_wind != null && !string.IsNullOrEmpty(WindClip))
        {
            _wind.Clip = WindClip; _wind.Loop = true; _wind.Volume = 0f; _wind.Play();
        }
        int start = IndexOf(StartPreset); if (start < 0) start = 0;
        _from = _to = start; _t = 1f;
        Apply(Presets[_to], Presets[_to], 1f, true);
        Debug.Log("Weather: " + Presets[_to].Name + " — O next preset, L previous");
    }

    public override void Update(float dt)
    {
        if (Input.GetKeyDown(NextKey)) Set(Presets[(_to + 1) % Presets.Length].Name);
        if (Input.GetKeyDown(PrevKey)) Set(Presets[(_to + Presets.Length - 1) % Presets.Length].Name);
        if (_t < 1f)
        {
            _t = System.Math.Min(1f, _t + dt / System.Math.Max(0.1f, TransitionSeconds));
            float s = _t * _t * (3f - 2f * _t);
            Apply(Presets[_from], Presets[_to], s, false);
        }
    }

    /// <summary>Blend to a preset by name over TransitionSeconds (a trigger volume, a timer, a quest can call this).</summary>
    public void Set(string name)
    {
        int i = IndexOf(name);
        if (i < 0 || i == _to) return;
        _from = _to; _to = i; _t = 0f;
        Debug.Log("Weather: " + Presets[_to].Name);
    }

    private static int IndexOf(string name)
    {
        for (int i = 0; i < Presets.Length; i++) if (string.Equals(Presets[i].Name, name, System.StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private static float L(float a, float b, float s) { return a + (b - a) * s; }

    /// <summary>A preset's haze colour as the renderer would show it: the sky's horizon times the tint when it follows the sky.</summary>
    private static void FogColorOf(Preset p, out float r, out float g, out float b)
    {
        float hr, hg, hb;
        if (p.FogFromSky && Atmosphere.SkyHorizon(out hr, out hg, out hb)) { r = hr * p.FogR; g = hg * p.FogG; b = hb * p.FogB; }
        else { r = p.FogR; g = p.FogG; b = p.FogB; }
    }

    private void Apply(Preset a, Preset b, float s, bool force)
    {
        // the haze: at rest a sky-following preset keeps tracking its sky; in between, both presets' colours are resolved
        // against the current sky and blended, so clear (the sky's horizon) crossfades into a sandstorm's brown without a jump
        Preset fogOf = s >= 1f ? b : a;
        if ((s <= 0f || s >= 1f) && fogOf.FogFromSky)
            Atmosphere.SetFog(L(a.FogDensity, b.FogDensity, s), L(a.FogHeightY, b.FogHeightY, s), L(a.FogFalloff, b.FogFalloff, s), fogOf.FogR, fogOf.FogG, fogOf.FogB, fromSky: true);
        else
        {
            float ar, ag, ab, br, bg, bb; FogColorOf(a, out ar, out ag, out ab); FogColorOf(b, out br, out bg, out bb);
            Atmosphere.SetFog(L(a.FogDensity, b.FogDensity, s), L(a.FogHeightY, b.FogHeightY, s), L(a.FogFalloff, b.FogFalloff, s), L(ar, br, s), L(ag, bg, s), L(ab, bb, s));
        }
        Atmosphere.SetFogSunGlow(L(a.FogSunGlow, b.FogSunGlow, s));
        Atmosphere.SetExposure(L(a.SceneExposure, b.SceneExposure, s));
        if (_sun > 0)
        {
            var light = Scene.GetLight(_sun);
            if (light != null) { light.Intensity = L(a.SunIntensity, b.SunIntensity, s); light.SetColor(L(a.SunR, b.SunR, s), L(a.SunG, b.SunG, s), L(a.SunB, b.SunB, s)); }
            Scene.SetRotationOf(_sun, new Vector3(L(a.SunPitch, b.SunPitch, s), L(a.SunYaw, b.SunYaw, s), 0f));   // the sun moves with the preset's sky
        }
        // the sky: the preset's own HDRI (a sunset for dusk) or its gradient (the sandstorm's brown sky) from the middle of
        // the transition, the exposure blended
        Preset skyOf = s < 0.5f ? a : b;
        string sky = string.IsNullOrEmpty(skyOf.Sky) ? (skyOf.TopR > 0f ? "gradient:" + skyOf.Name : SkyPath) : skyOf.Sky;
        float exposure = L(a.SkyExposure, b.SkyExposure, s);
        if (force || sky != _lastSky || System.Math.Abs(exposure - _lastExposure) > 0.004f)
        {
            _lastExposure = exposure; _lastSky = sky;
            if (sky.StartsWith("gradient:")) Sky.SetGradient(skyOf.TopR * exposure, skyOf.TopG * exposure, skyOf.TopB * exposure, skyOf.HorR * exposure, skyOf.HorG * exposure, skyOf.HorB * exposure, skyOf.BotR * exposure, skyOf.BotG * exposure, skyOf.BotB * exposure);
            else if (!string.IsNullOrEmpty(sky)) Sky.SetTexture(sky, exposure);
        }
        Sky.SetAmbient(L(a.Ambient, b.Ambient, s));
        float wind = L(a.Wind, b.Wind, s);
        Foliage.SetWind(wind);
        if (_wind != null) _wind.Volume = WindVolume * System.Math.Max(0f, System.Math.Min(1f, (wind - 0.4f) / 2.2f));
        // the dust: start when the target asks for it, let it die out when it no longer does (at Start the state is forced)
        bool wantHaze = s < 0.5f ? a.Haze : b.Haze, wantSand = s < 0.5f ? a.Storm : b.Storm;
        if (_haze > 0 && (force || wantHaze != _hazeOn)) { _hazeOn = wantHaze; if (wantHaze) Vfx.Play(_haze); else Vfx.Stop(_haze, force); }
        if (_sand > 0 && (force || wantSand != _sandOn)) { _sandOn = wantSand; if (wantSand) Vfx.Play(_sand); else Vfx.Stop(_sand, force); }
    }
}
