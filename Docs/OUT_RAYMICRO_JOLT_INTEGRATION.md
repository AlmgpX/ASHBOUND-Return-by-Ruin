# OUT RayMicro Jolt integration notes

Current status:

```text
OUT_RayMicro now targets net9.0.
Jolt package reference is intended as the next local install step.
OUT collision interface exists at src/Physics/OutmCollisionWorld.cs.
```

Install command from the OUT_RayMicro folder:

```powershell
dotnet add package JoltPhysicsSharp --version 2.21.0
```

If the command succeeds, the project file should contain:

```xml
<PackageReference Include="JoltPhysicsSharp" Version="2.21.0" />
```

Do not let gameplay systems call the external package directly.

Allowed direction:

```text
Gameplay -> IOutmCollisionWorld -> backend
```

Forbidden direction:

```text
WeaponSystem -> external physics package directly
CameraMotor -> external physics package directly
TriggerSystem -> external physics package directly
```

Next code phase:

```text
1. Add custom collision backend behind IOutmCollisionWorld.
2. Move current OutmDemoMap collision through that interface.
3. Add external backend adapter after the NuGet package restores locally.
4. Move character motor to MoveCharacter.
5. Move projectile collision to Raycast or shape cast.
6. Move trigger door to OverlapBox.
```

Reason:

```text
The external library is a backend, not the gameplay ontology.
OUT CORE keeps commands, events, saves and replay above the backend.
```
