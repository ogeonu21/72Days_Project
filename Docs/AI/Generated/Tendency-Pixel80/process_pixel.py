"""Generated art only: repack complete silhouettes, then run skill with nearest resizing."""
import importlib.util
import sys
from pathlib import Path
from PIL import Image

skill = Path('C:/Users/monke/.codex/skills/generate2dsprite/scripts/generate2dsprite.py')
spec = importlib.util.spec_from_file_location('sprite_processor', skill)
processor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(processor)
directory = Path(__file__).resolve().parent
raw = Image.open(sys.argv[1]).convert('RGBA')
raw.save(directory / 'raw-original.png')
clean = processor.remove_bg_magenta(raw)
parts = sorted(processor.connected_components(clean, min_area=1000), key=lambda c: c['bbox'][0])
assert len(parts) == 5, f'Expected five complete icons, found {len(parts)}'
cell = max(max(c['bbox'][2]-c['bbox'][0], c['bbox'][3]-c['bbox'][1]) for c in parts) + 64
packed = Image.new('RGBA', (cell*5,cell), (255,0,255,255))
for i, part in enumerate(parts):
    crop = clean.crop(part['bbox'])
    packed.paste(crop, (i*cell+(cell-crop.width)//2, cell-32-crop.height), crop)
packed.save(directory / 'repacked.png')
original_resize = Image.Image.resize
def nearest_resize(self, size, resample=None, box=None, reducing_gap=None):
    return original_resize(self, size, Image.Resampling.NEAREST, box=box)
Image.Image.resize = nearest_resize
sys.argv = [str(skill), 'process', '--input', str(directory/'repacked.png'),
    '--target','asset','--mode','tendency','--rows','1','--cols','5',
    '--cell-size','80','--fit-scale','0.94','--align','bottom', '--shared-scale',
    '--component-mode','all','--min-component-area','20','--strict-qc',
    '--prompt-file',str(directory/'prompt-used.txt'),'--output-dir',str(directory/'final')]
processor.main()
for image in (directory/'final').glob('tendency-*.png'):
    with Image.open(image) as icon:
        assert icon.size == (80,80)
        print(image.name, icon.size, icon.mode)
