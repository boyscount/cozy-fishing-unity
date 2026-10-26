# ลด polygon โมเดลจาก Poly Haven ให้เล่นบน WebGL ได้ แล้ว export เป็น .glb (พื้นผิว 1k ฝังในไฟล์)
#   blender -b --factory-startup --python Tools/blender_optimize.py -- <in.gltf> <out.glb> <target_tris> [leaf_keep]
# ต้นไม้: ใบไม้ (วัสดุชื่อมี "leaves") ใช้วิธีคัดใบไว้บางส่วน (ทั้งใบ ไม่ตัดครึ่ง) แล้วขยายใบที่เหลือ — decimate ธรรมดาจะทำใบพัง
import bpy, bmesh, random, sys
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
src, dst, target = args[0], args[1], int(args[2])
leaf_keep = float(args[3]) if len(args) > 3 else 0.0
random.seed(11)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.data.objects if o.type == 'MESH']

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def thin_leaves(o):
    """เก็บใบไว้ leaf_keep ส่วน (สุ่มทีละใบ = ชิ้นที่เชื่อมกัน) แล้วขยายใบที่เหลือ 1/sqrt(keep) เท่า (สูงสุด 2.6)"""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    leaf_idx = {i for i, m in enumerate(o.data.materials) if m and 'leaves' in m.name.lower()}
    if not leaf_idx:
        bm.free(); return
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    # union-find บนจุดยอดของหน้าใบไม้
    parent = {}
    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]; a = parent[a]
        return a
    leaf_faces = [f for f in bm.faces if f.material_index in leaf_idx]
    for f in leaf_faces:
        vs = [v.index for v in f.verts]
        for v in vs: parent.setdefault(v, v)
        r0 = find(vs[0])
        for v in vs[1:]:
            r = find(v)
            if r != r0: parent[r] = r0
    groups = {}
    for f in leaf_faces:
        groups.setdefault(find(f.verts[0].index), []).append(f)
    scale = min(4.0, (1 / max(leaf_keep, 1e-3)) ** 0.5)
    kill = []
    for faces in groups.values():
        if random.random() < leaf_keep:
            vs = {v for f in faces for v in f.verts}
            c = sum((v.co for v in vs), Vector()) / len(vs)
            for v in vs: v.co = c + (v.co - c) * scale
        else:
            kill.extend(faces)
    bmesh.ops.delete(bm, geom=kill, context='FACES')
    bm.to_mesh(o.data); bm.free()
    print(f'[opt] leaves: {len(groups)} kept ~{int(len(groups) * leaf_keep)} scale {scale:.2f}')

def decimate(o, ratio):
    if ratio >= 1: return
    mod = o.modifiers.new('dec', 'DECIMATE')
    mod.ratio = max(0.01, ratio)
    mod.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
    bpy.ops.object.modifier_apply(modifier='dec')

if leaf_keep > 0:
    # แยกใบไม้ออกจากกิ่ง/ลำต้นก่อน: ใบใช้วิธีคัดใบ + ลดรายละเอียดเบา ๆ, กิ่ง/ลำต้นลดแรง
    for o in list(meshes):
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='MATERIAL'); bpy.ops.object.mode_set(mode='OBJECT')
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    leaves = [o for o in meshes if any(m and 'leaves' in m.name.lower() for m in o.data.materials)]
    wood = [o for o in meshes if o not in leaves]
    for o in leaves:
        thin_leaves(o)
        decimate(o, 0.35)
    leaf_tris = sum(tris(o) for o in leaves)
    wood_tris = sum(tris(o) for o in wood)
    budget = max(target - leaf_tris, target * 0.3)
    for o in wood: decimate(o, budget / max(wood_tris, 1))
else:
    total = sum(tris(o) for o in meshes)
    for o in meshes: decimate(o, target / max(total, 1))
# ย่อพื้นผิวให้ไม่เกิน MAX_TEX px (ไฟล์เล็กลงมากสำหรับเว็บ)
MAX_TEX = 512
for img in bpy.data.images:
    w, h = img.size
    if max(w, h) > MAX_TEX:
        k = MAX_TEX / max(w, h)
        img.scale(max(1, int(w * k)), max(1, int(h * k)))
print('[opt] after', sum(tris(o) for o in meshes), 'objects', len(meshes))
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_apply=True, export_image_format='AUTO', export_animations=False, export_cameras=False, export_lights=False)
print('[opt] wrote', dst)
