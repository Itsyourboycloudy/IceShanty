using UnityEngine;

namespace IceShanty.Horror
{
    public sealed partial class BlackwaterRoom
    {
        static readonly Vector3 WellPosition = new Vector3(0,.06f,1.65f);
        static readonly Vector3 CrusherPosition = new Vector3(2.3f,0,2.3f);
        static readonly Vector3 ClockPosition = new Vector3(2.35f,.9f,-2.35f);
        static readonly Vector3 BoxPosition = new Vector3(-3.05f,0,0);
        Transform iceLid, wheel, bat, bucket, chumSurface, heldFish, hand, clockHand, alarmClock;
        readonly Transform[] storedFish = new Transform[HorrorRun.BoxCapacity];
        readonly Transform[] shadows = new Transform[HorrorRun.ViewCount];
        Light flashlight, iceLight, clockLight;
        Material fishMaterial, screamMaterial;
        AudioSource ambience, effects, clockAudio, bear;
        readonly AudioSource[] breaths = new AudioSource[HorrorRun.ViewCount];
        AudioClip click, grind, ding, alarm, knock, scream, footstep, eating;
        Vector3 CrusherBucketPosition => CrusherPosition - Direction(1)*1f - Quaternion.Euler(0,45,0)*Vector3.right*.5f + Vector3.up*.32f;
        Vector3 ClockBucketPosition => ClockPosition + Quaternion.Euler(0,135,0)*new Vector3(.8f,-.58f,-.4f);
        Vector3 FloorPosition(int sector, float side) => Direction(sector)*2.35f + Quaternion.Euler(0,sector*45,0)*Vector3.right*side + Vector3.up*.15f;

        Material MaterialFor(Color color, bool glow = false)
        {
            var shader = Shader.Find(glow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var material = new Material(shader); material.color = color;
            if (!glow)
            {
                var texture = new Texture2D(16,16) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                var data = new Color[256]; for (int i = 0; i < data.Length; i++) data[i] = Color.white*Random.Range(.65f,1);
                texture.SetPixels(data); texture.Apply(); material.mainTexture = texture; material.mainTextureScale = new Vector2(3,3); textures.Add(texture);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.1f);
            }
            materials.Add(material); return material;
        }
        Transform Group(string name, Vector3 position, float yaw = 0, Transform parent = null)
        {
            var o = new GameObject(name).transform; o.SetParent(parent ? parent : root,false); o.localPosition = position; o.localRotation = Quaternion.Euler(0,yaw,0); return o;
        }
        Transform Part(string name, Vector3 position, Vector3 size, Material material, Transform parent = null, PrimitiveType shape = PrimitiveType.Cube)
        {
            var o = GameObject.CreatePrimitive(shape); o.name = name; o.transform.SetParent(parent ? parent : root,false);
            o.transform.localPosition = position; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(o.GetComponent<Collider>()); return o.transform;
        }
        Light Lamp(string name, Vector3 position, Color color, float intensity, float range)
        {
            var light = Group(name,position).gameObject.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; return light;
        }
        void Sign(string text, Vector3 position, float yaw, float size, Color color)
        {
            var label = Group(text,position,yaw).gameObject.AddComponent<TextMesh>(); label.text = text; label.fontSize = 48; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.color = color;
        }
        Transform FishModel(string name, Transform parent = null)
        {
            var fish = Group(name,Vector3.zero,0,parent);
            Part("Body",Vector3.zero,new Vector3(.16f,.14f,.42f),fishMaterial,fish);
            Part("Head",new Vector3(0,0,.23f),new Vector3(.13f,.12f,.16f),fishMaterial,fish);
            Part("Tail",new Vector3(0,0,-.27f),new Vector3(.24f,.055f,.13f),fishMaterial,fish);
            return fish;
        }
        void BuildRoom()
        {
            root = new GameObject("Circular fishing room").transform; root.SetParent(transform,false);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.008f,.012f,.018f);
            RenderSettings.fog = true; RenderSettings.fogColor = Color.black; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .09f;
            var wood = MaterialFor(new Color(.25f,.19f,.13f)); var steel = MaterialFor(new Color(.21f,.26f,.28f));
            var ice = MaterialFor(new Color(.31f,.53f,.6f)); var rust = MaterialFor(new Color(.4f,.16f,.07f)); var dark = MaterialFor(new Color(.008f,.015f,.02f));
            var black = MaterialFor(Color.black,true); var warm = MaterialFor(Amber,true);
            fishMaterial = MaterialFor(new Color(.46f,.7f,.7f)); screamMaterial = MaterialFor(new Color(.65f,.22f,.19f));
            Part("Round ice floor",new Vector3(0,-.2f,0),new Vector3(10,.2f,10),ice,null,PrimitiveType.Cylinder);
            Part("Round ceiling",new Vector3(0,3.2f,0),new Vector3(10,.1f,10),wood,null,PrimitiveType.Cylinder);
            for (int i = 0; i < 24; i++)
            {
                float angle = i*15; var panel = Part("Circular wall",Quaternion.Euler(0,angle,0)*Vector3.forward*4.7f + Vector3.up*1.5f,new Vector3(1.27f,3,.16f),wood);
                panel.rotation = Quaternion.Euler(0,angle,0);
            }
            foreach (int sector in HorrorRun.DarkViews)
            {
                var recess = Group("Unlit recess " + sector,Direction(sector)*4.45f,sector*45);
                Part("Pitch black opening",new Vector3(0,1.35f,0),new Vector3(1.7f,2.7f,.12f),black,recess);
                var shadow = Group("Presence in darkness " + sector,Direction(sector)*4.1f,sector*45);
                Part("Silhouette",new Vector3(0,.85f,0),new Vector3(.38f,1.65f,.25f),dark,shadow);
                Part("Head",new Vector3(0,1.76f,0),new Vector3(.3f,.36f,.25f),dark,shadow);
                Part("Reflected eye",new Vector3(-.07f,1.79f,-.14f),new Vector3(.035f,.025f,.02f),warm,shadow);
                Part("Reflected eye",new Vector3(.07f,1.79f,-.14f),new Vector3(.035f,.025f,.02f),warm,shadow);
                shadow.gameObject.SetActive(false); shadows[sector] = shadow;
            }
            Part("Ice well rim",WellPosition,new Vector3(1.6f,.035f,1.6f),steel,null,PrimitiveType.Cylinder);
            Part("Water",WellPosition+Vector3.up*.05f,new Vector3(1.43f,.015f,1.43f),black,null,PrimitiveType.Cylinder);
            iceLid = Part("Ice cover",WellPosition+Vector3.up*.075f,new Vector3(1.43f,.025f,1.43f),ice,null,PrimitiveType.Cylinder);
            var crusher = Group("Right meat crusher",CrusherPosition,45);
            Part("Rusty body",new Vector3(0,.65f,0),new Vector3(1.05f,1.3f,.85f),rust,crusher);
            Part("Hopper",new Vector3(0,1.35f,0),new Vector3(.85f,.12f,.7f),dark,crusher);
            for (int i = 0; i < 6; i++) Part("Grinding tooth",new Vector3(-.35f+i*.14f,1.43f,0),new Vector3(.07f,.15f,.5f),steel,crusher);
            Part("Outlet",new Vector3(-.35f,.4f,-.55f),new Vector3(.3f,.24f,.45f),steel,crusher);
            wheel = Part("Flywheel",new Vector3(.58f,.85f,0),new Vector3(.5f,.07f,.5f),steel,crusher,PrimitiveType.Cylinder); wheel.localRotation = Quaternion.Euler(0,0,90);
            bucket = Group("Portable chum bucket",CrusherBucketPosition);
            Part("Bucket shell",Vector3.zero,new Vector3(.49f,.28f,.49f),steel,bucket,PrimitiveType.Cylinder);
            Part("Open bucket",new Vector3(0,.285f,0),new Vector3(.43f,.008f,.43f),black,bucket,PrimitiveType.Cylinder);
            chumSurface = Part("Chum",new Vector3(0,.29f,0),new Vector3(.4f,.008f,.4f),rust,bucket,PrimitiveType.Cylinder);
            Part("Bucket handle",new Vector3(0,.49f,0),new Vector3(.52f,.035f,.04f),steel,bucket);
            var box = Group("Left catch box",BoxPosition,270);
            Part("Box bottom",new Vector3(0,.38f,0),new Vector3(1.55f,.16f,1.12f),wood,box);
            foreach (int side in new[] {-1,1})
            {
                Part("Box side",new Vector3(side*.77f,.57f,0),new Vector3(.08f,.44f,1.12f),wood,box);
                Part("Box end",new Vector3(0,.57f,side*.53f),new Vector3(1.55f,.44f,.08f),wood,box);
            }
            for (int i = 0; i < storedFish.Length; i++)
            {
                storedFish[i] = FishModel("Stored catch " + (i+1),box);
                storedFish[i].localPosition = new Vector3(-.57f+(i%4)*.38f,.79f+(i/8)*.13f,-.25f+((i/4)%2)*.5f);
                storedFish[i].localScale = Vector3.one*.7f; storedFish[i].gameObject.SetActive(false);
            }
            var clockTable = Group("Alarm stand",new Vector3(ClockPosition.x,0,ClockPosition.z),135);
            Part("Small table",new Vector3(0,.75f,0),new Vector3(1.05f,.12f,.6f),wood,clockTable);
            Part("Stand",new Vector3(0,.35f,0),new Vector3(.16f,.7f,.18f),steel,clockTable);
            alarmClock = Group("Little alarm clock",ClockPosition,135);
            Part("Clock body",new Vector3(0,.12f,0),new Vector3(.34f,.37f,.16f),rust,alarmClock);
            Part("Clock face",new Vector3(0,.12f,-.09f),new Vector3(.28f,.28f,.015f),warm,alarmClock);
            clockHand = Part("Clock hand",new Vector3(0,.12f,-.11f),new Vector3(.017f,.12f,.01f),black,alarmClock);
            foreach (int side in new[] {-1,1}) Part("Bell",new Vector3(side*.14f,.35f,0),new Vector3(.13f,.08f,.13f),steel,alarmClock,PrimitiveType.Sphere);
            Part("Feeding mat beside clock",ClockBucketPosition-Vector3.up*.29f,new Vector3(.75f,.025f,.75f),rust);
            Part("Bucket collection mat",CrusherBucketPosition-Vector3.up*.29f,new Vector3(.65f,.025f,.65f),steel);
            Sign("KEEP THE BUCKET HERE",ClockPosition+Vector3.up*.65f,135,.027f,Frost);
            iceLight = Lamp("Cold well light",new Vector3(0,2,1.6f),new Color(.3f,.65f,1),3,3.8f);
            Lamp("Crusher red work light",CrusherPosition+Vector3.up*2.2f,new Color(1,.3f,.17f),2.2f,3.5f);
            Lamp("Box light",BoxPosition+Vector3.up*2,new Color(.3f,.58f,.7f),2,2.8f);
            clockLight = Lamp("Clock light",ClockPosition+Vector3.up*.7f,new Color(1,.3f,.15f),1.4f,2.3f);
            eye = Group("Point and click camera",Vector3.up*1.65f).gameObject.AddComponent<Camera>(); eye.gameObject.AddComponent<AudioListener>(); eye.tag = "MainCamera";
            eye.fieldOfView = 65; eye.nearClipPlane = .04f; eye.farClipPlane = 20; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = Color.black;
            pixels = new RenderTexture(640,360,24) { filterMode = FilterMode.Point, antiAliasing = 1 }; pixels.Create(); eye.targetTexture = pixels;
            flashlight = Group("Flashlight beam",Vector3.zero,0,eye.transform).gameObject.AddComponent<Light>();
            flashlight.type = LightType.Spot; flashlight.spotAngle = 48; flashlight.innerSpotAngle = 30; flashlight.range = 10; flashlight.intensity = 6; flashlight.color = new Color(.8f,.88f,1); flashlight.enabled = false;
            hand = Group("Holding hand",new Vector3(.38f,-.37f,.7f),0,eye.transform);
            Part("Glove",Vector3.zero,new Vector3(.16f,.14f,.19f),wood,hand);
            Part("Sleeve",new Vector3(.04f,-.13f,-.13f),new Vector3(.19f,.24f,.25f),steel,hand);
            bat = Group("Pickup baseball bat",FloorPosition(1,.65f));
            Part("Bat barrel",new Vector3(0,.2f,0),new Vector3(.085f,.34f,.085f),wood,bat,PrimitiveType.Cylinder);
            Part("Bat grip",new Vector3(0,-.25f,0),new Vector3(.045f,.2f,.045f),wood,bat,PrimitiveType.Cylinder);
            heldFish = FishModel("Fish in hand",eye.transform); heldFish.localPosition = new Vector3(.27f,-.28f,.7f); heldFish.localRotation = Quaternion.Euler(0,90,0);
        }
        void PoseItem(Transform item, bool held, Vector3 worldPosition, Vector3 heldPosition, Quaternion rotation)
        {
            var parent = held ? eye.transform : root;
            if (item.parent != parent) item.SetParent(parent,false);
            item.localPosition = held ? heldPosition : worldPosition; item.localRotation = rotation;
        }
        void UpdateWorld(float dt)
        {
            iceLid.gameObject.SetActive(!Run.HoleOpen);
            if (Run.Crushing) wheel.Rotate(0,dt*550,0,Space.Self);
            bool batHeld = Run.Hand == HorrorRun.HandItem.Bat;
            PoseItem(bat,batHeld,FloorPosition(Mathf.Max(0,Run.BatView),.65f),new Vector3(.4f,-.23f,.65f),batHeld ? Quaternion.Euler(-15,0,-25-Mathf.Sin(swing/.5f*Mathf.PI)*85) : Quaternion.Euler(85,Run.BatView*45,25));
            var bucketPosition = Run.Bucket == HorrorRun.BucketPlace.Clock ? ClockBucketPosition : Run.Bucket == HorrorRun.BucketPlace.Crusher ? CrusherBucketPosition : FloorPosition(Run.BucketFloorView,-.55f)+Vector3.up*.17f;
            PoseItem(bucket,Run.Hand == HorrorRun.HandItem.Bucket,bucketPosition,new Vector3(.3f,-.54f,.85f),Quaternion.identity);
            chumSurface.gameObject.SetActive(Run.Chum > 0);
            heldFish.gameObject.SetActive(Run.Hand == HorrorRun.HandItem.Fish);
            hand.gameObject.SetActive(Run.Hand != HorrorRun.HandItem.Empty);
            if (Run.HeldFish != null) ColorFish(heldFish,Run.HeldFish.Screaming);
            for (int i = 0; i < storedFish.Length; i++)
            {
                storedFish[i].gameObject.SetActive(i < Run.Inventory.Count);
                if (i < Run.Inventory.Count) ColorFish(storedFish[i],Run.Inventory[i].Screaming);
            }
            clockHand.localRotation = Quaternion.Euler(0,0,-clock*6);
            alarmClock.localRotation = Quaternion.Euler(0,135,Run.Alarm ? Mathf.Sin(clock*35)*7 : 0);
            clockLight.intensity = Run.Alarm ? 2+Mathf.Sin(clock*14)*.4f : 1.4f;
            flashlight.enabled = FlashlightOn;
            foreach (int sector in HorrorRun.DarkViews)
            {
                shadows[sector].gameObject.SetActive(FlashlightOn && view == sector && Run.Darkness[sector] > .12f);
                shadows[sector].position = Direction(sector)*Mathf.Lerp(4.2f,1.4f,Run.Darkness[sector]);
            }
        }
        void ColorFish(Transform fish, bool screaming)
        {
            foreach (var renderer in fish.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = screaming ? screamMaterial : fishMaterial;
        }
        AudioClip Tone(string name, float duration, float frequency, float noise, bool pulse = false)
        {
            const int rate = 22050; var data = new float[Mathf.CeilToInt(duration*rate)]; uint state = 1234567;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i/rate; state = state*1664525+1013904223; float random = state/(float)uint.MaxValue*2-1;
                float envelope = Mathf.Min(t*20,1)*Mathf.Min((duration-t)*15,1);
                data[i] = (Mathf.Sin(t*frequency*6.28318f+Mathf.Sin(t*7)*2)*(1-noise)+random*noise)*envelope*.35f*(pulse ? .55f+.45f*Mathf.Sin(t*23) : 1);
            }
            var clip = AudioClip.Create(name,data.Length,1,rate,false); clip.SetData(data,0); clips.Add(clip); return clip;
        }
        AudioSource Source(string name, Vector3 position, AudioClip clip, bool loop, float spatial)
        {
            var audio = Group(name,position).gameObject.AddComponent<AudioSource>(); audio.clip = clip; audio.loop = loop; audio.spatialBlend = spatial;
            audio.minDistance = 1; audio.maxDistance = 12; audio.rolloffMode = AudioRolloffMode.Linear; audio.volume = 0; if (loop) audio.Play(); return audio;
        }
        void BuildAudio()
        {
            click = Tone("Reel ratchet",.045f,1200,.65f); grind = Tone("Crusher",3.5f,63,.7f,true);
            ding = Tone("Clock warning ding",.4f,1700,.05f); alarm = Tone("Clock alarm",.65f,1450,.12f,true);
            knock = Tone("Bat impact",.22f,95,.7f); scream = Tone("Caught choir eel",1.6f,650,.3f,true);
            footstep = Tone("Heavy footsteps",2.3f,36,.28f,true); eating = Tone("Eating beyond the clock",2,70,.65f,true);
            ambience = Source("Under ice",Vector3.zero,Tone("Ice breathing",6,42,.22f,true),true,0);
            foreach (int sector in HorrorRun.DarkViews) breaths[sector] = Source("Darkness breath "+sector,Direction(sector)*4,Tone("Breathing",3,85,.5f,true),true,1);
            effects = Source("Room effects",Vector3.zero,null,false,0); effects.volume = volume;
            clockAudio = Source("The little clock",ClockPosition,null,false,.65f); clockAudio.volume = volume;
            bear = Source("Outside the wall by the clock",Direction(3)*5,null,false,1); bear.volume = volume;
        }
        void Sound(AudioClip clip, float amount) { effects.PlayOneShot(clip,amount); }
        void UpdateAudio()
        {
            bool live = mode == Mode.Playing;
            ambience.volume = mode == Mode.Paused ? 0 : volume*.12f;
            foreach (int sector in HorrorRun.DarkViews) breaths[sector].volume = live ? volume*Run.Darkness[sector]*.8f : 0;
            effects.volume = clockAudio.volume = bear.volume = live ? volume : 0;
        }
    }
}
