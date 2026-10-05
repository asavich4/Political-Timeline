"""Build tintable Unity map sprites from us-atlas Census boundary data.
Run with Python + Pillow. Each state's crop retains its projected placement.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1] / 'PoliticalTimelineGame/Assets/Content/Presidency/Map'
topology = json.loads((root / 'states-albers-10m.json').read_text())
scale, translate = topology['transform']['scale'], topology['transform']['translate']
arcs = []
for arc in topology['arcs']:
    x = y = 0
    points = []
    for dx, dy in arc:
        x += dx
        y += dy
        points.append(((x * scale[0] + translate[0]) * 2, (y * scale[1] + translate[1]) * 2))
    arcs.append(points)

def ring(indices):
    result = []
    for index in indices:
        segment = arcs[index] if index >= 0 else list(reversed(arcs[~index]))
        result.extend(segment if not result else segment[1:])
    return result

manifest = []
borders = Image.new('RGBA', (1950, 1220))
lines = ImageDraw.Draw(borders)
for state in topology['objects']['states']['geometries']:
    if int(state['id']) > 56:
        continue
    polygons = state['arcs'] if state['type'] == 'MultiPolygon' else [state['arcs']]
    mask = Image.new('RGBA', (1950, 1220))
    draw = ImageDraw.Draw(mask)
    for polygon in polygons:
        for index, indices in enumerate(polygon):
            points = ring(indices)
            draw.polygon(points, fill=(255,255,255,255) if index == 0 else (0,0,0,0))
            lines.line(points + points[:1], fill=(235,230,209,255), width=4, joint='curve')
    box = mask.getbbox()
    if not box:
        continue
    name = state['properties']['name']
    mask.crop(box).save(root / (name + '.png'))
    manifest.append(dict(name=name, x=box[0]/1950, y=box[1]/1220,
                         width=(box[2]-box[0])/1950, height=(box[3]-box[1])/1220))
borders.save(root / 'Borders.png')
(root / 'Placement.json').write_text(json.dumps(dict(states=manifest), indent=2))
assert len(manifest) == 51, len(manifest)
print('Built 51 geographic state sprites and border overlay.')
