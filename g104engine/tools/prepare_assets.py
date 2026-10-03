"""从已核对输入提取约定子集；FFmpeg只用于离线PCM16转换，不是运行时依赖。"""
import argparse
import hashlib
import json
import math
import pathlib
import struct
import subprocess
import uuid
import wave
import zipfile
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / '.cache' / 'v1-preparation'
ASSETS = ROOT / 'assets'
MANIFEST = []


def write(relative, data, source, license_name):
    target = ROOT / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    MANIFEST.append(dict(path=relative, source=source, license=license_name,
                         sha256=hashlib.sha256(data).hexdigest()))
    return target


def extract_inputs():
    packages = {
        'openal-soft-1.25.2-bin.zip': '67a0c4b800bd860c93c04f38caf8cbe4875f9c84700ac430efc451f70e265434',
        'Universal Animation Library[Standard].zip': 'cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724',
        'kenney_impact-sounds.zip': '029d734af1582474edf3a694d1b0cebc97c1c152f2f39fa34d4c2bafc5de77f8',
        'kenney_interface-sounds.zip': 'f2193d072726d6758a5f7871b2dcc54dcce0d5c35c6f0a62f92549b327c81232',
    }
    for name, expected in packages.items():
        if hashlib.sha256((CACHE / name).read_bytes()).hexdigest() != expected:
            raise ValueError('Input SHA-256 changed: ' + name)
    with zipfile.ZipFile(CACHE / 'openal-soft-1.25.2-bin.zip') as archive:
        prefix = 'openal-soft-1.25.2-bin/'
        for source, target in [('bin/Win64/soft_oal.dll', 'OpenAL32.dll'),
                               ('COPYING', 'COPYING'), ('LICENSE-pffft', 'LICENSE-pffft'),
                               ('readme.txt', 'readme.txt')]:
            write('third_party/openal-soft/1.25.2/' + target, archive.read(prefix + source),
                  'https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip#' + source,
                  'LGPL-2.0-or-later / bundled pffft license')
    with zipfile.ZipFile(CACHE / 'Universal Animation Library[Standard].zip') as archive:
        model = next(n for n in archive.namelist() if n.endswith('/Unreal-Godot/UAL1_Standard.glb'))
        write('assets/models/UAL1_Standard.glb', archive.read(model),
              'https://quaternius.itch.io/universal-animation-library#Standard/' + model, 'CC0')
        license_entries = [n for n in archive.namelist() if not n.endswith('/') and
                           ('license' in pathlib.PurePosixPath(n).name.lower() or
                            pathlib.PurePosixPath(n).name.lower().startswith('readme'))]
        for index, name in enumerate(license_entries):
            write(f'assets/licenses/quaternius-{index}-{pathlib.PurePosixPath(name).name}',
                  archive.read(name), 'Quaternius Standard ZIP#' + name, 'CC0')


def png(width, height, pixel):
    raw = b''.join(b'\0' + b''.join(bytes(pixel(x, y)) for x in range(width)) for y in range(height))
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))


def make_test_assets():
    write('assets/tests/checker.png', png(64, 64, lambda x, y:
          (210, 165, 70, 255) if (x // 8 + y // 8) % 2 else (45, 70, 100, 255)),
          'G104Engine procedural checker', 'project-authored')
    write('assets/tests/normal.png', png(64, 64, lambda x, y: (128, 128, 255, 255)),
          'G104Engine tangent-space flat normal', 'project-authored')
    # 测试立方体含法线/UV/切线和PNG引用，补足无纹理角色包不能覆盖的输入。
    positions, normals, uvs, tangents, indices = [], [], [], [], []
    faces = [((1,0,0), [(1,-1,-1),(1,1,-1),(1,1,1),(1,-1,1)]),
             ((-1,0,0),[(-1,-1,1),(-1,1,1),(-1,1,-1),(-1,-1,-1)]),
             ((0,1,0),[(-1,1,-1),(-1,1,1),(1,1,1),(1,1,-1)]),
             ((0,-1,0),[(-1,-1,1),(-1,-1,-1),(1,-1,-1),(1,-1,1)]),
             ((0,0,1),[(1,-1,1),(1,1,1),(-1,1,1),(-1,-1,1)]),
             ((0,0,-1),[(-1,-1,-1),(-1,1,-1),(1,1,-1),(1,-1,-1)])]
    for normal, vertices in faces:
        start = len(positions)
        for vertex, uv in zip(vertices, [(0,0),(0,1),(1,1),(1,0)]):
            positions.append(tuple(v * .5 for v in vertex)); normals.append(normal); uvs.append(uv)
            tangent = tuple((vertices[3][i] - vertices[0][i]) * .5 for i in range(3))
            # 此UV布局的dP/dv与cross(normal,tangent)相反，切线手性为-1。
            tangents.append((*tangent, -1))
        indices.extend([start, start+1, start+2, start, start+2, start+3])
    binary = bytearray(); views = []; accessors = []
    def attribute(values, count, typename, component=5126):
        offset = len(binary); format_code = 'f' if component == 5126 else 'H'
        for value in values:
            parts = value if isinstance(value, tuple) else (value,)
            binary.extend(struct.pack('<' + format_code * count, *parts))
        views.append(dict(buffer=0, byteOffset=offset, byteLength=len(binary)-offset))
        accessor = dict(bufferView=len(views)-1, componentType=component, count=len(values), type=typename)
        if values is positions:
            accessor.update(min=[-.5]*3, max=[.5]*3)
        accessors.append(accessor)
        while len(binary) % 4: binary.append(0)
        return len(accessors)-1
    attrs = dict(POSITION=attribute(positions,3,'VEC3'), NORMAL=attribute(normals,3,'VEC3'),
                 TEXCOORD_0=attribute(uvs,2,'VEC2'), TANGENT=attribute(tangents,4,'VEC4'))
    index_accessor = attribute(indices,1,'SCALAR',5123)
    model = dict(asset=dict(version='2.0', generator='G104Engine material verification'), scene=0,
                 scenes=[dict(nodes=[0])], nodes=[dict(mesh=0)],
                 meshes=[dict(primitives=[dict(attributes=attrs, indices=index_accessor, material=0)])],
                 materials=[dict(pbrMetallicRoughness=dict(baseColorTexture=dict(index=0),
                       metallicFactor=.1, roughnessFactor=.6), normalTexture=dict(index=1))],
                 textures=[dict(source=0),dict(source=1)],
                 images=[dict(uri='../tests/checker.png'),dict(uri='../tests/normal.png')],
                 buffers=[dict(byteLength=len(binary))], bufferViews=views, accessors=accessors)
    encoded = json.dumps(model, separators=(',', ':')).encode()
    encoded += b' ' * ((-len(encoded)) % 4)
    glb = struct.pack('<III',0x46546C67,2,12+8+len(encoded)+8+len(binary))
    glb += struct.pack('<I4s',len(encoded),b'JSON')+encoded+struct.pack('<I4s',len(binary),b'BIN\0')+binary
    write('assets/models/material-probe.glb',glb,'G104Engine procedural glTF material probe','project-authored')
    target = ASSETS / 'audio' / 'loop-test.wav'; target.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(target),'wb') as wav:
        wav.setparams((1,2,44100,0,'NONE','not compressed'))
        # 整周期低音，只用于循环/清理验证，默认不作为场景背景。
        wav.writeframes(b''.join(struct.pack('<h',int(900*math.sin(2*math.pi*110*i/44100))) for i in range(44100)))
    MANIFEST.append(dict(path='assets/audio/loop-test.wav',source='G104Engine 110Hz test signal',
                         license='project-authored',sha256=hashlib.sha256(target.read_bytes()).hexdigest()))


def convert_audio(ffmpeg):
    selected = [('kenney_interface-sounds.zip','click_001','click'),
                ('kenney_interface-sounds.zip','confirmation_001','complete'),
                ('kenney_interface-sounds.zip','switch_001','switch'),
                ('kenney_impact-sounds.zip','footstep_concrete_000','step0'),
                ('kenney_impact-sounds.zip','footstep_concrete_001','step1'),
                ('kenney_impact-sounds.zip','impactWood_medium_000','door'),
                ('kenney_impact-sounds.zip','impactMetal_light_000','jump')]
    for package in sorted(set(p for p,_,_ in selected)):
        with zipfile.ZipFile(CACHE / package) as archive:
            for name in archive.namelist():
                if not name.endswith('/') and 'license' in pathlib.PurePosixPath(name).name.lower():
                    write('assets/licenses/' + package[:-4] + '-' + pathlib.PurePosixPath(name).name,
                          archive.read(name),'https://kenney.nl/assets/' + package[7:-4] + '#' + name,'CC0')
            for _, stem, output in (row for row in selected if row[0] == package):
                entry = next(n for n in archive.namelist() if pathlib.PurePosixPath(n).stem == stem and n.lower().endswith('.ogg'))
                original = CACHE / 'selected-audio' / (stem + '.ogg'); original.parent.mkdir(exist_ok=True)
                original.write_bytes(archive.read(entry))
                destination = ASSETS / 'audio' / (output + '.wav'); destination.parent.mkdir(parents=True,exist_ok=True)
                command = [str(ffmpeg),'-nostdin','-hide_banner','-loglevel','error','-y','-i',str(original),
                           '-ac','1','-ar','44100','-c:a','pcm_s16le',str(destination)]
                subprocess.run(command,check=True)
                with wave.open(str(destination),'rb') as wav:
                    assert wav.getnchannels() == 1 and wav.getsampwidth() == 2 and wav.getframerate() == 44100
                MANIFEST.append(dict(path='assets/audio/'+output+'.wav',source=package+'#'+entry,
                         license='CC0',conversion=command,sha256=hashlib.sha256(destination.read_bytes()).hexdigest()))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(); parser.add_argument('--ffmpeg',type=pathlib.Path,required=True)
    arguments = parser.parse_args()
    extract_inputs(); make_test_assets(); convert_audio(arguments.ffmpeg.resolve())
    config = ASSETS / 'config' / 'character-animation.json'
    if config.exists():
        MANIFEST.append(dict(path='assets/config/character-animation.json',source='G104Engine finite animation configuration',
                             license='project-authored',sha256=hashlib.sha256(config.read_bytes()).hexdigest()))
    write('assets/licenses/asset-manifest.json',json.dumps(MANIFEST,ensure_ascii=False,indent=2).encode('utf-8'),
          'G104Engine preparation manifest','project-authored')
    print(json.dumps(dict(prepared=len(MANIFEST),root=str(ASSETS)),ensure_ascii=False))
