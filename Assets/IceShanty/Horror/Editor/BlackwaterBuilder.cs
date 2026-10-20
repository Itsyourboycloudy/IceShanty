using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceShanty.Horror.Editor
{
    public static class BlackwaterBuilder
    {
        const string ScenePath = "Assets/Scenes/Game.unity";
        static double deadline;
        static bool runtimePassed, runtimeFailed, previousPlayOptionsEnabled;
        static EnterPlayModeOptions previousPlayOptions;

        [MenuItem("Ice Shanty/Game/Open Circular Room")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            ValidateRules();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var room = UnityEngine.Object.FindFirstObjectByType<BlackwaterRoom>();
            if (!room) room = new GameObject("Game - Circular Point and Click Room").AddComponent<BlackwaterRoom>();
            // Keep the previous scene's authored hierarchy recoverable, without running two games at once.
            foreach (var item in scene.GetRootGameObjects()) if (item != room.gameObject) item.SetActive(false);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != "Assets/Scenes/Blackwater.unity").ToList();
            scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("BLACKWATER_GAME_SCENE_BUILD_PASSED");
        }
        public static void BuildAndSmoke()
        {
            Build(); deadline = EditorApplication.timeSinceStartup+65; runtimePassed = runtimeFailed = false;
            Application.logMessageReceived += CaptureLog; EditorApplication.update += WaitForSmoke;
            previousPlayOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled; previousPlayOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.isPlaying = true;
        }
        static void CaptureLog(string message, string stack, LogType type)
        {
            if (message.Contains("BLACKWATER_RUNTIME_SMOKE_PASSED")) runtimePassed = true;
            if (type == LogType.Exception || type == LogType.Error) runtimeFailed = true;
        }
        static void WaitForSmoke()
        {
            if (!runtimePassed && !runtimeFailed && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= WaitForSmoke; Application.logMessageReceived -= CaptureLog;
            EditorSettings.enterPlayModeOptionsEnabled = previousPlayOptionsEnabled; EditorSettings.enterPlayModeOptions = previousPlayOptions;
            EditorApplication.Exit(runtimePassed && !runtimeFailed ? 0 : 1);
        }
        static int checks;
        static void Check(bool condition, string rule)
        { checks++; if (!condition) throw new Exception("GAME RULE FAILED: " + rule); }

        [MenuItem("Ice Shanty/Game/Validate Circular Room Rules")]
        public static void ValidateRules()
        {
            checks = 0;
            Check(HorrorRun.WrapView(0,-1)==7 && HorrorRun.WrapView(7,1)==0,"Circle wraps at both ends");
            int view = 0; for (int i=0;i<8;i++) view=HorrorRun.WrapView(view,1);
            Check(view==0,"Eight right clicks return to the hole");
            var r = new HorrorRun(); var fish = new HorrorRun.Fish("Eel",60,2,1,true);
            Check(r.LandFish(fish) && r.Hand==HorrorRun.HandItem.Fish && r.Inventory.Count==0,"Catch goes into hand, not box");
            Check(!r.LandFish(fish) && !r.PickUpBat(1) && !r.PickUpBucket(1),"One held item at a time");
            Check(!r.CanFish && !r.Drop(0),"Cannot cast holding a fish or discard it with Q");
            Check(r.Screams==1,"Held screaming fish produces noise");
            Check(r.StoreFish() && r.Hand==HorrorRun.HandItem.Empty && r.Inventory.Count==1,"Deposit frees hand");
            Check(!r.HitFish(0),"Hitting fish requires picked-up bat");
            Check(!r.PickUpBat(6) && r.PickUpBat(1),"Bat initially on the right, requires its view");
            Check(r.HitFish(0) && !fish.Screaming && r.Screams==0 && fish.Value==50,"Bat silences caught fish");
            int value=fish.Value; r.HitFish(0); Check(fish.Value==value,"Repeated hits do not repeatedly deduct value");
            Check(r.Drop(6) && r.BatView==6 && r.Hand==HorrorRun.HandItem.Empty,"Q drops bat in current view");
            Check(!r.PickUpBat(1) && r.PickUpBat(6),"Dropped bat stays where left"); r.Drop(6);
            Check(r.TakeFish(0) && r.HeldFish==fish && r.Inventory.Count==0,"Can retrieve fish from box");
            r.Chum=7; Check(!r.BeginCrush() && r.HeldFish==fish,"Full bucket preserves held fish");
            r.Chum=0; Check(r.BeginCrush() && r.Hand==HorrorRun.HandItem.Empty && r.Crushing,"Crusher takes held fish");
            Check(!r.PickUpBucket(1),"Cannot move bucket during processing");
            r.Tick(2.6f); Check(r.Chum==2 && !r.Crushing && r.Noise>25,"Crusher yields chum and makes noise");
            Check(r.PickUpBucket(1) && r.Bucket==HorrorRun.BucketPlace.Hand,"Bucket can be carried");
            Check(r.PlaceBucket(3) && r.Bucket==HorrorRun.BucketPlace.Clock,"Place bucket beside clock");
            r.LandFish(new HorrorRun.Fish("Fish",30,2,1)); Check(!r.BeginCrush(),"No processing without bucket beneath crusher");
            r.StoreFish(); Check(r.PickUpBucket(3) && r.PlaceBucket(1),"Bucket can return to crusher for refilling");
            r.PickUpBucket(1); Check(r.Drop(2) && r.Bucket==HorrorRun.BucketPlace.Floor && r.BucketFloorView==2,"Q places bucket on floor");
            Check(!r.PickUpBucket(1) && r.PickUpBucket(2),"Dropped bucket only retrievable at current location");
            r = new HorrorRun(); r.Tick(47); Check(r.WarningDings==0,"No early warning");
            r.Tick(1.2f); Check(r.WarningDings==1,"First ding twelve seconds before meal");
            r.Tick(4); Check(r.WarningDings==2,"Second warning ding");
            r.Tick(4); Check(r.WarningDings==3,"Third warning ding");
            r.Tick(4); Check(r.Alarm && r.FeedRemaining>9.7f && r.AlarmCount==1,"Alarm starts ten-second window");
            r.Tick(10.1f); Check(r.Anger==1 && !r.Ended,"First missed meal warns");
            r.Tick(58.2f); Check(r.Ended && !r.Won,"Second missed meal ends run");
            r = new HorrorRun { Chum=4 }; r.PickUpBucket(1); r.PlaceBucket(3); r.Tick(61.2f);
            Check(r.Feeds==1 && r.Chum==2 && !r.Alarm,"Bear automatically eats from prepared bucket");
            r.Tick(56.2f); Check(r.Feeds==2 && r.Chum==0,"Prepared bucket can cover a second meal");
            r = new HorrorRun { Chum=2 }; r.PickUpBucket(1); r.Tick(70.2f);
            Check(r.Feeds==0 && r.Anger==1 && r.Chum==2,"Carrying bucket does not feed bear");
            r = new HorrorRun { Chum=1 }; r.PickUpBucket(1); r.PlaceBucket(3); r.Tick(70.2f);
            Check(r.Feeds==0 && r.Anger==1 && r.Chum==1,"Insufficient chum is not consumed");
            r = new HorrorRun { Chum=2 }; r.PickUpBucket(1); r.PlaceBucket(3); r.PickUpBucket(3); r.Drop(3); r.Tick(70.2f);
            Check(r.Feeds==0,"Dropping on floor near clock is not placement on feeding mat");
            r = new HorrorRun { Cash=200 };
            Check(!r.Buy(0) && r.Cash==200,"No shop purchases during night");
            r.Inventory.Add(new HorrorRun.Fish("Fish",80,2,1)); Check(r.SettleBox()==0 && r.Cash==200,"No sales during night");
            r.Chum=8; r.PickUpBucket(1); r.PlaceBucket(3); r.Tick(180.2f);
            Check(r.Ended && r.Won,"Surviving night automatically opens dawn");
            Check(r.SettleBox()==80 && r.Cash==280 && r.Inventory.Count==0,"Stored fish sell after night");
            Check(r.SettleBox()==0 && r.Cash==280,"Settlement cannot double-credit");
            Check(r.Buy(0) && r.Cash==235 && !r.Buy(0),"Dawn purchase deducts price and cannot duplicate");
            Check(r.Buy(1) && !r.Upgrades.Contains(0),"Reels mutually exclusive");
            var next=r.NextNight(); Check(next.Shift==2 && next.Cash==190 && next.Upgrades.Contains(1),"Cash and gear carry to next night");
            Check(next.Hand==HorrorRun.HandItem.Empty && next.Bucket==HorrorRun.BucketPlace.Crusher && next.Chum==0 && !next.Ended,"Next night resets physical items and hazards");
            r = new HorrorRun(); r.Inventory.Add(new HorrorRun.Fish("Scream",80,2,1,true)); r.End("Dawn",true);
            Check(r.SettleBox()==0,"Screaming fish do not sell at dawn");
            r = new HorrorRun(); r.End("Death"); Check(r.NextNight()==null && !r.Buy(0),"Death cannot access shop or continue night");
            r = new HorrorRun { Elapsed=30, Noise=70 }; r.Darkness[2]=.8f; r.TickDarkness(1,2,true);
            Check(r.Darkness[2]<.6f,"Flashlight drives presence back in the viewed darkness");
            float previous=r.Darkness[2]; r.TickDarkness(1,4,true); Check(r.Darkness[2]>previous,"Flashlight does not affect another view");
            r.Darkness[2]=.999f; r.TickDarkness(1,0,false); Check(r.Wounds==1,"Unchecked darkness attacks");
            r.Hurt(); r.Hurt(); Check(r.Ended && !r.Won,"Third wound ends run");
            r = new HorrorRun(); r.Upgrades.Add(7); r.Tick(60.2f); Check(r.FeedRemaining>14.7f,"Clock upgrade extends alarm window");
            r = new HorrorRun(); r.AddNoise(200); Check(r.Noise==100,"Noise is capped"); r.Tick(1); Check(Math.Abs(r.Noise-95)<.02f,"Quiet lets noise decay");
            Debug.Log("BLACKWATER_RULES_PASSED / " + checks + " assertions");
        }
    }
}
