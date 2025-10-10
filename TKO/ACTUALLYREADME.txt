WORLD SETUP INSTRUCTIONS:
Drag TKO folder into Assets
Drag TKO script into TKO asset "Source Script" field in inspector
Click "Compile all UdonSharp Programs"
Drag HandPhysicsManager into Hierarchy
In inspector, click "Add Udon Behavior"
Drag the TKO asset into program source
Expand the HandPhysicsManager prefab, and drag each collider into its respective slot in the inspector

LAYER SETUP INSTRUCTIONS:
Create a new physics layer called "HandColliders" (or similar)
Set the handCollidersLayer field to that layer number
3. Go to Edit > Project Settings > Physics
4. In the Layer Collision Matrix, UNCHECK the collision between:
- "HandColliders" and "Player"
- "HandColliders" and "PlayerLocal"
- "HandColliders" and "MirrorReflection"
5. This prevents the hand colliders from affecting the player's movement

For desktop users: Finger colliders are automatically disabled by default (vrOnlyFingers = true)

That should be it, tweak colliders if needed. Test in VR. 

This is version 0.1 so expect funky shit to occur. It's functional for me, but needs work. Updates will come. 