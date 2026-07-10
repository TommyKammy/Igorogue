from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math, random

OUT = Path(__file__).resolve().parents[1] / 'docs' / '25_UIUX' / 'assets'
OUT.mkdir(parents=True, exist_ok=True)
S=4
W,H=480,270
FONT_CANDIDATES_REG=[
 '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',
 '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',
]
FONT_CANDIDATES_BOLD=[
 '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc',
 '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',
]
FONT_REG=next((p for p in FONT_CANDIDATES_REG if Path(p).exists()), FONT_CANDIDATES_REG[-1])
FONT_BOLD=next((p for p in FONT_CANDIDATES_BOLD if Path(p).exists()), FONT_CANDIDATES_BOLD[-1])

C={
 'bg':'#090d10','bg2':'#0d1418','panel':'#121b20','panel2':'#172228','line':'#77694b','line2':'#b69b5e',
 'text':'#e9e4d2','muted':'#a9ad9f','gold':'#e4b653','jade':'#54b785','red':'#c6574c','indigo':'#7763b8',
 'cyan':'#4fa6a3','wood':'#b87e43','wood2':'#8e5d33','grid':'#4b2f1b','black':'#141518','white':'#e7dfc8',
 'orange':'#cf7947','darkgold':'#8a6a2e','shadow':'#050708','pink':'#b34f78','blue':'#4779a6'
}

def font(size,bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT_REG, size)

def txt(d,xy,s,size=8,fill=None,bold=False,anchor=None):
    d.text(xy,s,font=font(size,bold),fill=fill or C['text'],anchor=anchor,stroke_width=0)

def rect(d,box,fill=None,outline=None,w=1):
    d.rectangle(box,fill=fill,outline=outline,width=w)

def bevel(d,box,fill=C['panel'],outline=C['line2']):
    x0,y0,x1,y1=box
    rect(d,box,fill,outline,1)
    d.line((x0+1,y0+1,x1-1,y0+1),fill='#435058')
    d.line((x0+1,y0+1,x0+1,y1-1),fill='#354148')
    d.line((x0+1,y1-1,x1-1,y1-1),fill=C['shadow'])
    d.line((x1-1,y0+1,x1-1,y1-1),fill=C['shadow'])

def meter(d,box,val,maxv,fill,label=None):
    x0,y0,x1,y1=box
    rect(d,box,C['bg'],C['line'])
    p=max(0,min(1,val/maxv))
    rect(d,(x0+1,y0+1,int(x0+1+(x1-x0-2)*p),y1-1),fill,None)
    if label: txt(d,((x0+x1)//2,(y0+y1)//2),label,6,C['text'],True,'mm')

def coin(d,x,y,r=5,fill=None):
    fill=fill or C['gold']
    d.ellipse((x-r,y-r,x+r,y+r),fill=fill,outline='#f5d789')
    d.ellipse((x-r+2,y-r+2,x+r-2,y+r-2),outline=C['darkgold'])

def stone(d,x,y,color,r=7,king=False):
    base=C['black'] if color=='b' else C['white']
    edge='#000000' if color=='b' else '#7f796b'
    d.ellipse((x-r,y-r,x+r,y+r),fill=edge)
    d.ellipse((x-r+1,y-r+1,x+r-1,y+r-1),fill=base)
    if color=='b': d.ellipse((x-r+2,y-r+2,x-r+5,y-r+5),fill='#4b4e54')
    else: d.ellipse((x-r+2,y-r+2,x-r+5,y-r+5),fill='#fffaf0')
    if king:
        d.rectangle((x-3,y-3,x+3,y+3),outline=C['gold'])
        d.point((x,y),fill=C['gold'])

def icon(d,x,y,kind,c=None):
    c=c or C['gold']
    if kind=='qi':
        d.polygon([(x,y-6),(x+4,y-1),(x+1,y),(x+5,y+6),(x-2,y+2),(x-5,y+3),(x-2,y-1)],fill=c)
    elif kind=='soul':
        d.ellipse((x-4,y-5,x+4,y+3),outline=c)
        d.line((x-2,y+2,x-4,y+6),fill=c); d.line((x+2,y+2,x+4,y+6),fill=c)
    elif kind=='territory':
        d.rectangle((x-5,y-5,x+5,y+5),outline=c); d.line((x-5,y,x+5,y),fill=c); d.line((x,y-5,x,y+5),fill=c)
    elif kind=='momentum':
        d.polygon([(x-6,y-2),(x,y-2),(x,y-5),(x+6,0+y),(x,y+5),(x,y+2),(x-6,y+2)],fill=c)
    elif kind=='relic':
        d.polygon([(x,y-6),(x+5,y-3),(x+4,y+4),(x,y+6),(x-4,y+4),(x-5,y-3)],fill=c)
    elif kind=='enemy':
        d.rectangle((x-5,y-4,x+5,y+5),fill=c); d.rectangle((x-3,y-7,x-1,y-4),fill=c); d.rectangle((x+1,y-7,x+3,y-4),fill=c)
        d.point((x-2,y),fill=C['bg']); d.point((x+2,y),fill=C['bg'])
    elif kind=='card':
        d.rectangle((x-5,y-7,x+5,y+7),fill='#e2dcc8',outline=c); d.line((x-3,y-3,x+3,y-3),fill=c); d.line((x-3,y,x+3,y),fill=c)

def panel_title(d,box,title,accent=None):
    x0,y0,x1,y1=box
    bevel(d,box)
    rect(d,(x0,y0,x1,y0+14),C['panel2'],C['line'])
    txt(d,(x0+6,y0+7),title,7,accent or C['gold'],True,'lm')

def draw_board(d,x,y,size=164):
    rect(d,(x,y,x+size,y+size),C['wood2'],C['line2'])
    rect(d,(x+5,y+5,x+size-5,y+size-5),C['wood'],C['grid'])
    margin=12; step=(size-2*margin)//6
    for i in range(7):
        p=x+margin+i*step; q=y+margin+i*step
        d.line((p,y+margin,p,y+margin+6*step),fill=C['grid'])
        d.line((x+margin,q,x+margin+6*step,q),fill=C['grid'])
    # hoshi
    for gx,gy in [(1,1),(3,3),(5,5),(1,5),(5,1)]:
        px=x+margin+gx*step; py=y+margin+gy*step
        d.rectangle((px-1,py-1,px+1,py+1),fill=C['grid'])
    return margin,step

def card(d,box,cost,title,kind='stone',rarity='common',selected=False,disabled=False):
    x0,y0,x1,y1=box
    colors={'common':'#65737b','uncommon':C['jade'],'rare':C['gold'],'curse':C['red']}
    outline=colors.get(rarity,C['line2'])
    fill='#182228' if not disabled else '#111416'
    rect(d,box,fill,('#fff3b0' if selected else outline),2 if selected else 1)
    rect(d,(x0+2,y0+2,x1-2,y0+12),C['panel2'],None)
    coin(d,x0+9,y0+8,5,C['indigo'] if cost==0 else C['gold'])
    txt(d,(x0+9,y0+8),str(cost),6,C['bg'],True,'mm')
    txt(d,(x0+17,y0+7),title,6,C['text'],True,'lm')
    # icon area
    cx=(x0+x1)//2; cy=y0+27
    if kind=='stone': stone(d,cx,cy,'b',5)
    elif kind=='jump':
        stone(d,cx-8,cy+4,'b',4); stone(d,cx+8,cy-4,'b',4); d.line((cx-3,cy,cx+3,cy),fill=C['jade'])
    elif kind=='sacrifice': stone(d,cx,cy,'b',5); d.line((cx-6,cy-6,cx+6,cy+6),fill=C['red']); d.line((cx+6,cy-6,cx-6,cy+6),fill=C['red'])
    elif kind=='facility':
        d.rectangle((cx-7,cy-5,cx+7,cy+6),fill=C['wood2'],outline=C['gold']); d.polygon([(cx-9,cy-5),(cx,cy-11),(cx+9,cy-5)],fill=C['red'])
    elif kind=='seal': icon(d,cx,cy,'relic',C['gold'])
    elif kind=='capture':
        stone(d,cx-5,cy,'b',4); stone(d,cx+5,cy,'w',4); d.line((cx,cy-8,cx,cy+8),fill=C['red'])
    txt(d,(x0+5,y1-11),'配置条件',5,C['muted'])
    txt(d,(x0+5,y1-5),'効果の要約',5,C['text'])

def final(img,name):
    big=img.resize((W*S,H*S),resample=Image.Resampling.NEAREST)
    big.save(OUT/name)

# 1 Battle screen
img=Image.new('RGB',(W,H),C['bg']); d=ImageDraw.Draw(img)
# top bar
rect(d,(0,0,W-1,20),C['panel'],C['line2'])
txt(d,(8,10),'IGOROGUE（仮）',10,C['gold'],True,'lm')
txt(d,(126,10),'ACT 1 / BATTLE 3   TURN 6',7,C['muted'],False,'lm')
txt(d,(286,10),'妙手 ×2.8',8,C['jade'],True,'lm')
txt(d,(390,10),'KOMI 7',7,C['red'],True,'lm')
# left stats
panel_title(d,(5,25,91,201),'黒の盤勢')
for i,(kind,label,val,col) in enumerate([
 ('qi','気','7',C['gold']),('soul','魂','12',C['indigo']),('territory','領地','+4',C['jade']),('momentum','余勢','2/2',C['cyan'])]):
    yy=48+i*27; icon(d,18,yy,kind,col); txt(d,(32,yy-3),label,6,C['muted']); txt(d,(32,yy+6),val,10,col,True)
txt(d,(12,158),'王石安全度',6,C['muted'])
meter(d,(12,169,83,180),62,100,C['jade'],'62')
txt(d,(12,190),'次のドロー  5+1',6,C['text'])
# center board
bx,by,bs=108,27,194
m,st=draw_board(d,bx,by,bs)
# territory overlays
for gx,gy in [(0,0),(0,1),(1,0),(1,1),(2,0)]:
    px=bx+m+gx*st; py=by+m+gy*st
    d.rectangle((px-st//2+2,py-st//2+2,px+st//2-2,py+st//2-2),fill='#6c8b5b')
# grid redraw lightly after overlay
for i in range(7):
    p=bx+m+i*st; q=by+m+i*st
    d.line((p,by+m,p,by+m+6*st),fill=C['grid']); d.line((bx+m,q,bx+m+6*st,q),fill=C['grid'])
# stones
blacks=[(1,1,1),(1,2,0),(2,1,0),(0,2,0),(2,2,0),(3,2,0),(3,3,0),(4,3,0),(4,4,0)]
whites=[(5,5,1),(4,5,0),(5,4,0),(3,4,0),(3,5,0),(2,5,0),(5,3,0)]
for gx,gy,k in blacks: stone(d,bx+m+gx*st,by+m+gy*st,'b',7,k)
for gx,gy,k in whites: stone(d,bx+m+gx*st,by+m+gy*st,'w',7,k)
# target and atari
px=bx+m+4*st; py=by+m+5*st
d.rectangle((px-10,py-10,px+10,py+10),outline=C['red'],width=2)
txt(d,(bx+bs//2,by+bs+7),'赤枠：次の手でアタリ',6,C['red'],True,'mm')
# right enemy
panel_title(d,(318,25,475,94),'白：断ち切り僧',C['red'])
icon(d,334,52,'enemy',C['red']); txt(d,(348,44),'次の意図：切断',7,C['text'],True)
txt(d,(348,56),'最大黒グループの連結点',6,C['muted'])
txt(d,(348,67),'予告対象：中央の黒石',6,C['red'])
txt(d,(326,82),'反攻',6,C['muted']); meter(d,(354,78,467,88),72,100,C['red'],'72 / 100')
# relics
panel_title(d,(318,100,475,151),'遺物  4 / 5')
for i,(name,col) in enumerate([('妙手の鐘',C['gold']),('菌糸碁笥',C['jade']),('白骨',C['white']),('見張塔',C['cyan']),('空き',C['muted'])]):
    x=332+i*28; rect(d,(x,119,x+21,143),C['bg2'],col); icon(d,x+10,130,'relic',col)
# log/prediction
panel_title(d,(318,157,475,221),'この手の予測')
txt(d,(326,179),'✓ 白1群をアタリ',6,C['jade'])
txt(d,(326,190),'✓ 妙手 +0.3 → ×3.1',6,C['gold'])
txt(d,(326,201),'! 過伸展：反攻 +8',6,C['red'])
txt(d,(326,212),'王石捕獲まで推定 2手',6,C['text'])
# hand bottom
rect(d,(0,226,W-1,H-1),C['panel'],C['line2'])
txt(d,(6,232),'手札 7 / 山札 14 / 捨札 9',6,C['muted'])
labels=[('ノビ',1,'stone','common'),('ツケ',1,'capture','common'),('市場',2,'facility','uncommon'),('一間トビ',1,'jump','uncommon'),('血石',1,'sacrifice','common'),('締め',1,'capture','uncommon'),('大模様',3,'facility','rare')]
for i,(name,cost,kind,rar) in enumerate(labels):
    card(d,(6+i*55,238,57+i*55,267),cost,name,kind,rar,selected=(i==1))
bevel(d,(398,230,474,265),C['panel2'],C['gold']); txt(d,(436,247),'ターン終了',8,C['gold'],True,'mm'); txt(d,(436,258),'SPACE',5,C['muted'],False,'mm')
final(img,'mockup_01_battle_screen.png')

# 2 Run preparation
img=Image.new('RGB',(W,H),C['bg']); d=ImageDraw.Draw(img)
rect(d,(0,0,W-1,20),C['panel'],C['line2']); txt(d,(8,10),'ラン準備 — 流儀と家元印',10,C['gold'],True,'lm'); txt(d,(400,10),'棋譜片 1,280',7,C['muted'],False,'lm')
# boss preview right
panel_title(d,(340,27,475,222),'ACT 1 予告',C['red'])
icon(d,357,52,'enemy',C['red']); txt(d,(372,45),'ボス：断ち切り僧',7,C['text'],True)
txt(d,(348,64),'最大グループの連結を狙う',6,C['muted'])
rect(d,(348,78,467,130),C['bg2'],C['line'])
txt(d,(357,89),'盤面条件',6,C['gold'],True)
txt(d,(357,102),'中央に山が2点',7,C['text'])
txt(d,(357,116),'侵入者の出現率 ↑',6,C['red'])
txt(d,(348,143),'推奨',6,C['muted']); txt(d,(348,154),'分散領地 / 忍び / 補強',6,C['jade'])
txt(d,(348,174),'公開情報はActごとに更新',5,C['muted'])
# styles
panel_title(d,(5,27,202,162),'流儀を1つ選ぶ')
styles=[('地合い流','小領地と施設','facility',C['jade']),('攻め碁流','捕獲と速攻','capture',C['red']),('厚み流','中央と模様','jump',C['indigo']),('捨て石流','犠牲と再生','sacrifice',C['orange'])]
for i,(name,desc,kind,col) in enumerate(styles):
    x=11+(i%2)*94; y=47+(i//2)*55
    rect(d,(x,y,x+88,y+48),C['panel2'],col,2 if i==2 else 1); icon(d,x+13,y+18,'relic',col); txt(d,(x+26,y+11),name,7,C['text'],True); txt(d,(x+26,y+25),desc,6,C['muted']); txt(d,(x+8,y+40),'選択中' if i==2 else '詳細',5,col,True)
# seals
panel_title(d,(208,27,334,222),'家元印  0〜3枠')
seals=[('天元の心得',2,C['indigo']),('菌糸の心得',1,C['jade']),('白骨の心得',4,C['white'])]
for i,(name,k,col) in enumerate(seals):
    y=48+i*45; rect(d,(216,y,326,y+36),C['bg2'],col); icon(d,230,y+18,'relic',col); txt(d,(243,y+10),name,6,C['text'],True); txt(d,(243,y+23),f'コミ {k}',6,C['red'])
rect(d,(216,185,326,211),C['panel2'],C['gold']); txt(d,(224,193),'合計コミ',6,C['muted']); txt(d,(308,193),'7',9,C['red'],True,'rm'); meter(d,(224,201,318,208),7,9,C['red'])
# summary
panel_title(d,(5,168,202,222),'開始時の予測')
txt(d,(13,189),'白王石の厚み：高',6,C['red'])
txt(d,(13,201),'自然反攻：敵9T / 熱で前倒し',6,C['gold'])
txt(d,(13,213),'棋力スコア倍率：×1.35',6,C['jade'])
bevel(d,(105,231,375,263),C['panel2'],C['gold']); txt(d,(240,247),'この構成でラン開始',10,C['gold'],True,'mm')
txt(d,(240,258),'流儀は開始後変更不可 / 印はコミと引き換え',5,C['muted'],False,'mm')
final(img,'mockup_02_run_preparation.png')

# 3 Reward and shop
img=Image.new('RGB',(W,H),C['bg']); d=ImageDraw.Draw(img)
rect(d,(0,0,W-1,20),C['panel'],C['line2']); txt(d,(8,10),'戦闘報酬 / 序盤ショップ',10,C['gold'],True,'lm'); txt(d,(382,10),'魂 6',7,C['indigo'],True,'lm')
panel_title(d,(5,27,286,156),'カード報酬 — 1枚選択')
# 3 large cards
reward=[('炉',2,'facility','uncommon','流儀シナジー'),('連続捕獲',2,'capture','rare','現在ビルド'),('種還り',1,'sacrifice','uncommon','ワイルド')]
for i,(name,cost,kind,rar,tag) in enumerate(reward):
    x=14+i*89; card(d,(x,49,x+78,143),cost,name,kind,rar,selected=(i==1)); txt(d,(x+39,151),tag,5,C['jade'] if i<2 else C['gold'],True,'mm')
txt(d,(10,164),'報酬生成：流儀 25% / 現在ビルド 50% / ワイルド 25%',5,C['muted'])
bevel(d,(11,175,97,197),C['panel2'],C['line']); txt(d,(54,186),'スキップ +魂1',6,C['muted'],True,'mm')
# shop
panel_title(d,(296,27,475,222),'旅商人')
shop=[('妙手の鐘',3,'relic',C['gold']),('忍び',2,'card',C['jade']),('カード削除',2,'card',C['red']),('再抽選',1,'relic',C['cyan'])]
for i,(name,price,kind,col) in enumerate(shop):
    y=49+i*39; rect(d,(305,y,466,y+31),C['bg2'],col); icon(d,320,y+15,'relic' if kind=='relic' else 'card',col); txt(d,(337,y+10),name,7,C['text'],True); coin(d,440,y+15,5); txt(d,(451,y+15),str(price),7,C['gold'],True,'lm')
txt(d,(305,208),'品揃えはseed固定 / 買わずに進行可能',5,C['muted'])
# bottom deck info
panel_title(d,(5,205,286,265),'デッキ診断')
metrics=[('仕込み',7,C['jade']),('発火',5,C['gold']),('攻め',4,C['red']),('守り',3,C['cyan'])]
for i,(label,val,col) in enumerate(metrics):
    x=13+i*67; txt(d,(x,224),label,6,C['muted']); meter(d,(x,235,x+55,244),val,10,col,str(val))
txt(d,(13,255),'提案：触媒は十分。次は守りかカード削除を検討。',6,C['text'])
final(img,'mockup_03_reward_shop.png')

# 4 map
img=Image.new('RGB',(W,H),C['bg']); d=ImageDraw.Draw(img)
rect(d,(0,0,W-1,20),C['panel'],C['line2']); txt(d,(8,10),'ACT 1 — 盤上巡礼',10,C['gold'],True,'lm'); txt(d,(320,10),'流儀：厚み / KOMI 7 / 魂 4',6,C['muted'],False,'lm')
# left info
panel_title(d,(5,28,132,235),'ラン状況')
txt(d,(13,49),'戦闘 2 / 4',8,C['text'],True)
txt(d,(13,66),'デッキ 15枚',6,C['muted']); txt(d,(13,78),'遺物 2 / 5',6,C['muted']); txt(d,(13,90),'施設系 4枚',6,C['muted'])
txt(d,(13,111),'白の反攻傾向',6,C['gold'],True); txt(d,(13,124),'切断 / 最大領地侵入',6,C['red'])
txt(d,(13,145),'現在の発火候補',6,C['gold'],True); txt(d,(13,158),'市場 + 大模様',6,C['jade']); txt(d,(13,170),'完成度 2 / 3',6,C['muted'])
txt(d,(13,193),'未発見',6,C['gold'],True); txt(d,(13,206),'? 詰碁イベント',6,C['muted'])
# map area
panel_title(d,(140,28,475,235),'ルート選択')
# nodes positions columns
nodes=[
 (170,188,'battle',C['red'],'戦'),(225,188,'shop',C['gold'],'商'),(280,188,'event',C['indigo'],'?'),
 (200,140,'elite',C['orange'],'強'),(265,140,'rest',C['jade'],'休'),(330,140,'battle',C['red'],'戦'),
 (235,92,'shop',C['gold'],'商'),(310,92,'event',C['indigo'],'?'),(375,92,'elite',C['orange'],'強'),
 (305,49,'boss',C['red'],'王')]
links=[(0,3),(0,4),(1,3),(1,4),(1,5),(2,4),(2,5),(3,6),(3,7),(4,6),(4,7),(4,8),(5,7),(5,8),(6,9),(7,9),(8,9)]
for a,b in links:
    x1,y1,_,_,_=nodes[a]; x2,y2,_,_,_=nodes[b]
    d.line((x1,y1,x2,y2),fill=C['line'],width=2)
for i,(x,y,kind,col,ch) in enumerate(nodes):
    d.ellipse((x-11,y-11,x+11,y+11),fill=C['panel2'],outline=col,width=2 if i==4 else 1)
    txt(d,(x,y),ch,8,col,True,'mm')
    if i==4: d.rectangle((x-14,y-14,x+14,y+14),outline=C['gold'])
txt(d,(250,219),'点線ではなく、公開された分岐を比較して選ぶ',5,C['muted'])
# legend
for i,(lab,col) in enumerate([('戦闘',C['red']),('商人',C['gold']),('休憩',C['jade']),('未知',C['indigo']),('強敵',C['orange'])]):
    x=150+i*60; d.rectangle((x,244,x+7,251),fill=col); txt(d,(x+11,248),lab,5,C['muted'],'', 'lm')
bevel(d,(370,241,470,264),C['panel2'],C['gold']); txt(d,(420,252),'選択して進む',7,C['gold'],True,'mm')
final(img,'mockup_04_act_map.png')

# 5 meta hub
img=Image.new('RGB',(W,H),C['bg']); d=ImageDraw.Draw(img)
rect(d,(0,0,W-1,20),C['panel'],C['line2']); txt(d,(8,10),'棋院 — 永続進行',10,C['gold'],True,'lm'); txt(d,(385,10),'棋譜片 3,420',7,C['gold'],True,'lm')
# nav left
panel_title(d,(5,27,102,263),'施設')
items=[('棋院','解放',C['gold']),('碁笥','家元印',C['indigo']),('棋譜棚','記録',C['jade']),('詰碁部屋','練習',C['cyan']),('段位戦','挑戦',C['red'])]
for i,(name,sub,col) in enumerate(items):
    y=49+i*39; rect(d,(12,y,95,y+31),C['panel2'],col,2 if i==0 else 1); icon(d,25,y+15,'relic',col); txt(d,(38,y+10),name,6,C['text'],True); txt(d,(38,y+21),sub,5,C['muted'])
# tree
panel_title(d,(110,27,346,263),'解放盤 — 強さではなく選択肢を増やす')
# branches
center=(228,143)
d.ellipse((217,132,239,154),fill=C['panel2'],outline=C['gold'],width=2); txt(d,center,'始',7,C['gold'],True,'mm')
branches=[
 ('地合い',[(175,105),(146,75),(130,45)],C['jade']),
 ('攻め',[(280,106),(311,77),(329,46)],C['red']),
 ('捨石',[(176,181),(145,211),(126,241)],C['orange']),
 ('厚み',[(280,181),(312,212),(332,242)],C['indigo'])]
for name,pts,col in branches:
    prev=center
    for j,(x,y) in enumerate(pts):
        d.line((prev[0],prev[1],x,y),fill=col,width=2)
        locked=(j==2)
        d.ellipse((x-10,y-10,x+10,y+10),fill=C['panel2'],outline=col)
        txt(d,(x,y),'?' if locked else str(j+1),7,col,True,'mm')
        prev=(x,y)
    txt(d,(pts[-1][0],pts[-1][1]+16),name,5,col,True,'mm')
# right details
panel_title(d,(354,27,475,263),'選択ノード')
txt(d,(363,50),'家元印：死線の心得',7,C['text'],True)
icon(d,371,77,'relic',C['orange'])
txt(d,(387,69),'コミ 2',7,C['red'],True)
txt(d,(387,82),'危険状態を資源化',6,C['muted'])
rect(d,(363,98,467,154),C['bg2'],C['line'])
txt(d,(370,109),'解放効果',6,C['gold'],True)
txt(d,(370,122),'ラン開始時の持込候補に追加',5,C['text'])
txt(d,(370,134),'初回アタリ利用：-1気 +1枚',5,C['text'])
txt(d,(370,146),'直接的な基礎能力は増えない',5,C['muted'])
txt(d,(363,168),'必要棋譜片',6,C['muted']); coin(d,438,169,5); txt(d,(450,169),'600',7,C['gold'],True,'lm')
bevel(d,(363,187,467,216),C['panel2'],C['gold']); txt(d,(415,201),'解放する',8,C['gold'],True,'mm')
txt(d,(363,230),'全解放後の目標',6,C['gold'],True)
txt(d,(363,242),'流儀別段位 / 高コミ記録',5,C['muted'])
txt(d,(363,252),'未発見コンボ / 棋譜図鑑',5,C['muted'])
final(img,'mockup_05_meta_hub.png')
print('created', list(p.name for p in OUT.glob('mockup_*.png')))
