#!/usr/bin/env python3
"""Validate ADR-0001's machine-readable engine/toolchain decision."""
from __future__ import annotations
from pathlib import Path
import json
import re

ROOT=Path(__file__).resolve().parents[1]
DECISION=ROOT/'toolchain'/'engine_decision.json'
ADR=ROOT/'docs'/'60_Decisions'/'ADRs'/'ADR-0001 Engine and Repository.md'
ARCH=ROOT/'docs'/'30_Technical'/'Engine Toolchain and Repository Layout.md'
VERSION=ROOT/'.godot-version'

REQUIRED_CRITERIA={
    'domain_isolation_headless','deterministic_test_sim','pixel_ui_iteration',
    'ci_build_export','codex_git_worktree','desktop_distribution',
    'licensing_lockin','debugging_tooling'
}
REQUIRED_CANDIDATES={'godot_4_7_dotnet','unity_6','monogame_3_8_4'}
REQUIRED_HARD={
    'one_rules_kernel_for_game_replay_and_formal_simulator',
    'domain_tests_run_without_engine_process',
    'no_engine_types_in_domain_public_api',
    'deterministic_seed_command_and_content_hash_replay',
    'desktop_pc_first',
    'text_reviewable_source_control'
}

def main()->int:
    errors=[]
    try:
        data=json.loads(DECISION.read_text(encoding='utf-8'))
    except Exception as exc:
        print(f'Engine decision checks failed:\n- cannot load {DECISION.relative_to(ROOT)}: {exc}')
        return 1

    if data.get('decision_id')!='ADR-0001': errors.append('decision_id must be ADR-0001')
    if data.get('status')!='accepted': errors.append('machine-readable status must be accepted')
    if data.get('selected_candidate')!='godot_4_7_dotnet': errors.append('selected candidate must be godot_4_7_dotnet')

    criteria=data.get('criteria',[])
    ids=[c.get('id') for c in criteria if isinstance(c,dict)]
    if set(ids)!=REQUIRED_CRITERIA: errors.append(f'criteria mismatch: {set(ids)^REQUIRED_CRITERIA}')
    weights=[c.get('weight') for c in criteria if isinstance(c,dict)]
    if any(not isinstance(w,int) or w<=0 for w in weights): errors.append('criterion weights must be positive integers')
    if sum(w for w in weights if isinstance(w,int))!=100: errors.append('criterion weights must sum to 100')
    weight_map={c['id']:c['weight'] for c in criteria if isinstance(c,dict) and c.get('id') and isinstance(c.get('weight'),int)}

    candidates=data.get('candidates',{})
    if set(candidates)!=REQUIRED_CANDIDATES: errors.append(f'candidate mismatch: {set(candidates)^REQUIRED_CANDIDATES}')
    computed={}
    for cid, cand in candidates.items():
        scores=cand.get('scores',{}) if isinstance(cand,dict) else {}
        if set(scores)!=REQUIRED_CRITERIA:
            errors.append(f'{cid}: score criteria mismatch')
            continue
        if any(not isinstance(v,int) or v<1 or v>5 for v in scores.values()):
            errors.append(f'{cid}: scores must be integers 1..5')
            continue
        total=round(sum(weight_map[k]*scores[k]/5 for k in REQUIRED_CRITERIA),1)
        computed[cid]=total
        if cand.get('weighted_total')!=total:
            errors.append(f"{cid}: weighted_total expected {total}, got {cand.get('weighted_total')}")
    if computed and max(computed,key=computed.get)!=data.get('selected_candidate'):
        errors.append('selected candidate is not the highest weighted score')

    selected=data.get('selected',{})
    expected={
        'engine':'Godot','engine_line':'4.7-stable','edition':'.NET',
        'renderer':'gl_compatibility','production_language':'C#',
        'csharp_language_version':'12.0','dotnet_target_framework':'net8.0',
        'test_framework':'xUnit','repository':'Git','repository_host':'GitHub'
    }
    for key,value in expected.items():
        if selected.get(key)!=value: errors.append(f'selected.{key} expected {value!r}, got {selected.get(key)!r}')
    if selected.get('logical_resolution')!=[480,270]: errors.append('logical_resolution must be [480, 270]')
    if selected.get('stretch_scale_mode')!='integer': errors.append('stretch_scale_mode must be integer')

    hard=set(data.get('hard_constraints',[]))
    if hard!=REQUIRED_HARD: errors.append(f'hard constraint mismatch: {hard^REQUIRED_HARD}')
    policy=data.get('version_policy',{})
    if policy.get('prerelease')!='prohibited': errors.append('prerelease engines must be prohibited')
    if policy.get('minor_or_major')!='successor_adr_required': errors.append('minor/major upgrade must require successor ADR')

    if VERSION.read_text(encoding='utf-8').strip()!=selected.get('engine_line'):
        errors.append('.godot-version does not match selected engine_line')

    adr=ADR.read_text(encoding='utf-8')
    if not re.search(r'^status:\s*accepted\s*$',adr,re.M): errors.append('ADR-0001 frontmatter must be accepted')
    for token in ['Godot 4.7 stable, .NET edition','MonoGame 3.8.4','Unity 6','Igorogue.Domain','GitHub']:
        if token not in adr: errors.append(f'ADR-0001 missing required token: {token}')
    arch=ARCH.read_text(encoding='utf-8')
    for token in ['dotnet test','Igorogue.Sim.Cli','no Godot reference','GODOT_BIN']:
        if token not in arch: errors.append(f'engine architecture note missing required token: {token}')

    if errors:
        print('Engine decision checks failed:')
        for error in errors: print('-',error)
        return 1
    print(f"Engine decision checks passed — selected {selected['engine']} {selected['engine_line']} {selected['edition']}; score {computed['godot_4_7_dotnet']}/100.")
    return 0

if __name__=='__main__':
    raise SystemExit(main())
