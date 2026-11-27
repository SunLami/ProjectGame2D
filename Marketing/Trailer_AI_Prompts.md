# ORYNTHALS Trailer — Prompt tạo cảnh thoại (ChatGPT → Google Flow)

## Cách làm

Mỗi câu thoại là một **cảnh thoại** dài khoảng 3–5 giây: nền tối, có tàn lửa bay giống ảnh mẫu, phụ đề hiện ở dưới.
Đọc xong câu thoại thì chuyển sang clip gameplay của scene đó.

Trong mỗi cảnh thoại, phía sau tàn lửa có **một hình mờ** liên quan đến câu thoại, vẽ theo style pixel art của game
(giống video Intro): ánh sáng ấm, mái nhà xanh, nhân vật tóc xanh đen, khăn đỏ, balo da.

**Các bước cho mỗi scene:**
1. **ChatGPT:** dán *Prompt ảnh* vào. Chọn tỉ lệ **16:9**. Tải ảnh về.
2. **Google Flow:** chọn **Frames to Video**, đưa ảnh vừa tạo làm khung đầu, dán *Prompt video* vào. Chọn 16:9, 8 giây.
3. **Khi dựng phim:** cắt lấy 3–5 giây đẹp nhất, thêm phụ đề và giọng đọc lên trên.

**Lưu ý quan trọng:**
- **Không cho AI viết chữ lên hình.** Phụ đề tự thêm khi dựng (font có viền sáng như ảnh mẫu, hoặc font Digital Disco của game). Chữ do AI viết thường bị sai chính tả.
- Chừa **1/3 phía dưới màn hình tối** để đặt phụ đề. Các prompt dưới đây đã ghi sẵn yêu cầu này.
- **Giữ nhân vật giống nhau giữa các scene:** tạo ảnh **nhân vật mẫu** ở mục 1 trước. Ở mỗi scene có nhân vật, tải ảnh đó lên ChatGPT kèm câu: *"Use the attached character as reference."*
- **Tắt âm thanh của Flow,** hoặc khi dựng thì bỏ track âm thanh đó, vì nhạc và giọng đọc sẽ thêm riêng.

---

## 1. Ảnh nhân vật mẫu (làm đầu tiên)

**Prompt ảnh (ChatGPT):**
```
Character reference sheet, detailed HD pixel art in the style of a cozy fantasy RPG.
A young male wayfarer, around 16 years old, spiky dark navy-blue hair, determined eyes,
red scarf, blue tunic with brown leather belt, large brown leather backpack with a bedroll,
brown leather boots. Front view, side view and back view, standing pose, plain dark
background, warm rim lighting. Crisp pixels, no anti-aliasing blur, no text, no labels.
```

Tải thêm một ảnh chụp video Intro của game (ví dụ cảnh nhân vật đi trên con đường) lên ChatGPT kèm câu
*"Match this art style"*, để ra ảnh giống game nhất.

---

## 2. Đoạn prompt style dùng chung

Mọi prompt ảnh bên dưới đều đã chứa đoạn này. Nếu muốn tự viết prompt mới thì chép đoạn này vào cuối:

```
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood: the image is mostly
dark, the scene is dimly lit and fades into black at the edges (strong vignette).
Glowing orange ember sparks drifting in the air across the whole frame.
The bottom third of the image stays almost pure black and empty (space for subtitles).
16:9, no text, no letters, no logo, no watermark, no UI.
```

---

## 3. Từng scene

### Scene 01 — The Bard's Tale
**Thoại:** *"Countless journeys... and countless legends yet to be written."*
**Không khí:** câu chuyện cổ được kể bên đống lửa.

**Prompt ảnh:**
```
A lone old bard with a hood sits by a small campfire at night, playing a lute, seen from
a distance. Only the campfire lights his face and hands; everything else fades into deep
darkness. Sparks from the fire rise high into the black sky and turn into drifting embers.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood: the image is mostly
dark, strong vignette. Glowing orange ember sparks drifting across the whole frame.
The bottom third of the image stays almost pure black and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video (Flow):**
```
Slow cinematic push-in toward the bard. The campfire flickers gently, the bard slowly
strums the lute, sparks rise from the fire and drift upward as glowing embers across the
dark frame. Calm, mysterious, storybook mood. Keep the pixel art style, no camera cuts,
no new characters, no text, no speech.
```

---

### Scene 02 — The Journey Begins
**Thoại:** *"And some... are ready to take the first step."*
**Không khí:** bình tĩnh, sâu lắng, nhấn vào "first step".

**Prompt ảnh** (tải kèm ảnh nhân vật mẫu):
```
Use the attached character as reference. Low-angle close-up of the young wayfarer's
leather boots and lower legs standing at the start of a dirt road, his red scarf
fluttering at the edge of the frame. Faint dawn light glows far down the road ahead,
the rest of the scene fades into darkness.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood: mostly dark, strong
vignette. Glowing orange ember sparks drifting across the frame.
The bottom third of the image stays almost pure black and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
The boot lifts and takes one slow, deliberate step forward onto the road. Dust and embers
swirl softly around the step. The dawn light ahead grows slightly brighter. Slow, calm
camera, no cuts, keep the pixel art style, no text, no speech.
```

---

### Scene 03 — A New Land
**Thoại:** *"A new land."*
**Không khí:** ngạc nhiên, tò mò, khám phá.

**Prompt ảnh:**
```
Use the attached character as reference. The young wayfarer seen from behind, small in
the frame, standing on a hill at dawn, looking down at a peaceful valley village with
blue-roofed timber houses, a windmill, a wooden bridge over a river, blue banners and
distant mountains. The valley glows with soft early light, while the foreground and the
edges of the frame stay dark.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood, strong vignette.
A few glowing ember sparks drifting in the foreground.
The bottom third of the image stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
Slow crane-up reveal over the wayfarer's shoulder toward the valley village. The windmill
blades turn slowly, morning light spreads across the rooftops, birds fly in the distance,
embers drift gently in the foreground. Feeling of wonder. No cuts, keep the pixel art
style, no text, no speech.
```

---

### Scene 04 — A New Way of Life
**Thoại:** *"A new way of life."*
**Không khí:** nhẹ nhàng, ấm áp, bình yên.

**Prompt ảnh:**
```
Use the attached character as reference. Golden-hour farm field near a cozy village:
the wayfarer kneels and plants a seed in a neat row of crops (carrots, grape vines,
lemon saplings). A small wooden fishing dock on a calm lake in the background, a windmill
and blue roofs behind. Warm, peaceful light in the centre fading to darkness at the edges.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden highlights. Cinematic trailer mood, soft vignette. Slow golden fireflies and
a few ember sparks floating in the air.
The bottom third of the image stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
Gentle slow pan to the right across the field. The wayfarer pats the soil, a small green
sprout pops up, crop leaves sway in a light breeze, ripples move on the lake, fireflies
float. Calm, cozy, slow motion feeling. No cuts, keep the pixel art style, no text,
no speech.
```

---

### Scene 05 — Crafting (không có thoại)
**Không khí:** lò rèn, lửa, tia lửa. Cảnh này nối thẳng sang clip gameplay rèn đồ.

**Prompt ảnh:**
```
Interior of a village forge at night. A burly blacksmith's arm swings a hammer onto a
glowing orange sword blade on an anvil. Bright sparks burst from the impact. The furnace
glows behind; tools, potion bottles and weapons hang on the dark walls.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood, mostly dark, strong
vignette. Embers drifting across the whole frame.
The bottom third stays dark.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
The hammer strikes the glowing blade twice in rhythm, each strike throws a burst of
sparks that become drifting embers. The furnace fire pulses. Slow push-in toward the
anvil. No cuts, keep the pixel art style, no text, no speech.
```

---

### Scene 06 — Newfound Strength
**Thoại:** *"Newfound strength."*
**Không khí:** trầm, chắc chắn, sức mạnh đang lớn dần.

**Prompt ảnh:**
```
Use the attached character as reference. The wayfarer stands in a dark training yard
with wooden dummies, holding a sword. Three elemental lights orbit his open left hand:
a spiralling water orb (blue), floating angular rocks (brown, earthy), and a swirl of
mint-green wind with leaves and feathers. The magic lights his determined face from below,
everything else is dark.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
chunky cel-shaded magic effects. Cinematic trailer mood, strong vignette.
Ember sparks drifting.
The bottom third stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
The three elemental orbs spin faster around his hand and grow brighter; water swirls,
rocks rotate, leaves whirl. He closes his fist and the energy pulses outward in a soft
shockwave that pushes the embers away. Slow push-in, no cuts, keep the pixel art style,
no text, no speech.
```

---

### Scene 07 — Facing the Unknown
**Thoại:** *"Face the dangers that lie ahead."*
**Không khí:** nghiêm nghị, căng thẳng, nhấn vào "dangers".

**Prompt ảnh:**
```
Use the attached character as reference. The wayfarer seen from behind at the edge of a
dark forest at dusk, sword in hand. Ahead, between twisted trees and mist, several pairs
of glowing eyes watch him: small slimes, goblins and a huge tree creature (Ent) with
glowing eyes deep in the shadows.
Style: detailed HD pixel art like a medieval fantasy RPG, painterly pixel shading,
cold blue-green shadows with warm ember highlights. Cinematic trailer mood, very dark,
strong vignette, thin fog. A few embers drifting.
The bottom third stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
Slow dolly forward past the wayfarer into the dark forest. Fog drifts, branches creak
and sway, the glowing eyes blink and move closer, the Ent's eyes light up last.
Tense, ominous mood. No cuts, keep the pixel art style, no text, no speech.
```

---

### Scene 08 — Gathering Resources
**Thoại:** *"Gather resources. Build your strength."*
**Không khí:** dứt khoát, chuẩn bị, tích lũy tiềm lực.

**Prompt ảnh:**
```
Use the attached character as reference. Forest clearing at dusk: the wayfarer swings a
pickaxe at a glowing crystal ore rock; nearby an opened wooden treasure chest spills
golden light, a pile of logs, herbs and ore chunks. Light comes from the crystal and the
chest; the surroundings fade into darkness.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Cinematic trailer mood, strong vignette.
Ember sparks drifting.
The bottom third stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
The pickaxe strikes the crystal rock, shards and sparks fly; small resource items pop
out and float toward the wayfarer with a soft glow. The chest light pulses. Two clear
rhythmic beats of action. Steady camera, slight push-in, no cuts, keep the pixel art
style, no text, no speech.
```

---

### Scene 09 — The Awakening of Evil
**Thoại:** *"Countless dangers... await your arrival."*
**Không khí:** trầm thấp, bí ẩn, cảnh báo, thế lực đáng sợ đang thức tỉnh.

**Prompt ảnh:**
```
An ancient ruined stone temple at night with a tall carved teleport pillar in the
centre. Behind it, three colossal silhouettes loom in the dark sky, barely visible:
a floating stone golem with a glowing green-cracked core (left), a giant armored crab
with blue-white shell rising from stormy water (centre), and a huge white-lavender owl
with a wooden antler crown holding a blue crescent-moon gem, glowing eyes (right).
Only their eyes and cores glow.
Style: detailed HD pixel art like a medieval fantasy RPG, painterly pixel shading,
dark purple and cold blue tones with ember-orange sparks. Cinematic trailer mood,
very dark, strong vignette.
The bottom third stays dark and empty.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
Slow push-in toward the temple pillar. The runes on the pillar light up one by one,
then the eyes of the three giant guardians open in sequence: the golem's core glows
green, the crab's eyes glow blue, the owl's eyes glow white. Thunder flickers in the sky,
embers swirl. Ominous, slow. No cuts, keep the pixel art style, no text, no speech.
```

---

### Scene 10 — Write Your Own Legend
**Thoại:** *"Conquer the impossible... and write your own legend."*
**Không khí:** mạnh mẽ, hào hùng, cao trào.

**Prompt ảnh:**
```
Use the attached character as reference. Epic low-angle shot: the wayfarer stands his
ground in a vast square stone arena with glowing runes on the floor, sword raised,
elemental magic (water, earth, wind) swirling around him, facing a towering floating
stone golem with a glowing core and huge rock fists. Dramatic backlight, dust, debris
and embers fill the air.
Style: detailed HD pixel art like a medieval fantasy RPG, painterly pixel shading,
chunky cel-shaded magic effects, warm ember-orange and gold highlights. Cinematic
trailer climax mood, strong vignette.
The bottom third stays darker.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
The golem raises its giant fist and roars; floor runes flare. The wayfarer charges
forward, his elemental magic erupts into a bright blast of water, rock and wind toward
the golem. Camera slowly orbits and pushes in. Debris and embers fly. Epic, powerful,
slow-motion feeling at the impact. No cuts, keep the pixel art style, no text,
no speech.
```

---

### Scene 11 — Orynthals (Final Reveal)
**Thoại:** *"Orynthals."*
**Không khí:** trầm, uy nghiêm, vang ngân. Đây là nền để đặt logo game khi dựng.

**Prompt ảnh:**
```
A vast fantasy world at dawn seen from a high cliff: valleys, a river, a tiny village,
distant mountains and floating sky islands. The tiny silhouette of the wayfarer with a
red scarf stands on the cliff edge. The upper-centre sky is calm and empty (space for a
game logo). Golden sunrise light breaking through clouds.
Style: detailed HD pixel art like a cozy medieval fantasy RPG, painterly pixel shading,
warm golden and ember-orange highlights. Epic cinematic trailer finale, soft vignette.
Ember sparks drifting upward.
16:9, no text, no letters, no logo, no watermark, no UI.
```

**Prompt video:**
```
Very slow pull-back from the wayfarer to reveal the whole world. The sun rises, light
rays sweep across the valleys, clouds drift, embers rise upward into the sky.
Grand, majestic, calm. No cuts, keep the pixel art style, no text, no speech.
```

**Khi dựng:** chữ **ORYNTHALS** (logo game) hiện ở khoảng trời trống phía trên, lúc giọng đọc nói "Orynthals".

---

## 4. Cảnh thoại dự phòng (giống hệt ảnh mẫu: chỉ có tàn lửa trên nền đen)

Dùng khi một scene ra hình không ưng, hoặc khi cần thêm câu thoại mới.

**Prompt ảnh:**
```
Pure black background with glowing orange ember sparks drifting in the air, some sharp
and small, some short streaks, a few slightly out of focus. Pixel-art style embers.
Very dark, cinematic, nothing else in the frame.
16:9, no text, no letters, no logo, no watermark.
```

**Prompt video:**
```
The embers drift slowly upward and sideways like sparks from a distant fire, gently
flickering, some fading out and new ones appearing. Seamless, calm, no camera movement,
no text, no speech.
```

---

## 5. Thứ tự ghép trailer

| # | Cảnh thoại (AI) | Thoại | Sau đó chuyển sang clip gameplay |
|---|---|---|---|
| 01 | Bard bên đống lửa | Countless journeys... and countless legends yet to be written. | (chưa có gameplay, chuyển luôn sang 02) |
| 02 | Bước chân đầu tiên | And some... are ready to take the first step. | Nhân vật qua cầu vào làng |
| 03 | Ngắm thung lũng | A new land. | Dạo quanh làng Heorte |
| 04 | Trồng trọt, bến câu | A new way of life. | Trồng cây, thu hoạch, câu cá, mua đồ |
| 05 | Lò rèn | (không có) | Rèn đồ ở Merric, Crafting |
| 06 | Ba nguyên tố quanh bàn tay | Newfound strength. | Luyện kiếm, học phép, tung phép |
| 07 | Đôi mắt trong rừng tối | Face the dangers that lie ahead. | Ra rừng MapDuy đánh quái |
| 08 | Đào quặng, rương báu | Gather resources. Build your strength. | Khai thác tài nguyên, mở rương |
| 09 | Ba Hộ Vệ thức tỉnh | Countless dangers... await your arrival. | Trụ teleport, chọn boss |
| 10 | Đối đầu Golem | Conquer the impossible... and write your own legend. | Đánh boss (Earth Golem, Sky Owl) |
| 11 | Thế giới lúc bình minh + logo | Orynthals. | Kết thúc |

**Cách chuyển cảnh:** cảnh thoại fade từ đen vào → giọng đọc → câu cuối vừa dứt thì cắt sang clip gameplay đúng nhịp nhạc.
Hết clip gameplay thì fade về đen → sang cảnh thoại tiếp theo.
