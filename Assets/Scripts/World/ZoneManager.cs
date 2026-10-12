using Vortex;

// Zones (#areas): one script that hears EVERY entity entering or leaving any Area of the scene — the place for the rules
// that are not one entity's business: no flying in a cave, a different music in the village, a safe zone's immunity.
// Attach it to any entity (the template's "Zone Manager"). An entity's own behaviour gets the same as OnAreaEnter /
// OnAreaStay / OnAreaExit; anyone can ask Areas.IsInside(id, "cave") at any time.
public class ZoneManager : VortexBehaviour
{
    public bool LogEvents = true;   // the console shows "entered Grove [grove, no-fly]" — turn off when it gets noisy

    public override void Start()
    {
        Areas.Entered += OnEntered;
        Areas.Exited += OnExited;
        Debug.Log("ZoneManager: " + Areas.Count + " area(s) in the scene");
    }

    public override void OnDestroy()
    {
        Areas.Entered -= OnEntered;
        Areas.Exited -= OnExited;
    }

    private void OnEntered(long entity, AreaInfo area)
    {
        if (LogEvents) Debug.Log(Scene.NameOf(entity) + " entered " + area + (area.GetBool("flightAllowed", true) ? "" : " — no flying here"));
        // e.g.: if (area.HasTag("cave")) Music.Play("cave_drips"); if (!area.GetBool("flightAllowed", true)) Player.CanFly = false;
    }

    private void OnExited(long entity, AreaInfo area)
    {
        if (LogEvents) Debug.Log(Scene.NameOf(entity) + " left " + area);
        // e.g.: if (!Areas.IsInside(entity, "cave")) Player.CanFly = true;
    }
}
