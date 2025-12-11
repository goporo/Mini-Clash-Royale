# Implementation Checklist

## ✅ Completed

### Unity Client Updates
- [x] Created `EntityViewManager.cs` - manages all entity views
- [x] Created `EntityUpdateDto.cs` - server data transfer object
- [x] Updated `NetworkClient.cs` - listens for `"entity-update"` messages
- [x] Updated `GameplayBootstrap.cs` - thin client loop (visuals only)
- [x] Updated `EntityView.cs` - smooth interpolation support

### Documentation
- [x] Created `SERVER_CODE_STRUCTURE.ts` - complete server implementation
- [x] Created `SETUP_GUIDE.md` - full setup instructions

## ⚠️ TODO (Unity Side)

### Critical - Must Do Now
1. **Add EntityViewManager to Scene**
   - Create empty GameObject in your scene
   - Add `EntityViewManager` component
   - Assign `viewFactory` reference in Inspector

2. **Implement `CreateViewForType()` in EntityViewManager**
   ```csharp
   private EntityView CreateViewForType(string entityType, Vector3 position, EntityTeam team)
   {
       // Load prefab based on entity type
       EntityView prefab = Resources.Load<EntityView>($"Entities/{entityType}");
       var view = Instantiate(prefab, position, Quaternion.identity);
       return view;
   }
   ```

3. **Test Basic Flow**
   - Start Colyseus server
   - Press Play in Unity
   - Press Space to spawn units
   - Verify visuals update

### Optional Improvements
- [ ] Add health bar UI to EntityView
- [ ] Add attack animations
- [ ] Add movement animations
- [ ] Add death VFX
- [ ] Add match UI (winner screen)
- [ ] Add card drag-and-drop
- [ ] Add elixir UI

## ⚠️ TODO (Server Side)

### Critical - Must Do Now
1. **Set up Colyseus server project**
   ```bash
   npm create colyseus-app@latest my-clash-server
   cd my-clash-server
   ```

2. **Copy `ClashRoom.ts` code**
   - From `SERVER_CODE_STRUCTURE.ts`
   - To `src/rooms/ClashRoom.ts`

3. **Register room in `app.config.ts`**
   ```typescript
   gameServer.define("my_room", ClashRoom);
   ```

4. **Start server**
   ```bash
   npm start
   ```

### Optional Improvements
- [ ] Load entity stats from JSON config
- [ ] Add player resources (elixir system)
- [ ] Add spawn validation (position, cost)
- [ ] Add multiple unit types
- [ ] Add spell cards
- [ ] Add matchmaking
- [ ] Add persistent player data
- [ ] Add replay recording

## Testing Checklist

- [ ] Server starts without errors
- [ ] Unity connects to server successfully
- [ ] Initial towers spawn on both sides
- [ ] Press Space spawns a knight
- [ ] Knight moves toward enemy
- [ ] Knight attacks enemy tower
- [ ] Tower attacks knight
- [ ] Knight dies when HP reaches 0
- [ ] Match ends when all towers destroyed

## Architecture Verification

✅ **Client does NOT:**
- Run `director.Tick()`
- Run `ai.Tick()`
- Run `matchEnd.Tick()`
- Decide combat outcomes
- Decide death
- Decide movement

✅ **Client ONLY does:**
- Update visuals from server state
- Handle input (send commands)
- Smooth interpolation
- UI updates
- VFX/SFX

✅ **Server RUNS:**
- All entity simulation
- All combat logic
- All AI logic
- All spawn logic
- All win condition checks

---

## Quick Start (In Order)

1. **Set up Colyseus server** (5 min)
2. **Add EntityViewManager to Unity scene** (1 min)
3. **Implement CreateViewForType()** (5 min)
4. **Start server** (1 min)
5. **Press Play in Unity** (1 min)
6. **Test by pressing Space** (1 min)

Total: ~15 minutes to see it working!
