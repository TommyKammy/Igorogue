#!/usr/bin/env python3
"""Validate FEAT-011 temporary-liberty expiry semantics and fixtures.

This checker contains a small board/group implementation only for the accepted
spec fixtures. It is not the product Rules Kernel; M1 must port these fixtures.
"""
from __future__ import annotations
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ROOT=Path(__file__).resolve().parents[1]
FIXTURE_PATH=ROOT/'game_data'/'fixtures'/'temporary_liberty_expiry_fixtures.json'
SYSTEM_PATH=ROOT/'game_data'/'balance'/'system.json'
CARDS_PATH=ROOT/'game_data'/'content'/'cards.json'
BOARD_CONDITIONS_PATH=ROOT/'game_data'/'content'/'board_conditions.json'
RULES_PATH=ROOT/'docs'/'20_Design'/'Rules Canon.md'
FEATURE_PATH=ROOT/'docs'/'20_Design'/'Feature Specs'/'FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep.md'
SACRIFICE_PATH=ROOT/'docs'/'20_Design'/'Feature Specs'/'FEAT-005 Sacrifice Triggers.md'
ADR_PATH=ROOT/'docs'/'60_Decisions'/'ADRs'/'ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep.md'
FIXTURE_DOC_PATH=ROOT/'docs'/'50_Validation'/'Spec Fixtures'/'FEAT-011 Temporary Liberty Expiry Fixtures.md'

Point=tuple[int,int]
DIRS=((1,0),(-1,0),(0,1),(0,-1))
EXPECTED_IDS={f'TLE-{i:02d}' for i in range(1,16)}


def load(path:Path)->Any:
    return json.loads(path.read_text(encoding='utf-8'))

def pkey(p:Point)->int:
    return (p[1]-1)*7+(p[0]-1)

def parse_board(rows:list[str], overrides:list[dict[str,Any]]|None=None)->dict[Point,dict[str,Any]]:
    board:dict[Point,dict[str,Any]]={}
    serial=0
    for ridx,row in enumerate(rows):
        y=7-ridx
        if len(row)!=7:
            raise ValueError(f'row length != 7: {row!r}')
        for x,ch in enumerate(row,1):
            if ch=='.': continue
            serial+=1
            if ch in 'BK': color='black'
            elif ch in 'WQ': color='white'
            else: raise ValueError(f'unknown stone char {ch!r}')
            board[(x,y)]={'color':color,'king':ch in 'KQ','kind':'basic','instance_id':f'stone_{serial:02d}'}
    for override in overrides or []:
        point=tuple(override['point'])
        if point not in board:
            raise ValueError(f'override point has no stone: {point}')
        board[point].update({k:v for k,v in override.items() if k!='point'})
    return board

def neighbours(p:Point):
    for dx,dy in DIRS:
        q=(p[0]+dx,p[1]+dy)
        if 1<=q[0]<=7 and 1<=q[1]<=7:
            yield q

def groups(board:dict[Point,dict[str,Any]])->list[dict[str,Any]]:
    seen:set[Point]=set(); out=[]
    for start in sorted(board,key=pkey):
        if start in seen: continue
        color=board[start]['color']; stack=[start]; pts=[]; seen.add(start)
        while stack:
            p=stack.pop(); pts.append(p)
            for q in neighbours(p):
                if q in board and q not in seen and board[q]['color']==color:
                    seen.add(q); stack.append(q)
        pts.sort(key=pkey)
        libs={q for p in pts for q in neighbours(p) if q not in board}
        out.append({'color':color,'points':pts,'anchor':pts[0],'liberties':libs,'contains_king':any(board[p]['king'] for p in pts)})
    out.sort(key=lambda g:pkey(g['anchor']))
    return out

def find_group(gs:list[dict[str,Any]], point:Point):
    return next((g for g in gs if point in g['points']),None)

def territory_regions(board:dict[Point,dict[str,Any]])->list[dict[str,Any]]:
    empty={(x,y) for y in range(1,8) for x in range(1,8) if (x,y) not in board}
    seen:set[Point]=set(); out=[]
    for start in sorted(empty,key=pkey):
        if start in seen: continue
        stack=[start]; seen.add(start); pts=[]; colors=set()
        while stack:
            p=stack.pop(); pts.append(p)
            for q in neighbours(p):
                if q in board: colors.add(board[q]['color'])
                elif q not in seen:
                    seen.add(q); stack.append(q)
        owner=next(iter(colors)) if len(colors)==1 else 'neutral'
        pts.sort(key=pkey)
        out.append({'points':pts,'owner':owner,'size':len(pts),'basic_income':(len(pts)+2)//3})
    return out

def topo_key(board:dict[Point,dict[str,Any]])->str:
    chars=[]
    for y in range(1,8):
        for x in range(1,8):
            stone=board.get((x,y))
            if not stone: chars.append('.')
            elif stone['color']=='black': chars.append('K' if stone['king'] else 'B')
            else: chars.append('Q' if stone['king'] else 'W')
    return ''.join(chars)

def group_desc(g:dict[str,Any],board:dict[Point,dict[str,Any]])->dict[str,Any]:
    return {'color':g['color'],'anchor':list(g['anchor']),'count':len(g['points']),'contains_king':g['contains_king']}

def validate_subset(fid:str, actual:Any, expected:Any, path:str='')->list[str]:
    errors=[]
    if isinstance(expected,dict):
        if not isinstance(actual,dict): return [f'{fid}{path}: expected object, got {actual!r}']
        for k,v in expected.items():
            if k not in actual: errors.append(f'{fid}{path}: missing key {k!r}')
            else: errors.extend(validate_subset(fid,actual[k],v,path+f'.{k}'))
    elif isinstance(expected,list):
        if actual!=expected: errors.append(f'{fid}{path}: expected {expected!r}, got {actual!r}')
    elif actual!=expected:
        errors.append(f'{fid}{path}: expected {expected!r}, got {actual!r}')
    return errors

def run_case(case:dict[str,Any],config:dict[str,Any])->dict[str,Any]:
    if case['operation']=='phase_order':
        return {'phase_order':['enemy_normal_action','enemy_counterattack_action','consume_current_pending_and_reprime_overflow','temporary_liberty_expiry_sweep','enemy_turn_end_counterattack_gain','plan_next_intents']}
    board=parse_board(case['board'],case.get('stone_overrides'))
    pre_groups=groups(board)
    current=case['enemy_turn_index']
    effects=case.get('effects',[])
    due=sorted([e for e in effects if e['expires_after_enemy_turn_index']==current],key=lambda e:(e['created_sequence'],e['id']))
    remaining=[e for e in effects if e not in due]
    if not due:
        return {'expired_effect_ids':[],'remaining_effect_ids':[e['id'] for e in remaining],'captured_groups':[],'event_order':[],'battle_result':'ongoing'}

    pre_anchor=None
    if 'pre_expiry_anchor_group' in case.get('expected',{}):
        first=effects[0]; g=find_group(pre_groups,tuple(first['anchor_point']))
        pre_bonus=sum(e['amount'] for e in effects if g and tuple(e['anchor_point']) in g['points'])
        pre_anchor={'anchor':list(g['anchor']),'count':len(g['points']),'temporary_bonus':pre_bonus} if g else None

    continuous=case.get('continuous_modifiers',[])
    gs=groups(board); doomed=[]
    for g in gs:
        timed=sum(e['amount'] for e in remaining if tuple(e['anchor_point']) in g['points'])
        cont=sum(e['amount'] for e in continuous if tuple(e['anchor_point']) in g['points'])
        if len(g['liberties'])+timed+cont==0:
            doomed.append(g)
    doomed.sort(key=lambda g:pkey(g['anchor']))
    captured_stones=[]
    for g in doomed:
        for p in g['points']:
            captured_stones.append((g,p,board[p].copy()))
    for _,p,_ in captured_stones:
        board.pop(p,None)

    black_king=any(stone['king'] and stone['color']=='black' for _,_,stone in captured_stones)
    white_king=any(stone['king'] and stone['color']=='white' for _,_,stone in captured_stones)
    if black_king: result='loss'
    elif white_king: result='win'
    else: result='ongoing'
    terminal=result!='ongoing'

    event_order=[f'TemporaryLibertyExpirySweepStarted:{current}']+[f'TemporaryLibertyExpired:{e["id"]}' for e in due]
    event_order += [f'GroupCaptured:{g["color"]}:{g["anchor"][0]},{g["anchor"][1]}:{len(g["points"])}' for g in doomed]
    first_seen=not case.get('result_topology_seen_before',False) if doomed else None
    if doomed: event_order.append(f'StoneTopologyRegistered:first_seen={str(first_seen).lower()}')
    if black_king: event_order.append('BattleLost:black_king_captured')
    elif white_king: event_order.append('BattleWon:white_king_captured')

    reserved_draw=0; reserved_qi=0; soul=0; choices=[]; benefit_order=[]
    counter_adv=[]; remainder=case.get('start_sacrifice_remainder',0)
    capturing_colors=[]
    for g in doomed:
        capturing_colors.append('white' if g['color']=='black' else 'black')
    if terminal:
        benefits_suppressed=True
    else:
        benefits_suppressed=False
        # Standard reward: one soul per captured white group.
        white_groups=sum(1 for g in doomed if g['color']=='white')
        soul += white_groups
        # Captured-stone self effects in group/point order.
        for g in doomed:
            for p in g['points']:
                stone=next(st for gg,pp,st in captured_stones if pp==p)
                if stone['color']=='black' and stone.get('kind')=='lure':
                    reserved_draw+=2; benefit_order.append(f'lure:{stone["instance_id"]}:reserve_draw_2')
                elif stone['color']=='black' and stone.get('kind')=='blood':
                    reserved_draw+=1; soul+=1
                    benefit_order.append(f'blood:{stone["instance_id"]}:reserve_draw_1')
                    benefit_order.append(f'blood:{stone["instance_id"]}:soul_1')
        # Armed capture effect and equipment for captured white groups.
        if white_groups and case.get('armed_capture_chain'):
            reserved_qi+=1; reserved_draw+=2
            benefit_order += ['capture_chain:reserve_qi_1','capture_chain:reserve_draw_2']
        if white_groups and 'relic_hungry_furnace' in case.get('equipped_relics',[]):
            reserved_qi+=2; benefit_order.append('relic_hungry_furnace:reserve_qi_2')
        if white_groups and 'seal_bone' in case.get('equipped_seals',[]):
            choices.append('seal_bone:qi_or_draw'); benefit_order.append('seal_bone:deferred_choice')
        black_nonking=sum(1 for _,_,s in captured_stones if s['color']=='black' and not s['king'])
        if black_nonking and case.get('style_id')=='style_sacrifice' and not case.get('style_first_capture_used',False):
            reserved_draw+=2; benefit_order.append('style_sacrifice:first_capture:reserve_draw_2')
        if black_nonking and 'seal_sacrifice' in case.get('equipped_seals',[]) and not case.get('seal_first_capture_used',False):
            reserved_draw+=2; benefit_order.append('seal_sacrifice:first_capture:reserve_draw_2')
        if black_nonking and case.get('style_id')=='style_sacrifice':
            total=remainder+black_nonking; batches,remainder=divmod(total,3)
            if batches:
                counter_adv.append({'reason':'sacrifice_batch','delta_units':30*batches})
        event_order += benefit_order

    # Territory inspection after capture.
    regions=territory_regions(board)
    territory_result={}
    for raw in case.get('territory_queries',[]):
        point=tuple(raw); region=next(r for r in regions if point in r['points'])
        territory_result[f'{point[0]},{point[1]}']={'owner':region['owner'],'size':region['size'],'basic_income':region['basic_income']}

    pending=False; end_units=case.get('start_counterattack_units',0)
    if not terminal and ('start_counterattack_units' in case):
        threshold=config['counterattack']['threshold_units']
        for row in counter_adv:
            end_units += row['delta_units']
            if not pending and end_units>=threshold:
                end_units-=threshold; pending=True
        natural=config['counterattack']['enemy_turn_end_gain']['base_units']+config['counterattack']['enemy_turn_end_gain']['per_komi_units']*case.get('komi',0)
        counter_adv.append({'reason':'enemy_turn_end','delta_units':natural})
        end_units += natural
        if not pending and end_units>=threshold:
            end_units-=threshold; pending=True

    event_order.append(f'TemporaryLibertyExpirySweepResolved:{len(doomed)}')
    actual={
      'expired_effect_ids':[e['id'] for e in due],
      'remaining_effect_ids':[e['id'] for e in remaining],
      'continuous_modifier_ids':[e['id'] for e in continuous],
      'captured_groups':[group_desc(g,board) for g in doomed],
      'group_capture_event_count':len(doomed),
      'capturing_colors':capturing_colors,
      'captured_king_colors':([c for c,flag in [('black',black_king),('white',white_king)] if flag]),
      'battle_result':result,
      'benefits_suppressed':benefits_suppressed,
      'reserved_draw_delta':reserved_draw,
      'reserved_qi_delta':reserved_qi,
      'soul_delta':soul,
      'deferred_choices':choices,
      'benefit_event_order':benefit_order,
      'sacrifice_remainder':remainder,
      'counterattack_delta_units':sum(x['delta_units'] for x in counter_adv if x['reason']=='sacrifice_batch'),
      'counterattack_advances':counter_adv,
      'end_counterattack_units':end_units,
      'pending':pending,
      'topology_first_seen':first_seen,
      'capture_was_blocked_by_repetition':False,
      'territories':territory_result,
      'momentum_delta':0,
      'brilliant_delta':0.0,
      'event_order':event_order,
    }
    if pre_anchor is not None: actual['pre_expiry_anchor_group']=pre_anchor
    return actual

def main()->int:
    errors=[]
    system=load(SYSTEM_PATH); cfg=system.get('temporary_liberties',{})
    expected_cfg={
      'version':'1.0.0','timed_duration_kind':'enemy_turn_end',
      'expire_all_due_simultaneously':True,
      'capture_all_zero_effective_liberty_groups_simultaneously':True,
      'expiry_phase_before':'enemy_turn_end_counterattack_gain',
      'mandatory_topology_mutation_ignores_repetition_legality':True,
    }
    for k,v in expected_cfg.items():
        if cfg.get(k)!=v: errors.append(f'temporary_liberties.{k} expected {v!r}, got {cfg.get(k)!r}')
    cards={c['id']:c for c in load(CARDS_PATH)}
    reinforce=cards.get('card_reinforce',{})
    temp=next((e for e in reinforce.get('effects',[]) if e.get('op')=='temporary_liberty'),None)
    if not temp: errors.append('card_reinforce missing temporary_liberty effect')
    elif temp.get('duration',{}).get('kind')!='enemy_turn_end': errors.append('card_reinforce duration must be enemy_turn_end object')
    bcs={b['id']:b for b in load(BOARD_CONDITIONS_PATH)}
    spring=bcs.get('board_spirit_spring',{})
    if spring.get('duration_kind')!='continuous_while_adjacent': errors.append('spirit spring must be continuous, not timed')

    data=load(FIXTURE_PATH); cases=data.get('cases',[])
    ids={c.get('id') for c in cases}
    if ids!=EXPECTED_IDS: errors.append(f'fixture IDs mismatch: {sorted(ids)}')
    for case in cases:
        try: actual=run_case(case,system)
        except Exception as exc:
            errors.append(f'{case.get("id")}: checker exception: {exc}')
            continue
        errors.extend(validate_subset(case.get('id','?'),actual,case.get('expected',{})))

    required={
      RULES_PATH:['version: 0.2.7','仮呼吸点失効掃引','[[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]'],
      FEATURE_PATH:['status: accepted','TLE-01','両王石同時captureはloss'],
      SACRIFICE_PATH:['status: accepted','TurnReservedQi','terminal batch'],
      ADR_PATH:['status: accepted','mandatory mutation','black king captured'],
      FIXTURE_DOC_PATH:['TLE-01','TLE-15'],
    }
    for path,needles in required.items():
        if not path.exists(): errors.append(f'missing {path.relative_to(ROOT)}'); continue
        text=path.read_text(encoding='utf-8')
        for needle in needles:
            if needle not in text: errors.append(f'{path.relative_to(ROOT)} missing {needle!r}')
    if errors:
        print('Temporary liberty checks failed:')
        for e in errors: print('-',e)
        return 1
    print('Temporary liberty checks passed — 15 deterministic fixtures')
    return 0

if __name__=='__main__':
    raise SystemExit(main())
