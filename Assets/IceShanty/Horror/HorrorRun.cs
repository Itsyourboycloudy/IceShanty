using System;
using System.Collections.Generic;
using UnityEngine;

namespace IceShanty.Horror
{
    [Serializable]
    public sealed class HorrorRun
    {
        public enum HandItem { Empty, Fish, Bat, Bucket }
        public enum BucketPlace { Crusher, Clock, Floor, Hand }
        [Serializable] public sealed class Fish
        {
            public string Name;
            public int Value, Chum;
            public float Speed;
            public bool Screaming;
            public Fish(string name, int value, int chum, float speed, bool screaming = false)
            { Name = name; Value = value; Chum = chum; Speed = speed; Screaming = screaming; }
        }
        public const int ViewCount = 8, BoxCapacity = 12;
        public const int WellView = 0, CrusherView = 1, ClockView = 3, BoxView = 6;
        public static readonly int[] DarkViews = { 2, 4, 5, 7 };
        public readonly List<Fish> Inventory = new List<Fish>(); // Fish physically in the left box.
        public readonly HashSet<int> Upgrades = new HashSet<int>();
        public readonly float[] Darkness = new float[ViewCount];
        public HandItem Hand { get; private set; }
        public Fish HeldFish { get; private set; }
        public Fish ProcessingFish { get; private set; }
        public BucketPlace Bucket { get; private set; } = BucketPlace.Crusher;
        public int BatView { get; private set; } = CrusherView;
        public int BucketFloorView { get; private set; } = CrusherView;
        public float Elapsed, Noise, FeedIn = 60, FeedRemaining, NightLength = 180, CrushRemaining;
        public int Cash, Earnings, Chum, Feeds, Anger, Wounds, Caught, Shift = 1;
        public int WarningDings { get; private set; }
        public int AlarmCount { get; private set; }
        public bool HoleOpen, Ended, Won;
        public string EndReason;
        int warningMask;
        bool settled;
        public int Capacity => Upgrades.Contains(6) ? 16 : 8;
        public int FeedCost => 2 + (Shift - 1) / 2;
        public float Remaining => Mathf.Max(0, NightLength - Elapsed);
        public float CatchRate => Upgrades.Contains(1) ? .30f : Upgrades.Contains(0) ? .22f : .17f;
        public float ReelNoise => Upgrades.Contains(1) ? 14 : Upgrades.Contains(0) ? 4 : 8;
        public float FeedWindow => Upgrades.Contains(7) ? 15 : 10;
        public bool Alarm => FeedRemaining > 0;
        public bool Crushing => ProcessingFish != null;
        public bool CanFish => !Ended && Hand == HandItem.Empty && !Crushing;
        public int BoxValue { get { int value = 0; foreach (var f in Inventory) if (!f.Screaming) value += f.Value; return value; } }
        public int Screams
        {
            get { int n = HeldFish != null && HeldFish.Screaming ? 1 : 0; foreach (var f in Inventory) if (f.Screaming) n++; return n; }
        }
        public static int WrapView(int view, int direction) => ((view + direction) % ViewCount + ViewCount) % ViewCount;
        public static bool IsDark(int view) => Array.IndexOf(DarkViews, view) >= 0;
        public void AddNoise(float amount) { Noise = Mathf.Clamp(Noise + amount, 0, 100); }
        public void Tick(float dt)
        {
            if (Ended || dt <= 0) return;
            // Substeps preserve feeding deadlines and automatic feeding across long frames.
            while (dt > 0 && !Ended) { float step = Mathf.Min(dt, .05f); dt -= step; Step(step); }
        }
        void Step(float dt)
        {
            Elapsed += dt;
            Noise = Mathf.Clamp(Noise + (Screams * 9 + (Alarm ? 3 : 0) - 5) * dt, 0, 100);
            if (Crushing)
            {
                CrushRemaining -= dt;
                if (CrushRemaining <= 0) { Chum += ProcessingFish.Chum + (Upgrades.Contains(5) ? 1 : 0); ProcessingFish = null; CrushRemaining = 0; }
            }
            if (Alarm)
            {
                FeedRemaining -= dt;
                if (Bucket == BucketPlace.Clock && Chum >= FeedCost && FeedRemaining <= FeedWindow - 1)
                { Chum -= FeedCost; Feeds++; ResetFeeding(Mathf.Max(38, 55 - (Shift - 1) * 3)); AddNoise(6); }
                else if (FeedRemaining <= 0)
                {
                    Anger++; ResetFeeding(48); AddNoise(22);
                    if (Anger >= 2) End("The bucket wasn't beside the clock with enough chum. It stopped waiting.");
                }
            }
            else
            {
                FeedIn -= dt;
                for (int i = 0; i < 3; i++)
                    if (FeedIn <= 12 - i * 4 && (warningMask & (1 << i)) == 0) { warningMask |= 1 << i; WarningDings++; }
                if (FeedIn <= 0) { FeedRemaining = FeedWindow; AlarmCount++; }
            }
            if (Elapsed >= NightLength && !Ended) End("Dawn. The breathing fades. The catch box goes to market.", true);
        }
        void ResetFeeding(float delay) { FeedRemaining = 0; FeedIn = delay; warningMask = 0; }
        public bool LandFish(Fish fish)
        {
            if (!CanFish || fish == null) return false;
            Hand = HandItem.Fish; HeldFish = fish; Caught++; return true;
        }
        public bool StoreFish()
        {
            if (Ended || Hand != HandItem.Fish || Inventory.Count >= BoxCapacity) return false;
            Inventory.Add(HeldFish); HeldFish = null; Hand = HandItem.Empty; AddNoise(2); return true;
        }
        public bool TakeFish(int index)
        {
            if (Ended || Hand != HandItem.Empty || index < 0 || index >= Inventory.Count) return false;
            HeldFish = Inventory[index]; Inventory.RemoveAt(index); Hand = HandItem.Fish; return true;
        }
        public bool PickUpBat(int view)
        {
            if (Ended || Hand != HandItem.Empty || BatView != view) return false;
            Hand = HandItem.Bat; BatView = -1; return true;
        }
        public bool HitFish(int index)
        {
            if (Ended || Hand != HandItem.Bat || index < 0 || index >= Inventory.Count) return false;
            AddNoise(Upgrades.Contains(8) ? 5 : 12);
            var fish = Inventory[index];
            if (fish.Screaming) { fish.Screaming = false; fish.Value = Mathf.Max(1, fish.Value - 10); }
            return true;
        }
        public bool PickUpBucket(int view)
        {
            if (Ended || Crushing || Hand != HandItem.Empty) return false;
            bool here = Bucket == BucketPlace.Crusher && view == CrusherView || Bucket == BucketPlace.Clock && view == ClockView || Bucket == BucketPlace.Floor && view == BucketFloorView;
            if (!here) return false;
            Hand = HandItem.Bucket; Bucket = BucketPlace.Hand; return true;
        }
        public bool PlaceBucket(int view)
        {
            if (Ended || Hand != HandItem.Bucket || (view != CrusherView && view != ClockView)) return false;
            Bucket = view == ClockView ? BucketPlace.Clock : BucketPlace.Crusher; Hand = HandItem.Empty; AddNoise(2); return true;
        }
        public bool Drop(int view)
        {
            if (Ended) return false;
            if (Hand == HandItem.Bat) BatView = view;
            else if (Hand == HandItem.Bucket) { Bucket = BucketPlace.Floor; BucketFloorView = view; }
            else return false; // Fish must deliberately go in the box or crusher.
            Hand = HandItem.Empty; AddNoise(4); return true;
        }
        public bool BeginCrush()
        {
            if (Ended || Crushing || Hand != HandItem.Fish || Bucket != BucketPlace.Crusher) return false;
            int amount = HeldFish.Chum + (Upgrades.Contains(5) ? 1 : 0);
            if (Chum + amount > Capacity) return false;
            ProcessingFish = HeldFish; HeldFish = null; Hand = HandItem.Empty;
            CrushRemaining = Upgrades.Contains(4) ? 4 : 2.5f;
            AddNoise((Upgrades.Contains(4) ? 27 : 58) + (Upgrades.Contains(5) ? 8 : 0)); return true;
        }
        public void TickDarkness(float dt, int view, bool flashlight)
        {
            if (Ended || Elapsed < 25) return;
            foreach (int sector in DarkViews)
            {
                Darkness[sector] += dt * (.0025f + Noise * .00018f + (Shift - 1) * .0008f);
                if (flashlight && sector == view) Darkness[sector] -= dt * .32f;
                Darkness[sector] = Mathf.Clamp(Darkness[sector], 0, 1);
                if (Darkness[sector] >= 1) { Darkness[sector] = 0; Hurt(); }
            }
        }
        public int SettleBox()
        {
            if (!Ended || !Won || settled) return 0;
            settled = true; int sale = BoxValue; Cash += sale; Earnings += sale; Inventory.Clear(); return sale;
        }
        public bool Buy(int id)
        {
            if (!Ended || !Won || id < 0 || id >= Prices.Length || Upgrades.Contains(id) || Cash < Prices[id]) return false;
            Cash -= Prices[id]; Upgrades.Add(id);
            if (id == 0) Upgrades.Remove(1);
            if (id == 1) Upgrades.Remove(0);
            return true;
        }
        public HorrorRun NextNight()
        {
            if (!Ended || !Won) return null;
            var next = new HorrorRun { Cash = Cash, Shift = Shift + 1, NightLength = NightLength };
            foreach (int id in Upgrades) next.Upgrades.Add(id);
            return next;
        }
        public void End(string reason, bool won = false) { if (Ended) return; Ended = true; Won = won; EndReason = reason; }
        public void Hurt() { if (Ended) return; Wounds++; AddNoise(25); if (Wounds >= 3) End("You let the darkness get too close. The flashlight lies on the ice."); }
        public static readonly string[] Names = { "Felt reel", "Iron winch", "Braided line", "Blood bait", "Crusher baffle", "Bone separator", "Deep bucket", "Clock governor", "Wrapped bat" };
        public static readonly int[] Prices = { 45, 45, 35, 40, 55, 50, 30, 40, 35 };
        public static readonly string[] Descriptions = {
            "Quieter reel, wider tracking zone. Replaces winch.", "Faster catches, louder reel. Replaces felt reel.",
            "Progress drains half as fast when you lose contact.", "Faster bites. More valuable, screaming fish.",
            "Quieter grinding, but each fish takes longer.", "Extra chum per fish. Grinding makes more noise.",
            "The portable bucket holds 16 chum instead of 8.", "The alarm allows 15 seconds instead of 10.",
            "Silence caught fish with less impact noise." };
    }
}
