const { chromium } = require('D:/Git/PlanningPoker/PlanningPoker.Client/node_modules/playwright-core');
const pick = (sel) => { const e=document.querySelector(sel); if(!e) return null; const s=getComputedStyle(e); return {font:s.fontFamily.split(',')[0]+' '+s.fontSize+' '+s.fontWeight,color:s.color,bg:s.backgroundColor,pad:s.padding,radius:s.borderRadius}; };
(async()=>{const b=await chromium.launch();
for (const [app,url,sels] of [['old','http://localhost:1333',['body','input[type=text]','input[value="Create room"]','#room-creation-settings-btn','footer','nav, header']],['new','http://localhost:5000',['body','input','button[type=submit]','button[type=button]','footer','header']]]){
 const p=await (await b.newContext({viewport:{width:1280,height:800},colorScheme:'light'})).newPage(); await p.goto(url); await p.waitForTimeout(800);
 for (const s of sels) console.log(app,s,JSON.stringify(await p.evaluate(pick,s)));
}
await b.close()})();
