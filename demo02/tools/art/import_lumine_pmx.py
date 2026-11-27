"""Import the approved Lumine PMX asset and export a Unity-ready GLB.

The approved archive contains Shift-JIS paths that were decoded as garbled
characters while extracting. The bytes are intact, but a clean source folder
cannot resolve several texture names. This script maps those names back to the
human-readable texture files and embeds the corrected images in the GLB.
"""

import argparse
import json
import os
import sys

import addon_utils
import bpy
from mathutils import Vector


TEXTURE_NAME_MAP = {
    "敮.png": "髪.png",
    "堖.png": "衣.png",
    "敡.png": "肌.png",
    "尦慺1.png": "元素1.png",
}


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--pmx", required=True)
    parser.add_argument("--texture-root", required=True)
    parser.add_argument("--glb", required=True)
    parser.add_argument("--blend", default="")
    parser.add_argument("--report", default="")
    parser.add_argument("--scale", type=float, default=0.08)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def enable_mmd_tools():
    module_name = "bl_ext.user_default.mmd_tools"
    enabled = addon_utils.enable(
        module_name,
        default_set=True,
    )
    if not enabled:
        enabled = addon_utils.enable(
            "mmd_tools",
            default_set=True,
        )
    if not enabled:
        raise RuntimeError(
            "Blender could not enable the mmd_tools add-on. Install "
            "mmd_tools for Blender 4.2 before converting the PMX."
        )


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)


def find_texture(texture_root, basename):
    mapped = TEXTURE_NAME_MAP.get(basename, basename)
    direct = os.path.join(texture_root, *mapped.split("/"))
    if os.path.isfile(direct):
        return direct
    lower = mapped.lower()
    for root, _, files in os.walk(texture_root):
        for name in files:
            if name.lower() == lower:
                return os.path.join(root, name)
    return ""


def repair_images(texture_root):
    repaired = []
    unresolved = []
    for image in bpy.data.images:
        if image.source in {"GENERATED", "VIEWER"}:
            continue
        current = bpy.path.abspath(image.filepath) if image.filepath else ""
        if current and os.path.isfile(current):
            if not image.packed_file:
                image.pack()
            continue

        basename = os.path.basename(current.replace("\\", "/"))
        replacement = find_texture(texture_root, basename)
        if not replacement:
            unresolved.append(
                {
                    "image": image.name,
                    "requested": current,
                    "mapped": TEXTURE_NAME_MAP.get(basename, basename),
                }
            )
            continue

        image.filepath = replacement
        image.reload()
        image.pack()
        repaired.append(
            {
                "image": image.name,
                "from": current,
                "to": replacement,
                "size": list(image.size),
            }
        )
    return repaired, unresolved


def create_export_materials():
    converted = []
    for material in list(bpy.data.materials):
        images = [
            node.image
            for node in material.node_tree.nodes
            if node.type == "TEX_IMAGE"
            and node.image is not None
            and node.name == "mmd_base_tex"
        ] if material.use_nodes and material.node_tree else []
        if not images:
            continue

        image = images[0]
        mmd_material = getattr(material, "mmd_material", None)
        alpha = float(getattr(mmd_material, "alpha", 1.0))
        material.use_nodes = True
        material.diffuse_color = (
            material.diffuse_color[0],
            material.diffuse_color[1],
            material.diffuse_color[2],
            alpha,
        )
        material.use_backface_culling = not bool(
            getattr(mmd_material, "is_double_sided", True)
        )
        nodes = material.node_tree.nodes
        nodes.clear()
        output = nodes.new("ShaderNodeOutputMaterial")
        shader = nodes.new("ShaderNodeBsdfPrincipled")
        texture = nodes.new("ShaderNodeTexImage")
        texture.image = image
        shader.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
        shader.inputs["Roughness"].default_value = 0.72
        shader.inputs["Metallic"].default_value = 0.0
        shader.inputs["Alpha"].default_value = alpha
        material.node_tree.links.new(
            texture.outputs["Color"],
            shader.inputs["Base Color"],
        )
        material.node_tree.links.new(
            texture.outputs["Alpha"],
            shader.inputs["Alpha"],
        )
        if alpha < 0.999 or image.depth == 32:
            material.surface_render_method = "DITHERED"
        material.node_tree.links.new(
            shader.outputs["BSDF"],
            output.inputs["Surface"],
        )
        converted.append((material, material))

    return [
        {
            "source": old.name,
            "export": new.name,
            "texture": next(
                (
                    node.image.name
                    for node in new.node_tree.nodes
                    if node.type == "TEX_IMAGE" and node.image
                ),
                "",
            ),
        }
        for old, new in converted
    ]


def world_bounds(meshes):
    points = []
    for obj in meshes:
        points.extend(
            obj.matrix_world @ Vector(corner)
            for corner in obj.bound_box
        )
    minimum = Vector(
        (
            min(point.x for point in points),
            min(point.y for point in points),
            min(point.z for point in points),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in points),
            max(point.y for point in points),
            max(point.z for point in points),
        )
    )
    return minimum, maximum


def main():
    args = parse_args()
    pmx_path = os.path.abspath(args.pmx)
    texture_root = os.path.abspath(args.texture_root)
    glb_path = os.path.abspath(args.glb)
    if not os.path.isfile(pmx_path):
        raise FileNotFoundError(pmx_path)

    enable_mmd_tools()
    reset_scene()
    bpy.ops.mmd_tools.import_model(
        filepath=pmx_path,
        types={"MESH", "ARMATURE", "MORPHS"},
        scale=args.scale,
        clean_model=True,
        remove_doubles=False,
        fix_bone_order=True,
        fix_ik_links=True,
        rename_bones=True,
        use_underscore=False,
        use_mipmap=True,
    )

    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(armatures) != 1 or not meshes:
        raise RuntimeError(
            f"Expected one armature and at least one mesh; got "
            f"{len(armatures)} armatures and {len(meshes)} meshes."
        )

    repaired, unresolved = repair_images(texture_root)
    export_materials = create_export_materials()

    for image in bpy.data.images:
        if image.filepath and not image.packed_file:
            image.pack()

    minimum, maximum = world_bounds(meshes)
    armature = armatures[0]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj == armature or obj in meshes or (
            obj.parent is not None and obj.parent == armature
        ):
            obj.hide_set(False)
            obj.select_set(True)
    bpy.context.view_layer.objects.active = armature

    os.makedirs(os.path.dirname(glb_path), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=glb_path,
        export_format="GLB",
        use_selection=True,
        export_apply=False,
        export_yup=True,
        export_animations=False,
        export_skins=True,
        export_morph=True,
        export_morph_normal=True,
        export_morph_tangent=False,
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )

    if args.blend:
        blend_path = os.path.abspath(args.blend)
        os.makedirs(os.path.dirname(blend_path), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    material_images = []
    for material in bpy.data.materials:
        if not material.use_nodes or not material.node_tree:
            continue
        images = [
            node.image.filepath
            for node in material.node_tree.nodes
            if node.type == "TEX_IMAGE" and node.image
        ]
        if images:
            material_images.append(
                {
                    "material": material.name,
                    "images": images,
                }
            )

    report = {
        "source": pmx_path,
        "glb": glb_path,
        "armature": armature.name,
        "bone_count": len(armature.data.bones),
        "bones": [bone.name for bone in armature.data.bones],
        "mesh_count": len(meshes),
        "polygon_count": sum(len(obj.data.polygons) for obj in meshes),
        "vertex_count": sum(len(obj.data.vertices) for obj in meshes),
        "shape_key_count": sum(
            len(obj.data.shape_keys.key_blocks)
            if obj.data.shape_keys
            else 0
            for obj in meshes
        ),
        "height": maximum.z - minimum.z,
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "repaired_images": repaired,
        "unresolved_images": unresolved,
        "materials": material_images,
        "export_materials": export_materials,
    }
    report_path = args.report or glb_path + ".json"
    os.makedirs(os.path.dirname(os.path.abspath(report_path)), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    print("LUMINE_REPORT " + json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
