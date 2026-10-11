// Original SHAGHAD vector artwork. Run with Node.js and sharp available via NODE_PATH.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '..');
const brand = path.join(root, 'branding');
const colors = { navy:'#0B1120', turquoise:'#20C5C7', slate:'#1E293B', white:'#F8FAFC', gray:'#94A3B8', teal:'#087E80', mutedLight:'#526379', success:'#22C55E', successLight:'#15803D', error:'#EF4444', errorLight:'#B91C1C' };
const body = 'M104 282 V310 Q104 368 162 368 H180 Q232 368 232 310 V232 M232 292 Q232 320 274 320 Q316 320 316 292 V232 M316 292 Q316 320 358 320 Q400 320 400 292 V232';
const markA = color => `<g fill="none" stroke="${color}" stroke-width="44" stroke-linecap="round" stroke-linejoin="round"><path d="${body}"/></g><g fill="${color}"><circle cx="276" cy="164" r="22"/><circle cx="316" cy="112" r="22"/><circle cx="356" cy="164" r="22"/></g>`;
const markB = color => `<g fill="none" stroke="${color}" stroke-width="32" stroke-linecap="round" stroke-linejoin="round"><path d="M104 312 L168 376 H232 L288 320 H392 V224 M200 224 V320 H392 M296 224 V320"/><path d="M200 144 H296 L392 144"/></g><g fill="${color}"><circle cx="200" cy="144" r="24"/><circle cx="296" cy="144" r="24"/><circle cx="392" cy="144" r="24"/></g>`;
const originalC = color => `<path d="M388 184 H180 L116 248 L336 312 L272 376 H116" fill="none" stroke="${color}" stroke-width="52" stroke-linecap="square" stroke-linejoin="bevel"/><g fill="${color}"><rect x="132" y="96" width="36" height="36" rx="5"/><rect x="196" y="96" width="36" height="36" rx="5"/><rect x="260" y="96" width="36" height="36" rx="5"/></g>`;
// Refined C preserves the angular S, with rotationally balanced arms and centered dots.
const monogram = { path:'M384 192 H184 L120 256 L392 320 L328 384 H128', stroke:48, dotSize:40, dotRadius:6, dots:[[172,100],[236,100],[300,100]] };
const markC = color => `<path d="${monogram.path}" fill="none" stroke="${color}" stroke-width="${monogram.stroke}" stroke-linecap="square" stroke-linejoin="bevel"/><g fill="${color}">${monogram.dots.map(([x,y])=>`<rect x="${x}" y="${y}" width="${monogram.dotSize}" height="${monogram.dotSize}" rx="${monogram.dotRadius}"/>`).join('')}</g>`;
const selectedMark = markC;
const marks = [markA, markB, markC];
const letters = {
 S:'M44 8 Q36 4 24 4 H18 Q4 4 4 18 Q4 29 18 32 L30 35 Q44 38 44 48 Q44 60 30 60 H22 Q10 60 4 55',
 H:'M4 4 V60 M44 4 V60 M4 32 H44',
 A:'M4 60 L24 4 L44 60 M12 40 H36',
 G:'M44 12 Q36 4 24 4 Q4 4 4 32 Q4 60 24 60 Q36 60 44 52 V34 H28',
 D:'M4 4 H20 Q46 4 46 32 Q46 60 20 60 H4 Z'
};
function wordmark(color) { return `<g fill="none" stroke="${color}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round">${[...'SHAGHAD'].map((letter,i)=>`<path transform="translate(${i*64},0)" d="${letters[letter]}"/>`).join('')}</g>`; }
function svg(width,height,content,title) { return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" role="img" aria-labelledby="title"><title id="title">${title}</title>${content}</svg>\n`; }
function write(relative,content) { const target=path.join(brand,relative); fs.mkdirSync(path.dirname(target),{recursive:true}); fs.writeFileSync(target,content); }
function appIcon(mark=selectedMark) { return svg(512,512,`<rect width="512" height="512" rx="112" fill="${colors.navy}"/>${mark(colors.turquoise)}`,'SHAGHAD application icon — refined Concept C'); }
function horizontal(symbol,text) { return `<g transform="translate(0,2) scale(.36)">${selectedMark(symbol)}</g><g transform="translate(216,62) scale(1.7)">${wordmark(text)}</g>`; }
function label(x,y,text,size=24,color=colors.gray,extra='') { return `<text x="${x}" y="${y}" fill="${color}" font-family="Segoe UI, sans-serif" font-size="${size}" ${extra}>${text}</text>`; }
function contrast(a,b) {
  const lum = hex => { const v=hex.slice(1).match(/../g).map(x=>parseInt(x,16)/255).map(x=>x<=.04045?x/12.92:((x+.055)/1.055)**2.4); return .2126*v[0]+.7152*v[1]+.0722*v[2]; };
  const x=lum(a),y=lum(b); return Number(((Math.max(x,y)+.05)/(Math.min(x,y)+.05)).toFixed(2));
}
async function render(relative,source,width,height) { await sharp(Buffer.from(source),{density:192}).resize(width,height).png().toFile(path.join(brand,relative)); }
async function main() {
  for (let i=0;i<3;i++) write(`concepts/concept-${'abc'[i]}.svg`, svg(512,512,marks[i](colors.turquoise),`SHAGHAD concept ${'ABC'[i]} — original geometric symbol`));
  write('concepts/concept-c-original.svg',svg(512,512,originalC(colors.turquoise),'SHAGHAD concept C — original study retained for comparison'));
  write('logo/shaghad-symbol.svg',svg(512,512,selectedMark(colors.turquoise),'SHAGHAD — refined angular monogram with three dots'));
  write('logo/shaghad-logo-horizontal.svg',svg(980,190,horizontal(colors.teal,colors.navy),'SHAGHAD horizontal vector wordmark'));
  write('logo/shaghad-logo-vertical.svg',svg(560,650,`<g transform="translate(24,0)">${selectedMark(colors.teal)}</g><g transform="translate(74,534)">${wordmark(colors.navy)}</g>`,'SHAGHAD vertical vector lockup'));
  write('logo/shaghad-logo-light.svg',svg(980,190,horizontal(colors.teal,colors.navy),'SHAGHAD logo for light backgrounds'));
  write('logo/shaghad-logo-dark.svg',svg(980,190,horizontal(colors.turquoise,colors.white),'SHAGHAD logo for dark backgrounds'));
  write('logo/shaghad-monochrome.svg',svg(512,512,selectedMark(colors.navy),'SHAGHAD single-color symbol; recolor as one unit'));
  write('icons/shaghad-app-icon.svg',appIcon());
  const sizes=[16,24,32,48,64,128,256,512];
  const frames=[];
  for(const size of sizes) {
    // Supersample so the three dots remain separate at 16 px.
    const png=await sharp(Buffer.from(appIcon()),{density:192}).resize(size,size,{kernel:'lanczos3'}).png().toBuffer();
    write(`icons/shaghad-${size}.png`,png);
    if(size<=256) frames.push({size,png});
  }
  const header=Buffer.alloc(6+frames.length*16); header.writeUInt16LE(1,2); header.writeUInt16LE(frames.length,4);
  let offset=header.length;
  frames.forEach(({size,png},i)=>{const p=6+i*16; header[p]=size===256?0:size; header[p+1]=size===256?0:size; header.writeUInt16LE(1,p+4);header.writeUInt16LE(32,p+6);header.writeUInt32LE(png.length,p+8);header.writeUInt32LE(offset,p+12);offset+=png.length;});
  write('icons/shaghad.ico',Buffer.concat([header,...frames.map(f=>f.png)]));
  let comparison=`<rect width="1680" height="1080" fill="${colors.navy}"/>${label(72,78,'SHAGHAD / THREE DIRECTIONS',18,colors.turquoise,'letter-spacing="3"')}${label(72,140,'Persian roots. Digital form.',46,colors.white)}${label(72,183,'Original vector studies • The three dots remain the cultural signature.',22)}`;
  const names=['A — Persian Minimal','B — Network Geometry','C — Premium Monogram'];
  const captions=['Open connected stroke · alternative','Connected nodes · alternative','Balanced angular S · selected refinement'];
  for(let i=0;i<3;i++) {
    const x=72+i*522;
    comparison+=`<rect x="${x}" y="230" width="492" height="644" rx="24" fill="${colors.slate}" stroke="${i===2?colors.turquoise:'#45566C'}" stroke-width="${i===2?2:1}"/><g transform="translate(${x+72},260) scale(.68)">${marks[i](colors.turquoise)}</g>${label(x+28,668,names[i],27,colors.white)}${label(x+28,710,captions[i],18)}${label(x+28,766,'ACTUAL PIXEL SIZES',13,colors.gray,'letter-spacing="2"')}`;
    let sx=x+28;
    for(const size of [16,24,32,48]) {
      const png=await sharp(Buffer.from(appIcon(marks[i]))).resize(size,size).png().toBuffer();
      comparison+=`<image x="${sx}" y="792" width="${size}" height="${size}" href="data:image/png;base64,${png.toString('base64')}"/>`;
      comparison+=label(sx,854,String(size),13); sx+=92;
    }
  }
  comparison+=label(72,950,'C — the chosen direction, refined for balance and small-size clarity.',27,colors.white)+label(72,995,'The angular monogram is retained. Three centered dots keep a subtle Persian signature.',20);
  const board=svg(1680,1080,comparison,'SHAGHAD — three original logo concepts with actual small-size samples');
  write('previews/concepts-preview.svg',board); await render('previews/concepts-preview.png',board,1680,1080);
  for(const dark of [true,false]) {
    const bg=dark?colors.navy:colors.white,fg=dark?colors.white:colors.navy,accent=dark?colors.turquoise:colors.teal,muted=dark?colors.gray:colors.mutedLight;
    const panel=`<rect width="1400" height="840" fill="${bg}"/>${label(72,76,'SHAGHAD / BRAND IDENTITY',17,accent,'letter-spacing="3"')}<g transform="translate(200,200)">${horizontal(accent,fg)}</g>${label(416,447,'Modern Secure Network Client',27,muted)}<line x1="72" y1="535" x2="1328" y2="535" stroke="${dark?'#45566C':'#CBD5E1'}"/>${label(72,600,'THE SIGNATURE',15,accent,'letter-spacing="2"')}${label(72,650,'Three dots. A sharper signature.',30,fg)}${label(72,695,'Refined angular monogram • Original symbol and drawn wordmark',20,muted)}<g transform="translate(1140,580) scale(.35)">${selectedMark(accent)}</g>`;
    const source=svg(1400,840,panel,`SHAGHAD selected logo — ${dark?'dark':'light'} background design preview`);
    write(`previews/${dark?'dark':'light'}-preview.svg`,source);await render(`previews/${dark?'dark':'light'}-preview.png`,source,1400,840);
  }
  let refinement=`<rect width="1400" height="900" fill="${colors.navy}"/>${label(64,70,'SHAGHAD / CONCEPT C',17,colors.turquoise,'letter-spacing="3"')}${label(64,128,'Same direction. Better balance.',40,colors.white)}${label(64,170,'Symmetric arms • Centered dots • More open counters',21)}`;
  for(const [i,mark,dark,name] of [[0,originalC,true,'Original C'],[1,selectedMark,true,'Refined C / dark'],[2,selectedMark,false,'Refined C / light']]) {
    const x=64+i*432,fg=dark?colors.white:colors.navy,accent=dark?colors.turquoise:colors.teal;
    refinement+=`<rect x="${x}" y="220" width="408" height="540" rx="24" fill="${dark?colors.slate:colors.white}" stroke="${i===1?colors.turquoise:'#64748B'}"/><g transform="translate(${x+40},244) scale(.64)">${mark(accent)}</g>${label(x+28,626,name,26,fg)}`;
    for(const [j,size] of [16,24,32,48].entries()) {
      const png=await sharp(Buffer.from(appIcon(mark)),{density:192}).resize(size,size,{kernel:'lanczos3'}).png().toBuffer();
      refinement+=`<image x="${x+28+j*90}" y="660" width="${size}" height="${size}" href="data:image/png;base64,${png.toString('base64')}"/>${label(x+28+j*90,736,String(size)+' px',13,dark?colors.gray:colors.mutedLight)}`;
    }
  }
  refinement+=label(64,832,'One geometry across SVG, Windows icons and WPF. Small samples are shown at actual size.',20);
  const refinedBoard=svg(1400,900,refinement,'SHAGHAD concept C — original versus refined design preview');
  write('previews/concept-c-refinement.svg',refinedBoard);await render('previews/concept-c-refinement.png',refinedBoard,1400,900);
  const checks=[
    ['Soft white / navy',colors.white,colors.navy,'body text',4.5],['Gray / navy',colors.gray,colors.navy,'secondary text',4.5],
    ['Soft white / slate',colors.white,colors.slate,'card text',4.5],['Gray / slate',colors.gray,colors.slate,'secondary card text',4.5],
    ['Turquoise / navy',colors.turquoise,colors.navy,'brand symbol, status',3],['Turquoise / slate',colors.turquoise,colors.slate,'brand on card',3],
    ['Navy / turquoise',colors.navy,colors.turquoise,'selected/primary control text',4.5],['Navy / soft white',colors.navy,colors.white,'light body',4.5],
    ['Muted light / soft white',colors.mutedLight,colors.white,'light secondary text',4.5],['Deep teal / white',colors.teal,'#FFFFFF','light accent/symbol',4.5],
    ['Success / slate',colors.success,colors.slate,'dark status text',4.5],['Error / slate',colors.error,colors.slate,'error icon only',3],
    ['Error text / slate','#F87171',colors.slate,'dark error text',4.5],
    ['Success light / white',colors.successLight,'#FFFFFF','light status text',4.5],['Error light / white',colors.errorLight,'#FFFFFF','light error text',4.5],
    ['Turquoise / white — decorative only',colors.turquoise,'#FFFFFF','not permitted as small text or essential icon',null]
  ].map(([pair,foreground,background,usage,minimum])=>({pair,foreground,background,usage,ratio:contrast(foreground,background),minimum,pass:minimum===null?null:contrast(foreground,background)>=minimum}));
  if(checks.some(c=>c.pass===false))throw new Error('A required color pairing fails contrast');
  write('contrast-report.json',JSON.stringify(checks,null,2)+'\n');
  // One authoritative set of vector geometry is shared with WPF, not a raster in the sidebar.
  const wpfMark=(key,brush)=>`<DrawingImage x:Key="${key}"><DrawingImage.Drawing><DrawingGroup><GeometryDrawing Brush="Transparent" Geometry="M0 0 H512 V512 H0 Z"/><GeometryDrawing Geometry="${monogram.path}"><GeometryDrawing.Pen><Pen Brush="${brush}" Thickness="${monogram.stroke}" StartLineCap="Square" EndLineCap="Square" LineJoin="Bevel"/></GeometryDrawing.Pen></GeometryDrawing>${monogram.dots.map(([x,y])=>`<GeometryDrawing Brush="${brush}"><GeometryDrawing.Geometry><RectangleGeometry Rect="${x},${y},${monogram.dotSize},${monogram.dotSize}" RadiusX="${monogram.dotRadius}" RadiusY="${monogram.dotRadius}"/></GeometryDrawing.Geometry></GeometryDrawing>`).join('')}</DrawingGroup></DrawingImage.Drawing></DrawingImage>`;
  const drawing=`<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">${wpfMark('ShaghadSymbolLight',colors.teal)}${wpfMark('ShaghadSymbolDark',colors.turquoise)}${wpfMark('ShaghadSymbolHighContrast','{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}')}</ResourceDictionary>\n`;
  fs.writeFileSync(path.join(root,'apps/windows/AJ.VPN.Desktop/Assets/BrandGeometry.xaml'),drawing);
  const outputs=[];
  for(const folder of ['concepts','logo','icons','previews'])for(const file of fs.readdirSync(path.join(brand,folder))) {
    const p=path.join(folder,file).replaceAll('\\','/');outputs.push({path:p,bytes:fs.statSync(path.join(brand,p)).size,sha256:require('node:crypto').createHash('sha256').update(fs.readFileSync(path.join(brand,p))).digest('hex')});
  }
  write('asset-manifest.json',JSON.stringify({generator:'scripts/export-branding.cjs',selectedConcept:'C — Premium Monogram (refined)',outputs},null,2)+'\n');
  console.log(`Exported ${outputs.length} branded assets and WPF vector resource. All required contrast pairings pass.`);
}
main().catch(error=>{console.error(error);process.exitCode=1;});
