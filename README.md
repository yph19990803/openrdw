# openrdw

This repository hosts a source snapshot of the Unity OpenRDW project focused on the `Opportunity-Aware RDW Controller` validation pass.

## Included in this upload
- `Assets/OpenRDW`
- `Packages`
- `ProjectSettings`
- `Assembly-CSharp.csproj`
- `OpenRDW.sln`
- `OpenRdw_vbdi.sln`
- `Opportunity_Aware_RDW_Controller.md`
- `RDW_Aware_Finetune_Plan.md`

## Opportunity-Aware RDW Controller
The new runtime redirector is implemented at:

- `Assets/OpenRDW/Scripts/Redirection/Redirectors/OpportunityAwareRDWRedirector.cs`

It adds an opportunity-aware gain scheduling layer on top of the existing OpenRDW injection path:

`temporal state -> opportunity / steerability / gain budget -> gain scheduler -> InjectTranslation / InjectRotation / InjectCurvature`

Legacy redirectors are preserved.

## Validation
The uploaded version passed local compilation with:

```bash
dotnet build Assembly-CSharp.csproj -nologo
```

Implementation details, issues encountered, and the validation result are documented in `Opportunity_Aware_RDW_Controller.md`.
