"""Export the mesh content of an approved Blender scene to a GLB file."""

import argparse
import os
import sys

import bpy


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--max-texture-size", type=int, default=0)
    parser.add_argument("--decimate-over", type=int, default=0)
    parser.add_argument("--decimate-ratio", type=float, default=1.0)
    parser.add_argument("--collections", default="")
    return parser.parse_args(
        sys.argv[sys.argv.index("--") + 1 :]
    )


def prepare_textures(max_size):
    if max_size <= 0:
        return
    for image in bpy.data.images:
        width, height = image.size
        if max(width, height) <= max_size or not image.packed_file:
            continue
        scale = max_size / max(width, height)
        image.scale(
            max(1, round(width * scale)),
            max(1, round(height * scale)),
        )
        image.pack()
        print(f"RESIZED_TEXTURE {image.name} to {tuple(image.size)}")


def decimate_large_meshes(meshes, threshold, ratio):
    if threshold <= 0 or ratio >= 1.0:
        return
    for obj in meshes:
        if len(obj.data.polygons) <= threshold:
            continue
        before = len(obj.data.polygons)
        bpy.ops.object.select_all(action="DESELECT")
        obj.hide_set(False)
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        modifier = obj.modifiers.new("MapPerformanceDecimate", "DECIMATE")
        modifier.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        print(
            f"DECIMATED {obj.name}: {before} -> "
            f"{len(obj.data.polygons)} polygons"
        )


def main():
    args = parse_args()
    output = os.path.abspath(args.output)
    os.makedirs(os.path.dirname(output), exist_ok=True)

    if args.collections:
        collection_names = {
            name.strip()
            for name in args.collections.split(",")
            if name.strip()
        }
        meshes = sorted(
            {
                obj
                for name in collection_names
                for collection in [bpy.data.collections.get(name)]
                if collection is not None
                for obj in collection.all_objects
                if obj.type == "MESH"
            },
            key=lambda obj: obj.name,
        )
        if not meshes:
            raise RuntimeError(
                "No mesh objects found in requested collections."
            )
    else:
        meshes = [
            obj
            for obj in bpy.context.scene.objects
            if obj.type == "MESH"
        ]
    decimate_large_meshes(
        meshes,
        args.decimate_over,
        args.decimate_ratio,
    )
    prepare_textures(args.max_texture_size)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.hide_set(False)
        obj.select_set(True)

    if not meshes:
        raise RuntimeError("The Blender scene contains no mesh objects.")

    bpy.context.view_layer.objects.active = meshes[0]
    polygon_count = sum(len(obj.data.polygons) for obj in meshes)
    bpy.ops.export_scene.gltf(
        filepath=output,
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_cameras=False,
        export_lights=False,
        export_yup=True,
    )
    print(
        f"EXPORTED {len(meshes)} meshes / {polygon_count} polygons "
        f"to {output}"
    )


if __name__ == "__main__":
    main()
