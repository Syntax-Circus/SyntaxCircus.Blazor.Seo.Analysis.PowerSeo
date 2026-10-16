import fs from 'node:fs/promises';
import path from 'node:path';
import {build} from 'esbuild';
import {gzipSync} from 'node:zlib';
const root=path.resolve(import.meta.dirname,'..'),out=path.join(root,'src/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/wwwroot');
await fs.mkdir(out,{recursive:true});await fs.mkdir(path.join(root,'artifacts'),{recursive:true});
const worker=await build({entryPoints:[path.join(root,'js/worker.mjs')],outfile:path.join(out,'power-seo.worker.js'),bundle:true,format:'esm',platform:'browser',target:'es2022',minify:true,metafile:true,legalComments:'none'});
await build({entryPoints:[path.join(root,'js/bridge.mjs')],outfile:path.join(out,'power-seo.js'),format:'esm',platform:'browser',target:'es2022',minify:true});
const packages=new Map();
for(const file of Object.keys(worker.metafile.inputs)){
 let dir=path.dirname(path.resolve(root,file));
 while(dir.startsWith(root)&&dir!==root){
  try{const pkg=JSON.parse(await fs.readFile(path.join(dir,'package.json'),'utf8'));if(pkg.name){
   if(!packages.has(pkg.name)){
    const notices=[];
    for(const name of await fs.readdir(dir))if(/^(license|licence|copying|notice)/i.test(name))notices.push(await fs.readFile(path.join(dir,name),'utf8'));
    packages.set(pkg.name,{name:pkg.name,version:pkg.version,license:pkg.license,notices});
   }break;
  }}catch{}dir=path.dirname(dir);
 }
}
const inventory=[...packages.values()];
if(inventory.some(x=>x.license!=='MIT'||x.notices.length===0))throw new Error('Unreviewed shipping license or missing notice');
if(!inventory.some(x=>x.name==='@power-seo/content-analysis'&&x.version==='1.0.19'))throw new Error('Unexpected engine version');
const notice=inventory.map(x=>x.name+' '+x.version+' ('+x.license+')\n'+x.notices.join('\n')).join('\n\n');
await fs.writeFile(path.join(root,'THIRD-PARTY-NOTICES.txt'),notice);
await fs.writeFile(path.join(out,'THIRD-PARTY-NOTICES.txt'),notice);
const bytes=await fs.readFile(path.join(out,'power-seo.worker.js'));
const measurement={workerBytes:bytes.length,gzipBytes:gzipSync(bytes).length,bridgeBytes:(await fs.stat(path.join(out,'power-seo.js'))).size,packages:inventory.map(({notices,...pkg})=>pkg)};
await fs.writeFile(path.join(root,'artifacts/bundle.json'),JSON.stringify(measurement,null,2));
console.log(JSON.stringify(measurement));
