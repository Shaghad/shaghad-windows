// flag-icons 7.3.2 (MIT). Download/extract the npm archive to artifacts/flag-icons-source first.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '..');
const source = path.join(root, 'artifacts/flag-icons-source/package');
const target = path.join(root, 'apps/windows/AJ.VPN.Desktop/Assets/Flags');
async function main() {
  fs.mkdirSync(target, {recursive:true});
  for (const name of fs.readdirSync(path.join(source,'flags/4x3')).filter(n => /^[a-z]{2}\.svg$/.test(n)))
    await sharp(path.join(source,'flags/4x3',name)).resize(64,48).png().toFile(path.join(target,name.replace('.svg','.png')));
  fs.copyFileSync(path.join(source,'LICENSE'), path.join(root,'docs/legal/FLAG-ICONS-LICENSE.txt'));
}
main().catch(e=>{ console.error(e.message); process.exitCode=1; });
