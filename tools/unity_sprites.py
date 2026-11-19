"""Рендер спрайтов окон для Unity (белые формы, красятся в Unity через Image.color).
Запуск: python3 tools/unity_sprites.py <папка Assets/Koksohim/UI/Sprites>"""
import sys, os
from playwright.sync_api import sync_playwright

out = sys.argv[1]
os.makedirs(out, exist_ok=True)

def cut(w, h, c):  # скошены правый верхний и левый нижний углы
    return f"{0},{0} {w-c},{0} {w},{c} {w},{h} {c},{h} {0},{h-c}"

ICON = 'fill="none" stroke="#fff" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"'
S = {}
# панель: 128x128, угол 30, 9-slice border 40
S['panel_fill'] = (128, 128, f'<polygon points="{cut(128,128,30)}" fill="#fff"/>')
S['panel_frame'] = (128, 128, f'<defs><clipPath id="c"><polygon points="{cut(128,128,30)}"/></clipPath></defs>'
                    f'<polygon points="{cut(128,128,30)}" fill="none" stroke="#fff" stroke-width="4" clip-path="url(#c)"/>')
S['btn_heat'] = (96, 96, f'<polygon points="{cut(96,96,16)}" fill="#fff"/>')
S['btn_outline'] = (48, 48, '<rect x="1.5" y="1.5" width="45" height="45" fill="none" stroke="#fff" stroke-width="3"/>')
S['chip'] = (32, 32, '<rect x=".6" y=".6" width="30.8" height="30.8" rx="3" fill="#fff" fill-opacity=".06" stroke="#fff" stroke-opacity=".2" stroke-width="1.2"/>')
S['card'] = (32, 32, '<rect x="1" y="1" width="30" height="30" fill="#fff" fill-opacity=".05" stroke="#fff" stroke-opacity=".14" stroke-width="2"/>')
S['frame4'] = (32, 32, '<rect x="2" y="2" width="28" height="28" fill="none" stroke="#fff" stroke-width="4"/>')
S['dot'] = (24, 24, '<circle cx="12" cy="12" r="10" fill="#fff"/>')
# штриховка обложки: тайл 32x32, полосы "/" жёлтые 10%
bands = ''.join(f'<polygon points="{a+40},-40 {a+16+40},-40 {a+16-40},40 {a-40},40" fill="#FFC400" fill-opacity=".10"/>' for a in (-32, 0, 32, 64))
S['stripes_tile'] = (32, 32, bands)
# полоса «опасно» 260x10, 135deg: 14px жёлтый / 14px тёмный
hz = ''.join(f'<polygon points="{a+30},-5 {a+14+30},-5 {a+14-30},15 {a-30},15" fill="#FFC400"/>' for a in range(-40, 300, 28))
S['hazard'] = (260, 10, '<rect width="260" height="10" fill="#14181c"/>' + hz)
S['hazard_red'] = (260, 10, '<rect width="260" height="10" fill="#14181c"/>' + hz.replace('#FFC400', '#FF4B3E'))
# иконки 96x96 (viewBox 24)
IC = {
 'icon_clock': '<path d="M12 7v5l3 2M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z"/>',
 'icon_list': '<path d="M4 6h4M4 12h4M4 18h4M11 6h9M11 12h9M11 18h9"/>',
 'icon_box': '<path d="M4 5h16v14H4zM4 9h16M8 13h8"/>',
 'icon_flame': '<path d="M12 3c3 4 6 6 6 10a6 6 0 0 1-12 0c0-2 1-3 2-4 0 2 1 3 2 3 0-4 0-6 2-9z"/>',
 'icon_search': '<path d="M10 4a6 6 0 1 0 0 12 6 6 0 0 0 0-12zM14.5 14.5L21 21"/>',
}
for k, p in IC.items():
    S[k] = (96, 96, f'<g transform="scale(4)" {ICON}>{p}</g>')

# крупные иконки помещений 192x192 (viewBox 24, тоньше линия)
ICON_L = 'fill="none" stroke="#fff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"'
IC_BIG = {
 # диспетчерская: монитор с графиком, стойка
 'icon_room_control': '<path d="M3.5 4.5h17v11h-17zM12 15.5v3M8 19.5h8M7 12.5l3-3.5 2.5 2.5 4-4.5"/>',
 # лебёдки / кантующая / декарбонизация: барабан с тросом и крюком на раме
 'icon_room_winch': '<path d="M3 22h18M5 22l4-12M19 22l-4-12M7.5 4.5h9v5.5h-9zM10.5 4.5V10M13.5 4.5V10M12 10v7M12 17v1.5a2.2 2.2 0 1 1 2.2 2.2M4 7.5h3.5"/>',
 # тоннель газовоздушных клапанов: труба с задвижкой и штурвалом
 'icon_room_valve': '<path d="M2 9.5h7M15 9.5h7M2 16.5h7M15 16.5h7M9 7.5h6v11H9zM12 7.5v-4M8.5 3.5h7M9 11.5h6"/>',
}
for k, p in IC_BIG.items():
    S[k] = (192, 192, f'<g transform="scale(8)" {ICON_L}>{p}</g>')
only = set(sys.argv[2:])

with sync_playwright() as pw:
    b = pw.chromium.launch(); pg = b.new_page()
    for name, (w, h, body) in S.items():
        if only and name not in only: continue
        pg.set_viewport_size({'width': w, 'height': h})
        pg.set_content(f'<html><body style="margin:0;background:transparent"><svg id="s" xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" style="display:block;overflow:hidden">{body}</svg></body></html>')
        pg.locator('#s').screenshot(path=os.path.join(out, name + '.png'), omit_background=True)
    b.close()
print(len(S), 'sprites ->', out)
