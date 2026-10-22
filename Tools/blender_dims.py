# พิมพ์ขนาดรวม (กว้าง/ลึก/สูง หน่วยเมตร) ของโมเดลแต่ละไฟล์ หรือของ node ที่ระบุ
#   blender -b --python Tools/blender_dims.py -- <file.glb> [node ...]
import bpy, sys
from mathutils import Vector
args = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=args[0])
def dims(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == 'MESH' for c in o.bound_box]
    return [round(max(v[i] for v in pts) - min(v[i] for v in pts), 2) for i in range(3)] if pts else None
if len(args) == 1:
    print('DIM', args[0], dims(bpy.data.objects), [m.name for m in bpy.data.materials])
for name in args[1:]:
    r = bpy.data.objects.get(name)
    print('DIM', name, dims([r] + list(r.children_recursive)) if r else 'missing')
