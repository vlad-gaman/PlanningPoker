const fs=require('fs');
const [,, size, scheme, props='font,size,weight,color,bg,align,deco,radius,pad'] = process.argv;
const P=props.split(',');
const states=['home','home-dialog','join','room-lobby','room-voting','room-revealed','room-settings','credits'];
for(const st of states){
  const ld=a=>JSON.parse(fs.readFileSync(`out/${a}-${size}-${scheme}-${st}.json`,'utf8'));
  const o=ld('old'),n=ld('new');
  console.log(`\n## ${st}`);
  const groups={};
  for(const k of Object.keys(o)) if(n[k]) for(const p of P){
    if(o[k][p]!==n[k][p]){const g=`${p}: ${o[k][p]} -> ${n[k][p]}`;(groups[g]=groups[g]||[]).push(k.slice(0,18));}
  }
  for(const [g,v] of Object.entries(groups)) console.log(` ${g}  [${v.length}] ${v.slice(0,3).join(' | ')}`);
  const oo=Object.keys(o).filter(k=>!n[k]).map(k=>k.slice(0,22)), nn=Object.keys(n).filter(k=>!o[k]).map(k=>k.slice(0,22));
  console.log(` only old: ${oo.join(' | ')}`);
  console.log(` only new: ${nn.join(' | ')}`);
}
