# Opportunity-Aware RDW Controller

## Goal
This document tracks the first implementation pass of the `Opportunity-Aware RDW Controller` inside the Unity OpenRDW project. The runtime objective is to keep the original OpenRDW redirectors intact while adding a new redirector that follows the PDF design:

`temporal state -> opportunity / steerability / gain budget -> gain scheduler -> existing RDW injection`

## Implemented Version
- Version: `v0.1`
- Runtime class: `OpportunityAwareRDWRedirector`
- Registration entry: `RedirectionManager.RedirectorChoice.OpportunityAware`
- Scope: heuristic predictor + opportunity-constrained gain scheduling for initial validation

## Files Changed
- `Assets/OpenRDW/Scripts/Redirection/Redirectors/OpportunityAwareRDWRedirector.cs`
- `Assets/OpenRDW/Scripts/Redirection/RedirectionManager.cs`
- `Assembly-CSharp.csproj`

## Runtime Design
### 1. Temporal State Collection
Each update stores a short history window with:
- physical position and forward direction
- speed and angular speed
- nearest boundary distance
- left/right lateral clearance
- distance/bearing relative to the tracking-space center
- distance to waypoint
- previously applied gains
- reset state

### 2. Heuristic Opportunity Predictor
The current predictor estimates:
- `opportunityScore`
  - raised by natural turning
  - raised by deceleration
- `steerability`
  - inferred from left/right clearance asymmetry
  - sign is aligned to OpenRDW steering direction semantics
- `gainBudget`
  - per-frame budget for curvature / rotation / translation
  - increases with boundary risk, steerability confidence, and opportunity

### 3. Base Controller
The base geometric proposal uses:
- center-facing guidance as the default safe direction
- lateral escape bias when one side offers more clearance
- curvature and rotation magnitudes based on existing OpenRDW gain limits
- translation gain only when boundary pressure is already non-trivial

### 4. Gain Scheduler
The scheduler applies:
- `alpha = phi(opportunityScore)` with a configurable minimum
- `selectedDirection = sign(steerability)` when confidence is high, otherwise base direction
- `finalGain = clip(alpha * baseGain, budget)`
- temporal smoothing before injection
- a critical-boundary fallback that raises `alpha` when safety is urgent but opportunity is weak

### 5. Injection Path
The new controller still uses the original OpenRDW injection hooks:
- `InjectTranslation`
- `InjectRotation`
- `InjectCurvature`

No legacy redirector was removed or replaced.

## Inspector / Validation Outputs
The controller exposes runtime debug fields for:
- last `opportunityScore`
- last `steerability`
- last gain budget
- last base control suggestion
- last applied gains
- last boundary distance
- last steering direction
- whether the critical fallback path was used
- a compact decision summary string

These fields are intended for the first validation pass before a learned predictor is wired in.

## Testing Plan
The current verification target for this version is:
1. The project compiles with the new redirector included.
2. `RedirectionManager` can resolve `OpportunityAware`.
3. The new redirector can compute and schedule gains without touching legacy redirectors.

## Test Result
- Date: `2026-03-31`
- Command: `dotnet build Assembly-CSharp.csproj -nologo`
- Result: `passed`
- Output summary:
  - `Assembly-CSharp.dll` built successfully
  - no errors from the new controller integration
  - only pre-existing deprecation warnings remained in SteamVR / Photon code

## Issues Encountered During Implementation
- The shared ChatGPT conversation page could not be reliably scraped from the CLI, so the exported PDF became the source of truth.
- The PDF is formula-heavy, so text extraction was partial; the implementation therefore follows the stable architectural requirements rather than every formula literal.
- The workspace is not currently a git repository, which blocks a normal local `git commit/push` flow to GitHub from this directory.
- `Assembly-CSharp.csproj` uses an explicit compile list, so the new script had to be added there for local build verification.
- Local .NET build initially failed because the generated Unity project referenced a missing VSCode Unity analyzer path (`visualstudiotoolsforunity.vstuc-1.1.2`). The environment was repaired by linking that path to the installed `1.2.1` analyzer folder, after which the build passed.

## Next Iteration Targets
- Replace the heuristic predictor with a learned opportunity predictor.
- Log predictor outputs to experiment artifacts instead of only Unity runtime fields.
- Compare this controller against an existing baseline redirector on the same scene / path setup.
