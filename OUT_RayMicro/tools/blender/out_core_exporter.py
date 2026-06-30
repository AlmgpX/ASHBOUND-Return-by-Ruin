bl_info = {
    "name": "OUT CORE Exporter",
    "author": "Delirium Interactive / OUT CORE",
    "version": (0, 1, 0),
    "blender": (3, 6, 0),
    "location": "File > Export > OUT CORE Level",
    "description": "Exports OUT CORE visual GLB and OUTMAP gameplay metadata from named Blender objects.",
    "category": "Import-Export",
}

import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


PREFIX_PLAYER_START = "OUT_SPAWN_PlayerStart"
PREFIX_VISUAL = "OUT_VIS_"
PREFIX_COLLIDER_BOX = "OUT_COLLIDER_Box_"
PREFIX_COLLIDER_MESH = "OUT_COLLIDER_Mesh_"
PREFIX_DOOR = "OUT_DOOR_"
PREFIX_TRIGGER = "OUT_TRIGGER_"
PREFIX_PICKUP = "OUT_PICKUP_"


DEFAULT_SURFACE = "surface.stone"
DEFAULT_DOOR_SURFACE = "surface.wood"


def prop(obj, name, fallback):
    return obj.get(name, fallback)


def clean_id(value):
    value = value.strip().replace(" ", "_")
    allowed = []
    for ch in value:
        if ch.isalnum() or ch in "._-":
            allowed.append(ch)
    return "".join(allowed) or "unnamed"


def strip_prefix(name, prefix):
    return clean_id(name[len(prefix):]) if name.startswith(prefix) else clean_id(name)


def vec3(value):
    return [round(float(value.x), 4), round(float(value.y), 4), round(float(value.z), 4)]


def dimensions(obj):
    dims = obj.dimensions
    return [round(max(0.01, float(abs(dims.x))), 4), round(max(0.01, float(abs(dims.y))), 4), round(max(0.01, float(abs(dims.z))), 4)]


def color_rgba(obj, fallback):
    color = prop(obj, "color", None)
    if isinstance(color, (list, tuple)) and len(color) >= 3:
        r = int(max(0, min(255, color[0])))
        g = int(max(0, min(255, color[1])))
        b = int(max(0, min(255, color[2])))
        a = int(max(0, min(255, color[3] if len(color) >= 4 else 255)))
        return [r, g, b, a]
    return fallback


def euler_deg(obj):
    rot = obj.rotation_euler
    return [round(math.degrees(rot.x), 4), round(math.degrees(rot.y), 4), round(math.degrees(rot.z), 4)]


def make_box(obj):
    name = strip_prefix(obj.name, PREFIX_COLLIDER_BOX)
    return {
        "id": "box." + name,
        "center": vec3(obj.location),
        "size": dimensions(obj),
        "color": color_rgba(obj, [74, 84, 96, 255]),
        "solid": bool(prop(obj, "solid", True)),
        "surface": str(prop(obj, "surface", DEFAULT_SURFACE)),
    }


def make_door(obj):
    name = strip_prefix(obj.name, PREFIX_DOOR)
    door_id = str(prop(obj, "id", "door." + name))
    return {
        "id": door_id,
        "center": vec3(obj.location),
        "size": dimensions(obj),
        "color": color_rgba(obj, [120, 62, 48, 255]),
        "startsOpen": bool(prop(obj, "startsOpen", False)),
        "surface": str(prop(obj, "surface", DEFAULT_DOOR_SURFACE)),
    }


def make_trigger(obj):
    name = strip_prefix(obj.name, PREFIX_TRIGGER)
    target = str(prop(obj, "target", "door.main"))
    kind = str(prop(obj, "kind", "door_toggle"))
    return {
        "id": str(prop(obj, "id", "trigger." + name)),
        "kind": kind,
        "target": target,
        "center": vec3(obj.location),
        "size": dimensions(obj),
    }


def make_pickup(obj):
    name = strip_prefix(obj.name, PREFIX_PICKUP)
    pickup_kind = str(prop(obj, "kind", "Health"))
    entry = {
        "id": str(prop(obj, "id", "pickup." + name)),
        "kind": pickup_kind,
        "position": vec3(obj.location),
        "radius": round(float(prop(obj, "radius", 0.75)), 4),
        "amount": int(prop(obj, "amount", 25)),
        "surface": str(prop(obj, "surface", DEFAULT_SURFACE)),
    }
    if pickup_kind.lower() == "armor":
        entry["armorTier"] = str(prop(obj, "armorTier", "Yellow"))
    return entry


def make_mesh_collider_ref(obj):
    name = strip_prefix(obj.name, PREFIX_COLLIDER_MESH)
    return {
        "id": "collider." + name,
        "path": str(prop(obj, "path", "")),
        "position": vec3(obj.location),
        "rotation": euler_deg(obj),
        "scale": dimensions(obj),
        "collision": "mesh",
        "surface": str(prop(obj, "surface", DEFAULT_SURFACE)),
    }


def collect_level(scene):
    map_id = clean_id(scene.get("out_core_map_id", "map.blender_export"))
    display_name = str(scene.get("out_core_display_name", "Blender Export"))
    visual_name = clean_id(scene.get("out_core_visual_name", map_id.replace("map.", "")))

    player_start = [0.0, 1.2, 0.0]
    boxes = []
    doors = []
    triggers = []
    pickups = []
    meshes = []
    visual_objects = []

    for obj in scene.objects:
        if obj.name.startswith(PREFIX_PLAYER_START):
            player_start = vec3(obj.location)
        elif obj.name.startswith(PREFIX_VISUAL):
            visual_objects.append(obj)
        elif obj.name.startswith(PREFIX_COLLIDER_BOX):
            boxes.append(make_box(obj))
        elif obj.name.startswith(PREFIX_COLLIDER_MESH):
            meshes.append(make_mesh_collider_ref(obj))
        elif obj.name.startswith(PREFIX_DOOR):
            doors.append(make_door(obj))
        elif obj.name.startswith(PREFIX_TRIGGER):
            triggers.append(make_trigger(obj))
        elif obj.name.startswith(PREFIX_PICKUP):
            pickups.append(make_pickup(obj))

    visual_path = f"meshes/rooms/{visual_name}.glb"
    if visual_objects:
        meshes.insert(0, {
            "id": "visual." + visual_name,
            "path": visual_path,
            "position": [0.0, 0.0, 0.0],
            "rotation": [0.0, 0.0, 0.0],
            "scale": [1.0, 1.0, 1.0],
            "collision": "none",
            "surface": DEFAULT_SURFACE,
        })

    return {
        "id": map_id,
        "displayName": display_name,
        "playerStart": player_start,
        "boxes": boxes,
        "doors": doors,
        "triggers": triggers,
        "pickups": pickups,
        "meshes": meshes,
    }, visual_objects, visual_path


def export_visual_glb(visual_objects, output_root, visual_path):
    if not visual_objects:
        return None

    target = Path(output_root) / visual_path
    target.parent.mkdir(parents=True, exist_ok=True)

    previous_selection = list(bpy.context.selected_objects)
    previous_active = bpy.context.view_layer.objects.active

    bpy.ops.object.select_all(action='DESELECT')
    for obj in visual_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = visual_objects[0]

    bpy.ops.export_scene.gltf(
        filepath=str(target),
        export_format='GLB',
        use_selection=True,
        export_apply=True,
    )

    bpy.ops.object.select_all(action='DESELECT')
    for obj in previous_selection:
        if obj.name in bpy.context.scene.objects:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = previous_active
    return str(target)


def write_outmap(outmap, output_root):
    map_id = outmap["id"].replace("map.", "")
    path = Path(output_root) / "maps" / f"{map_id}.outmap.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(outmap, ensure_ascii=False, indent=2), encoding="utf-8")
    return str(path)


def export_out_core_level(output_root):
    outmap, visual_objects, visual_path = collect_level(bpy.context.scene)
    glb_path = export_visual_glb(visual_objects, output_root, visual_path)
    map_path = write_outmap(outmap, output_root)
    return map_path, glb_path


class OUTCORE_OT_export_level(bpy.types.Operator):
    bl_idname = "out_core.export_level"
    bl_label = "Export OUT CORE Level"
    bl_options = {'REGISTER'}

    output_root: bpy.props.StringProperty(
        name="OUT CORE data folder",
        subtype='DIR_PATH',
        default="//../OUT_RayMicro/data/",
    )

    def execute(self, context):
        root = bpy.path.abspath(self.output_root)
        try:
            map_path, glb_path = export_out_core_level(root)
            message = f"OUTMAP: {map_path}"
            if glb_path:
                message += f" | GLB: {glb_path}"
            self.report({'INFO'}, message)
            return {'FINISHED'}
        except Exception as ex:
            self.report({'ERROR'}, str(ex))
            return {'CANCELLED'}

    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self, width=620)


class OUTCORE_PT_export_panel(bpy.types.Panel):
    bl_label = "OUT CORE Export"
    bl_idname = "OUTCORE_PT_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "OUT CORE"

    def draw(self, context):
        layout = self.layout
        scene = context.scene
        layout.prop(scene, "out_core_map_id")
        layout.prop(scene, "out_core_display_name")
        layout.prop(scene, "out_core_visual_name")
        layout.operator("out_core.export_level")
        layout.label(text="Use OUT_* object names. Yes, naming conventions, the duct tape of civilization.")


def menu_func_export(self, context):
    self.layout.operator(OUTCORE_OT_export_level.bl_idname, text="OUT CORE Level (.outmap + .glb)")


def register():
    bpy.types.Scene.out_core_map_id = bpy.props.StringProperty(name="Map Id", default="map.blender_export")
    bpy.types.Scene.out_core_display_name = bpy.props.StringProperty(name="Display Name", default="Blender Export")
    bpy.types.Scene.out_core_visual_name = bpy.props.StringProperty(name="Visual GLB Name", default="blender_export")
    bpy.utils.register_class(OUTCORE_OT_export_level)
    bpy.utils.register_class(OUTCORE_PT_export_panel)
    bpy.types.TOPBAR_MT_file_export.append(menu_func_export)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)
    bpy.utils.unregister_class(OUTCORE_PT_export_panel)
    bpy.utils.unregister_class(OUTCORE_OT_export_level)
    del bpy.types.Scene.out_core_map_id
    del bpy.types.Scene.out_core_display_name
    del bpy.types.Scene.out_core_visual_name


if __name__ == "__main__":
    register()
