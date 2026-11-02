const fs=require('fs'),{PNG}=require('pngjs');
const n=process.argv[2];const ims=['old','new','diff'].map(k=>PNG.sync.read(fs.readFileSync(`out/${k}-${n}.png`)));
const h=Math.max(...ims.map(i=>i.height)),w=ims.reduce((a,i)=>a+i.width+10,0);
const o=new PNG({width:w,height:h});o.data.fill(200);let x=0;for(const i of ims){PNG.bitblt(i,o,0,0,i.width,i.height,x,0);x+=i.width+10}
fs.writeFileSync(`pair-${n}.png`,PNG.sync.write(o));
