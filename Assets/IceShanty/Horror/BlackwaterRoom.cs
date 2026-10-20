using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IceShanty.Horror
{
    public sealed partial class BlackwaterRoom : MonoBehaviour
    {
        public HorrorRun Run { get; private set; }
        enum Mode { Title, Playing, Paused, Shop, End }
        enum Activity { Idle, Drilling, Waiting, Fishing }
        Mode mode;
        Activity activity;
        Camera eye;
        RenderTexture pixels;
        Transform root;
        readonly List<Material> materials = new List<Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        int view, bank, legacy, best, nightSale;
        float turnTime = 1, actionTime, catchProgress, fishAngle, fishTime, clickIn, alarmIn, screamIn, swing, flash, clock, noticeTime;
        Vector3 turnPosition;
        Quaternion turnRotation;
        bool flashlightToggle, showHelp, smokeMode;
        float volume = .65f;
        string notice;
        HorrorRun.Fish incoming;
        static readonly string[] ViewNames = { "ICE WELL", "CRUSHER / BAT", "RIGHT DARKNESS", "ALARM CLOCK", "BEHIND YOU", "BACK LEFT DARKNESS", "CATCH BOX", "LEFT DARKNESS" };
        static readonly Color Ink = new Color(.025f,.045f,.055f,.96f), Frost = new Color(.65f,.85f,.86f), Amber = new Color(.94f,.66f,.31f), Danger = new Color(.96f,.29f,.23f);
        bool Turning => turnTime < 1;
        bool CanTouch => mode == Mode.Playing && !Turning && activity == Activity.Idle && !showHelp;
        bool FlashlightOn => mode == Mode.Playing && !Turning && activity != Activity.Fishing && (flashlightToggle || (Mouse.current != null && Mouse.current.rightButton.isPressed));

        void Awake()
        {
            bank = PlayerPrefs.GetInt("Blackwater.Echoes", 0); legacy = PlayerPrefs.GetInt("Blackwater.Insulation", 0); best = PlayerPrefs.GetInt("Blackwater.Best", 0);
            smokeMode = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-blackwaterSmoke") >= 0;
            BuildRoom(); BuildAudio(); Run = new HorrorRun();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            SetView(0, true); UpdateWorld(0);
            if (smokeMode) StartCoroutine(SmokeCapture());
        }
        void StartNight(bool continueRun = false)
        {
            Run = continueRun ? Run.NextNight() : new HorrorRun { Cash = legacy * 10 };
            if (Run == null) Run = new HorrorRun();
            mode = Mode.Playing; activity = Activity.Idle;
            flashlightToggle = showHelp = false; swing = flash = alarmIn = screamIn = 0;
            SetView(0, true); UpdateWorld(0);
            Say("Click the ice to drill. Arrows turn around the room. Catch box left; crusher and bat right.", 12);
        }
        void Say(string message, float seconds = 6) { notice = message; noticeTime = seconds; }
        static Vector3 Direction(int sector) => Quaternion.Euler(0,sector*45,0) * Vector3.forward;
        Vector3 ViewTarget(int sector)
        {
            if (sector == HorrorRun.WellView) return WellPosition;
            if (sector == HorrorRun.CrusherView) return CrusherPosition + Vector3.up * .7f;
            if (sector == HorrorRun.ClockView) return ClockPosition + Vector3.up * .2f;
            if (sector == HorrorRun.BoxView) return BoxPosition + Vector3.up * .45f;
            return Direction(sector) * 4.5f + Vector3.up * 1.35f;
        }
        void SetView(int sector, bool immediate = false)
        {
            view = sector; turnPosition = eye.transform.position; turnRotation = eye.transform.rotation; turnTime = immediate ? 1 : 0;
            if (immediate) { eye.transform.position = Direction(view)*.3f + Vector3.up*1.65f; eye.transform.LookAt(ViewTarget(view)); }
        }
        void Turn(int direction)
        {
            if (mode != Mode.Playing || Turning || showHelp) return;
            if (activity == Activity.Drilling) { Say("Finish drilling before turning."); return; }
            CancelFishing(); flashlightToggle = false; SetView(HorrorRun.WrapView(view,direction));
        }
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime,.1f); clock += dt;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (mode == Mode.Playing) { mode = Mode.Paused; showHelp = false; }
                else if (mode == Mode.Paused) mode = Mode.Playing;
            }
            if (mode != Mode.Playing) { UpdateAudio(); flashlight.enabled = false; return; }
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Turn(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Turn(1);
                if (keyboard.fKey.wasPressedThisFrame) flashlightToggle = !flashlightToggle;
                if (keyboard.hKey.wasPressedThisFrame) showHelp = !showHelp;
                if (keyboard.qKey.wasPressedThisFrame && CanTouch) DropHeld();
                if (keyboard.xKey.wasPressedThisFrame) CancelFishing();
            }
            if (Turning)
            {
                turnTime = Mathf.Min(1,turnTime + dt/.45f); float t = Mathf.SmoothStep(0,1,turnTime);
                var destination = Direction(view)*.3f + Vector3.up*1.65f;
                eye.transform.position = Vector3.Lerp(turnPosition,destination,t);
                eye.transform.rotation = Quaternion.Slerp(turnRotation,Quaternion.LookRotation(ViewTarget(view)-destination),t);
            }
            int oldDings = Run.WarningDings, oldAlarms = Run.AlarmCount, oldFeeds = Run.Feeds, oldAnger = Run.Anger, oldWounds = Run.Wounds;
            bool grinding = Run.Crushing;
            Run.Tick(dt); Run.TickDarkness(dt,view,FlashlightOn);
            if (Run.WarningDings > oldDings) { clockAudio.PlayOneShot(ding,.45f); Say("A little ding from the clock. Feeding time is close.",3); }
            if (Run.AlarmCount > oldAlarms) { clockAudio.PlayOneShot(alarm,.8f); bear.PlayOneShot(footstep,.7f); Say("The clock is ringing. Leave the chum bucket beside it!",5); }
            if (Run.Feeds > oldFeeds) { bear.PlayOneShot(eating,.65f); Say("Heavy breathing outside. It eats from the bucket, then retreats.",5); }
            if (Run.Anger > oldAnger) { bear.PlayOneShot(footstep,1); Say("It found no meal. One more missed feeding and it comes inside.",7); }
            if (Run.Wounds > oldWounds) { flash = .6f; Sound(knock,.8f); Say("Something reached out of the darkness. Check the other views with your flashlight.",6); }
            if (grinding && !Run.Crushing) Say("Chum ready. Pick up the bucket and place it beside the clock.");
            noticeTime -= dt; swing = Mathf.Max(0,swing-dt); flash = Mathf.Max(0,flash-dt);
            if (Run.Ended) { Finish(); UpdateWorld(dt); UpdateAudio(); return; }
            UpdateFishing(dt);
            if (Run.Alarm) { alarmIn -= dt; if (alarmIn <= 0) { clockAudio.PlayOneShot(alarm,.6f); alarmIn = .9f; } }
            if (Run.Screams > 0) { screamIn -= dt; if (screamIn <= 0) { Sound(scream,.5f); screamIn = 2.2f; } }
            UpdateWorld(dt); UpdateAudio();
        }
        void UseWell()
        {
            if (!CanTouch) return;
            if (!Run.CanFish) { Say("Your hands must be empty. Put the fish away, or press Q to drop your tool."); return; }
            if (!Run.HoleOpen) { activity = Activity.Drilling; actionTime = 3; Sound(grind,.6f); return; }
            float roll = Random.value; bool blood = Run.Upgrades.Contains(3);
            incoming = roll < (blood ? .35f : .17f) ? new HorrorRun.Fish("Choir eel",85,3,1.5f,true) : roll < .4f ? new HorrorRun.Fish("Glass sturgeon",70,3,1.25f) : roll < .7f ? new HorrorRun.Fish("Blind char",42,2,.95f) : new HorrorRun.Fish("Frost perch",26,2,.7f);
            activity = Activity.Waiting; actionTime = Random.Range(2.5f,6f)*(blood ? .55f : 1); Run.AddNoise(4);
            Say("Line in. Turning away cuts the line.",4);
        }
        void CancelFishing()
        {
            if (activity != Activity.Waiting && activity != Activity.Fishing) return;
            activity = Activity.Idle; incoming = null; Run.AddNoise(3); Say("You pulled the line back. No catch.",3);
        }
        Vector2 FishPoint() => new Vector2(640 + Mathf.Cos(fishAngle)*126,330 + Mathf.Sin(fishAngle)*126);
        void UpdateFishing(float dt)
        {
            if (activity == Activity.Drilling)
            {
                actionTime -= dt; Run.AddNoise(dt*9);
                if (actionTime <= 0) { Run.HoleOpen = true; activity = Activity.Idle; Say("Open water. Click the hole to cast."); }
            }
            if (activity == Activity.Waiting)
            {
                actionTime -= dt;
                if (actionTime <= 0) { activity = Activity.Fishing; catchProgress = .25f; fishTime = 0; fishAngle = Random.Range(0,6.28f); Sound(click,.5f); }
            }
            if (activity != Activity.Fishing) return;
            fishTime += dt;
            float pattern = incoming.Screaming ? Mathf.Cos(fishTime*1.4f)*1.8f : 1+.45f*Mathf.Sin(fishTime*2.7f);
            fishAngle += dt*incoming.Speed*pattern;
            var mouse = Mouse.current; Vector2 cursor = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            cursor = new Vector2(cursor.x/Screen.width*1280,(1-cursor.y/Screen.height)*720);
            bool contact = !showHelp && Vector2.Distance(cursor,FishPoint()) < (Run.Upgrades.Contains(0) ? 37 : 29);
            bool fast = mouse != null && mouse.leftButton.isPressed;
            catchProgress += dt*(contact ? Run.CatchRate*(fast ? 1.6f : 1) : -(Run.Upgrades.Contains(2) ? .045f : .09f));
            if (contact)
            {
                Run.AddNoise(dt*Run.ReelNoise*(fast ? 2 : 1)); clickIn -= dt;
                if (clickIn <= 0) { Sound(click,fast ? .45f : .2f); clickIn = fast ? .07f : .17f; }
            }
            if (catchProgress >= 1)
            {
                Run.LandFish(incoming); activity = Activity.Idle;
                Say(incoming.Screaming ? "A screaming fish in your hand. Box it, pick up the bat on the right, then click the fish to silence it." : "You are holding "+incoming.Name+". Put it in the left box or the right crusher.",10);
            }
            else if (catchProgress <= 0 || fishTime > 25) { activity = Activity.Idle; Say("The fish slipped away.",4); }
        }
        void UseCrusher()
        {
            if (!CanTouch) return;
            if (Run.Hand == HorrorRun.HandItem.Bucket)
            { Say(Run.PlaceBucket(view) ? "Bucket under the crusher. Ready to collect chum." : "You cannot put that here."); return; }
            if (Run.BeginCrush()) { Sound(grind,.8f); Say("Grinding. The sound carries into the dark."); }
            else Say(Run.Bucket != HorrorRun.BucketPlace.Crusher ? "Put the bucket beneath the crusher first." : Run.Crushing ? "The grinder is still running." : Run.Hand != HorrorRun.HandItem.Fish ? "Hold a caught fish, then click the crusher." : "The bucket is too full. Leave it by the clock for the bear.");
        }
        void UseBox()
        {
            if (!CanTouch) return;
            Say(Run.StoreFish() ? "Fish stored. Its value is paid after the night." : Run.Hand == HorrorRun.HandItem.Fish ? "The box is full." : "Click a fish to pick it up. With the bat held, click a fish to hit it.");
        }
        void UseFish(int index)
        {
            if (!CanTouch) return;
            if (Run.Hand == HorrorRun.HandItem.Bat)
            {
                if (swing > 0) return;
                if (Run.HitFish(index)) { swing = .5f; Sound(knock,.7f); Say("The catch is silent. Press Q to put the bat down.",4); }
            }
            else Say(Run.TakeFish(index) ? "Fish in hand. Take it to the crusher, or put it back." : "Your hand is full.");
        }
        void PickBat()
        {
            if (CanTouch) Say(Run.PickUpBat(view) ? "Bat in hand. Click a caught fish in the box to hit it. Q drops the bat." : "Put the fish or bucket down first.");
        }
        void PickBucket()
        {
            if (CanTouch) Say(Run.PickUpBucket(view) ? "Bucket in hand. Click the marked space beside the clock to leave it there." : Run.Crushing ? "Wait for the grinder to finish." : "Your hand must be empty.");
        }
        void PlaceAtClock()
        {
            if (CanTouch) Say(Run.PlaceBucket(HorrorRun.ClockView) ? "Bucket beside the clock. It will eat when the alarm rings." : "Carry the chum bucket here, then click this space.");
        }
        void DropHeld()
        {
            Say(Run.Drop(view) ? "Set down on the floor. Click it here to pick it up again." : "Put caught fish in the box or crusher. Q drops the bat or bucket.");
        }
        void Finish()
        {
            activity = Activity.Idle; flashlightToggle = false;
            nightSale = Run.SettleBox(); mode = Run.Won ? Mode.Shop : Mode.End;
            if (!smokeMode)
            {
                bank += Run.Caught/3 + Run.Feeds*2 + (Run.Won ? 8 : 0);
                if (Run.Won) best = Mathf.Max(best,Run.Shift);
                PlayerPrefs.SetInt("Blackwater.Echoes",bank); PlayerPrefs.SetInt("Blackwater.Best",best); PlayerPrefs.Save();
            }
        }
        IEnumerator SmokeCapture()
        {
            StartNight(); SetView(HorrorRun.CrusherView,true);
            for (int i = 0; i < 8; i++) yield return null;
            var previous = RenderTexture.active; RenderTexture.active = pixels;
            var shot = new Texture2D(pixels.width,pixels.height,TextureFormat.RGB24,false);
            shot.ReadPixels(new Rect(0,0,pixels.width,pixels.height),0,0); shot.Apply();
            System.IO.Directory.CreateDirectory("Artifacts"); System.IO.File.WriteAllBytes("Artifacts/game-circle-room.png",shot.EncodeToPNG());
            Destroy(shot); RenderTexture.active = previous; Debug.Log("BLACKWATER_RUNTIME_SMOKE_PASSED");
        }
        void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (pixels) { pixels.Release(); Destroy(pixels); }
            foreach (var m in materials) Destroy(m); foreach (var t in textures) Destroy(t); foreach (var clip in clips) Destroy(clip);
        }
    }
}
