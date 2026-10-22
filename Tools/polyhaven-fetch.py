# ดาวน์โหลดชุดภาพฟรี CC0 จาก Poly Haven (https://polyhaven.com) — ท้องฟ้า HDRI / พื้นผิว PBR / โมเดล glTF
#   python Tools/polyhaven-fetch.py <out_dir>
import json, os, sys, urllib.request

OUT = sys.argv[1] if len(sys.argv) > 1 else 'Raw/PolyHaven'
UA = {'User-Agent': 'cozy-fishing-unity/1.0'}
HDRIS = {'kloofendal_48d_partly_cloudy_puresky': '2k'}
TEXTURES = ['leafy_grass', 'brown_mud_02', 'grass_path_3', 'weathered_brown_planks', 'bark_brown_02', 'thatch_roof_angled']
TEX_MAPS = ['Diffuse', 'nor_gl', 'Rough', 'AO']
MODELS = ['jacaranda_tree', 'rock_moss_set_01', 'boulder_01', 'namaqualand_boulder_02', 'stone_01', 'fern_02', 'shrub_02', 'shrub_03', 'shrub_04', 'flower_empodium', 'weed_plant_02']

def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA)) as r:
        return r.read()

def save(url, path):
    if os.path.exists(path):
        return 0
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = get(url)
    with open(path, 'wb') as f:
        f.write(data)
    return len(data)

total = 0
for hid, res in HDRIS.items():
    files = json.loads(get(f'https://api.polyhaven.com/files/{hid}'))
    total += save(files['hdri'][res]['hdr']['url'], f'{OUT}/hdri/{hid}_{res}.hdr')
for tid in TEXTURES:
    files = json.loads(get(f'https://api.polyhaven.com/files/{tid}'))
    for m in TEX_MAPS:
        if m in files and '1k' in files[m]:
            fmt = 'jpg' if 'jpg' in files[m]['1k'] else 'png'
            total += save(files[m]['1k'][fmt]['url'], f'{OUT}/textures/{tid}/{tid}_{m}_1k.{fmt}')
for mid in MODELS:
    files = json.loads(get(f'https://api.polyhaven.com/files/{mid}'))
    g = files['gltf']['1k']['gltf']
    total += save(g['url'], f'{OUT}/models/{mid}/{mid}_1k.gltf')
    for rel, inc in g.get('include', {}).items():
        total += save(inc['url'], f'{OUT}/models/{mid}/{rel}')
    print('model', mid, flush=True)
print(f'done, downloaded {total / 1e6:.1f} MB')
