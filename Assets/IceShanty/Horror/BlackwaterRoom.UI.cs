using UnityEngine;

namespace IceShanty.Horror
{
    public sealed partial class BlackwaterRoom
    {
        GUIStyle small, body, heading, title, button, arrow;
        string hoverHint;
        void Styles()
        {
            if (body != null) return;
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true }; body.normal.textColor = Frost;
            small = new GUIStyle(body) { fontSize = 13 }; heading = new GUIStyle(body) { fontSize = 27, fontStyle = FontStyle.Bold };
            title = new GUIStyle(heading) { fontSize = 58 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14,12,8,8), wordWrap = true }; button.normal.textColor = Frost;
            arrow = new GUIStyle(button) { fontSize = 44, alignment = TextAnchor.MiddleCenter };
        }
        void Box(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = Color.white; }
        void Text(string text, float x, float y, float width = 400, float height = 30, GUIStyle style = null) { GUI.Label(new Rect(x,y,width,height),text,style ?? body); }
        bool Button(string text, float x, float y, float width = 230, float height = 44) => GUI.Button(new Rect(x,y,width,height),text,button);
        void Meter(float x, float y, float width, float value, Color color)
        { Box(new Rect(x,y,width,5),new Color(.15f,.21f,.23f)); Box(new Rect(x,y,width*Mathf.Clamp01(value),5),color); }
        bool Hotspot(Vector3 position, Vector2 size, string hint)
        {
            Vector3 p = eye.WorldToViewportPoint(position);
            if (p.z <= 0 || p.x < .08f || p.x > .92f || p.y < .15f || p.y > .86f) return false;
            var area = new Rect(p.x*1280-size.x/2,(1-p.y)*720-size.y/2,size.x,size.y);
            bool hover = area.Contains(Event.current.mousePosition);
            if (hover)
            {
                hoverHint = hint;
                Box(new Rect(area.x,area.y,area.width,2),Amber); Box(new Rect(area.x,area.yMax-2,area.width,2),Amber);
                Box(new Rect(area.x,area.y,2,area.height),Amber); Box(new Rect(area.xMax-2,area.y,2,area.height),Amber);
            }
            else Box(new Rect(p.x*1280-2,(1-p.y)*720-2,4,4),new Color(.94f,.66f,.31f,.65f));
            return GUI.Button(area,GUIContent.none,GUIStyle.none);
        }
        void OnGUI()
        {
            Styles(); GUI.depth = -10;
            if (pixels) GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),pixels,ScaleMode.StretchToFill,false);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            for (int y = 0; y < 720; y += 4) Box(new Rect(0,y,1280,1),new Color(0,0,0,.1f));
            if (mode == Mode.Title) { DrawTitle(); return; }
            Box(new Rect(0,0,1280,91),Ink); Box(new Rect(0,620,1280,100),Ink);
            Text("ICE SHANTY",25,15,270,40,heading); Text("NIGHT " + Run.Shift.ToString("00"),27,56,270,25,small);
            Text("BOX  $" + Run.BoxValue,300,19,200); Text("MARKET OPENS AT DAWN",300,56,230,24,small);
            Text("CHUM  " + Run.Chum + " / " + Run.Capacity,560,19,210); Text("MEALS  " + Run.Feeds + "   MISSED  " + Run.Anger + "/2",560,56,230,24,small);
            Text("NOISE  " + Mathf.RoundToInt(Run.Noise) + "%",810,19,170); Meter(810,65,165,Run.Noise/100,Run.Noise > 65 ? Danger : Amber);
            Text("DAWN IN  " + Mathf.CeilToInt(Run.Remaining) + "s",1030,19,240); Text("COAT  " + (3-Run.Wounds) + "/3",1030,56,200,24,small);
            if (mode == Mode.Shop) { DrawShop(); return; }
            if (mode == Mode.End) { DrawEnd(); return; }
            if (mode == Mode.Paused) { DrawPause(); return; }
            Text(ViewNames[view],480,105,500,35,heading);
            if (Run.Alarm)
            {
                Box(new Rect(325,149,630,46),new Color(.4f,.05f,.03f,.95f));
                Text("CLOCK RINGING  /  " + Run.FeedCost + " CHUM  /  " + Run.FeedRemaining.ToString("0.0") + "s",350,158,600,30);
            }
            GUI.enabled = !Turning && !showHelp && activity != Activity.Drilling;
            if (GUI.Button(new Rect(18,294,78,110),"<",arrow)) Turn(-1);
            if (GUI.Button(new Rect(1184,294,78,110),">",arrow)) Turn(1);
            GUI.enabled = true;
            Text(ViewNames[HorrorRun.WrapView(view,-1)],18,415,155,48,small);
            Text(ViewNames[HorrorRun.WrapView(view,1)],1110,415,155,48,small);
            hoverHint = null;
            if (CanTouch) DrawWorldActions();
            if (activity == Activity.Fishing) DrawFishing();
            else if (activity == Activity.Waiting) Text("LINE IN  /  WAITING FOR A BITE",440,522,520);
            else if (activity == Activity.Drilling) Text("DRILLING  /  " + actionTime.ToString("0.0") + "s",470,522,480);
            if (Run.Crushing) Text("CRUSHER RUNNING  /  " + Run.CrushRemaining.ToString("0.0") + "s",28,106,410,28,small);
            if (HorrorRun.IsDark(view) && !FlashlightOn) Text("Pitch black. Hold RIGHT MOUSE or turn on the flashlight.",370,520,600,45);
            foreach (int sector in HorrorRun.DarkViews)
                if (Run.Darkness[sector] > .5f) { Text("[BREATHING]  " + ViewNames[sector],28,148+sector*24,300,24,small); }
            if (!string.IsNullOrEmpty(hoverHint)) Text(hoverHint,145,567,1000,43);
            else if (noticeTime > 0) Text(notice,145,567,1000,47);
            Text(HandLabel(),28,634,720,32);
            GUI.enabled = !showHelp;
            if (Button(FlashlightOn ? "FLASHLIGHT ON  [F]" : "FLASHLIGHT OFF  [F]",960,630,292,39)) flashlightToggle = !flashlightToggle;
            GUI.enabled = true;
            Text("CLICK objects   |   SIDE ARROWS turn in a circle   |   Q drop bat / bucket   |   RMB hold flashlight   |   X cut line   |   H help   |   ESC pause",28,682,1230,24,small);
            if (showHelp)
            {
                Box(new Rect(235,158,810,420),Ink); Text("THE NIGHT ROUTINE",270,178,700,40,heading); HelpText(270,235);
                if (Button("CLOSE HELP  [H]",270,517,700,42)) showHelp = false;
            }
            if (flash > 0) Box(new Rect(0,0,1280,720),new Color(.7f,.02f,.01f,flash*.5f));
        }
        string HandLabel()
        {
            switch (Run.Hand)
            {
                case HorrorRun.HandItem.Fish: return "IN HAND  /  " + Run.HeldFish.Name + "  /  " + Run.HeldFish.Chum + " chum" + (Run.HeldFish.Screaming ? "  /  SCREAMING" : "");
                case HorrorRun.HandItem.Bat: return "IN HAND  /  BASEBALL BAT  /  Click a caught fish. Q to drop.";
                case HorrorRun.HandItem.Bucket: return "IN HAND  /  CHUM BUCKET  /  Place by clock or under crusher.";
                default: return "HANDS EMPTY  /  Click an object to use it or pick it up.";
            }
        }
        void DrawWorldActions()
        {
            bool canSee = !HorrorRun.IsDark(view) || FlashlightOn;
            if (canSee && Run.BatView == view && Hotspot(bat.position,new Vector2(110,58),"Pick up the baseball bat")) PickBat();
            bool bucketHere = Run.Bucket == HorrorRun.BucketPlace.Crusher && view == 1 || Run.Bucket == HorrorRun.BucketPlace.Clock && view == 3 || Run.Bucket == HorrorRun.BucketPlace.Floor && view == Run.BucketFloorView;
            if (canSee && bucketHere && Hotspot(bucket.position+Vector3.up*.15f,new Vector2(90,96),"Pick up bucket  /  " + Run.Chum + " chum")) PickBucket();
            if (view == HorrorRun.WellView && Hotspot(WellPosition,new Vector2(360,190),Run.HoleOpen ? "Cast into the water hole" : "Drill the ice")) UseWell();
            if (view == HorrorRun.CrusherView)
            {
                if (Hotspot(CrusherPosition+Vector3.up*1.25f,new Vector2(250,130),"Place held fish into the crusher  /  LOUD")) UseCrusher();
                if (Run.Hand == HorrorRun.HandItem.Bucket && Hotspot(CrusherBucketPosition,new Vector2(105,90),"Place bucket beneath crusher")) UseCrusher();
            }
            if (view == HorrorRun.ClockView)
            {
                if (Hotspot(ClockPosition+Vector3.up*.15f,new Vector2(100,100),"Little alarm clock  /  next meal in " + Mathf.CeilToInt(Run.FeedIn) + "s")) Say("Three warning dings, then the alarm. Leave at least " + Run.FeedCost + " chum in the bucket beside it.");
                if (Run.Bucket != HorrorRun.BucketPlace.Clock && Hotspot(ClockBucketPosition,new Vector2(125,105),"Place chum bucket beside the alarm clock")) PlaceAtClock();
            }
            if (view == HorrorRun.BoxView)
            {
                for (int i = 0; i < Run.Inventory.Count; i++)
                    if (Hotspot(storedFish[i].position,new Vector2(40,40),(Run.Hand == HorrorRun.HandItem.Bat ? "Hit " : "Pick up ") + Run.Inventory[i].Name + (Run.Inventory[i].Screaming ? "  /  SCREAMING" : ""))) { UseFish(i); break; }
                if (Hotspot(BoxPosition+Vector3.up*.35f,new Vector2(300,115),"Put held fish into the box  /  " + Run.Inventory.Count + "/" + HorrorRun.BoxCapacity)) UseBox();
                Text("Caught fish stay here until dawn. Click one to take it out or hit it with a held bat.",325,480,670,55,small);
            }
        }
        void DrawFishing()
        {
            Box(new Rect(425,172,430,385),Ink);
            for (int i = 0; i < 64; i++)
            {
                float angle = i*Mathf.PI*2/64;
                Box(new Rect(640+Mathf.Cos(angle)*126-2,330+Mathf.Sin(angle)*126-2,4,4),i/64f < catchProgress ? Amber : new Color(.2f,.35f,.37f));
            }
            var p = FishPoint(); Box(new Rect(p.x-15,p.y-9,27,18),incoming.Screaming ? Danger : Amber); Box(new Rect(p.x-23,p.y-5,9,10),Amber); Box(new Rect(p.x+6,p.y-5,4,4),Ink);
            Text(Mathf.RoundToInt(catchProgress*100) + "%",585,298,180,65,title); Text(incoming.Name.ToUpperInvariant(),510,390,300,24,small);
            Meter(465,464,350,catchProgress,Amber);
            Text("FOLLOW THE FISH WITH YOUR CURSOR",463,482,400,24,small); Text("HOLD LMB  Faster + louder    X  Cut line",463,514,400,24,small);
        }
        void HelpText(float x, float y)
        {
            Text("1. Click the ice, then cast. Hover over the moving fish to catch it.\n2. Your catch goes into your hand. Box it on the left or grind it on the right.\n3. A screaming catch? Box it, pick up the bat on the right, then click that fish. Q drops the bat.\n4. Keep the bucket beneath the crusher to collect chum. Pick it up and place it beside the little clock before its alarm.\n5. Three dings warn you. When it rings, the bear eats from the placed bucket. Return the bucket to the crusher to refill.\n6. Check pitch-black views with the flashlight. Light drives things back; the bat only hits caught fish.\n7. Survive until dawn. Stored, silent fish fund the shop between nights.",x,y,720,275,body);
        }
        void DrawTitle()
        {
            Box(new Rect(0,0,735,720),new Color(.015f,.03f,.04f,.96f));
            Text("POINT & CLICK  /  CIRCULAR ROOM",45,48,620,30,small); Text("ICE SHANTY",40,107,660,90,title);
            Text("Turn. Catch. Keep it fed.",45,212,640,45,heading);
            Text("Left and right arrows move around the room.\n\nFish go into your hand. Keep them in the left box or feed them into the right crusher.\n\nThe little clock warns you before the bear gets hungry. Carry the chum bucket over before it rings.\n\nKeep checking the darkness with your flashlight.",45,284,620,235);
            if (Button("BEGIN NIGHT 01  >",45,566,620,56)) StartNight();
            Text("CLICK OBJECTS  /  Q DROP  /  F FLASHLIGHT  /  ESC PAUSE",45,649,660,30,small);
            Text("ECHOES  " + bank + "   LINING  " + legacy + "/3",800,485,440,35);
            GUI.enabled = bank >= 5+legacy*3 && legacy < 3;
            if (Button("SEW LINING  /  " + (5+legacy*3) + " ECHOES",800,532,420,48))
            {
                bank -= 5+legacy*3; legacy++; PlayerPrefs.SetInt("Blackwater.Echoes",bank); PlayerPrefs.SetInt("Blackwater.Insulation",legacy); PlayerPrefs.Save();
            }
            GUI.enabled = true; Text("Permanent: +$10 starting funds per tier.\nBest night survived: " + best,800,601,420,65,small);
        }
        void DrawShop()
        {
            Box(new Rect(160,116,960,489),Ink);
            Text("DAWN  /  THE SHOP IS OPEN",190,133,860,45,heading);
            Text("Box sold for $" + nightSale + ". Wallet: $" + Run.Cash + ". Upgrades carry into the next night.",190,184,850,30);
            for (int i = 0; i < HorrorRun.Prices.Length; i++)
            {
                float x = 190+(i%3)*302, y = 233+(i/3)*94;
                GUI.enabled = !Run.Upgrades.Contains(i) && Run.Cash >= HorrorRun.Prices[i];
                if (Button((Run.Upgrades.Contains(i) ? "OWNED  " : "$" + HorrorRun.Prices[i] + "  ") + HorrorRun.Names[i],x,y,284,38)) Run.Buy(i);
                GUI.enabled = true; Text(HorrorRun.Descriptions[i],x,y+42,282,46,small);
            }
            if (Button("BEGIN NIGHT " + (Run.Shift+1),190,539,430,48)) StartNight(true);
            if (Button("RETURN TO TITLE",652,539,430,48)) mode = Mode.Title;
            Text("Only fish stored and silenced before dawn are sold. Held fish and unfinished processing are lost.",190,631,950,42,small);
        }
        void DrawPause()
        {
            Box(new Rect(235,131,810,482),Ink); Text("PAUSED",270,148,700,40,heading); HelpText(270,205);
            Text("VOLUME",270,489,170,30,small); volume = GUI.HorizontalSlider(new Rect(440,500,535,20),volume,0,1);
            if (Button("RESUME",270,548,335,45)) mode = Mode.Playing;
            if (Button("END RUN",640,548,335,45)) { Run.End("You put the rod down before dawn."); Finish(); }
        }
        void DrawEnd()
        {
            Box(new Rect(235,178,810,390),Ink); Text("THE NIGHT KEPT YOU",270,204,700,45,heading);
            Text(Run.EndReason,270,281,700,90); Text("CATCHES  " + Run.Caught + "    MEALS  " + Run.Feeds + "    ECHOES  " + bank,270,384,700,35);
            if (Button("TRY AGAIN",270,478,335,48)) StartNight();
            if (Button("RETURN TO TITLE",640,478,335,48)) mode = Mode.Title;
        }
    }
}
