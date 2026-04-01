# RDW-Aware Fine-Tuning Plan

## 1. Objective
Implement redirected-walking (RDW) aware fine-tuning for three trajectory predictors—SingularTrajectory, PECNet, and AgentFormer—so the models ingest both kinematic cues and RDW control signals. The end-state system uses 2.5 Hz sequences extracted from `RDWdatas_2_5hz/` and produces short-horizon forecasts robust to gains/resets.

## 2. Dataset & Required Signals
- **Source**: `@/Users/yph/Desktop/My_project/Unity/OpenRdw_vbdi/RDWdatas_2_5hz` (25 participants × multiple conditions, generated from `RDWdatas_10hz` via 4× downsampling).
- **Structure**: `<subject>_<cond>_<controller>/Sampled Metrics/Result/trialId_X/userId_Y/` mirrors Unity logger outputs (real/virtual positions, gains, injected motions, reset metrics). See `StatisticsLogger.AvatarStatistics` for field definitions @Assets/OpenRDW/Scripts/Analysis/StatisticsLogger.cs#89-377.
- **Per-frame feature vector (6 dims)** for time index *t*:
  1. `v_x`, `v_y`: real-position finite differences over 0.4 s (use `user_real_positions.csv` + `sampling_intervals.csv`).
  2. `g_r`: from `g_r.csv` (rotation gain samples).
  3. `g_c`: from `g_c.csv` (curvature gain samples, already scaled by `GetDeltaTime()` @Assets/OpenRDW/Scripts/Analysis/StatisticsLogger.cs#441-477).
  4. `g_t`: from `g_t.csv` (translation gain samples @Assets/OpenRDW/Scripts/Analysis/StatisticsLogger.cs#398-417).
  5. `reset_flag`: derive from reset timing—set 1 during frames between `Event_Reset_Triggered` and completion (use `virtual_distances_between_resets.csv` / `time_elapsed_between_resets.csv` and per-frame metadata; logger skips position sampling during resets unless overridden @Assets/OpenRDW/Scripts/Analysis/StatisticsLogger.cs#215-538).
- **Optional supervisory signals**: `injected_translations.csv`, `injected_rotations*.csv` for auxiliary losses; trial-level metadata from `Summary Statistics/Result.csv` (tracking space, redirector/resetter choices) for conditioning or stratified evaluation.

## 3. Preprocessing Pipeline
1. **Traverse dataset**: iterate participants → trials → avatars; gather contiguous sequences of length `History_Length` (e.g., 12 frames = 4.8 s) with corresponding prediction horizons (e.g., 12 future frames).
2. **Feature assembly**:
   - Compute velocities with central differencing where possible; fallback to forward diff at boundaries.
   - Normalize gains & velocities: compute dataset-wide mean/std per feature; store stats for inference.
   - Reset flag labeling: mark frames falling within reset windows (use logger timestamps or glean from `avatarIsResetting` if you replay raw logs; ensure flag aligns with missing trajectory points).
3. **Segmentation**: discard windows spanning missing data or large sampling gaps (>0.45 s) to maintain uniform stride.
4. **Splits**: subject-level splits recommended (e.g., 18 train / 4 val / 3 test) to measure cross-user generalization.
5. **Serialization**: save processed tensors as `.npz` or `.pt` with shape `[num_seq, hist_len, 6]` and targets `[num_seq, pred_len, 2]`. Keep metadata (participant, trialId, condition) for analysis.

## 4. Model Adaptations
### 4.1 Shared Conventions
- Replace original 2-D position inputs with the 6-dim RDW vector described above.
- Maintain original positional targets (future `(x, y)`), but optionally append future gain supervision for auxiliary heads.
- During evaluation, provide the same RDW-aware inputs (velocity + gains + reset flag) from the observed window.

### 4.2 SingularTrajectory (Transformer-based)
1. **Input embedding**: change first linear/projection layer to `in_features=6` (was 2). If the architecture uses learnable tokenizers (e.g., conv1D), update channel count accordingly.
2. **Positional encodings**: unchanged (still per time step). Ensure masking logic handles the longer channel dimension.
3. **Checkpoint loading**: when importing pre-trained weights, load with `strict=False` so only matching shapes are restored; reinitialize the resized input projection.
4. **Aux losses**: add optional heads predicting injected rotations/translations (L1) to encourage RDW-awareness—weights tuned via validation.
5. **Training hyperparams**: start with LR `1e-4`, batch `64`, history/pred horizons `12/12`, cosine LR decay over 60 epochs, teacher forcing for first 40 epochs.

### 4.3 PECNet
1. **Encoder MLP**: update first `nn.Linear` handling observed trajectories to accept 6 channels. If PECNet ingests concatenated `(x, y)` across history, rebuild the flattening step to include new features (i.e., reshape `[B, H, 6] → [B, H*6]`).
2. **Latent sampler**: untouched; still predicts future `(x, y)`.
3. **Losses**: standard ADE/FDE plus optional penalty encouraging consistent behavior near reset frames (e.g., weight residuals by reset flag to avoid overshooting after resets).
4. **Training tweaks**: because PECNet is sensitive to input scaling, standardize each feature before flattening and store stats. Recommended LR `5e-4`, batch `256`, KL warm-up 10 epochs.

### 4.4 AgentFormer
1. **Input projector**: change embedding layer to `nn.Linear(6, d_model)` (default `d_model=256`).
2. **Agent tokens**: when constructing multimodal tokens, include RDW vector per time step before temporal attention. No other attention changes needed.
3. **Weight loading**: instantiate new model, call `load_state_dict(state_dict, strict=False)` to reuse all non-input weights from ETH/UCY pretrain. Freeze first transformer block for first 5 epochs to stabilize.
4. **Training recipe**: LR `1e-4`, batch `32`, history/pred `8/12`, label smoothing 0.1, dropout 0.1. Use scheduled sampling (increase prediction conditioning probability from 0→0.5 between epochs 5-20).

## 5. Additional Parameters & Implementation Notes
- **Normalization stats**: compute from training split only; apply to val/test.
- **Data loaders**: support variable trial lengths; mask padded steps and ensure reset flag also padded with 0.
- **Augmentations**: optional spatial rotation/translation should also rotate velocities but leave gains/reset flag untouched (since they are already rotation-invariant scalars).
- **Evaluation metrics**: ADE/FDE overall, plus subset metrics for frames where reset flag=1 or gains exceed thresholds.
- **Logging**: store experiment configs (feature means, LR schedule, subject splits) alongside checkpoints for reproducibility.

## 6. Next Steps
1. Implement preprocessing script that reads `RDWdatas_2_5hz`, builds 6-dim features, and writes standardized tensors.
2. Update each model codebase with the input-layer changes and weight-loading logic described above.
3. Run three experiments (baseline zero-shot, naive fine-tune, RDW-aware fine-tune) for each model, log metrics, and compare on shared test split.

## 7. Runtime Controller Note
- The runtime-side first validation of the `Opportunity-Aware RDW Controller` is documented in `Opportunity_Aware_RDW_Controller.md`.
- That implementation keeps the original OpenRDW redirectors intact and adds a new redirector entry for opportunity-aware gain scheduling.
