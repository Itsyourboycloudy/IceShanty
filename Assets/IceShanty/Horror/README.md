# Ice Shanty — circular point-and-click night

## Launch in the Game scene

Open `Assets/Scenes/Game.unity` and press **Play**, then **Begin Night**. The menu **Ice Shanty > Game > Open Circular Room** opens that same scene. `Game` is the first build scene; the separate Blackwater scene is no longer in the build list.

The Game scene now contains an active **Game - Circular Point and Click Room** controller. The previous six root groups remain in the scene, inactive and recoverable, so old cameras, UI, shop and quota systems do not run alongside the new gameplay. The circular room is constructed when Play begins. The cursor stays visible; there is no mouse-look or walking.

## Around the circle

Click the arrows at the left and right edges to transition one view around the circle. Both directions wrap, with eight steps returning to the starting view.

Clockwise from the water hole:

1. Water hole.
2. Right-hand meat crusher, bucket collection spot, and baseball bat on the ground.
3. Pitch-black right recess.
4. Little alarm clock and feeding mat beside it.
5. Darkness behind the chair.
6. Back-left darkness.
7. Left-hand catch box.
8. Left darkness, then back to the water hole.

A/D and left/right arrow keys are optional equivalents. Turning away while waiting for a bite or tracking a fish abandons that catch. Drilling must finish before turning.

## Hands and caught fish

Click the ice to drill, then click the water to fish. Hover over the moving fish in the circular challenge; holding left mouse reels faster and makes more noise.

A landed fish goes visibly into your hand. You cannot cast or pick up another object until you put it down. Click the left box to store it, or the right crusher to grind it. Click a stored fish with an empty hand to retrieve it. The box holds twelve catches.

Screaming fish are catches from the water, not enemies from vents. To silence one, put it in the box, turn to the right, click the bat on the ground, return to the box, and click that fish. A hit makes noise and reduces its value by $10. The bat does not attack anything in the dark.

**Q drops the bat or bucket** onto the floor in the current view. It remains there and can be picked up again. Fish must be deliberately put in the box or crusher; Q does not discard them. Dropped objects in dark views require the flashlight to find and click.

## Chum, clock, and bear

The portable bucket starts beneath the crusher. Processing requires a held fish, space in the bucket, and the bucket in its collection spot. The crusher takes 2.5 seconds and produces loud noise. The bucket cannot be moved until processing finishes.

Pick up the filled bucket, turn to the little clock, and click the marked space beside it to place the bucket. You can prepare it before the alarm. The clock dings at twelve, eight, and four seconds before feeding time, then rings. The bear automatically eats the required chum from the placed bucket after about one second. You have ten seconds to get it there if it was not prepared. Merely carrying the bucket or dropping it on the floor is not the same as placing it on the feeding mat.

Two missed meals end the run. Pick the bucket up again and return it beneath the crusher to refill. Extra chum remains in the bucket after feeding and can cover later meals.

## Darkness and flashlight

There are no vents or gillwalker combat encounters. Four unlit recesses conceal presences. Noise makes them approach faster. Directional breathing and text captions hint where to check. Hold **right mouse**, press **F**, or click the flashlight control to illuminate the current view. Holding the beam on a presence drives it back. Light only affects the view you are facing. Three unchecked attacks end the run.

## Dawn and the shop

Survive the 180-second night. Dawn ends the active night automatically and opens the shop. Silent fish in the left box are sold once, funding upgrades. Held fish, screaming fish and unfinished processing are not sold. There is no trade hatch, in-room terminal, manual shift bell, or mid-night shopping.

Purchases and remaining money carry to the next night. Hand state, catches, chum and threats reset; the bat and empty bucket return to the crusher area. Later nights increase feeding requirements and darkness pressure. Echoes still buy permanent starting-funds upgrades on the title screen.

**H** opens instructions while time continues. **Escape** pauses the game and provides volume and exit controls.

## Verification and implementation

- `HorrorRun.cs`: hand ownership, item placement, feeding, warning dings, processing, darkness and dawn economy.
- `BlackwaterRoom.cs`: circular transitions, fishing, input and night lifecycle.
- `BlackwaterRoom.World.cs`: room, visible held objects, clock, flashlight and spatial sound.
- `BlackwaterRoom.UI.cs`: arrow controls, projected object click targets, fishing UI and after-night shop.
- `Editor/BlackwaterBuilder.cs`: Game scene integration and 54 deterministic rule assertions.
- `Tools/ValidateBlackwater.ps1`: checks the Game scene reference, build order and active root, compiles gameplay/editor code, and runs the same 54 assertions against Unity's managed math.

Verified: scene wiring, first build scene, gameplay/editor compilation and all 54 rule assertions. A Unity runtime smoke launch was blocked because this project is already open in another Unity editor instance. Rendering, audio balance and a complete interactive playthrough still need verification in that session. The active editor may need to refresh/reload Game to pick up disk changes.

The optional smoke command is `-executeMethod IceShanty.Horror.Editor.BlackwaterBuilder.BuildAndSmoke -blackwaterSmoke` with Unity in batch mode. Run only when the project is closed elsewhere. It saves Game integration and writes `Artifacts/game-circle-room.png`; do not add `-quit` because the callback exits after the capture.
