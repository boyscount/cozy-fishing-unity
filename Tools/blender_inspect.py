# พิมพ์โครงสร้างโมเดล glTF (object / จำนวนสามเหลี่ยม / วัสดุ / ขนาด) — blender -b --python Tools/blender_inspect.py -- <file.gltf>
import bpy, sys
path = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=path)
total = 0
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    total += tris
    mats = ','.join(m.name for m in o.data.materials if m)
    print(f'OBJ {o.name} tris={tris} mats={mats} dims={tuple(round(d, 2) for d in o.dimensions)}')
print('TOTAL', total)
