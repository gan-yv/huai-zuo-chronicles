"""Compose the approved billboard trees into one GLB scenery line."""

import argparse
import os
import sys

import bpy
from mathutils import Matrix, Vector


TREE_LAYOUT = (
    ("BB_spruce", -13.0, 0.0, 0.95),
    ("BB_fir", -6.5, 9.0, 1.05),
    ("BB_birch", 0.0, -7.0, 0.9),
    ("BB_alder", 6.5, 11.0, 1.0),
    ("BB_apple", 13.0, -5.0, 0.95),
)


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    return parser.parse_args(
        sys.argv[sys.argv.index("--") + 1 :]
    )


def mesh_at_ground_origin(source):
    mesh = source.data.copy()
    mesh.transform(source.matrix_world)
    coordinates = [vertex.co.copy() for vertex in mesh.vertices]
    minimum = Vector(
        (
            min(point.x for point in coordinates),
            min(point.y for point in coordinates),
            min(point.z for point in coordinates),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in coordinates),
            max(point.y for point in coordinates),
            max(point.z for point in coordinates),
        )
    )
    center = (minimum + maximum) * 0.5
    offset = Vector((center.x, minimum.y, center.z))
    mesh.transform(Matrix.Translation(-offset))
    return mesh


def main():
    args = parse_args()
    output = os.path.abspath(args.output)
    os.makedirs(os.path.dirname(output), exist_ok=True)

    for obj in bpy.context.scene.objects:
        obj.select_set(False)

    created = []
    for name, x, yaw, scale in TREE_LAYOUT:
        source = bpy.data.objects.get(name)
        if source is None or source.type != "MESH":
            raise RuntimeError(f"Missing source billboard tree: {name}")
        tree = bpy.data.objects.new(
            f"ART_TreeLine_{name}",
            mesh_at_ground_origin(source),
        )
        bpy.context.scene.collection.objects.link(tree)
        tree.location = (x, 0.0, 0.0)
        tree.rotation_euler[2] = yaw * 0.017453292519943295
        tree.scale = (scale, scale, scale)
        tree.select_set(True)
        created.append(tree)

    bpy.context.view_layer.objects.active = created[0]
    bpy.ops.export_scene.gltf(
        filepath=output,
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_cameras=False,
        export_lights=False,
        export_yup=True,
    )
    print(f"EXPORTED_TREE_LINE {len(created)} billboards to {output}")


if __name__ == "__main__":
    main()
